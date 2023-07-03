using Dan200.Core.Assets;
using System.Text;
using System.Collections.Generic;
using System;
using System.Globalization;

namespace Dan200.Core.Main
{
    internal class ProgramArguments
    {
		public static ProgramArguments Empty = new ProgramArguments(new string[0]);

        private string m_representation;
		private Dictionary<string, string> m_arguments;

		public bool IsEmpty
		{
			get
			{
				return m_arguments.Count == 0;
			}
		}

        public ProgramArguments(string[] args)
        {
            var representation = new StringBuilder();
			var arguments = new Dictionary<string, string>();
            string lastOption = null;
            foreach (string arg in args)
            {
				if (arg.StartsWith("-", StringComparison.InvariantCulture))
                {
                    if (lastOption != null)
                    {
                        representation.Append("-" + lastOption + " ");
						arguments[lastOption] = "true";
                    }
                    lastOption = arg.Substring(1);
                }
                else if (lastOption != null)
                {
                    representation.Append("-" + lastOption + " " + arg + " ");
					arguments[lastOption] = arg;
                    lastOption = null;
                }
            }
            if (lastOption != null)
            {
                representation.Append("-" + lastOption + " ");
				arguments[lastOption] = "true";
            }
            m_representation = representation.ToString().TrimEnd();
			m_arguments = arguments;
        }

        public override string ToString()
        {
            return m_representation;
        }

		public bool Contains(string key)
		{
			return m_arguments.ContainsKey(key);
		}

		public string GetString(string key, string _default=null)
		{
			string arg;
			if (m_arguments.TryGetValue(key, out arg))
			{
				return arg;
			}
			return _default;
		}

		public float GetFloat(string key, float _default = 0.0f)
		{
			string arg;
			float result;
			if (m_arguments.TryGetValue(key, out arg) &&
			    float.TryParse(arg, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
			{
				return result;
			}
			return _default;
		}

		public int GetInteger(string key, int _default = 0)
		{
			string arg;
			int result;
			if (m_arguments.TryGetValue(key, out arg) &&
			    int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
			{
				return result;
			}
			return _default;
		}

		public bool GetBool(string key, bool _default = false)
		{
			string arg;
			if (m_arguments.TryGetValue(key, out arg))
			{
				return arg == "true" || arg == "1";
			}
			return _default;
		}
    }
}
