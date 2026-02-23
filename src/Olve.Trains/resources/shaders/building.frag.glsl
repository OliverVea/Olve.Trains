#version 330 core

in vec3 FragNormal;
out vec4 fragColor;

uniform vec3 uColor;
uniform vec3 uColorOverride;
uniform float uColorMix;
uniform float uOpacity;

uniform vec3 ambientLightColor;
uniform float ambientLightIntensity;

uniform vec3 directionalLight0Color;
uniform vec3 directionalLight0Dir;
uniform float directionalLight0Intensity;

uniform vec3 directionalLight1Color;
uniform vec3 directionalLight1Dir;
uniform float directionalLight1Intensity;

void main()
{
    vec3 norm = normalize(FragNormal);

    // Directional light 0
    vec3 lightDir0 = normalize(-directionalLight0Dir);
    float diff0 = max(dot(norm, lightDir0), 0.0);
    vec3 diffuse0 = diff0 * directionalLight0Color * directionalLight0Intensity;

    // Directional light 1
    vec3 lightDir1 = normalize(-directionalLight1Dir);
    float diff1 = max(dot(norm, lightDir1), 0.0);
    vec3 diffuse1 = diff1 * directionalLight1Color * directionalLight1Intensity;

    // Ambient component
    vec3 ambient = ambientLightColor * ambientLightIntensity;

    vec3 baseColor = mix(uColor, uColorOverride, uColorMix);
    vec3 color = (ambient + diffuse0 + diffuse1) * baseColor;
    fragColor = vec4(color, uOpacity);
}
