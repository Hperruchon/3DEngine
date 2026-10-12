// Compiles each shader of this folder to SPIR-V and writes shaders.lock (ADR-0022,
// TASK-0069). Run it after each change of a shader, on a computer with the Vulkan
// SDK, and commit the source, the SPIR-V and the lock together:
//
//   node 3DEngine.Vulkan/Shaders/compile.js
//
// The test ShaderSourceTests of 3DEngine.Tests compares each source and each SPIR-V
// file with its line of the lock, so a change of a source with no new compile
// fails it. No runner of the pipeline has a compiler; this script is the only
// place that compiles.
const { execFileSync } = require('child_process');
const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

const folder = __dirname;
const sdk = process.env.VULKAN_SDK;
if (!sdk) {
  console.error('VULKAN_SDK is not set; install the Vulkan SDK first.');
  process.exit(1);
}

const exe = name => path.join(sdk, process.platform === 'win32' ? 'Bin' : 'bin', name + (process.platform === 'win32' ? '.exe' : ''));
const arguments_ = ['--target-env=vulkan1.3', '-O'];
const version = execFileSync(exe('glslc'), ['--version'], { encoding: 'utf8' }).split(/\r?\n/)[0].trim();
const sha256 = bytes => crypto.createHash('sha256').update(bytes).digest('hex');

const lines = [
  '# The lock of the shaders of 3DEngine.Vulkan (ADR-0022). compile.js writes this file;',
  '# do not change it by hand. Each line: the shader, the SHA-256 of its source with LF',
  '# line ends, and the SHA-256 of its SPIR-V.',
  `# compiler: ${version}`,
  `# command: glslc ${arguments_.join(' ')} -o <shader>.spv <shader>`,
];

const shaders = fs.readdirSync(folder).filter(name => /\.(vert|frag)$/.test(name)).sort();
for (const shader of shaders) {
  const source = path.join(folder, shader);
  const spirv = source + '.spv';
  execFileSync(exe('glslc'), [...arguments_, '-o', spirv, source], { stdio: 'inherit' });
  execFileSync(exe('spirv-val'), ['--target-env', 'vulkan1.3', spirv], { stdio: 'inherit' });
  const text = fs.readFileSync(source, 'utf8').split('\r\n').join('\n');
  lines.push(`${shader} ${sha256(Buffer.from(text, 'utf8'))} ${sha256(fs.readFileSync(spirv))}`);
}

fs.writeFileSync(path.join(folder, 'shaders.lock'), lines.join('\n') + '\n');
console.log(`compiled ${shaders.length} shaders with ${version}`);
