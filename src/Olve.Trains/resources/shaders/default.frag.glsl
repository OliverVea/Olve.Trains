#version 330 core

in vec3 FragPos;
in vec3 FragNormal;
in vec2 TexCoords;
in vec4 FragPosLightSpace;

out vec4 FragColor;

// @implements(IDaylightShader.AmbientLightColor)
uniform vec3 ambientLightColor;
// @implements(IDaylightShader.AmbientLightIntensity)
uniform float ambientLightIntensity;

// @implements(IDaylightShader.DirectionalLight0Color)
uniform vec3 directionalLight0Color;
// @implements(IDaylightShader.DirectionalLight0Dir)
uniform vec3 directionalLight0Dir;
// @implements(IDaylightShader.DirectionalLight0Intensity)
uniform float directionalLight0Intensity;

// @implements(IDaylightShader.DirectionalLight1Color)
uniform vec3 directionalLight1Color;
// @implements(IDaylightShader.DirectionalLight1Dir)
uniform vec3 directionalLight1Dir;
// @implements(IDaylightShader.DirectionalLight1Intensity)
uniform float directionalLight1Intensity;

// @implements(ICameraDirectionShader.CameraDirection)
uniform vec3 cameraDirection;

// @pixelType(RGBA)
uniform sampler2D textureSampler;

// @implements(IShadowShader.ShadowMap)
// @pixelType(Rg32f)
uniform sampler2D shadowMap;

uniform vec3 uColor;
uniform vec3 uColorOverride;
uniform float uColorMix;
uniform float uOpacity;

float ShadowCalculation(vec4 fragPosLightSpace)
{
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;

    // Outside shadow map — no shadow
    if (projCoords.z > 1.0 || projCoords.x < 0.0 || projCoords.x > 1.0
        || projCoords.y < 0.0 || projCoords.y > 1.0)
        return 0.0;

    float currentDepth = projCoords.z;

    // Sample depth moments from VSM
    vec2 moments = texture(shadowMap, projCoords.xy).rg;

    // Fully lit if closer than mean depth
    if (currentDepth <= moments.x)
        return 0.0;

    // Chebyshev's inequality
    float variance = moments.y - moments.x * moments.x;
    variance = max(variance, 0.00002);

    float d = currentDepth - moments.x;
    float pMax = variance / (variance + d * d);

    // Light bleeding reduction
    pMax = smoothstep(0.3, 1.0, pMax);

    return 1.0 - pMax;
}

void main()
{
    vec3 norm = normalize(FragNormal);

    // Compute screen-space derivatives for edge detection
    vec3 dx = dFdx(norm);
    vec3 dy = dFdy(norm);
    float edgeFactor = length(dx) + length(dy);
    float smoothing = smoothstep(0.2, 0.5, edgeFactor);

    // Shadow
    float shadow = ShadowCalculation(FragPosLightSpace);

    // Directional light 0 (sun) — attenuated by shadow
    vec3 lightDir0 = normalize(-directionalLight0Dir);
    float diff0 = max(dot(norm, lightDir0), 0.0) * (1.0 - smoothing);
    vec3 diffuse0 = diff0 * directionalLight0Color * directionalLight0Intensity * (1.0 - shadow);

    // Directional light 1 (moon) — not shadowed
    vec3 lightDir1 = normalize(-directionalLight1Dir);
    float diff1 = max(dot(norm, lightDir1), 0.0) * (1.0 - smoothing);
    vec3 diffuse1 = diff1 * directionalLight1Color * directionalLight1Intensity;

    // Ambient component
    vec3 ambient = ambientLightColor * ambientLightIntensity;

    // Sample the texture color using texture coordinates
    vec4 textureColor = texture(textureSampler, TexCoords);

    // Color tinting: mix base color with override, then multiply by texture
    vec3 baseColor = mix(uColor, uColorOverride, uColorMix) * textureColor.rgb;

    float rimFactor = 1.0 - max(dot(cameraDirection, norm), 0.0);
    rimFactor = smoothstep(0.3, 0.8, rimFactor);
    vec3 rimLight = rimFactor * vec3(1.0) * 0.5;

    // Combine lighting components
    vec3 finalColor = (ambient + diffuse0 + diffuse1 + rimLight) * baseColor;
    FragColor = vec4(finalColor, uOpacity);
}
