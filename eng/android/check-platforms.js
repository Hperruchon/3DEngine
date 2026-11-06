// Finding T162 of the codebase review of 2026-11-06 (e) (TASK-0251): the gate of Engine.Tests reads the text of the
// project files, so a Condition on the item SupportedPlatform, or CA1416 in a NoWarn, silenced the analyzer of platform
// compatibility, and each gate passed. This step asks MSBuild for the evaluated item and properties of each project that
// the program of the phone reaches, so no text form escapes it. The test
// Each_Project_That_The_Phone_Reaches_Supports_Android holds that this list names each project that the phone reaches.
//
// Usage: node eng/android/check-platforms.js
'use strict';
const { execFileSync } = require('child_process');
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

const root = path.resolve(__dirname, '..', '..');
const offences = [];
for (const name of projects) {
  const file = path.join(root, name, name + '.csproj');
  const args = ['msbuild', file, '-getItem:SupportedPlatform', ...[...lists, ...switches].map(p => '-getProperty:' + p)];
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
  console.log(`${name}: ${platforms.join(', ')}`);
}

if (offences.length > 0) {
  console.error('The analyzer CA1416 must see Android in each project that the phone reaches (TASK-0229, TASK-0251):');
  for (const offence of offences)
    console.error('  ' + offence);
  process.exit(1);
}
console.log(`Each of the ${projects.length} projects supports android, and no property silences CA1416.`);
