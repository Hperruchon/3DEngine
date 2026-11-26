#version 450

// The highlight of the selected body (ADR-0029 item 2, TASK-0086). The pipeline draws the
// mesh of the selected body again over itself, with mesh.vert, a depth test that passes on
// equal depth, and blending, so this one colour tints the visible faces of the body.
// ADR-0109 item 3 (phase R18b, TASK-0333): the selected body has no opaque draw before the
// highlight, only its depth, so the tint is a transparent fill over what is behind the body.
// ADR-0187 item 9 (phase R33, TASK-0470): the colour comes from the draw, after the two
// matrices of mesh.vert (RenderPipeline.TintOffset). The selected body takes the accent while
// a preview shows, and the new shape of a preview takes a faint amber in "After". Until
// TASK-0470 the colour was the constant (1.0, 0.55, 0.1, 0.25).

layout(location = 0) in vec3 viewPosition;

layout(push_constant) uniform Tint
{
    layout(offset = 128) vec4 colour;
} tint;

layout(location = 0) out vec4 outColour;

void main()
{
    outColour = tint.colour;
}
