#version 450

// The first triangle of roadmap phase R3 (TASK-0069, ADR-0022). It has no vertex
// input: the three corners and their colours are constants. The viewport has a
// negative height, so y points up, and the corners are counter-clockwise, which
// is the front face of the pipeline.

layout(location = 0) out vec3 fragColor;

const vec2 corners[3] = vec2[](vec2(0.0, 0.6), vec2(-0.6, -0.5), vec2(0.6, -0.5));
const vec3 colors[3] = vec3[](vec3(1.0, 0.2, 0.2), vec3(0.2, 1.0, 0.2), vec3(0.2, 0.4, 1.0));

void main()
{
    gl_Position = vec4(corners[gl_VertexIndex], 0.0, 1.0);
    fragColor = colors[gl_VertexIndex];
}
