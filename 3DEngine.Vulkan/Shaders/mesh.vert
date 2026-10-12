#version 450

// The indexed mesh of roadmap phase R3 (TASK-0069, ADR-0022). Each position arrives
// as three 32-bit floats, centred on the origin and scaled into the unit sphere
// (MeshVertex.Narrow). One fixed rotation shows three faces of a box; R4 brings a
// camera and depth. The push constant gives the width of the extent divided by its
// height, so that a square stays a square.

layout(location = 0) in vec3 position;

layout(push_constant) uniform Frame
{
    float aspect;
} frame;

layout(location = 0) out vec3 viewPosition;

// A rotation of 25 degrees about x after -35 degrees about y, in columns.
const mat3 view = mat3(
    vec3(0.819152, -0.242404, 0.519837),
    vec3(0.000000, 0.906308, 0.422618),
    vec3(-0.573576, -0.346189, 0.742404));

void main()
{
    viewPosition = view * position;
    gl_Position = vec4(viewPosition.x * 0.8 / frame.aspect, viewPosition.y * 0.8, 0.5, 1.0);
}
