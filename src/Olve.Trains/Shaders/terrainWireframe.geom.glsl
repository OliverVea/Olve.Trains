#version 330 core

layout (triangles) in;
layout (line_strip, max_vertices = 20) out;

in vec3 vWorldPos[];  // Incoming world positions from vertex shader
// Mark the incoming varying as flat.
flat in float distanceToMouse[];

uniform mat4 world;
uniform mat4 view;
uniform mat4 projection;

uniform bool drawNormals;
uniform bool drawDiagonals;

// Rename the output variable to avoid conflict.
out vec3 GS_FragPos;
flat out vec3 FragNormal;
flat out int isNormalLine;
// Rename output to gsDistanceToMouse.
out float gsDistanceToMouse;

vec3 calculateNormal(vec3 p0, vec3 p1, vec3 p2) {
    return normalize(cross(p1 - p0, p2 - p0));
}

void EmitLine(vec3 p1, vec3 p2, int normalFlag, float maxDistance1, float maxDistance2)
{
    GS_FragPos = p1;
    isNormalLine = normalFlag;
    gl_Position = projection * view * vec4(p1, 1.0);
    gsDistanceToMouse = maxDistance1;
    EmitVertex();

    GS_FragPos = p2;
    isNormalLine = normalFlag;
    gl_Position = projection * view * vec4(p2, 1.0);
    gsDistanceToMouse = maxDistance2;
    EmitVertex();

    EndPrimitive();
}

void main()
{
    vec3 p0 = vWorldPos[0];
    vec3 p1 = vWorldPos[1];
    vec3 p2 = vWorldPos[2];

    float d0 = distanceToMouse[0];
    float d1 = distanceToMouse[1];
    float d2 = distanceToMouse[2];

    vec3 normal = calculateNormal(p0, p1, p2);
    FragNormal = normal;

    // Emit the three edges of the triangle
    EmitLine(p0, p1, 0, d0, d1);
    EmitLine(p1, p2, 0, d1, d2);

    // Emit the third edge
    if (drawDiagonals) {
        EmitLine(p2, p0, 0, d2, d0);
    }

    if (drawNormals) {
        // Emit normal from centroid
        vec3 c = (p0 + p1 + p2) / 3.0;
        EmitLine(c, c - normal * 0.3, 1, d0, d0);
    }
}
