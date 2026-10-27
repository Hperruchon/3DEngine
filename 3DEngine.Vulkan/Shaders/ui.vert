#version 450

// The 2D shader of the user interface (ADR-0027 item 3, TASK-0084). Each vertex gives a
// position in pixels of the display, with y down, a texture coordinate and a colour of
// four bytes. The push constant turns a position in pixels into clip space: the scale is
// 2 divided by the size of the display, and the translation puts the top-left corner of
// the display at (-1, -1).

layout(location = 0) in vec2 position;
layout(location = 1) in vec2 textureCoordinate;
layout(location = 2) in vec4 colour;

layout(push_constant) uniform Display
{
    vec2 scale;
    vec2 translation;
} display;

layout(location = 0) out vec2 fragmentCoordinate;
layout(location = 1) out vec4 fragmentColour;

void main()
{
    fragmentCoordinate = textureCoordinate;
    fragmentColour = colour;
    gl_Position = vec4(position * display.scale + display.translation, 0.0, 1.0);
}
