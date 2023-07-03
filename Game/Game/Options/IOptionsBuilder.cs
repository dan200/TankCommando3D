using System;
namespace Dan200.Game.Options
{
	internal interface IOptionsBuilder
	{
		void AddCheckbox(string label, IOption<bool> option);
		void AddSlider(string label, IOption<float> option, float min, float max);
	}
}
