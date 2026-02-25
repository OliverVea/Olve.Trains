#version 330 core

// Per-vertex from unit quad [0..1]
layout(location = 0) in vec2 aPosition;

// @instanced
layout(location = 1) in vec2 iPosPx;    // top-left position in pixels
// @instanced
layout(location = 2) in vec2 iSizePx;   // glyph size in pixels
// @instanced
layout(location = 3) in vec4 iTint;     // RGBA tint
// @instanced
layout(location = 4) in vec2 iUvMin;    // atlas UV min
// @instanced
layout(location = 5) in vec2 iUvMax;    // atlas UV max

out VS_OUT {
    vec2 texCoord;
    vec4 tint;
} vs_out;

uniform vec2 uResolution; // framebuffer size in pixels

void main()
{
    // Glyph position in pixel space
    vec2 posPx = iPosPx + aPosition * iSizePx;

    // Convert to NDC [-1, 1]
    vec2 ndc = vec2(
        (posPx.x / uResolution.x) * 2.0 - 1.0,
        1.0 - (posPx.y / uResolution.y) * 2.0
    );

    gl_Position = vec4(ndc, 0.0, 1.0);

    // Interpolate UVs
    vs_out.texCoord = mix(iUvMin, iUvMax, aPosition);
    vs_out.tint = iTint;
}
