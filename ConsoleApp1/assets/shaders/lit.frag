#version 450

layout(location = 0) in vec3 fragColor;
layout(location = 1) in vec3 fragWorldPos;
layout(location = 2) in vec3 fragWorldNormal;

layout(location = 0) out vec4 outColor;

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

struct pointLight {
    vec4 positionRadius;
    vec4 colorIntensity;
};

layout(set = 0, binding = 1, std140) readonly buffer Lights{
    pointLight pointLights[];
};

void main() {
    vec3 diffuseLight = ubo.ambientLightColor.xyz * ubo.ambientLightColor.w;
    vec3 specularLight = vec3(0.0);
    vec3 surfaceNormal = normalize(fragWorldNormal);

    vec3 cameraWorldPos = ubo.inverseViewMatrix[3].xyz;
    vec3 viewDirection = normalize(cameraWorldPos - fragWorldPos);
    
    for(int i = 0; i < pointLights.length(); i++) {
        pointLight light = pointLights[i];

        vec3 directionToLight = light.positionRadius.xyz - fragWorldPos;
        float attenuation = 1.0 / dot(directionToLight, directionToLight); // distance squared
        directionToLight = normalize(directionToLight);

        //diffuse lighting
        float cosAngIncidence = max(dot(surfaceNormal, directionToLight), 0);
        vec3 intensity = light.colorIntensity.xyz * light.colorIntensity.w * attenuation;
        diffuseLight += intensity * cosAngIncidence;

        //specular lighting
        vec3 halfAngle = normalize(directionToLight + viewDirection);
        float blinnTerm = dot(surfaceNormal, halfAngle);
        blinnTerm = clamp(blinnTerm, 0, 1);
        blinnTerm = pow(blinnTerm, 512.0);
        specularLight += light.colorIntensity.xyz * light.colorIntensity.w * attenuation * blinnTerm;
    }

    vec3 color = fragColor;
    outColor = vec4(diffuseLight * color + specularLight * color, 1.0);
}