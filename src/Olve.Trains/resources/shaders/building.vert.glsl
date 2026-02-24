#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;

// @instanced
layout(location = 2) in mat4 iWorld;

uniform mat4 view;
uniform mat4 projection;

out vec3 FragNormal;

void main()
{
    FragNormal = mat3(transpose(inverse(iWorld))) * aNormal;
    gl_Position = projection * view * iWorld * vec4(aPosition, 1.0);
}
