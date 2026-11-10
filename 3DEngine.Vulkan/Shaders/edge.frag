#version 450

// The colour of an edge line (ADR-0109 items 2 and 3, phase R18b, TASK-0333): the colour of the draw, with a soft border
// of one pixel across the line. A dashed line (a hidden edge of the selected body) keeps the first half of each period of
// two dash lengths along the line, from the end A.

layout(push_constant) uniform Edge
{
    mat4 modelViewProjection;
    vec4 colour;
    vec4 parameters;
} edge;

layout(location = 0) noperspective in float across;
layout(location = 1) noperspective in float along;

layout(location = 0) out vec4 outColour;

void main()
{
    float dash = edge.parameters.w;
    if (dash > 0.0 && mod(max(along, 0.0), 2.0 * dash) >= dash)
        discard;
    float cover = clamp(edge.parameters.z + 0.5 - abs(across), 0.0, 1.0);
    outColour = vec4(edge.colour.rgb, edge.colour.a * cover);
}
