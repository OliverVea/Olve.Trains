#version 330 core

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 normal;

uniform mat4 world;
uniform mat4 view;
uniform mat4 projection;

uniform vec3 mouseRayOrigin;
uniform vec3 mouseRayDirection;

out vec3 FragPos;
out vec3 FragNormal;
out float DistanceToMouseRay;

void main()
{
    // Compute the world-space position
    vec4 worldPos = world * vec4(position, 1.0);
    FragPos = worldPos.xyz;

    // Transform the normal correctly (for non-uniform scaling, use inverse transpose)
    FragNormal = normalize((transpose(inverse(world)) * vec4(normal, 0.0)).xyz);

    // Calculate the distance from the vertex to the mouse ray in world space.
    vec3 rayToVertex = FragPos - mouseRayOrigin;
    // Optionally adjust the divisor (e.g., / 10.0) to suit your scene scale.
    DistanceToMouseRay = length(cross(mouseRayDirection, rayToVertex)) / 10.0;

    // Compute final position on screen
    gl_Position = projection * view * worldPos;
}