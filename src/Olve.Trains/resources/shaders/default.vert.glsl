#version 330 core

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 normal;
layout (location = 2) in vec2 texCoords;

// @instanced
layout (location = 3) in mat4 iWorld;

uniform mat4 view;
uniform mat4 projection;

out vec3 FragPos;
out vec3 FragNormal;
out vec2 TexCoords;

void main()
{
    FragPos = vec3(iWorld * vec4(position, 1.0));
    FragNormal = mat3(transpose(inverse(iWorld))) * normal;
    TexCoords = texCoords;

    gl_Position = projection * view * iWorld * vec4(position, 1.0);
}
