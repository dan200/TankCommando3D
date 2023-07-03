
uniform sampler2D inputTexture;

in vec2 vertTexCoord;

out vec4 fragColor;

void main(void)
{
	fragColor = texture( inputTexture, vertTexCoord );
}