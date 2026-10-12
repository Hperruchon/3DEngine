#version 450

// The mesh shader of the renderer (TASK-0069, TASK-0070). Each position arrives as
// three 32-bit floats, relative to the origin of its mesh (MeshVertex.Narrow). The
// push constant gives the model-view matrix of the draw, relative to the eye
// (ADR-0023 item 2), and the reversed perspective projection (ADR-0023 item 1).

layout(location = 0) in vec3 position;

layout(push_constant) uniform Draw
{
    mat4 modelView;
    mat4 projection;
} draw;

layout(location = 0) out vec3 viewPosition;

void main()
{
    vec4 inView = draw.modelView * vec4(position, 1.0);
    viewPosition = inView.xyz;
    gl_Position = draw.projection * inView;
}
