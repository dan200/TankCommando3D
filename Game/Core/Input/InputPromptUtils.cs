using System;
using Dan200.Core.Assets;

namespace Dan200.Core.Input
{
    internal static class InputPromptUtils
    {
        private static int CommonPrefix(string a, string b)
        {
            var dotIndexA = a.LastIndexOf('.');
            var dotIndexB = b.LastIndexOf('.');
            if (dotIndexA >= 0 && dotIndexA == dotIndexB)
            {
                if (a.Substring(0, dotIndexA) == b.Substring(0, dotIndexB))
                {
                    return dotIndexA + 1;
                }
            }
            return 0;
        }

        public static string GetCombinedPrompt(string promptN, string promptP, Language language)
        {
            var prefix = CommonPrefix(promptN, promptP);
            if (prefix > 0)
            {
                var prefixStr = promptN.Substring(0, prefix);
                var suffixN = promptN.Substring(prefix);
                var suffixP = promptP.Substring(prefix);

                // Normal
                var combinedNP = prefixStr + suffixN + '+' + suffixP;
                if (language.CanTranslate(combinedNP))
                {
                    return combinedNP;
                }

                // Inverted
                var combinedPN = prefixStr + suffixP + '+' + suffixN;
                if (language.CanTranslate(combinedPN))
                {
                    return combinedPN;
                }
            }
            return null; 
        }

        private static int CommonPrefix(string a, string b, string c, string d)
        {
            var dotIndexA = a.LastIndexOf('.');
            var dotIndexB = b.LastIndexOf('.');
            var dotIndexC = c.LastIndexOf('.');
            var dotIndexD = d.LastIndexOf('.');
            if (dotIndexA >= 0 && dotIndexA == dotIndexB && dotIndexB == dotIndexC && dotIndexC == dotIndexD)
            {
                if (a.Substring(0, dotIndexA) == b.Substring(0, dotIndexB) &&
                    b.Substring(0, dotIndexB) == c.Substring(0, dotIndexC) &&
                    c.Substring(0, dotIndexC) == d.Substring(0, dotIndexD))
                {
                    return dotIndexA + 1;
                }
            }
            return 0;
        }
       
        public static string GetCombinedPrompt(string promptU, string promptD, string promptL, string promptR, Language language)
        {
            var prefix = CommonPrefix(promptU, promptD, promptL, promptR);
            if (prefix > 0)
            {
                var prefixStr = promptU.Substring(0, prefix);
                var suffixU = promptU.Substring(prefix);
                var suffixD = promptD.Substring(prefix);
                var suffixL = promptL.Substring(prefix);
                var suffixR = promptR.Substring(prefix);

                // Normal
                var combinedUD = prefixStr + suffixU + '+' + suffixD;
                var combinedLR = suffixL + '+' + suffixR;
                var combinedUDLR = combinedUD + '+' + combinedLR;
                if (language.CanTranslate(combinedUDLR))
                {
                    return combinedUDLR;
                }

                // Inverted Y
                var combinedDU = prefixStr + suffixD + '+' + suffixU;
                var combinedDULR = combinedDU + '+' + combinedLR;
                if (language.CanTranslate(combinedDULR))
                {
                    return combinedDULR;
                }

                // Inverted X
                var combinedRL = suffixR + '+' + suffixL;
                var combinedUDRL = combinedUD + '+' + combinedRL;
                if (language.CanTranslate(combinedUDRL))
                {
                    return combinedUDRL;
                }

                // Inverted X and Y
                var combinedDURL = combinedDU + '+' + combinedRL;
                if (language.CanTranslate(combinedDURL))
                {
                    return combinedDURL;
                }
            }
            return null;
        }
    }
}
