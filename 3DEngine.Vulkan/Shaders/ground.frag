#version 450

// The colour of the ground of the scene (ADR-0178 item 1, phase R18p, TASK-0423): a soft light at the middle of the target.
// The square distance from the middle, divided by the square distance from the middle to a corner, gives s: 0 at the
// middle and 1 at each corner. The colour is the colour of a corner plus (1 - s) times the step to the colour of the
// middle. The shader uses + - * / only, and Theme.GroundAt computes the same colour for a test.

layout(push_constant) uniform Ground
{
    vec4 middle;
    vec4 corner;
    vec2 size;
} ground;

layout(location = 0) out vec4 outColour;

void main()
{
    vec2 half_ = ground.size * 0.5;
    vec2 offset = gl_FragCoord.xy - half_;
    float s = dot(offset, offset) / dot(half_, half_);
    vec3 colour = ground.corner.rgb + (1.0 - s) * (ground.middle.rgb - ground.corner.rgb);
    outColour = vec4(colour, 1.0);
}
