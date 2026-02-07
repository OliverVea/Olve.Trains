#version 330 core

in vec3 vColor;
out vec4 fragColor;

uniform vec3 uColorOverride;
uniform float uColorMix;
uniform float uOpacity;

void main()
{
    vec3 color = mix(vColor, uColorOverride, uColorMix);
    fragColor = vec4(color, uOpacity);
}
