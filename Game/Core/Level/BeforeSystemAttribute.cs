using System;
namespace Dan200.Core.Level
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	internal class BeforeSystemAttribute : Attribute
	{
		public readonly Type DependentType;

		public BeforeSystemAttribute(Type dependentType)
		{
			DependentType = dependentType;
		}
	}
}
