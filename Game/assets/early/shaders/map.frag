
#include "include/wireframe.glsl"

layout(std140) uniform CameraData
{
	mat4 viewMatrix;
	mat4 projectionMatrix;	
	vec3 cameraPosition;
};

uniform sampler2D _texture;

in vec3 vertPosition;
in vec3 vertNormal;
in vec2 vertTexCoord;

out vec4 fragColour;

void main(void)
{
	// Construct surface
	surface_t surface;
	surface.position = vertPosition;

	vec4 texColour = texture(_texture, vertTexCoord);
	surface.diffuseColour = texColour.rgb;
	surface.alpha = texColour.a;
	surface.specularColour = vec3( 0.0, 0.0, 0.0 );
	surface.emissiveColour = vec3( 0.0, 0.0, 0.0 );
	surface.normal = normalize( vertNormal );

	// Shade
	fragColour = shade_for_wireframe( surface, cameraPosition );
}
