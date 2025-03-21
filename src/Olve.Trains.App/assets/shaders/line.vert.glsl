#version 330 core

layout (location = 0) in vec3 position;

uniform mat4 world;
uniform mat4 view;
uniform mat4 projection;

out vec3 FragPos;

void main()
{
    FragPos = position;
    gl_Position = projection * view * world * vec4(position, 1.0);
}