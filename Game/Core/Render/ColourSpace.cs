using Dan200.Core.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Core.Render
{
    internal enum ColourSpace
    {
        Linear,
        SRGB
    }

    internal static class ColourSpaceUtils
    {
        private const float SRGB_GAMMA = 1.0f / 2.2f;
        private const float SRGB_INVERSE_GAMMA = 2.2f;
        private const float SRGB_ALPHA = 0.055f;

        public static float LinearToSRGB(float channel)
        {
            if (channel <= 0.0031308f)
                return 12.92f * channel;
            else
                return (1.0f + SRGB_ALPHA) * Mathf.Pow(channel, 1.0f / 2.4f) - SRGB_ALPHA;
        }

        public static float SRGBToLinear(float channel)
        {
            if (channel <= 0.04045f)
                return channel / 12.92f;
            else
                return Mathf.Pow((channel + SRGB_ALPHA) / (1.0f + SRGB_ALPHA), 2.4f);
        }
    }
}
