
#include "surface.glsl"

struct directional_light_t
{
	vec4 colour;
	vec3 direction;
};

struct point_light_t
{
	vec4 colour;
	vec3 position;
	float range;
};

uniform vec4 ambientLightColour;

#ifdef NUM_DIRECTIONAL_LIGHTS
uniform directional_light_t directional_lights[ NUM_DIRECTIONAL_LIGHTS ];
#endif

#ifdef NUM_POINT_LIGHTS
uniform point_light_t point_lights[ NUM_POINT_LIGHTS ];
#endif

vec3 add_directional_light( surface_t surface, directional_light_t light, vec3 cameraDirection )
{
	vec3 lightDirection = -light.direction;

	float diffuseStrength = dot( surface.normal, lightDirection );
	vec3 halfwayDirection = normalize( lightDirection + cameraDirection );
	float specularStrength = pow( max( dot( cameraDirection, halfwayDirection ), 0.0 ), 128.0 );

	vec3 final = vec3(0.0, 0.0, 0.0);
	final += surface.diffuseColour * max( diffuseStrength, 0.0 ) * light.colour.rgb;
	final += surface.specularColour * specularStrength * light.colour.rgb;
	return final;
}

vec3 add_point_light( surface_t surface, point_light_t light, vec3 cameraDirection )
{
	vec3 lightDirection = normalize( light.position - surface.position );
	float lightDistance = length( light.position - surface.position );
	float strength = 10.0 / (lightDistance * lightDistance);

	float diffuseStrength = dot( surface.normal, lightDirection ) * strength;
	vec3 halfwayDirection = normalize( lightDirection + cameraDirection );
	float specularStrength = pow( max( dot( cameraDirection, halfwayDirection ), 0.0 ), 128.0 ) * strength;

	vec3 final = vec3(0.0, 0.0, 0.0);
	final += surface.diffuseColour * max( diffuseStrength, 0.0 ) * light.colour.rgb;
	final += surface.specularColour * specularStrength * light.colour.rgb;
	return final;
}

vec4 apply_standard_lighting(surface_t surface, vec3 cameraPosition)
{
	// Calculate direction to camera
	vec3 cameraDirection = normalize( cameraPosition - surface.position );

	// Start with the emissive colour
	vec3 final = surface.emissiveColour;

	// Add ambient light
	final += surface.diffuseColour * ambientLightColour.rgb;
	
#ifdef NUM_DIRECTIONAL_LIGHTS
	// Add directional lights
	for( int i=0; i < NUM_DIRECTIONAL_LIGHTS; ++i )
	{
		final += add_directional_light( surface, directional_lights[i], cameraDirection );	
	}
#endif

#ifdef NUM_POINT_LIGHTS
	// Add point lights
	for( int i=0; i < NUM_POINT_LIGHTS; ++i )
	{
		final += add_point_light( surface, point_lights[i], cameraDirection );	
	}
#endif

	return vec4( final.rgb, surface.alpha );
}
