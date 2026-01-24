#version 330 core

in VS_OUT {
    vec2 texCoord;
    vec4 tint;
} fs_in;

out vec4 fragColor;

uniform sampler2D uFontAtlas;

// Median of RGB channels for MSDF
float median(float r, float g, float b)
{
    return max(min(r, g), min(max(r, g), b));
}

void main()
{
    // Sample MSDF atlas (must be linear, NOT sRGB)
    vec3 msd = texture(uFontAtlas, fs_in.texCoord).rgb;

    // Signed distance in range [-0.5, +0.5]
    float sd = median(msd.r, msd.g, msd.b) - 0.5;

    // Convert distance to screen-space pixels
    float pxDist = sd / fwidth(sd);

    // Anti-aliased coverage
    float opacity = clamp(pxDist + 0.5, 0.0, 1.0);

    // Output tinted glyph
    fragColor = vec4(fs_in.tint.rgb, fs_in.tint.a * opacity);
}
