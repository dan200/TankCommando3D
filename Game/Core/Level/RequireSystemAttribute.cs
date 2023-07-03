using System;
namespace Dan200.Core.Level
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	internal class RequireSystemAttribute : Attribute
	{
		public readonly Type RequiredType;

		public RequireSystemAttribute(Type requiredType)
		{
			RequiredType = requiredType;
		}
	}
}
