#version 330 core

// Per-vertex from unit quad [0..1] (two triangles)
layout(location = 0) in vec2 aUnit;

// Per-instance attributes
layout(location = 1) in vec2 iPosPx;
layout(location = 2) in vec2 iSizePx;
layout(location = 3) in vec4 iTint;

out VS_OUT {
    vec2 texCoord;
    vec4 tint;
} vs_out;

uniform vec2 uResolution;

void main()
{
    // Position in screen space (pixels)
    vec2 posPx = iPosPx + aUnit * iSizePx;

    // Convert to NDC [-1,1]
    vec2 ndc = vec2(
        (posPx.x / uResolution.x) * 2.0 - 1.0,
        1.0 - (posPx.y / uResolution.y) * 2.0
    );

    gl_Position = vec4(ndc, 0.0, 1.0);

    // Pass texture coordinates (aUnit is already [0..1])
    vs_out.texCoord = aUnit;
    vs_out.tint = iTint;
}
