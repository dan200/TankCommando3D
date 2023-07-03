using System;
namespace Dan200.Core.Level
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	internal class RequireComponentAttribute : Attribute
	{
		public readonly Type RequiredType;

		public RequireComponentAttribute(Type requiredType)
		{
			RequiredType = requiredType;
		}
	}
}
