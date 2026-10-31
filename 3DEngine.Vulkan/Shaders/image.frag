#version 450

// The image of a quad of the scene (ADR-0043 item 3, TASK-0146): the colour of the texture,
// opaque.

layout(set = 0, binding = 0) uniform sampler2D image;

layout(location = 0) in vec2 coordinate;

layout(location = 0) out vec4 outColour;

void main()
{
    outColour = vec4(texture(image, coordinate).rgb, 1.0);
}
