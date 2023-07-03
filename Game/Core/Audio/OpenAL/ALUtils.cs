using Dan200.Core.Main;
using OpenTK.Audio.OpenAL;
using System;
using System.Diagnostics;

namespace Dan200.Core.Audio.OpenAL
{
    internal class OpenALException : Exception
    {
        public OpenALException(ALError error) : this(error.ToString())
        {
        }

        public OpenALException(string message) : base(message)
        {
        }
    }

    internal static class ALUtils
    {
        private static bool s_openALErrorPrinted = false;

        [Conditional("DEBUG_OPENAL")]
        public static void CheckError(bool throwExceptionInNonDebug = false)
        {
            var error = AL.GetError();
            if (error != ALError.NoError)
            {
                if (App.Debug || throwExceptionInNonDebug)
                {
                    throw new OpenALException(error);
                }
                else if (!s_openALErrorPrinted)
                {
					App.LogError("Encountered OpenAL error: " + error);
                    App.LogError(Environment.StackTrace);
                    s_openALErrorPrinted = true;
                }
            }
        }
    }
}
