using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Shallows
{
	public static class MiniJson
	{
		public static object Parse(string json)
		{
			// Rebuilt from native code (token 0x06000713, RVA 0x2809034)
			if (string.IsNullOrEmpty(json))
			{
				return null;
			}
			int i = 0;
			return Value(json, ref i);
		}

		public static string Write(object value)
		{
			// Rebuilt from native code (token 0x06000714, RVA 0x2809228)
			StringBuilder sb = new StringBuilder();
			Write(sb, value);
			return sb.ToString();
		}

		private static object Value(string s, ref int i)
		{
			// Rebuilt from native code (token 0x06000715, RVA 0x28090E8)
			Skip(s, ref i);
			switch (s[i])
			{
			case '{':
				return Object(s, ref i);
			case '[':
				return Array(s, ref i);
			case '"':
				return String(s, ref i);
			case 't':
				i += 4;
				return true;
			case 'f':
				i += 5;
				return false;
			case 'n':
				i += 4;
				return null;
			default:
				return Number(s, ref i);
			}
		}

		private static Dictionary<string, object> Object(string s, ref int i)
		{
			// Rebuilt from native code (token 0x06000716, RVA 0x2809B48)
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			i++;
			Skip(s, ref i);
			if (s[i] != '}')
			{
				do
				{
					string key = String(s, ref i);
					Skip(s, ref i);
					i++;
					dictionary[key] = Value(s, ref i);
					Skip(s, ref i);
					if (s[i] == ',')
					{
						i++;
					}
					Skip(s, ref i);
				}
				while (s[i] != '}');
			}
			i++;
			return dictionary;
		}

		private static List<object> Array(string s, ref int i)
		{
			// Rebuilt from native code (token 0x06000717, RVA 0x2809CC4)
			List<object> list = new List<object>();
			i++;
			Skip(s, ref i);
			if (s[i] != ']')
			{
				do
				{
					list.Add(Value(s, ref i));
					Skip(s, ref i);
					if (s[i] == ',')
					{
						i++;
					}
					Skip(s, ref i);
				}
				while (s[i] != ']');
			}
			i++;
			return list;
		}

		private static string String(string s, ref int i)
		{
			// Rebuilt from native code (token 0x06000718, RVA 0x2809E5C)
			StringBuilder sb = new StringBuilder();
			i++;
			char c;
			while ((c = s[i++]) != '"')
			{
				if (c != '\\')
				{
					sb.Append(c);
					continue;
				}
				c = s[i++];
				switch (c)
				{
				case 'b':
					sb.Append('\b');
					break;
				case 'f':
					sb.Append('\f');
					break;
				case 'n':
					sb.Append('\n');
					break;
				case 'r':
					sb.Append('\r');
					break;
				case 't':
					sb.Append('\t');
					break;
				case 'u':
					sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
					i += 4;
					break;
				default:
					sb.Append(c);
					break;
				}
			}
			return sb.ToString();
		}

		private static double Number(string s, ref int i)
		{
			// Rebuilt from native code (token 0x06000719, RVA 0x280A098)
			int start = i;
			while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0)
			{
				i++;
			}
			return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
		}

		private static void Skip(string s, ref int i)
		{
			// Rebuilt from native code (token 0x0600071A, RVA 0x2809ACC)
			while (char.IsWhiteSpace(s[i]))
			{
				i++;
			}
		}

		private static void Write(StringBuilder sb, object value)
		{
			// Rebuilt from native code (token 0x0600071B, RVA 0x280929C)
			if (value == null)
			{
				sb.Append("null");
				return;
			}
			if (value is string s)
			{
				Quote(sb, s);
				return;
			}
			if (value is bool b)
			{
				sb.Append(b ? "true" : "false");
				return;
			}
			if (value is IDictionary<string, object> dictionary)
			{
				sb.Append('{');
				bool first = true;
				foreach (KeyValuePair<string, object> pair in dictionary)
				{
					if (!first)
					{
						sb.Append(',');
					}
					Quote(sb, pair.Key);
					sb.Append(':');
					Write(sb, pair.Value);
					first = false;
				}
				sb.Append('}');
				return;
			}
			if (value is IEnumerable enumerable)
			{
				sb.Append('[');
				bool first = true;
				foreach (object item in enumerable)
				{
					if (!first)
					{
						sb.Append(',');
					}
					Write(sb, item);
					first = false;
				}
				sb.Append(']');
				return;
			}
			if (value is float f)
			{
				sb.Append(f.ToString("R", CultureInfo.InvariantCulture));
				return;
			}
			if (value is double d)
			{
				sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
				return;
			}
			sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
		}

		private static void Quote(StringBuilder sb, string s)
		{
			// Rebuilt from native code (token 0x0600071C, RVA 0x280A19C)
			sb.Append('"');
			foreach (char c in s)
			{
				switch (c)
				{
				case '"':
					sb.Append("\\\"");
					break;
				case '\\':
					sb.Append("\\\\");
					break;
				case '\n':
					sb.Append("\\n");
					break;
				default:
					if (c < ' ')
					{
						sb.Append("\\u").Append(((int)c).ToString("x4"));
					}
					else
					{
						sb.Append(c);
					}
					break;
				}
			}
			sb.Append('"');
		}

		public static Dictionary<string, object> Obj(object o, string key)
		{
			// Rebuilt from native code (token 0x0600071D, RVA 0x280A344)
			if (o is Dictionary<string, object> dictionary && dictionary.TryGetValue(key, out object value))
			{
				return value as Dictionary<string, object>;
			}
			return null;
		}

		public static List<object> List(object o, string key)
		{
			// Rebuilt from native code (token 0x0600071E, RVA 0x280A42C)
			if (o is Dictionary<string, object> dictionary && dictionary.TryGetValue(key, out object value))
			{
				return value as List<object>;
			}
			return null;
		}

		public static string Str(object o, string key)
		{
			// Rebuilt from native code (token 0x0600071F, RVA 0x280A528)
			if (o is Dictionary<string, object> dictionary && dictionary.TryGetValue(key, out object value))
			{
				return value as string;
			}
			return null;
		}

		public static double Num(object o, string key, double fallback = 0.0)
		{
			// Rebuilt from native code (token 0x06000720, RVA 0x280A5FC)
			if (o is Dictionary<string, object> dictionary && dictionary.TryGetValue(key, out object value) && value is double number)
			{
				return number;
			}
			return fallback;
		}
	}
}
