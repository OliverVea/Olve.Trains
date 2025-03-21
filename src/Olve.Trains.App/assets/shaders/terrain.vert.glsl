#version 330 core

layout (location = 0) in vec2 position;

uniform mat4 world;
uniform mat4 view;
uniform mat4 projection;

uniform sampler2D heightmap;

out vec3 FragPos;

float getHeight(in vec2 coord)
{
    return texture(heightmap, coord).r;
}

void main()
{
    vec4 worldPos = world * vec4(position.x, getHeight(position), position.y, 1.0);
    FragPos = worldPos.xyz;
    gl_Position = projection * view * worldPos;
}