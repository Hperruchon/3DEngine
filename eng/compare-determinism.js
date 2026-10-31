// Compares the determinism hashes of the three runners (TASK-0156, anti-objective 16).
// Usage: node eng/compare-determinism.js <folder>
//
// The folder holds one subfolder for each artifact "determinism-<os>" of the job "Gate", each with
// the file hash.txt that Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs writes: the hash on
// the first line, then a line for each body and each solved point. The script fails when a runner
// gave no file, or when two hashes differ; it then gives each line that differs, as an annotation,
// so a person with no sign-in can read it.
'use strict';
const fs = require('fs');
const path = require('path');

const runners = ['ubuntu-latest', 'windows-latest', 'macos-latest'];
const folder = process.argv[2];
if (!folder) {
  console.error('Usage: node eng/compare-determinism.js <folder>');
  process.exit(2);
}

const files = {};
for (const runner of runners) {
  const file = path.join(folder, `determinism-${runner}`, 'hash.txt');
  if (!fs.existsSync(file)) {
    console.log(`::error title=Determinism::the runner ${runner} gave no hash; its test did not run or did not write ${file}`);
    process.exit(1);
  }
  files[runner] = fs.readFileSync(file, 'utf8').replace(/\r/g, '').split('\n').filter(line => line.length > 0);
}

const hashes = runners.map(runner => files[runner][0]);
for (const runner of runners) console.log(`::notice title=Determinism::${runner} gives ${files[runner][0]}`);
if (hashes.every(hash => hash === hashes[0])) {
  console.log('The three runners give one hash.');
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
