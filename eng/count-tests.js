// Counts the tests that a run executed, from the .trx files of `dotnet test`, and
// fails when the count is below the floor of eng/test-floor.txt (TASK-0073).
//
// Until TASK-0073 a file that is not a gate file could take a test project out of
// the step "Test", and the step passed with no test (finding T48 of the codebase
// review of 2026-10-13). Each file that MSBuild reads for the solution is now a gate
// file; this count is the second line, for a way that nobody has found yet. A task
// that removes tests lowers the floor in the same commit, and the floor is a gate
// file, so the change is visible.
//
// Usage: node eng/count-tests.js <folder of the .trx files> <floor file>
// Exit code 0: the run executed at least the floor. 1: it executed fewer, or the
// folder holds no result. 2: the arguments are wrong.
const fs = require('fs');
const path = require('path');

const [folder, floorFile] = process.argv.slice(2);
if (!folder || !floorFile) {
  console.error('usage: node eng/count-tests.js <folder of the .trx files> <floor file>');
  process.exit(2);
}

const floorLine = fs.readFileSync(floorFile, 'utf8').split(/\r?\n/).map(line => line.trim()).find(line => line.length > 0 && !line.startsWith('#'));
const floor = Number.parseInt(floorLine ?? '', 10);
if (!Number.isInteger(floor) || floor < 0) {
  console.error(`${floorFile} gives no floor`);
  process.exit(2);
}

const files = fs.existsSync(folder)
  ? fs.readdirSync(folder, { recursive: true }).filter(name => String(name).endsWith('.trx')).map(name => path.join(folder, String(name)))
  : [];

let executed = 0;
for (const file of files) {
  const counters = fs.readFileSync(file, 'utf8').match(/<Counters\b[^>]*\bexecuted="(\d+)"/);
  if (counters) executed += Number.parseInt(counters[1], 10);
}

console.log(`${executed} tests ran in ${files.length} result files; the floor is ${floor}`);
if (executed < floor) {
  console.log(`::error title=Tests::${executed} tests ran, and the floor of ${floorFile} is ${floor}. A file can have taken a test project out of the run (finding T48).`);
  process.exit(1);
}
