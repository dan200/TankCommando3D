using System;
namespace Dan200.Core.Level
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	internal class AfterComponentAttribute : Attribute
	{
		public readonly Type DependentType;

		public AfterComponentAttribute(Type dependentType)
		{
			DependentType = dependentType;
		}
	}
}
