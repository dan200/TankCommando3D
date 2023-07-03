using System.Collections.Generic;

namespace Dan200.Core.Input
{
    internal interface ITouchscreen : IDevice, IVibrator
    {
        IEnumerable<Touch> Touches { get; }
        Input GetInput(TouchscreenButton button);
    }
}
