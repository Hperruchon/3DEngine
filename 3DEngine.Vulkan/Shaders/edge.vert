#version 450

// The edges of a body as lines of a fixed width in pixels (ADR-0109 item 2, phase R18b, TASK-0333). Each edge comes as two
// triangles of six vertices (EdgeLines.Vertices); each vertex has both ends of its edge and its corner. The shader puts
// the corner at its end, then moves it across the line by the half width and along the line past the end by the same
// distance, in pixels, so the device needs no wide lines. An end behind the eye moves to the plane w = near first.
//
// The depth of the line comes nearer to the eye by a small fraction, so a line on a face of its body wins the depth test
// against that face. A hidden edge (the pipeline with the depth test Less) is behind the stored depth by more than that.

layout(location = 0) in vec3 endA;
layout(location = 1) in vec3 endB;
layout(location = 2) in vec2 corner;

layout(push_constant) uniform Edge
{
    mat4 modelViewProjection;
    vec4 colour;
    // x and y: the size of the target in pixels; z: the half width of the line in pixels; w: the length of a dash in
    // pixels, or 0 for a solid line.
    vec4 parameters;
} edge;

// The distance from the centre line across the line, and from the end A along the line, both in pixels.
layout(location = 0) noperspective out float across;
layout(location = 1) noperspective out float along;

const float Nearest = 1e-4;
const float DepthPull = 2e-3;

void main()
{
    vec4 a = edge.modelViewProjection * vec4(endA, 1.0);
    vec4 b = edge.modelViewProjection * vec4(endB, 1.0);
    if (a.w < Nearest && b.w < Nearest)
    {
        gl_Position = vec4(0.0, 0.0, 0.0, 1.0);
        across = 0.0;
        along = 0.0;
        return;
    }
    if (a.w < Nearest)
        a = mix(a, b, (Nearest - a.w) / (b.w - a.w));
    else if (b.w < Nearest)
        b = mix(b, a, (Nearest - b.w) / (a.w - b.w));

    vec2 half_ = edge.parameters.xy * 0.5;
    vec2 pixelA = a.xy / a.w * half_;
    vec2 pixelB = b.xy / b.w * half_;
    vec2 line = pixelB - pixelA;
    float length_ = length(line);
    vec2 direction = length_ > 1e-6 ? line / length_ : vec2(1.0, 0.0);
    vec2 normal = vec2(-direction.y, direction.x);

    // One pixel more than the half width, for the soft border of the line.
    float reach = edge.parameters.z + 1.0;
    float end = corner.x * 2.0 - 1.0;
    vec4 base = corner.x < 0.5 ? a : b;
    vec2 offset = normal * corner.y * reach + direction * end * reach;
    gl_Position = vec4(base.xy + offset / half_ * base.w, base.z * (1.0 + DepthPull), base.w);
    across = corner.y * reach;
    along = corner.x * length_ + end * reach;
}
