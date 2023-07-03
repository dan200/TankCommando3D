using System;
using Dan200.Core.Render;

namespace Dan200.Core.GUI
{
    internal interface IAreaProvider
    {
        Quad Area { get; }
    }
}
