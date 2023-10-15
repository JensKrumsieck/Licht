#version 450

layout (binding = 1) uniform sampler2D image;
layout (location = 0) in vec2 inUV;
layout (location = 0) out vec4 fragColor;

void main()
{
    fragColor = texture(image, inUV);
}