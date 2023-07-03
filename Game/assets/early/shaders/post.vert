
in vec3 position;
in vec2 texCoord;

out vec2 vertTexCoord;

void main(void)
{
	gl_Position = vec4( position.xyz, 1.0 );
	vertTexCoord = texCoord;
}
