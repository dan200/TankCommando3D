
uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

uniform sampler2D noiseTexture;
uniform float time;
uniform float stopTime;

uniform float lifetime;
uniform float emitterRate;
uniform vec3 emitterPos;
uniform vec3 emitterPosRange;
uniform vec3 emitterVel;
uniform vec3 emitterVelRange;
uniform vec3 gravity;
uniform float initialRadius;
uniform float finalRadius;
uniform vec4 initialColour;
uniform vec4 finalColour;

in float particleIndex;
in vec3 position;
in vec2 texCoord;

out vec4 vertColour;
out vec2 vertTexCoord;

void main(void)
{
	// Get unique particle info
	vec4 noise1 = texture(noiseTexture, vec2(0.5 / 512.0, (particleIndex + 0.5) / 512.0));
	vec4 noise2 = texture(noiseTexture, vec2(1.5 / 512.0, (particleIndex + 0.5) / 512.0));

	float t = mod( time - (particleIndex / emitterRate), lifetime );
	float spawnTime = time - t;
	float f = t / lifetime;

	// Determine particle position
	vec3 particleVel =
		emitterVel +
		(-1.0 + 2.0 * noise1.rgb) * emitterVelRange;
	vec3 particlePos =
		emitterPos +
		(-1.0 + 2.0 * noise2.rgb) * emitterPosRange +
		particleVel * t +
		0.5 * gravity * t * t;
	float radius = mix( initialRadius, finalRadius, f );

	// Project to screen space
	vec3 particlePosVS = (modelMatrix * viewMatrix * vec4( particlePos.xyz, 1.0 )).xyz;
	vec3 vertPosVS = particlePosVS + position * radius;
	gl_Position = projectionMatrix * vec4( vertPosVS.xyz, 1.0 );

	// Emit texture and colour
	if( spawnTime > stopTime )
	{
		vertColour = vec4( 0.0, 0.0, 0.0, 0.0);
	}
	else
	{
		vertColour = mix( initialColour, finalColour, f );
	}
	vertTexCoord = texCoord;
}
