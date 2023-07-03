using System;
namespace Dan200.Core.Level
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	internal class BeforeComponentAttribute : Attribute
	{
		public readonly Type DependentType;

		public BeforeComponentAttribute(Type dependentType)
		{
			DependentType = dependentType;
		}
	}
}
