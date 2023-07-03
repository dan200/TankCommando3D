
#include "include/wireframe.glsl"

layout(std140) uniform CameraData
{
	mat4 viewMatrix;
	mat4 projectionMatrix;	
	vec3 cameraPosition;
};

#ifdef USE_DIFFUSE_TEXTURE
uniform sampler2D diffuseTexture;
#endif

in vec3 vertPosition;
in vec3 vertNormal;
in vec4 vertColour;
in vec2 vertTexCoord;

out vec4 fragColour;

void main()
{
	// Construct surface
	surface_t surface;
	surface.position = vertPosition;

#ifdef USE_DIFFUSE_TEXTURE
	vec4 diffuseColour = vertColour * texture( diffuseTexture, vertTexCoord );
	surface.diffuseColour = diffuseColour.rgb;
	surface.alpha = diffuseColour.a;
#else
	surface.diffuseColour = vertColour.rgb;
	surface.alpha = vertColour.a;
#endif
	surface.specularColour = vec3( 0.0, 0.0, 0.0 );
	surface.emissiveColour = vec3( 0.0, 0.0, 0.0 );
	surface.normal = normalize( vertNormal );

	// Shade
	fragColour = shade_for_wireframe( surface, cameraPosition );
}
