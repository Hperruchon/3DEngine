#version 450

// The colour of the first triangle of R3, interpolated between its corners.

layout(location = 0) in vec3 fragColor;
layout(location = 0) out vec4 outColor;

void main()
{
    outColor = vec4(fragColor, 1.0);
}
