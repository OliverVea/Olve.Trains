#version 330 core

in vec3 FragPos;
in vec4 FragPosLightSpace;

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

// @implements(IWorldMousePositionShader.MousePosition)
uniform vec3 mousePosition;
uniform float mouseRadius;

// @implements(IShadowShader.ShadowMap)
// @pixelType(Depth)
uniform sampler2D shadowMap;

out vec4 FragColor;

float ShadowCalculation(vec4 fragPosLightSpace, vec3 norm)
{
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;

    if (projCoords.z > 1.0)
        return 0.0;

    float currentDepth = projCoords.z;

    vec3 lightDir = normalize(-directionalLight0Dir);
    float bias = max(0.005 * (1.0 - dot(norm, lightDir)), 0.001);

    float storedDepth = texture(shadowMap, projCoords.xy).r;
    float shadow = currentDepth - bias > storedDepth ? 1.0 : 0.0;

    return shadow;
}

void main()
{
    // Flat normal from screen-space derivatives of world position
    vec3 norm = normalize(cross(dFdx(FragPos), dFdy(FragPos)));
    // Ensure normal points upward (screen-space winding may flip it)
    if (norm.y < 0.0) norm = -norm;

    // Shadow
    float shadow = ShadowCalculation(FragPosLightSpace, norm);

    // Directional light 0 (sun) — attenuated by shadow
    vec3 lightDir0 = normalize(-directionalLight0Dir);
    float diff0 = max(dot(norm, lightDir0), 0.0);
    vec3 diffuse0 = diff0 * directionalLight0Color * directionalLight0Intensity * (1.0 - shadow);

    // Directional light 1 (moon) — not shadowed
    vec3 lightDir1 = normalize(-directionalLight1Dir);
    float diff1 = max(dot(norm, lightDir1), 0.0);
    vec3 diffuse1 = diff1 * directionalLight1Color * directionalLight1Intensity;

    // Ambient component
    vec3 ambient = ambientLightColor * ambientLightIntensity;

    // Terrain base color
    vec4 textureColor = vec4(65, 152, 10, 255) / 255.0;

    float rimFactor = 1.0 - max(dot(cameraDirection, norm), 0.0);
    rimFactor = smoothstep(0.3, 0.8, rimFactor);
    vec3 rimLight = rimFactor * vec3(1.0) * 0.5;

    // Combine lighting
    vec3 finalColor = (ambient + diffuse0 + diffuse1 + rimLight) * textureColor.rgb;

    // SDF grid overlay — fades in near mouse cursor
    // Distance to nearest grid line: 0 at grid lines, 0.5 at cell centers
    vec2 gridDist = 0.5 - abs(fract(FragPos.xz) - 0.5);
    vec2 fw = fwidth(FragPos.xz);
    float gridLine = max(
        1.0 - smoothstep(0.0, fw.x * 1.5, gridDist.x),
        1.0 - smoothstep(0.0, fw.y * 1.5, gridDist.y)
    );

    float gridAlpha = 0.0;
    if (mouseRadius > 0.0)
    {
        float dist = distance(FragPos.xz, mousePosition.xz);
        float t = clamp(1.0 - dist / mouseRadius, 0.0, 1.0);
        gridAlpha = smoothstep(0.1, 0.5, t);
    }

    finalColor = mix(finalColor, vec3(1.0), gridLine * gridAlpha * 0.6);

    FragColor = vec4(finalColor, 1.0);
}
