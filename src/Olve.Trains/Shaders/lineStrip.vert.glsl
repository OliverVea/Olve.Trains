#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;

out vec3 vColor;

uniform mat4 world;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    vColor = aColor;
    vec4 worldPos = world * vec4(aPosition, 1.0);
    gl_Position = projection * view * worldPos;
}
