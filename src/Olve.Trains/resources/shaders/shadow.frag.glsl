#version 330 core

layout(location = 0) out vec2 FragMoments;

void main()
{
    float depth = gl_FragCoord.z;
    FragMoments = vec2(depth, depth * depth);
}
