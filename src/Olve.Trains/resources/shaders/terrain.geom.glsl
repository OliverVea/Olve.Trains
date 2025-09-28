#version 330 core

layout (triangles) in;
layout (triangle_strip, max_vertices = 10) out;

in vec3 vWorldPos[];

uniform mat4 world;
uniform mat4 view;
uniform mat4 projection;

out vec3 GS_FragPos;
flat out vec3 FragNormal;

// Helper function to compute the normal of a triangle
vec3 calculateNormal(vec3 p0, vec3 p1, vec3 p2) {
    return -normalize(cross(p1 - p0, p2 - p0));
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
    GS_FragPos = p0;
    gl_Position = projection * view * vec4(p0, 1.0);
    EmitVertex();

    GS_FragPos = p1;
    gl_Position = projection * view * vec4(p1, 1.0);
    EmitVertex();

    GS_FragPos = p2;
    gl_Position = projection * view * vec4(p2, 1.0);
    EmitVertex();

    EndPrimitive();
}
