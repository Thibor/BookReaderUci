using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace RapIni
{
	public class CRapIni:List<string>
	{
		readonly string name = String.Empty;
		readonly string path = String.Empty;

		public CRapIni()
		{
			name = Assembly.GetExecutingAssembly().GetName().Name;
			path = new FileInfo(name + ".ini").FullName.ToString();
		}

		public CRapIni(string name)
		{
			this.name = name;
			path = new FileInfo(name).FullName.ToString();
		}

		string ListToString(List<string> list)
		{
			return String.Join(",", list.ToArray());
		}

		List<string> StringToList(string s)
		{
			if (String.IsNullOrEmpty(s))
				return new List<string>();
			return new List<string>(s.Split(','));
		}

		public void Write(string key, string value)
		{
			if (Load())
			{
				DeleteKey(key);
				if (!String.IsNullOrEmpty(value))
					Add($"{key}>{value}");
				Save();
			}

		}

		public void Write(string key, bool value)
		{
			Write(key, value.ToString());
		}

		public void Write(string key, int value)
		{
			Write(key, value.ToString());
		}

		public void Write(string key, decimal value)
		{
			Write(key, value.ToString());
		}

		public void Write(string key, double value)
		{
			Write(key, value.ToString());
		}

		public void Write(string key, Color value)
		{
			Write(key, ColorTranslator.ToHtml(value));
		}

		public void Write(string key, List<string> value)
		{
			Write(key, ListToString(value));
		}

		public string Read(string key, string def = "")
		{
			if (Load())
			{
				string[] ak = key.Split(new[] { '>' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string e in this)
				{
					if (e.IndexOf($"{key}>") == 0)
					{
						string[] ae = e.Split(new[] { '>' }, StringSplitOptions.RemoveEmptyEntries);
						if (ae.Length > ak.Length)
							return ae[ak.Length];
						else
							return "";
					}
				}
			}
			return def;
		}

		public Color ReadColor(string key, Color def)
		{
			string s = Read(key, ColorTranslator.ToHtml(def));
			return ColorTranslator.FromHtml(s);
		}

		public decimal ReadDecimal(string key, decimal def = 0)
		{
			string s = Read(key, Convert.ToString(def));
			decimal.TryParse(s, out decimal result);
			return result;
		}

		public double ReadDouble(string key, double def = 0)
		{
			string s = Read(key, Convert.ToString(def));
			double.TryParse(s, out double result);
			return result;
		}

		public int ReadInt(string key, int def = 0)
		{
			string s = Read(key, Convert.ToString(def));
			int.TryParse(s, out int result);
			return result;
		}

		public bool ReadBool(string key, bool def = false)
		{
			string s = Read(key, Convert.ToString(def));
			bool.TryParse(s, out bool result);
			return result;
		}

		public List<string> ReadList(string key)
		{
			string s = Read(key);
			return StringToList(s);
		}

		public List<string> ReadKeyList(string key)
		{
			List<string> result = new List<string>();
			if (Load())
			{
				string[] ak = key.Split(new[] { '>' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string e in this)
				{
					if (e.IndexOf($"{key}>") == 0)
					{
						string[] ae = e.Split(new[] { '>' }, StringSplitOptions.RemoveEmptyEntries);
						string s = "";
						if (ae.Length > ak.Length)
							s = ae[ak.Length];
						if (!result.Contains(s))
							result.Add(s);
					}
				}
			}
			return result;
		}


		private void DeleteKeyFromFile(string key)
		{
			if (Load())
			{
				for (int n = Count - 1; n >= 0; n--)
				{

					if (this[n].IndexOf($"{key}>") == 0)
						RemoveAt(n);
				}
				Save();
			}
		}

		public void DeleteKey(string key)
		{
			DeleteKeyFromFile(key);
		}

		private bool Save()
		{
			Sort();
			try
			{
				using (FileStream fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None))
				using (StreamWriter sw = new StreamWriter(fs))
				{
					foreach (String line in this)
					{
						string l = line.Trim('\0');
						if (!String.IsNullOrEmpty(l))
							sw.WriteLine(l);
					}
				}
			}
			catch
			{
				return false;
			}
			return true;
		}


		bool Load()
		{
			Clear();
			try
			{
				using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
				using (StreamReader reader = new StreamReader(fs))
				{
					string line = String.Empty;
					while ((line = reader.ReadLine()) != null)
					{
						string l = line.Trim('\0');
						if (!String.IsNullOrEmpty(l))
							Add(l);
					}
				}
			}
			catch
			{
				return false;
			}
			return true;
		}

		public bool Exists()
		{
			return File.Exists(path);
		}

	}
}
