#version 330 core

in vec3 FragPos;
in vec3 FragNormal;

out vec4 FragColor;

uniform vec3 ambientLightColor;
uniform float ambientLightIntensity;

uniform vec3 directionalLightColor;
uniform vec3 directionalLightDir;
uniform float directionalIntensity;

void main()
{
    vec3 norm = normalize(FragNormal);
    vec3 lightDir = normalize(-directionalLightDir);

    // Compute screen-space derivatives
    vec3 dx = dFdx(norm);
    vec3 dy = dFdy(norm);

    // Edge detection using the magnitude of the normal variation
    float edgeFactor = length(dx) + length(dy);

    // Use smoothstep to make edges rounder
    float smoothing = smoothstep(0.2, 0.5, edgeFactor);

    // Apply edge smoothing to diffuse lighting
    float diff = max(dot(norm, lightDir), 0.0) * (1.0 - smoothing);
    vec3 diffuse = diff * directionalLightColor * directionalIntensity;

    vec3 ambient = ambientLightColor * ambientLightIntensity;

    FragColor = vec4(diffuse + ambient, 1.0);
}
