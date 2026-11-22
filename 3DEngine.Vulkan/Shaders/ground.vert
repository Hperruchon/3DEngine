#version 450

// The ground of the scene (ADR-0186 item 2, phase R18p, TASK-0458): one triangle that covers the whole target, with no
// vertex buffer. The vertices 0, 1 and 2 go to (-1, -1), (3, -1) and (-1, 3) in clip space, so the target lies inside it.

void main()
{
    vec2 corner = vec2(float((gl_VertexIndex << 1) & 2), float(gl_VertexIndex & 2));
    gl_Position = vec4(corner * 2.0 - 1.0, 0.0, 1.0);
}
