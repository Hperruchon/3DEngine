// Finding T162 of the codebase review of 2026-11-06 (e) (TASK-0251): the gate of Engine.Tests reads the text of the
// project files, so a Condition on the item SupportedPlatform, or CA1416 in a NoWarn, silenced the analyzer of platform
// compatibility, and each gate passed. This step asks MSBuild for the evaluated item and properties of each project that
// the program of the phone reaches, so no text form escapes it. The test
// Each_Project_That_The_Phone_Reaches_Supports_Android holds that this list names each project that the phone reaches.
//
// Findings T165 and T166 of the codebase review of 2026-11-07 (TASK-0263): the severity of a category in an analyzer
// file silenced CA1416 too, and three projects did not treat a warning as an error. Now each project must evaluate
// TreatWarningsAsErrors to true, and the step compiles a call that Android does not have in each project and requires
// the error CA1416 for it. So a silence of any form, in a property, an analyzer file or a switch, fails the step.
//
// Usage: node eng/android/check-platforms.js
// The step runs after a build of the projects, because the probe build does not build the referenced projects.
'use strict';
const { execFileSync, spawnSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const projects = [
  '3DEngine.App',
  '3DEngine.Core',
  '3DEngine.Vulkan',
  'Engine.Contracts',
  'Engine.Core',
  'Engine.Geometry.Manifold',
  'Engine.Kernel',
];

// A property that holds a list of codes, and a property that stops the analyzers when it is false.
const lists = ['NoWarn', 'WarningsNotAsErrors'];
const switches = ['RunAnalyzers', 'RunAnalyzersDuringBuild', 'EnableNETAnalyzers'];

// T165: the probe of each project. Console.ForegroundColor has no Android form, so CA1416 must report this line.
const probeName = 'CheckPlatformsProbe.cs';
const probe = [
  '// The probe of eng/android/check-platforms.js (TASK-0263). The step deletes this file after its build.',
  'internal static class CheckPlatformsProbe',
  '{',
  '    internal static void Run() => System.Console.ForegroundColor = System.ConsoleColor.Red;',
  '}',
  '',
].join('\n');

const root = path.resolve(__dirname, '..', '..');
const offences = [];
for (const name of projects) {
  const file = path.join(root, name, name + '.csproj');
  const args = ['msbuild', file, '-getItem:SupportedPlatform', ...[...lists, ...switches, 'TreatWarningsAsErrors'].map(p => '-getProperty:' + p)];
  const result = JSON.parse(execFileSync('dotnet', args, { encoding: 'utf8' }));
  const platforms = (result.Items.SupportedPlatform || []).map(item => item.Identity.toLowerCase());
  if (!platforms.includes('android'))
    offences.push(`${name}: the evaluated items SupportedPlatform are ${platforms.join(', ') || 'none'}, with no android`);
  for (const property of lists) {
    const codes = (result.Properties[property] || '').split(/[;,\s]+/).map(code => code.toUpperCase());
    if (codes.includes('CA1416'))
      offences.push(`${name}: the evaluated property ${property} holds CA1416`);
  }
  for (const property of switches) {
    if ((result.Properties[property] || '').trim().toLowerCase() === 'false')
      offences.push(`${name}: the evaluated property ${property} is false, so the analyzer CA1416 does not run`);
  }
  // T166: a warning CA1416 must fail the build.
  if ((result.Properties.TreatWarningsAsErrors || '').trim().toLowerCase() !== 'true')
    offences.push(`${name}: the evaluated property TreatWarningsAsErrors is not true, so CA1416 is a warning only`);

  // T165: the build of the project with the probe must give the error CA1416 at the probe.
  const probeFile = path.join(root, name, probeName);
  fs.writeFileSync(probeFile, probe);
  let output;
  try {
    const build = spawnSync('dotnet', ['build', file, '-p:BuildProjectReferences=false', '-nologo', '-clp:NoSummary'], { encoding: 'utf8' });
    output = (build.stdout || '') + (build.stderr || '');
  } finally {
    fs.rmSync(probeFile, { force: true });
  }
  const reported = output.split(/\r?\n/).some(line => line.includes(probeName) && /error CA1416/.test(line));
  if (!reported)
    offences.push(`${name}: the build with ${probeName} gave no error CA1416 at the probe, so a silence or a severity hides it`);
  console.log(`${name}: ${platforms.join(', ')}; the probe gives ${reported ? 'the error CA1416' : 'no error CA1416'}`);
}

if (offences.length > 0) {
  console.error('The analyzer CA1416 must see Android in each project that the phone reaches (TASK-0229, TASK-0251, TASK-0263):');
  for (const offence of offences)
    console.error('  ' + offence);
  process.exit(1);
}
console.log(`Each of the ${projects.length} projects supports android, treats a warning as an error, and gives the error CA1416 for its probe.`);
