
layout(std140) uniform CameraData
{
	mat4 viewMatrix;
	mat4 projectionMatrix;	
	vec3 cameraPosition;
};

const int MAX_GROUPS = 16;
uniform mat4 modelMatrices[MAX_GROUPS];
uniform mat3 normalMatrices[MAX_GROUPS];
uniform mat3 uvMatrices[MAX_GROUPS];
uniform vec4 colours[MAX_GROUPS];

in float groupIndex;
in vec3 position;
in vec3 normal;
in vec2 texCoord;

#ifdef USE_NORMAL_TEXTURE
in vec3 tangent;
#endif

out vec3 vertPosition;
out vec3 vertNormal;
out vec2 vertTexCoord;
out vec4 vertColour;

#ifdef USE_NORMAL_TEXTURE
out vec3 vertTangent;
out vec3 vertBiNormal;
#endif

void main(void)
{
	int groupIndexI = int(groupIndex);
	vertNormal = normalize( normal * normalMatrices[groupIndexI] );	
	vertTexCoord = ( uvMatrices[groupIndexI] * vec3( texCoord, 1.0 ) ).xy;
	vertColour = colours[groupIndexI];

#ifdef USE_NORMAL_TEXTURE
	vertTangent = normalize( tangent * normalMatrices[groupIndexI] );
	vertBiNormal = cross( vertNormal, vertTangent );
#endif

	vertPosition = ( modelMatrices[groupIndexI] * vec4( position, 1.0 ) ).xyz;
	gl_Position = projectionMatrix * viewMatrix * vec4( vertPosition, 1.0 );
}
