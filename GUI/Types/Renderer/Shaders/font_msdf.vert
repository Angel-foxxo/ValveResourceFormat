#version 460

layout (location = 0) in vec2 vPOSITION;
layout (location = 1) in vec2 vTEXCOORD;
layout (location = 2) in vec4 vCOLOR;

out vec2 vTexCoord;
out vec4 vFragColor;

uniform mat4 transform;

void main(void) {
    gl_Position = transform * vec4(vPOSITION, 0, 1);
    vTexCoord = vTEXCOORD;
    vFragColor = vCOLOR;
}
