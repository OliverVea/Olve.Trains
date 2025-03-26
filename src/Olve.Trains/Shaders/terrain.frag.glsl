#version 330 core

in vec3 GS_FragPos;
flat in vec3 FragNormal;

uniform vec3 ambientLightColor;
uniform float ambientLightIntensity;

uniform vec3 directionalLightColor;
uniform vec3 directionalLightDir;
uniform float directionalIntensity;

uniform vec3 cameraDirection;

out vec4 FragColor;

void main()
{
    vec3 norm = normalize(FragNormal);
    vec3 lightDir = normalize(-directionalLightDir);

    // Compute screen-space derivatives for edge detection
    vec3 dx = dFdx(norm);
    vec3 dy = dFdy(norm);
    float edgeFactor = length(dx) + length(dy);
    float smoothing = smoothstep(0.2, 0.5, edgeFactor);

    // Diffuse shading with edge smoothing
    float diff = max(dot(norm, lightDir), 0.0) * (1.0 - smoothing);
    vec3 diffuse = diff * directionalLightColor * directionalIntensity;

    // Ambient component
    vec3 ambient = ambientLightColor * ambientLightIntensity;

    // Rim lighting based on view direction
    float rimFactor = 1.0 - max(dot(cameraDirection, norm), 0.0);
    rimFactor = smoothstep(0.3, 0.8, rimFactor); // Control rim intensity
    vec3 rimLight = rimFactor * vec3(1.0) * 0.5; // White rim light with strength

    // Sample the texture color
    vec3 textureColor = vec3(17, 124, 19) / 255.0;
    vec3 weightedTextureColor = textureColor * (GS_FragPos.y / 10.0 + 1.0);

    // Combine lighting components
    vec3 finalColor = (ambient + diffuse + rimLight) * weightedTextureColor;
    FragColor = vec4(finalColor, 1.0);
}
