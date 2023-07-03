
uniform sampler2D _texture;

in vec4 vertColour;
in vec2 vertTexCoord;

out vec4 fragColor;

void main(void)
{
	fragColor = vertColour * texture(_texture, vertTexCoord);
}
