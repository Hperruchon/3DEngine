// Writes the name and the message of each failed test as a GitHub annotation.
//
// The log of a job needs admin rights, and the annotations of a check run are
// public. Before this script a failed test in the pipeline had no name that a
// person without a sign-in could read (register entry R-0032, TASK-0045).
//
// Usage: node eng/report-failed-tests.js <directory with .trx files>
// The step "Test" of .github/workflows/ci.yml calls it when `dotnet test` fails.
// The script always exits with 0. The caller gives the exit code of the step.

"use strict";

const fs = require("fs");
const path = require("path");

// GitHub shows at most 10 error annotations for each step.
const MaxAnnotations = 10;
const MaxMessageLength = 2000;
// TASK-0440 (phase P0.175): the cut of each cause of a crash. The name of the test and the dumps come before the cause, and
// a second cause stays inside the cut of the message.
const MaxCauseLength = 1200;
const StackLines = 6;

function filesIn(directory, test) {
  if (!fs.existsSync(directory)) return [];
  const found = [];
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) found.push(...filesIn(full, test));
    else if (test(entry.name)) found.push(full);
  }
  return found;
}

function decode(text) {
  return text
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/&quot;/g, "\"")
    .replace(/&apos;/g, "'")
    .replace(/&#x([0-9a-fA-F]+);/g, (_, hex) => String.fromCodePoint(parseInt(hex, 16)))
    .replace(/&#([0-9]+);/g, (_, dec) => String.fromCodePoint(parseInt(dec, 10)))
    .replace(/&amp;/g, "&");
}

function element(xml, name) {
  const match = new RegExp(`<${name}>([\\s\\S]*?)</${name}>`).exec(xml);
  return match ? decode(match[1]).replace(/\r\n?/g, "\n").trim() : "";
}

function failedTests(file) {
  const xml = fs.readFileSync(file, "utf8");
  const results = [];
  // A passed result is an empty element, <UnitTestResult ... />. A failed result
  // has a body. The pattern must accept both, or it reads past an empty element
  // into the body of the next result.
  const pattern = /<UnitTestResult\b([^>]*?)(?:\/>|>([\s\S]*?)<\/UnitTestResult>)/g;
  for (let match; (match = pattern.exec(xml)) !== null; ) {
    const attributes = match[1];
    if (!/\boutcome="(Failed|Error|Timeout|Aborted)"/.test(attributes)) continue;
    const name = /\btestName="([^"]*)"/.exec(attributes);
    const body = match[2] || "";
    results.push({
      name: name ? decode(name[1]) : "(no name)",
      message: element(body, "Message"),
      stack: element(body, "StackTrace").split(/\r?\n/).slice(0, StackLines).join("\n"),
    });
  }
  return results;
}

// The reason that the test platform gives for a run that it stopped, for
// example "Test host process crashed" after a hang limit. Only the outcome
// Error: a Warning holds the output of the test framework, for example a
// skipped test, which run 37189044285 showed as a misleading reason.
function runReasons(file) {
  const xml = fs.readFileSync(file, "utf8");
  const reasons = [];
  const pattern = /<RunInfo\b[^>]*\boutcome="Error"[^>]*>([\s\S]*?)<\/RunInfo>/g;
  for (let match; (match = pattern.exec(xml)) !== null; ) {
    const text = element(match[1], "Text");
    if (text) reasons.push(text);
  }
  return reasons;
}

// A test that hangs gives no result in the .trx file. With --blame-hang-timeout
// the test platform stops it and writes a sequence file, where the test that
// did not complete has Completed="False" (TASK-0047, finding T8).
function incompleteTests(file) {
  const xml = fs.readFileSync(file, "utf8");
  const names = [];
  const pattern = /<Test\b([^>]*)\/?>/g;
  for (let match; (match = pattern.exec(xml)) !== null; ) {
    if (!/\bCompleted="False"/.test(match[1])) continue;
    const name = /\bName="([^"]*)"/.exec(match[1]);
    if (name) names.push(decode(name[1]));
  }
  return names;
}

// TASK-0440 (phase P0.175): the blame data collector can also name the running test in a message of the .trx file. The
// words are those of the collector of the test platform; no run of the pipeline showed them yet.
const BlameRunning = "The test running when the crash occurred:";
const BlameEnd = "This test may, or may not be the source of the crash.";

