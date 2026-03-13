#version 330 core

// Per-vertex from unit quad [0..1] (two triangles)
// @implements(IWithPosition2D.Position)
layout(location = 0) in vec2 aPosition;

// @instanced
layout(location = 1) in vec2 iPosPx;
// @instanced
layout(location = 2) in vec2 iSizePx;
// @instanced
layout(location = 3) in vec4 iTint;
// @instanced
layout(location = 4) in vec4 iBorderWidthPx;  // left, top, right, bottom
// @instanced
layout(location = 5) in vec4 iBorderColor;    // RGBA
// @instanced
layout(location = 6) in vec4 iBorderRadiusPx; // topLeft, topRight, bottomRight, bottomLeft
// @instanced
layout(location = 7) in vec2 iUvMin;          // atlas UV min
// @instanced
layout(location = 8) in vec2 iUvMax;          // atlas UV max

out VS_OUT {
    vec2 texCoord;
    vec4 tint;
    vec2 fragPosPx;        // Fragment position in box space
    vec2 boxSizePx;        // Box size
    vec4 borderWidthPx;
    vec4 borderColor;
    vec4 borderRadiusPx;
} vs_out;

uniform vec2 uResolution;

void main()
{
    // Position in screen space (pixels)
    vec2 posPx = iPosPx + aPosition * iSizePx;

    // Convert to NDC [-1,1]
    vec2 ndc = vec2(
        (posPx.x / uResolution.x) * 2.0 - 1.0,
        1.0 - (posPx.y / uResolution.y) * 2.0
    );

    gl_Position = vec4(ndc, 0.0, 1.0);

    // Pass data to fragment shader
    // Interpolate UVs within atlas region (flip Y for OpenGL)
    vec2 uv = vec2(aPosition.x, 1.0 - aPosition.y);
    vs_out.texCoord = mix(iUvMin, iUvMax, uv);
    vs_out.tint = iTint;
    vs_out.fragPosPx = aPosition * iSizePx; // Position within the box
    vs_out.boxSizePx = iSizePx;
    vs_out.borderWidthPx = iBorderWidthPx;
    vs_out.borderColor = iBorderColor;
    vs_out.borderRadiusPx = iBorderRadiusPx;
}
