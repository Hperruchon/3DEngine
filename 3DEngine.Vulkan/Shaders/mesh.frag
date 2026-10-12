#version 450

// Flat shading of the mesh of R3. The derivatives of the view position give the
// normal of each triangle, so the mesh needs no normal; ADR-0018 item 5 leaves the
// normals to the host.

layout(location = 0) in vec3 viewPosition;
layout(location = 0) out vec4 outColor;

void main()
{
    vec3 normal = normalize(cross(dFdx(viewPosition), dFdy(viewPosition)));
    float light = 0.35 + 0.65 * abs(dot(normal, normalize(vec3(0.3, 0.5, 1.0))));
    outColor = vec4(vec3(0.55, 0.65, 0.80) * light, 1.0);
}
