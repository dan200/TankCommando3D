
#include "include/project_shadows.glsl"

const int MAX_GROUPS = 16;
uniform mat4 modelMatrices[MAX_GROUPS];

in float groupIndex;
in vec3 position;
in float push;
in vec3 a;
in vec3 b;
in vec3 c;
in vec3 d;

void main(void)
{
	int groupIndexI = int(groupIndex);

	shadow_vertex_t vertex;
	vertex.position = ( modelMatrices[groupIndexI] * vec4( position.xyz, 1.0 ) ).xyz;
	vertex.push = push;
	vertex.a = ( modelMatrices[groupIndexI] * vec4( a.xyz, 1.0 ) ).xyz;
	vertex.b = ( modelMatrices[groupIndexI] * vec4( b.xyz, 1.0 ) ).xyz;
	vertex.c = ( modelMatrices[groupIndexI] * vec4( c.xyz, 1.0 ) ).xyz;
	vertex.d = ( modelMatrices[groupIndexI] * vec4( d.xyz, 1.0 ) ).xyz;
	gl_Position = project_shadow_vertex(vertex);
}
