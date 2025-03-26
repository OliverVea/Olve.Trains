#version 330 core

layout (triangles) in;
layout (line_strip, max_vertices = 10) out;

in vec3 vWorldPos[];  // Incoming world positions from vertex shader

uniform mat4 world;
uniform mat4 view;
uniform mat4 projection;

out vec3 GS_FragPos;
flat out vec3 FragNormal;
flat out int isNormalLine; // 1 for normal line, 0 for edge lines

// Helper function to compute the normal of a triangle
vec3 calculateNormal(vec3 p0, vec3 p1, vec3 p2) {
    return normalize(cross(p1 - p0, p2 - p0));
}

// Emit a line segment between two points
void EmitLine(vec3 p1, vec3 p2, int normalFlag)
{
    GS_FragPos = p1;
    isNormalLine = normalFlag;
    gl_Position = projection * view * vec4(p1, 1.0);
    EmitVertex();

    GS_FragPos = p2;
    isNormalLine = normalFlag;
    gl_Position = projection * view * vec4(p2, 1.0);
    EmitVertex();

    EndPrimitive();
}

void main()
{
    // Extract triangle vertices
    vec3 p0 = vWorldPos[0];
    vec3 p1 = vWorldPos[1];
    vec3 p2 = vWorldPos[2];

    // Compute normal
    vec3 normal = calculateNormal(p0, p1, p2);
    FragNormal = normal;

    // Emit the three triangle edges
    EmitLine(p0, p1, 0);
    EmitLine(p1, p2, 0);
    EmitLine(p2, p0, 0);

    // Compute and emit the missing diagonal
    // Assumption: The two triangles of a quad are processed sequentially
    // This assumes consistent ordering, which depends on how your mesh is set up.
    vec3 diagonalStart, diagonalEnd;

    if (gl_PrimitiveIDIn % 2 == 0) {
        // First triangle of the quad: define diagonal
        diagonalStart = p0;
        diagonalEnd = p2;
    } else {
        // Second triangle of the quad: define diagonal
        diagonalStart = p1;
        diagonalEnd = p2;
    }

    EmitLine(diagonalStart, diagonalEnd, 0);

    // Emit normal from centroid
    vec3 c = (p0 + p1 + p2) / 3.0;
    EmitLine(c, c - normal * 0.3, 1);
}
