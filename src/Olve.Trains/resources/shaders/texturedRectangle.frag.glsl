#version 330 core

in VS_OUT {
    vec2 texCoord;
    vec4 tint;
    vec2 fragPosPx;
    vec2 boxSizePx;
    vec4 borderWidthPx;
    vec4 borderColor;
    vec4 borderRadiusPx;
} fs_in;

out vec4 fragColor;

uniform sampler2D uTexture;

// SDF for rounded rectangle
// Returns signed distance: negative inside, positive outside, 0 at edge
float sdRoundedBox(vec2 p, vec2 b, vec4 r)
{
    // Select correct corner radius based on quadrant
    r.xy = (p.x > 0.0) ? r.xy : r.zw;
    r.x  = (p.y > 0.0) ? r.x  : r.y;

    vec2 q = abs(p) - b + r.x;
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
}

void main()
{
    // Calculate position relative to box center
    vec2 halfSize = fs_in.boxSizePx * 0.5;
    vec2 p = fs_in.fragPosPx - halfSize;

    // Extract border parameters
    float borderLeft = fs_in.borderWidthPx.x;
    float borderTop = fs_in.borderWidthPx.y;
    float borderRight = fs_in.borderWidthPx.z;
    float borderBottom = fs_in.borderWidthPx.w;

    // Border radii: topLeft, topRight, bottomRight, bottomLeft
    vec4 outerRadii = fs_in.borderRadiusPx;

    // Calculate inner radii (for border inner edge)
    // Inner radius = max(0, outer radius - border width)
    vec4 innerRadii = max(vec4(0.0), outerRadii - vec4(
        max(borderLeft, borderTop),     // top-left
        max(borderRight, borderTop),    // top-right
        max(borderRight, borderBottom), // bottom-right
        max(borderLeft, borderBottom)   // bottom-left
    ));

    // SDF for outer edge (with outer radii)
    float distOuter = sdRoundedBox(p, halfSize, outerRadii);

    // SDF for inner edge (with inner radii and reduced size)
    vec2 innerHalfSize = halfSize - vec2(
        (borderLeft + borderRight) * 0.5,
        (borderTop + borderBottom) * 0.5
    );
    float distInner = sdRoundedBox(p, innerHalfSize, innerRadii);

    // Anti-aliasing: 1px smooth transition
    float aaRange = 1.0;

    // Outer mask (1 inside box, 0 outside)
    float outerMask = 1.0 - smoothstep(-aaRange, aaRange, distOuter);

    // Border region: between outer and inner edges
    float borderMask = smoothstep(-aaRange, aaRange, distInner);

    // Sample texture for background
    vec4 texColor = texture(uTexture, fs_in.texCoord);
    vec4 backgroundColor = texColor * fs_in.tint;

    // Blend border and background
    vec4 borderCol = fs_in.borderColor;
    vec4 finalColor = mix(backgroundColor, borderCol, borderMask);

    // Apply outer mask
    finalColor.a *= outerMask;

    // Discard fully transparent pixels
    if (finalColor.a < 0.01) {
        discard;
    }

    fragColor = finalColor;
}
