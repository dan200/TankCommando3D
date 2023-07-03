using Dan200.Core.Render;

namespace Dan200.Core.GUI
{
    internal interface IAreaHolder
    {
        Quad GetSubArea(int index);
    }
}
