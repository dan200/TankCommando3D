using Dan200.Core.Input;
using Dan200.Core.Math;

namespace Dan200.Core.GUI
{
    internal interface IPress
    {
        bool Released { get; }
        bool Held { get; }
    }

	internal interface ISpatialPress : IPress
	{
		Vector2 CurrentPosition { get; }
	}
}
