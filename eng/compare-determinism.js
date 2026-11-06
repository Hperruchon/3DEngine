// Compares the determinism hashes of the three runners (TASK-0156, anti-objective 16).
// Usage: node eng/compare-determinism.js <folder> [result of the emulator job]
//
// Finding T152 of the codebase review of 2026-11-06 (d) (TASK-0242): a failure of the emulator job skipped this job, so
// seven runs compared no hash at all. The job now runs when the gate passed; the three desktop runners are required, and
// the phone is required only when its job passed. With no hash of the phone the script compares three and says so.
//
// The folder holds one subfolder for each artifact "determinism-<os>" of the job "Gate", each with
// the file hash.txt that Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs writes: the hash on
// the first line, then a line for each body and each solved point. The script fails when a runner
// gave no file, or when two hashes differ; it then gives each line that differs, as an annotation,
// so a person with no sign-in can read it.
'use strict';
const fs = require('fs');
const path = require('path');

// Phase R14f3 (ADR-0074, TASK-0233): the program of the phone on the emulator of x86-64 is the fourth platform.
const desktop = ['ubuntu-latest', 'windows-latest', 'macos-latest'];
const folder = process.argv[2];
const emulator = process.argv[3] ?? 'success';
if (!folder) {
  console.error('Usage: node eng/compare-determinism.js <folder> [result of the emulator job]');
  process.exit(2);
}

const phone = path.join(folder, 'determinism-android-x64', 'hash.txt');
const runners = emulator === 'success' || fs.existsSync(phone) ? [...desktop, 'android-x64'] : desktop;
if (runners.length === desktop.length)
  console.log(`::warning title=Determinism::the emulator job gave ${emulator} and no hash of the phone, so the three desktop runners are compared`);

const files = {};
for (const runner of runners) {
  const file = path.join(folder, `determinism-${runner}`, 'hash.txt');
  if (!fs.existsSync(file)) {
    console.log(`::error title=Determinism::the runner ${runner} gave no hash; its test did not run or did not write ${file}`);
    process.exit(1);
  }
  files[runner] = fs.readFileSync(file, 'utf8').replace(/\r/g, '').split('\n').filter(line => line.length > 0);

  // Finding T123 of the codebase review of 2026-11-01 (TASK-0161): three empty files gave three undefined hashes,
  // which are equal. The first line must be the SHA-256 of the test, 64 lower-case hexadecimal digits.
  if (!/^[0-9a-f]{64}$/.test(files[runner][0] ?? '')) {
    console.log(`::error title=Determinism::the runner ${runner} gave no hash on the first line of ${file}: ${JSON.stringify(files[runner][0] ?? '(an empty file)')}`);
    process.exit(1);
  }
}

const hashes = runners.map(runner => files[runner][0]);
for (const runner of runners) console.log(`::notice title=Determinism::${runner} gives ${files[runner][0]}`);
if (hashes.every(hash => hash === hashes[0])) {
  console.log(`The ${runners.length} platforms give one hash.`);
  process.exit(0);
}

const lines = Math.max(...runners.map(runner => files[runner].length));
let shown = 0;
for (let i = 1; i < lines && shown < 20; i++) {
  const texts = runners.map(runner => files[runner][i] ?? '(no line)');
  if (texts.every(text => text === texts[0])) continue;
  shown++;
  console.log(`::error title=Determinism::line ${i + 1} differs: ` + runners.map((runner, k) => `${runner}: ${texts[k]}`).join(' | '));
}
if (shown === 0)
  console.log('::error title=Determinism::the hashes differ, and each line of the summary is equal: a mesh or its indices differ');
process.exit(1);
