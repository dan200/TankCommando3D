using System;
using System.Collections.Generic;

namespace Dan200.Core.Input
{
    internal interface IDevice
    {
        bool Connected { get; }
        DeviceCategory Category { get; }
        IEnumerable<Input> Inputs { get; }
        Input GetInput( string name );
    }
}
