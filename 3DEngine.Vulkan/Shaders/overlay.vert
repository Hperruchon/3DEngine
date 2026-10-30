#version 450

// The 2D shader of the overlay of the scene: the translate gizmo (ADR-0038 item 9, TASK-0113).
// Each vertex gives a position in window points, with y down. The push constant turns a
// position into clip space, as ui.vert does, and gives the colour of the draw to overlay.frag.

layout(location = 0) in vec2 position;

layout(push_constant) uniform Overlay
{
    vec2 scale;
    vec2 translation;
    vec4 colour;
} overlay;

void main()
{
    gl_Position = vec4(position * overlay.scale + overlay.translation, 0.0, 1.0);
}
