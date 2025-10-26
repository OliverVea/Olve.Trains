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
    fragColor = texColor * fs_in.tint;
}
