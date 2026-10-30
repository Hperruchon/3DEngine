#version 450

// The colour of one draw of the overlay (ADR-0038 item 9, TASK-0113), from the push constant.

layout(push_constant) uniform Overlay
{
    vec2 scale;
    vec2 translation;
    vec4 colour;
} overlay;

layout(location = 0) out vec4 outColour;

void main()
{
    outColour = overlay.colour;
}
