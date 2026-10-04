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
// example "Test host process crashed" after a hang limit.
function runReasons(file) {
  const xml = fs.readFileSync(file, "utf8");
  const reasons = [];
  const pattern = /<RunInfo\b[^>]*\boutcome="(Error|Warning)"[^>]*>([\s\S]*?)<\/RunInfo>/g;
  for (let match; (match = pattern.exec(xml)) !== null; ) {
    const text = element(match[2], "Text");
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
const reasons = [...new Set(files.flatMap(runReasons))];
const failures = files.flatMap(failedTests);
for (const name of new Set(sequences.flatMap(incompleteTests))) {
  if (failures.some(f => f.name === name)) continue;
  failures.push({
    name,
    message: "The test did not complete. The test platform stopped the run: " +
      (reasons.length > 0 ? reasons.join(" ") : "no reason was written."),
    stack: "",
  });
}

if (failures.length === 0) {
  // A crash of the test host or a failed build gives no failed result. Say so,
  // so that an empty report is not read as "no test failed".
  annotate(
    `Test step failed on ${runner}`,
    `The test step failed, and no .trx file in '${directory}' names a failed test. ` +
      `Files read: ${files.length}. The test host can have stopped before it wrote a result.` +
      (reasons.length > 0 ? ` Reason: ${reasons.join(" ")}` : ""));
} else {
  const shown = failures.length > MaxAnnotations ? failures.slice(0, MaxAnnotations - 1) : failures;
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
