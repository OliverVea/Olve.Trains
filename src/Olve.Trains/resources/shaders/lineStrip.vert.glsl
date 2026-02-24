#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;

// @instanced
layout(location = 2) in mat4 iWorld;

out vec3 vColor;

uniform mat4 view;
uniform mat4 projection;

void main()
{
    vColor = aColor;
    vec4 worldPos = iWorld * vec4(aPosition, 1.0);
    gl_Position = projection * view * worldPos;
}