function runningInTrx(file) {
  const xml = fs.readFileSync(file, "utf8");
  const names = [];
  const pattern = /<RunInfo\b[^>]*>([\s\S]*?)<\/RunInfo>/g;
  for (let match; (match = pattern.exec(xml)) !== null; ) {
    const text = element(match[1], "Text");
    const at = text.indexOf(BlameRunning);
    if (at < 0) continue;
    const rest = text.slice(at + BlameRunning.length);
    const end = rest.indexOf(BlameEnd);
    names.push(...(end >= 0 ? rest.slice(0, end) : rest).split("\n").map(line => line.trim()).filter(line => line.length > 0));
  }
  return names;
}

// TASK-0441 (phase P0.176): a test that runs longer than the hang limit of the step "Test". The blame data collector stops
// the test host, and the test platform then gives the reason "Test host process crashed", with no dump when the hang dump
// type is none. The collector says why in a message of the .trx file with the outcome Warning, which runReasons does not
// read. The words are those of the SDK 10.0.400; a local run with a limit of 20 seconds gave "The specified inactivity time
// of 20 seconds has elapsed." Until TASK-0441 the annotation called such a stop a crash (attempt 1 of run 44758589835, run
// 44908442870), and two tasks looked for a crash of the code.
const HangLimit = /The specified inactivity time of (.+?) has elapsed/;

function hangLimits(file) {
  const xml = fs.readFileSync(file, "utf8");
  const limits = [];
  const pattern = /<RunInfo\b[^>]*>([\s\S]*?)<\/RunInfo>/g;
  for (let match; (match = pattern.exec(xml)) !== null; ) {
    const limit = HangLimit.exec(element(match[1], "Text"));
    if (limit) limits.push(limit[1]);
  }
  return limits;
}

// The first words of the report of the .NET runtime when a process stops: an exception that no code caught, a stack
// overflow, a FailFast and an internal error of the runtime (on Windows an access violation is "Fatal error.").
const CrashMarkers = ["Unhandled exception.", "Unhandled Exception:", "Stack overflow.", "Process terminated.", "Fatal error."];

// The words of the test platform before the error output of a test host that stopped. The strings are those of the test
// platform of the SDK 10.0.
const PlatformHeads = [
  /^([\s\S]*?Test host process crashed)\s*:\s*/,
  /^([\s\S]*?Testhost process for source\(s\) '[^']*' exited with error)\s*:\s*/,
];
const PlatformTail = /\s*\.?\s*Please check the diagnostic logs for more information\.\s*$/;

// A line of the error output that the runtime or the test platform writes, and not a test: createdump of the runtime on
// Linux and macOS, and a line of the test platform with an exit code.
const PlatformLines = [/^\[createdump\]/, /^Test ?host process\b/i, /\bexit(ed)? with exit ?code\b/i];

function cut(text) {
  return text.length > MaxCauseLength ? text.slice(0, MaxCauseLength) + " ..." : text;
}

// The cause of a crash from the reason of the test platform. The reason holds the error output of the test host: the
// output of the tests, and the report of the runtime at the end. TASK-0435 gave the text from the last marker of the
// runtime, or else the last 1000 characters, and in run 44908442870 those characters were the output of other tests
// ("3DEngine does not import bracket.stl"). TASK-0440: the cause is the words of the platform, the lines of the runtime and
// the platform, the count of the lines of the tests that it leaves out, and the report of the runtime. A reason with no
// words of a stopped host, for example "Data collection : Test case event channel failed", is a text of the platform.
function crashCause(reason) {
  const head = PlatformHeads.map(pattern => pattern.exec(reason)).find(match => match !== null);
  if (!head) return cut(reason);
  const output = reason.slice(head[0].length).replace(PlatformTail, "");
  const at = Math.max(...CrashMarkers.map(marker => output.lastIndexOf(marker)));
  const report = at >= 0 ? output.slice(at).trim() : "";
  const lines = (at >= 0 ? output.slice(0, at) : output).split("\n").map(line => line.trim()).filter(line => line.length > 0);
  const kept = lines.filter(line => PlatformLines.some(pattern => pattern.test(line)));
  const parts = [head[1] + ".", ...kept];
  if (lines.length > kept.length) parts.push(`${lines.length - kept.length} lines of the output of the tests are left out.`);
  parts.push(report || "The error output holds no report of the runtime.");
  return cut(parts.join(" "));
}

// A reason of a test host that stopped, and not only a message of the platform.
function isCrash(reason) {
  return PlatformHeads.some(pattern => pattern.test(reason)) || /\baborted\b/i.test(reason);
}

// The escape rules of a workflow command: a property also escapes ':' and ','.
function escapeData(text) {
  return text.replace(/%/g, "%25").replace(/\r/g, "%0D").replace(/\n/g, "%0A");
}

