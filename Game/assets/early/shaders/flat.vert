
layout(std140) uniform CameraData
{
	mat4 viewMatrix;
	mat4 projectionMatrix;	
	vec3 cameraPosition;
};

uniform mat4 modelMatrix;

in vec3 position;
in vec4 colour;

out vec4 vertColour;

void main(void)
{
	vertColour = colour;

	vec3 vertPosition = (modelMatrix * vec4( position.xyz, 1.0 )).xyz;
	gl_Position = projectionMatrix * viewMatrix * vec4( vertPosition.xyz, 1.0 );
}
