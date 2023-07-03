using System;
namespace Dan200.Core.Level
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	internal class AfterSystemAttribute : Attribute
	{
		public readonly Type DependentType;

		public AfterSystemAttribute(Type dependentType)
		{
			DependentType = dependentType;
		}
	}
}
