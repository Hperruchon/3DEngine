#version 450

// The highlight of the selected body (ADR-0029 item 2, TASK-0086). The pipeline draws the
// mesh of the selected body again over itself, with mesh.vert, a depth test that passes on
// equal depth, and blending, so this one colour tints the visible faces of the body.

layout(location = 0) in vec3 viewPosition;

layout(location = 0) out vec4 outColour;

void main()
{
    outColour = vec4(1.0, 0.55, 0.1, 0.45);
}
