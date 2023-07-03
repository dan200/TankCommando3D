
uniform sampler2D particleTexture;

in vec4 vertColour;
in vec2 vertTexCoord;

out vec4 fragColor;

void main(void)
{
	fragColor = texture( particleTexture, vertTexCoord ) * vertColour;
}
