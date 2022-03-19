using System;

namespace NSUci
{
	class CUci
	{
		public string command;
		public string[] tokens;

		public int GetIndex(string key, int def = -1)
		{
			for (int n = 0; n < tokens.Length; n++)
				if (tokens[n] == key)
					return n;
			return def;
		}

		public int GetInt(int index,int def = 0)
		{
			if ((index < 0)||(index >= tokens.Length))
				return def;
			if (Int32.TryParse(tokens[index], out int result))
				return result;
			return def;
		}

		public int GetInt(string key, int def = 0)
		{
			int index = GetIndex(key);
			return GetInt(index + 1,def);
		}

		public bool GetValue(string name, out string value)
		{
			int i = GetIndex(name, tokens.Length) + 1;
			if (i < tokens.Length)
			{
				value = tokens[i];
				return true;
			}
			value = "";
			return false;
		}

		public string GetValue(string start, string end)
		{
			int istart = GetIndex(start, tokens.Length);
			int iend = GetIndex(end, tokens.Length);
			return GetValue(istart+1,iend-1);
		}

		public string GetValue(int start, int end)
		{
			if (end < start)
				end = tokens.Length - 1;
			string value = String.Empty;
			for (int n = start; n <= end; n++)
			{
				if (n >= tokens.Length)
					break;
				value += $" {tokens[n]}";
			}
			return value.Trim();
		}

		public string Last()
		{
			if (tokens.Length > 0)
				return tokens[tokens.Length - 1];
			return String.Empty;
		}

		public void SetMsg(string msg)
		{
			if (String.IsNullOrEmpty(msg))
				msg = String.Empty;
			tokens = msg.Split(new[] { ' '}, StringSplitOptions.RemoveEmptyEntries);
			command = tokens.Length > 0 ? tokens[0] : String.Empty;
		}
	}
}
