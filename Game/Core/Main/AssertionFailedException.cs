using System;

namespace Dan200.Core.Main
{
    internal class AssertionFailedException : Exception
    {
        public AssertionFailedException(string message) : base(message)
        {
        }
    }
}