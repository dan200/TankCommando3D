using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Dan200.Core.Assets
{
    internal class Language : ICompoundAsset
    {
        public static Language GetMostSimilarTo(string code)
        {
            // Look for an exact match
            code = code.ToLowerInvariant();
            var exactPath = "languages/" + code + ".lang";
            if (Assets.Exists<Language>(exactPath))
            {
                return Language.Get(exactPath);
            }

            int underscoreIndex = code.IndexOf('_');
            if (underscoreIndex >= 0)
            {
                // Look for a root match on the language part (ie: en_GB -> en)
                var langPart = (underscoreIndex > 0) ? code.Substring(0, underscoreIndex) : code;
                var langPartPath = "languages/" + langPart + ".lang";
                if (Assets.Exists<Language>(langPartPath))
                {
                    return Language.Get(langPartPath);
                }

                // Look for a similar match on the language part (ie: en_GB -> en_US)
                foreach (var otherLanguage in Assets.List<Language>("languages"))
                {
                    var otherCode = otherLanguage.Code;
                    if (otherCode.StartsWith(langPart + "_", StringComparison.Ordinal))
                    {
                        return otherLanguage;
                    }
                }
            }

            // If there was nothing simular, use english
            if (Assets.Exists<Language>("languages/en.lang"))
            {
                return Language.Get("languages/en.lang");
            }

            // If english isn't loaded yet, use debug
            return Language.Get("languages/debug.lang");
        }

        public static Language Get(string path)
        {
            return Assets.Get<Language>(path);
        }

        public static IEnumerable<Language> GetAll()
        {
            return Assets.List<Language>("languages");
        }

        private string m_path;
        private string m_code;
        private Dictionary<string, string> m_translations;
		private CultureInfo m_culture;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public string Code
        {
            get
            {
                return m_code;
            }
        }

        public bool IsEnglish
        {
            get
            {
                return Code.StartsWith("en", StringComparison.Ordinal);
            }
        }

        public bool IsDebug
        {
            get
            {
                return Code == "debug";
            }
        }

        public Language Fallback
        {
            get
            {
                string fallbackCode;
                if (m_translations.TryGetValue("meta.fallback_language", out fallbackCode) && fallbackCode != m_code)
                {
                    var fallbackPath = "languages/" + fallbackCode + ".lang";
                    if (Assets.Exists<Language>(fallbackPath))
                    {
                        return Language.Get(fallbackPath);
                    }
                }
                return null;
            }
        }

        public string Name
        {
            get
            {
                return Translate("meta.native_language_name");
            }
        }

        public string EnglishName
        {
            get
            {
                return Translate("meta.english_language_name");
            }
        }

        public string CustomFont
        {
            get
            {
                string customFont;
                if (m_translations.TryGetValue("meta.custom_font", out customFont))
                {
                    return customFont;
                }
                return null;
            }
        }

        public CultureInfo Culture
        {
            get
            {
				return m_culture;
            }
        }

		private class LangFile
		{
			public string Comment;
			public readonly Dictionary<string, string> Translations;

			public LangFile()
			{
				Comment = null;
				Translations = new Dictionary<string, string>();
			}
		}

        public static object LoadData(Stream stream, string path)
        {
            var reader = new StreamReader(stream, Encoding.UTF8);
			var file = new LangFile();

			string line;
            while ((line = reader.ReadLine()) != null)
            {
                int commentIndex;
                if (line.StartsWith("//", StringComparison.Ordinal))
                {
                    commentIndex = 0;
                }
                else
                {
                    commentIndex = line.IndexOf(" //", StringComparison.Ordinal);
                    commentIndex = (commentIndex >= 0) ? (commentIndex + 1) : -1;
                }
                if (commentIndex >= 0)
                {
					if (file.Translations.Count == 0 && file.Comment == null)
                    {
						file.Comment = line.Substring(commentIndex + 2).Trim();
                    }
                    line = line.Substring(0, commentIndex);
                }

                int equalsIndex = line.IndexOf('=');
                if (equalsIndex >= 0)
                {
                    string key = line.Substring(0, equalsIndex).Trim();
					string value = line.Substring(equalsIndex + 1).Replace("\\n", "\n").Trim();
					file.Translations[key] = value;
                }
            }

			return file;
        }

        public Language(string path)
        {
            m_path = path;
            m_code = AssetPath.GetFileNameWithoutExtension(path);
            m_translations = new Dictionary<string, string>();
            try
            {
                int underscoreIndex = m_code.IndexOf('_');
                if (underscoreIndex >= 0)
                {
                    try
                    {
						m_culture = CultureInfo.GetCultureInfo(m_code.Replace('_', '-'));
                    }
                    catch (CultureNotFoundException)
                    {
                        m_culture = CultureInfo.GetCultureInfo(m_code.Substring(0, underscoreIndex));
                    }
                }
                else
                {
                    m_culture = CultureInfo.GetCultureInfo(m_code);
                }
            }
            catch (CultureNotFoundException)
            {
                m_culture = CultureInfo.CurrentCulture;
            }
        }

        public void Dispose()
        {
        }

        public void Reset()
        {
            m_translations.Clear();
        }

        public void AddLayer(object data)
        {
			var file = (LangFile)data;
			foreach (var pair in file.Translations)
            {
				m_translations[pair.Key] = pair.Value;
            }
        }

        public bool CanTranslate(string symbol)
        {
            if (m_translations.ContainsKey(symbol))
            {
                return true;
            }
            if (Fallback != null)
            {
                return Fallback.CanTranslate(symbol);
            }
            return false;
        }

        public string GetRandomVariant(string symbol)
        {
            int variants = 0;
            while(CanTranslate(symbol + '.' + variants))
            {
                ++variants;
            }
            if(variants > 0)
            {
                var num = GlobalRandom.Int(0, variants-1);
                return symbol + '.' + num;
            }
            return symbol;
        }

        public string Translate(string symbol)
        {
            string value;
            if (m_translations.TryGetValue(symbol, out value))
            {
                return value;
            }
            else if (Fallback != null)
            {
                return Fallback.Translate(symbol);
            }
            else
            {
                return symbol;
            }
        }

        public string TranslateCount(string baseSymbol, long number)
        {
            var fullSymbol = baseSymbol + '.' + number;
            if (CanTranslate(fullSymbol))
            {
				return Translate(fullSymbol, number);
            }
            else
            {
                return Translate(baseSymbol, number);
            }
        }

        public string Translate(string symbol, object arg1)
        {
			return string.Format(Culture, Translate(symbol), arg1);
        }

        public string Translate(string symbol, object arg1, object arg2)
        {
            return string.Format(Culture, Translate(symbol), arg1, arg2);
        }

        public string Translate(string symbol, object arg1, object arg2, object arg3)
        {
            return string.Format(Culture, Translate(symbol), arg1, arg2, arg3);
        }

        public string Translate(string symbol, params object[] args)
        {
            return string.Format(Culture, Translate(symbol), args);
        }
    }
}

