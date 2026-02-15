#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;

uniform mat4 world;
uniform mat4 view;
uniform mat4 projection;

out vec3 FragNormal;

void main()
{
    FragNormal = mat3(transpose(inverse(world))) * aNormal;
    gl_Position = projection * view * world * vec4(aPosition, 1.0);
}
