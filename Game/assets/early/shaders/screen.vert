uniform vec2 screenSize;

in vec2 position;
in vec2 texCoord;
in vec4 colour;

out vec2 vertTexCoord;
out vec4 vertColour;

const vec2 viewOrigin = vec2( -1.0, 1.0 );
const vec2 viewSize = vec2( 2.0, -2.0 );

void main(void)
{
	vec2 screenPos = position / screenSize;
	gl_Position = vec4( viewOrigin + screenPos * viewSize, 0.0, 1.0 );
	vertTexCoord = texCoord;
	vertColour = colour;
}
