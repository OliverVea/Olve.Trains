#version 330 core

in vec3 GS_FragPos;
flat in vec3 FragNormal;

uniform vec3 ambientLightColor;
uniform float ambientLightIntensity;

uniform vec3 directionalLight0Color;
uniform vec3 directionalLight0Dir;
uniform float directionalLight0Intensity;

uniform vec3 directionalLight1Color;
uniform vec3 directionalLight1Dir;
uniform float directionalLight1Intensity;

uniform vec3 cameraDirection;

out vec4 FragColor;

void main()
{
    vec3 norm = normalize(FragNormal);

    // Compute screen-space derivatives for edge detection
    vec3 dx = dFdx(norm);
    vec3 dy = dFdy(norm);
    float edgeFactor = length(dx) + length(dy);
    float smoothing = smoothstep(0.2, 0.5, edgeFactor);

    // Directional light 0
    vec3 lightDir0 = normalize(-directionalLight0Dir);
    float diff0 = max(dot(norm, lightDir0), 0.0) * (1.0 - smoothing);
    vec3 diffuse0 = diff0 * directionalLight0Color * directionalLight0Intensity;

    // Directional light 1
    vec3 lightDir1 = normalize(-directionalLight1Dir);
    float diff1 = max(dot(norm, lightDir1), 0.0) * (1.0 - smoothing);
    vec3 diffuse1 = diff1 * directionalLight1Color * directionalLight1Intensity;

    // Ambient component
    vec3 ambient = ambientLightColor * ambientLightIntensity;

    // Sample the texture color using texture coordinates
    vec4 textureColor = vec4(65,152,10, 255) / 255.0;

    float rimFactor = 1.0 - max(dot(cameraDirection, norm), 0.0);
    rimFactor = smoothstep(0.3, 0.8, rimFactor);
    vec3 rimLight = rimFactor * vec3(1.0) * 0.5;

    // Combine lighting components
    vec3 finalColor = (ambient + diffuse0 + diffuse1 + rimLight) * textureColor.rgb;
    FragColor = vec4(finalColor, 1.0);
}
