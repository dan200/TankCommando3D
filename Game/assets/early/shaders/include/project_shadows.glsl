
layout(std140) uniform CameraData
{
	mat4 viewMatrix;
	mat4 projectionMatrix;	
	vec3 cameraPosition;
};

#ifdef SHADOW_LIGHT_IS_POSITIONAL
uniform vec3 lightPosition;
#else
uniform vec3 lightDirection;
#endif

/*
   A
 / | \
D <|  B
 \ | /
   C
*/

struct shadow_vertex_t
{
	vec3 position;
	float push;
	vec3 a; 
	vec3 b; 
	vec3 c; 
	vec3 d; 
};

vec4 project_shadow_vertex(shadow_vertex_t vertex)
{
#ifdef SHADOW_LIGHT_IS_POSITIONAL
	vec3 lightDirection = vertex.position - lightPosition;
#endif
	vec3 inFaceNormal = cross(vertex.c - vertex.a, vertex.b - vertex.a);
	vec3 outFaceNormal = cross(vertex.a - vertex.c, vertex.d - vertex.c);

	vec4 worldPos;
	if( dot( inFaceNormal, lightDirection ) > 0.0 && dot( outFaceNormal, lightDirection ) <= 0.0 )
	{
		if( vertex.push > 0.0 )
		{
			worldPos = vec4( lightDirection, 0.0 );
		}
		else
		{
			worldPos = vec4( vertex.position, 1.0 );
		}
	}
	else
	{
		worldPos = vec4( 999.0, 999.0, 999.0, 1.0 );
	}

	return projectionMatrix * viewMatrix * worldPos;
}
