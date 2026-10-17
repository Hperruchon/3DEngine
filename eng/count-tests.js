// Checks the tests that a run executed, from the .trx files of `dotnet test`, against
// the exact count of each test project in eng/test-floor.txt (TASK-0073, TASK-0078).
//
// Until TASK-0073 a file that is not a gate file could take a test project out of the
// step "Test", and the step passed with no test (finding T48 of the codebase review of
// 2026-10-13). TASK-0073 added one floor for the whole run. The review of 2026-10-17
// found two ways past it: a project that leaves the run while another one grows, and
// a .trx file that a test writes into the folder (findings T54 and T58). Since
// TASK-0078 each test project has its own exact count, and the folder must hold
// exactly one result file for each project and no other. A task that adds or removes
// tests sets the new count in the same commit; the file is a gate file, so the change
// is visible.
//
// The file of counts: one line for each test project, the name of its assembly and
// its count, and an optional count for one platform, for example
// "3DEngine.ImageTests 6 linux=7". A line that starts with # is a comment.
//
// Usage: node eng/count-tests.js <folder of the .trx files> <file of counts> [platform]
// The platform is linux, windows or macos; by default, the platform of the run.
// Exit code 0: each project executed its count. 1: a count differs, a project has no
// result or more than one, or a result names another assembly. 2: the arguments are
// wrong.
const fs = require('fs');
const path = require('path');

const [folder, countFile, platformArgument] = process.argv.slice(2);
if (!folder || !countFile) {
  console.error('usage: node eng/count-tests.js <folder of the .trx files> <file of counts> [platform]');
  process.exit(2);
}

const platform = platformArgument ?? ({ linux: 'linux', win32: 'windows', darwin: 'macos' }[process.platform] ?? process.platform);

// The expected count of each assembly, by its name in lower case.
const expected = new Map();
for (const raw of fs.readFileSync(countFile, 'utf8').split(/\r?\n/)) {
  const line = raw.trim();
  if (line.length === 0 || line.startsWith('#'))
    continue;

  const [name, count, ...overrides] = line.split(/\s+/);
  let value = Number.parseInt(count, 10);
  for (const override of overrides) {
    const [key, other] = override.split('=');
    if (key === platform)
      value = Number.parseInt(other, 10);
  }

  if (!name || !Number.isInteger(value) || value < 0) {
    console.error(`${countFile}: the line '${line}' gives no count`);
    process.exit(2);
  }

  expected.set(name.toLowerCase(), { name, value });
}

if (expected.size === 0) {
  console.error(`${countFile} gives no count`);
  process.exit(2);
}

const files = fs.existsSync(folder)
  ? fs.readdirSync(folder, { recursive: true }).filter(name => String(name).endsWith('.trx')).map(name => path.join(folder, String(name)))
  : [];

const problems = [];
const seen = new Map();
for (const file of files) {
  const text = fs.readFileSync(file, 'utf8');
  const assemblies = new Set([...text.matchAll(/\bstorage="([^"]*)"/g)].map(m => path.basename(m[1].replace(/\\/g, '/')).replace(/\.dll$/i, '').toLowerCase()));
  const counters = text.match(/<Counters\b[^>]*\bexecuted="(\d+)"/);
  const name = path.basename(file);

  if (assemblies.size !== 1) {
    problems.push(`${name} names ${assemblies.size} test assemblies, and a result of the runner names one`);
    continue;
  }

  const [assembly] = assemblies;
  if (!expected.has(assembly)) {
    problems.push(`${name} names the assembly ${assembly}, which ${countFile} does not list`);
    continue;
  }

  if (seen.has(assembly)) {
    problems.push(`${assembly} has two result files, ${seen.get(assembly).file} and ${name}`);
    continue;
  }

  seen.set(assembly, { file: name, executed: counters ? Number.parseInt(counters[1], 10) : 0 });
}

for (const [key, { name, value }] of expected) {
  const result = seen.get(key);
  if (!result)
    problems.push(`${name} has no result file: the project left the run`);
  else if (result.executed !== value)
    problems.push(`${name} executed ${result.executed} tests, and ${countFile} gives ${value} on ${platform}`);
  else
    console.log(`${name}: ${result.executed} tests`);
}

if (problems.length > 0) {
  for (const problem of problems)
    console.log(`::error title=Tests::${problem}. A task that adds or removes tests sets the new count in ${countFile} (findings T48, T54 and T58).`);
  process.exit(1);
}
