
uniform sampler2D elementTexture;

in vec2 vertTexCoord;
in vec4 vertColour;

out vec4 fragColor;

void main(void)
{
	vec4 texColour = texture( elementTexture, vertTexCoord );
	fragColor = vertColour * texColour;
}
