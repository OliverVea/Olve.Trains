#version 330 core

layout (location = 0) in vec2 position;
layout (location = 1) in vec2 texCoords;

uniform mat4 world;
uniform mat4 view;
uniform mat4 projection;

uniform sampler2D heightMap;

out vec3 FragPos;
out vec3 FragNormal;
out vec2 TexCoords;

void main()
{
    FragPos = vec3(world * vec4(position, 1.0));
    FragNormal = mat3(transpose(inverse(world))) * normal;
    TexCoords = texCoords;

    gl_Position = projection * view * world * vec4(position, 1.0);
}