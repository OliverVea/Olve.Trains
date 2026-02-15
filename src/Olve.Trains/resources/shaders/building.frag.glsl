#version 330 core

in vec3 FragNormal;
out vec4 fragColor;

uniform vec3 uColor;
uniform vec3 uColorOverride;
uniform float uColorMix;
uniform float uOpacity;

void main()
{
    vec3 norm = normalize(FragNormal);
    vec3 lightDir = normalize(vec3(0.3, 1.0, 0.5));

    float diff = max(dot(norm, lightDir), 0.0);
    float ambient = 0.3;
    float lighting = ambient + diff * 0.7;

    vec3 baseColor = mix(uColor, uColorOverride, uColorMix);
    vec3 color = baseColor * lighting;
    fragColor = vec4(color, uOpacity);
}
