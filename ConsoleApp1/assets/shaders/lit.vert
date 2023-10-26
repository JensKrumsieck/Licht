#version 450

layout(location = 0) in vec3 position;
layout(location = 1) in vec4 color;
layout(location = 2) in vec3 normal;
layout(location = 3) in vec2 uv;

layout(location = 0) out vec3 fragColor;
layout(location = 1) out vec3 fragWorldPos;
layout(location = 2) out vec3 fragWorldNormal;
layout(location = 3) out vec2 texCoord;

layout(set = 0, binding = 0) uniform GlobalUbo{
    mat4 projectionMatrix;
    mat4 viewMatrix;
    mat4 inverseViewMatrix;
    vec4 ambientLightColor;
} ubo;

layout(push_constant) uniform Push {
    mat4 modelMatrix;
    mat4 normalMatrix;
} push;

void main() {
    vec4 worldPos = push.modelMatrix * vec4(position, 1);
    gl_Position = ubo.projectionMatrix * (ubo.viewMatrix * worldPos);

    fragWorldPos = worldPos.xyz;
    fragWorldNormal = normalize(mat3(push.normalMatrix) * normal);
    fragColor = color.xyz;
    texCoord = uv;
}