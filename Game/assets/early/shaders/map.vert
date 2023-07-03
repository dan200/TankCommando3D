
layout(std140) uniform CameraData
{
	mat4 viewMatrix;
	mat4 projectionMatrix;	
	vec3 cameraPosition;
};

uniform mat4 modelMatrix;
uniform vec2 textureSize;

in vec3 position;
in vec3 normal;
in vec2 texCoord;

out vec3 vertPosition;
out vec3 vertNormal;
out vec2 vertTexCoord;

void main(void)
{
	vertNormal = normal;
	vertTexCoord = texCoord / textureSize;

	vertPosition = (modelMatrix * vec4( position.xyz, 1.0 )).xyz;
	gl_Position = projectionMatrix * viewMatrix * vec4( vertPosition.xyz, 1.0 );
}
