// The first demonstration (objective 6 of docs/CHARTER.md, ADR-0025 items 1 and 7).
// It sends four commands over HTTP to a host that serves a document, for example the
// desktop 3DEngine: a box A of 10 x 10 x 10 at the origin, a box B of the same size,
// a move of box B by (5, 5, 5), and the subtraction of box B from box A. The cut
// removes the corner cube from (0, 0, 0) to (5, 5, 5), so the body shows a notch.
//
// Usage: node scripts/first-demonstration.js [address]
// The address is http://127.0.0.1:5180 when no argument is given. The exit code is 0
// when each command is applied, and 1 at the first command that is not.
const address = (process.argv[2] ?? 'http://127.0.0.1:5180').replace(/\/+$/, '');

async function apply(name, parameters) {
  const response = await fetch(`${address}/commands`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ name, schemaVersion: 1, parameters }),
  });
  const result = await response.json();
  if (result.status !== 'Applied')
    throw new Error(`${name} was not applied: ${JSON.stringify(result)}`);

  const bodyId = result.outputs?.bodyId;
  console.log(`${name}: ${result.status}${bodyId ? ` (body ${bodyId})` : ''}`);
  return bodyId;
}

(async () => {
  const boxA = await apply('CreateBox', { sizeX: 10, sizeY: 10, sizeZ: 10 });
  const boxB = await apply('CreateBox', { sizeX: 10, sizeY: 10, sizeZ: 10 });
  const movedB = await apply('Translate', { bodyId: boxB, dx: 5, dy: 5, dz: 5 });
  await apply('Subtract', { minuendBodyId: boxA, subtrahendBodyId: movedB });
})().catch(error => {
  console.error(error.message);
  process.exit(1);
});