function escapeProperty(text) {
  return escapeData(text).replace(/:/g, "%3A").replace(/,/g, "%2C");
}

function annotate(title, body) {
  const text = body.length > MaxMessageLength ? body.slice(0, MaxMessageLength) + " ..." : body;
  console.log(`::error title=${escapeProperty(title)}::${escapeData(text)}`);
}

const directory = process.argv[2];
const runner = process.env.RUNNER_OS || "this computer";
const files = directory ? filesIn(directory, name => name.endsWith(".trx")) : [];
const sequences = directory ? filesIn(directory, name => /^Sequence_.*\.xml$/.test(name)) : [];
const dumps = directory ? filesIn(directory, name => name.endsWith(".dmp")).map(file => path.basename(file)) : [];
const reasons = [...new Set(files.flatMap(runReasons))];
const causes = reasons.map(crashCause);
const crashed = reasons.some(isCrash);
// The dumps of the step: the job keeps them as the artifact test-host-crash-<system> (TASK-0435). TASK-0440: a crash
// with no dump says so.
// TASK-0441: the hang limit that the blame data collector names. A stop at that limit is a hang, so it gives no text of a
// missing dump of a crash.
const hangs = [...new Set(files.flatMap(hangLimits))];
const hangText = hangs.length === 0 ? "" :
  ` A test ran longer than the hang limit of ${hangs.join(", ")}, so the test platform stopped the test host: a hang, not a crash.`;
const dumpText = dumps.length > 0 ? ` Dumps of the crash: ${dumps.join(", ")}.` : crashed && hangs.length === 0 ? " No dump of the crash." : "";
const causeText = " Cause: " + (causes.length > 0 ? causes.join(" ") : "no reason was written.");
// The tests that were running when the test host stopped: first from the sequence files, then from the .trx files.
const running = [...new Set([...sequences.flatMap(incompleteTests), ...files.flatMap(runningInTrx)])];
const failures = files.flatMap(failedTests);
for (const name of running) {
  if (failures.some(f => f.name === name)) continue;
  // TASK-0440: the name comes first, so that no cut of the message hides it (run 44908442870 named no test in its message).
  const others = running.filter(other => other !== name);
  const otherText = others.length === 0 ? "" :
    ` The same run also stopped: ${others.slice(0, 3).join(", ")}${others.length > 3 ? ` and ${others.length - 3} more` : ""}.`;
  failures.push({
    name,
    message: `${name}: the test did not complete, because the test host stopped while it ran.` + hangText + otherText + dumpText + causeText,
    stack: "",
  });
}

// TASK-0440: a crash with no running test says that the crashed test is not known.
const unknownText = sequences.length === 0
  ? "No sequence file: the crashed test is not known."
  : "No sequence file names a running test: the crashed test is not known.";
const unknownCrash = crashed && running.length === 0;

if (failures.length === 0) {
  // A crash of the test host or a failed build gives no failed result. Say so,
  // so that an empty report is not read as "no test failed".
  annotate(
    `Test step failed on ${runner}`,
    unknownCrash
      ? `The test host stopped, and no .trx file in '${directory}' names a failed test. ${unknownText} ` +
          `Files read: ${files.length}.` + hangText + dumpText + causeText
      : `The test step failed, and no .trx file in '${directory}' names a failed test. ` +
          `Files read: ${files.length}. The test host can have stopped before it wrote a result.` + dumpText +
          (causes.length > 0 ? causeText : ""));
} else {
  if (unknownCrash) annotate(`Test host stopped on ${runner}`, unknownText + hangText + dumpText + causeText);
  const room = unknownCrash ? MaxAnnotations - 1 : MaxAnnotations;
  const shown = failures.length > room ? failures.slice(0, room - 1) : failures;
  for (const failure of shown) {
    annotate(`Failed test on ${runner}: ${failure.name}`, `${failure.message}\n${failure.stack}`);
  }
  if (shown.length < failures.length) {
    const rest = failures.slice(shown.length).map(f => f.name);
    annotate(`${rest.length} more failed tests on ${runner}`, rest.join("\n"));
  }
}

if (process.env.GITHUB_STEP_SUMMARY) {
  const lines = failures.length === 0
    ? [`### Test step failed on ${runner}`, "", "No .trx file names a failed test."]
    : [`### ${failures.length} failed tests on ${runner}`, "", ...failures.map(f => `- \`${f.name}\``)];
  fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY, lines.join("\n") + "\n");
}
