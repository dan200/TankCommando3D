
uniform sampler2D inputTexture;
uniform vec2 viewportSize;

in vec2 vertTexCoord;
out vec4 fragColor;

void main(void)
{
	vec2 texCoord = vertTexCoord;

	// Sample the texture
	vec2 p = 1.0 / viewportSize;
	vec3 texColour = texture( inputTexture, texCoord ).rgb;

	// Apply wireframe
	float threshold = 0.01;
	bool n = distance(texColour, texture( inputTexture, texCoord + vec2(0.0, p.y) ).rgb) < threshold;
	bool e =  distance(texColour, texture( inputTexture, texCoord + vec2(-p.x, 0.0) ).rgb) < threshold;
	bool ne = distance(texColour, texture( inputTexture, texCoord + vec2(-p.x, p.y) ).rgb) < threshold;

	vec3 finalColour;
	vec3 whiteColour = vec3(0.3, 1.0, 0.5);
	vec3 blackColour = whiteColour * 0.01;
	if(!n || (!e && !ne) || texColour == vec3(1.0))
	{
		finalColour = whiteColour;
	}
	else
	{
		finalColour = blackColour;
	}
	//finalColour = texColour;

    // Emit result
	fragColor = vec4( finalColour, 1.0 );
}
