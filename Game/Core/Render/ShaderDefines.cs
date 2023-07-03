using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Dan200.Core.Main;

namespace Dan200.Core.Render
{
	internal class ShaderDefines : IEquatable<ShaderDefines>
	{
		public static readonly ShaderDefines Empty = new ShaderDefines();

		private readonly Dictionary<string, int?> m_defines;
		private string m_text;

		public ShaderDefines()
		{
			m_defines = new Dictionary<string, int?>();
			m_text = "";
		}

		public void Clear()
		{
			m_defines.Clear();
			m_text = "";
		}

		public void Define(string key)
		{
			m_defines[key] = null;
			m_text = null;
		}

		public void Define(string key, int value)
		{
			m_defines[key] = value;
			m_text = null;
		}

		public void Undefine(string key)
		{
			if (m_defines.Remove(key))
			{
				m_text = null;
			}
		}

		public int? Get(string key)
		{
			int? result;
			if (m_defines.TryGetValue(key, out result))
			{
				return result;
			}
			return null;
		}

		public override int GetHashCode()
		{
			int hash = 0;
			foreach (var pair in m_defines)
			{
				hash ^= pair.Key.GetHashCode();
				if (pair.Value != null)
				{
					hash ^= pair.Value.GetHashCode();
				}
			}
			return hash;
		}

		public bool Equals(ShaderDefines other)
		{
			if (other == this)
			{
				return true;
			}
			if (m_defines.Count == other.m_defines.Count)
			{
				foreach (var pair in m_defines)
				{
					int? otherValue;
					if (!other.m_defines.TryGetValue(pair.Key, out otherValue) || otherValue != pair.Value)
					{
						return false;
					}
				}
				App.Assert(GetHashCode() == other.GetHashCode());
				return true;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is ShaderDefines)
			{
				return Equals((ShaderDefines)obj);
			}
			return false;
		}

		public override string ToString()
		{
			if (m_text == null)
			{
				var textBuilder = new StringBuilder();
				foreach (var pair in m_defines)
				{
					textBuilder.Append("#define ");
					if (pair.Value.HasValue)
					{
						textBuilder.Append(pair.Key);
						textBuilder.Append(' ');
						textBuilder.AppendLine(pair.Value.Value.ToString(CultureInfo.InvariantCulture));
					}
					else
					{
						textBuilder.AppendLine(pair.Key);
					}
				}
				m_text = textBuilder.ToString();
			}
			return m_text;
		}
	}
}
