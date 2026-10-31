#version 450

// The image of a quad of the scene (ADR-0043 item 3, TASK-0146). Each vertex is a corner of the
// unit square; the push constant gives the model-view matrix that takes the square to the quad,
// relative to the eye, and the reversed perspective projection, as mesh.vert does. Row 0 of an
// image is its top, so the coordinate of the texture turns v.

layout(location = 0) in vec2 corner;

layout(push_constant) uniform Draw
{
    mat4 modelView;
    mat4 projection;
} draw;

layout(location = 0) out vec2 coordinate;

void main()
{
    coordinate = vec2(corner.x, 1.0 - corner.y);
    gl_Position = draw.projection * (draw.modelView * vec4(corner, 0.0, 1.0));
}
