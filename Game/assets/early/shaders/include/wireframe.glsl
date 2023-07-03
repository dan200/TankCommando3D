
#include "surface.glsl"

vec4 shade_for_wireframe(surface_t surface, vec3 cameraPosition)
{
	vec3 normalColour = vec3(0.5) + 0.5 * surface.normal;
	vec3 texColour = surface.diffuseColour;
	vec3 combinedColour = (texColour + normalColour) * 0.5;

	float distance = dot(cameraPosition - surface.position, surface.normal);
	float distanceNormalised = clamp( distance / 100.0, 0.0, 1.0 );
	vec3 finalColour = mix(combinedColour, vec3(1.0) - combinedColour, pow(distanceNormalised, 1.0));

	if(all(greaterThan(texColour, vec3(0.99))))
	{
		finalColour = texColour;
	}
	return vec4( finalColour, 1.0 );
}
