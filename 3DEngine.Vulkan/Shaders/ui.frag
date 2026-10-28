#version 450

// The 2D shader of the user interface (ADR-0027 item 3, TASK-0084): the colour of the
// vertex times the texture of the command. The font atlas gives white with an alpha, so
// a glyph takes the colour of its vertex.

layout(set = 0, binding = 0) uniform sampler2D image;

layout(location = 0) in vec2 fragmentCoordinate;
layout(location = 1) in vec4 fragmentColour;

layout(location = 0) out vec4 outColour;

void main()
{
    outColour = fragmentColour * texture(image, fragmentCoordinate);
}
