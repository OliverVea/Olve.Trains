#version 330 core

in VS_OUT {
    vec2 texCoord;
    vec4 tint;
} fs_in;

out vec4 fragColor;

uniform sampler2D uFontAtlas;
uniform float uPxRange;  // Distance range from font atlas metadata (typically 4.0)

// MSDF median function - extracts the correct signed distance from RGB channels
float median(float r, float g, float b) {
    return max(min(r, g), min(max(r, g), b));
}

void main()
{
    // Sample the MSDF atlas
    vec3 msd = texture(uFontAtlas, fs_in.texCoord).rgb;

    // Get the median signed distance
    float sd = median(msd.r, msd.g, msd.b);

    // Compute screen-space pixel range for proper anti-aliasing at any scale
    // This uses derivatives to determine how much the texture is being scaled
    vec2 texSize = vec2(textureSize(uFontAtlas, 0));
    vec2 unitRange = vec2(uPxRange) / texSize;
    vec2 screenTexSize = vec2(1.0) / fwidth(fs_in.texCoord);
    float screenPxRange = max(0.5 * dot(unitRange, screenTexSize), 1.0);

    // Convert signed distance to screen pixels
    float screenPxDistance = screenPxRange * (sd - 0.5);

    // Compute alpha with smooth anti-aliasing
    float opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);

    // Discard fully transparent pixels
    if (opacity < 0.01) {
        discard;
    }

    // Apply tint color with computed opacity
    fragColor = vec4(fs_in.tint.rgb, fs_in.tint.a * opacity);
}
