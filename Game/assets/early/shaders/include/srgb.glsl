
const float SRGB_GAMMA = 1.0 / 2.2;
const float SRGB_ALPHA = 0.055;

float linear_to_srgb(float channel)
{
    if(channel <= 0.0031308)
        return 12.92 * channel;
    else
        return (1.0 + SRGB_ALPHA) * pow(channel, 1.0/2.4) - SRGB_ALPHA;
}

vec3 rgb_to_srgb(vec3 color)
{
    return vec3(
        linear_to_srgb(color.r),
        linear_to_srgb(color.g),
        linear_to_srgb(color.b)
    );
}
