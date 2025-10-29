#version 330 core

in VS_OUT {
    vec2 texCoord;
    vec4 tint;
} fs_in;

out vec4 fragColor;

uniform sampler2D uTexture;

void main()
{
    // Sample texture and multiply by tint color
    vec4 texColor = texture(uTexture, fs_in.texCoord);
    vec4 result = texColor * fs_in.tint;

    // Discard fully transparent pixels (don't render boxes without background color)
    if (result.a < 0.01) {
        discard;
    }

    fragColor = result;
}
