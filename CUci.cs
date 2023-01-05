using System;

namespace NSUci
{
	class CUci
	{
		public string command = string.Empty;
		public string[] tokens;

		public int GetIndex(string key, int def = -1)
		{
			for (int n = 0; n < tokens.Length; n++)
				if (tokens[n] == key)
					return n;
			return def;
		}

		public string GetStr(string key, string def = "")
		{
			int index = GetIndex(key) + 1;
			if ((index <= 0) || (index >= tokens.Length))
				return def;
			return tokens[index];
		}

		public int GetInt(string key, int def = 0)
		{
			if (Int32.TryParse(GetStr(key), out int result))
				return result;
			return def;
		}

		public bool GetValue(string name, out string value)
		{
			int i = GetIndex(name, tokens.Length) + 1;
			if (i < tokens.Length)
			{
				value = tokens[i];
				return true;
			}
			value = String.Empty;
			return false;
		}

		public string GetValue(string start)
		{
			int istart = GetIndex(start, tokens.Length);
			return GetValue(istart + 1);
		}

		public string GetValue(string start, string end)
		{
			int istart = GetIndex(start, tokens.Length);
			int iend = GetIndex(end, tokens.Length);
			return GetValue(istart + 1, iend - 1);
		}

		public string GetValue(int start, int end = 0)
		{
			string result = string.Empty;
			if (start < 0)
				start = 0;
			if ((end < start) || (end >= tokens.Length))
				end = tokens.Length - 1;
			for (int n = start; n <= end; n++)
				result += $" {tokens[n]}";
			return result.Trim();
		}

		public void SetMsg(string msg)
		{
			tokens = msg.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			command = tokens.Length == 0 ? String.Empty : tokens[0];
		}

	}
}
