using System;
namespace Dan200.Core.Level
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    internal class RequireComponentOnAncestorAttribute : Attribute
    {
        public readonly Type RequiredType;
        public readonly bool IncludeSelf;

        public RequireComponentOnAncestorAttribute(Type requiredType, bool includeSelf = false)
        {
            RequiredType = requiredType;
            IncludeSelf = includeSelf;
        }
    }
}
