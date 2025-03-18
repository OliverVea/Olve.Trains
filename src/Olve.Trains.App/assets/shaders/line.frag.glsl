#version 330 core

out vec4 FragColor;

uniform vec4 uLineColor;

void main()
{
    float alpha = smoothstep(0.8, 1.0, gl_FragCoord.w); // Anti-aliasing based on fragment depth
    FragColor = vec4(uLineColor.rgb, uLineColor.a * alpha);
}
