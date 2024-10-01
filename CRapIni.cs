using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace RapIni
{
	public class CRapIni : List<string>
	{
		bool loaded = false;
		readonly string name = String.Empty;
		string path = String.Empty;

		bool Loaded
		{
			get
			{
				if (loaded)
					return true;
				Load();
				return loaded;
			}
		}

		public CRapIni()
		{
			name = Assembly.GetExecutingAssembly().GetName().Name;
			path = new FileInfo(name + ".ini").FullName.ToString();
			Load();
		}

		public CRapIni(string name)
		{
			this.name = name;
			path = new FileInfo(name).FullName.ToString();
			Load();
		}

		string ListToString(List<string> list,string separator = ",")
		{
			return String.Join(separator, list.ToArray());
		}

		public void Write(string key)
		{
			DeleteKey(key);
			Add(key);
		}

		public void Write(string key, string value)
		{
			DeleteKey(key);
			if (!String.IsNullOrEmpty(value))
				Add($"{key}>{value}");
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
			Write(key, Convert.ToString(value, CultureInfo.InvariantCulture.NumberFormat));
		}

		public void Write(string key,DateTime dt)
		{
              Write(key,dt.ToString(CultureInfo.InvariantCulture));
        }

		public void Write(string key, List<string> value,string separator=",")
		{
			Write(key, ListToString(value,separator));
		}

		public void Write(string key, int[] arr,string separator = ",")
		{
			Write(key, String.Join(separator, arr));
		}

		public void Write(string key, List<int> list,string separator = ",")
		{
			Write(key, list.ToArray(),separator);
		}

		public void Write(string key, string[] arrStr,string separator = ",")
		{
			Write(key, String.Join(separator, arrStr));
		}

		public DateTime ReadDateTime(string key,DateTime def)
		{
			string dt = Read(key);
			if (String.IsNullOrEmpty(dt))
				return def;
            return DateTime.Parse(dt, CultureInfo.InvariantCulture);
        }

		public List<int> ReadListInt(string key)
		{
			List<int> list = new List<int>();
			string[] arrStr = ReadArrStr(key);
			foreach (string e in arrStr)
				list.Add(Convert.ToInt32(e));
			return list;
		}

		public List<string> ReadListStr(string key,string separator = ",")
		{
			string[] arrStr = ReadArrStr(key,separator);
			return arrStr.ToList();
		}

		public string[] ReadArrStr(string key,string separator = ",")
		{
			string s = Read(key);
			return s.Split(new[] {separator }, StringSplitOptions.RemoveEmptyEntries);
		}

		public string Read(string key, string def = "", bool restore = false)
		{
			if (restore)
				return def;
			string[] ak = key.Split('>');
			foreach (string e in this)
			{
				if (e.IndexOf($"{key}>") == 0)
				{
					string[] ae = e.Split('>');
					if (ae.Length > ak.Length)
						return ae[ak.Length];
					else
						return string.Empty;
				}
			}
			return def;
		}

		public decimal ReadDecimal(string key, decimal def = 0, bool restore = false)
		{
			if (restore)
				return def;
			string s = Read(key, Convert.ToString(def));
			decimal.TryParse(s, out decimal result);
			return result;
		}

		public double ReadDouble(string key, double def = 0, bool restore = false)
		{
			if (restore)
				return def;
			string s = Read(key, Convert.ToString(def, CultureInfo.InvariantCulture.NumberFormat));
			double.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out double result);
			return result;
		}

		public int ReadInt(string key, int def = 0, bool restore = false)
		{
			if (restore)
				return def;
			string s = Read(key, Convert.ToString(def));
			int.TryParse(s, out int result);
			return result;
		}

		public bool ReadBool(string key, bool def = false, bool restore = false)
		{
			if (restore)
				return def;
			string s = Read(key, Convert.ToString(def));
			bool.TryParse(s, out bool result);
			return result;
		}

		public List<string> ReadKeyList(string key)
		{
			List<string> result = new List<string>();
			string[] ak = key.Split('>');
			foreach (string e in this)
			{
				if (e.IndexOf($"{key}>") == 0)
				{
					string[] ae = e.Split('>');
					string s = String.Empty;
					if (ae.Length > ak.Length)
						s = ae[ak.Length];
					if (!result.Contains(s))
						result.Add(s);
				}
			}
			return result;
		}

		public void DeleteKey(string key)
		{
			for (int n = Count - 1; n >= 0; n--)
				if (this[n].IndexOf($"{key}>") == 0)
					RemoveAt(n);
		}

		public bool Save()
		{
			if (!Loaded)
				return false;
			Sort();
			string pt = path + ".tmp";
			try
			{
				using (FileStream fs = File.Open(pt, FileMode.Create, FileAccess.Write, FileShare.None))
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
			try
			{
				if (File.Exists(path) && File.Exists(pt))
					File.Delete(path);
			}
			catch
			{
				return false;
			}
			try
			{
				if (!File.Exists(path) && File.Exists(pt))
					File.Move(pt, path);
			}
			catch
			{
				return false;
			}
			return true;
		}


		public bool Load()
		{
			loaded = Load(path);
			return loaded;
		}

		public bool Load(string p)
		{
			path = p;
			string pt = path + ".tmp";
			try
			{
				if (!File.Exists(path) && File.Exists(pt))
					File.Move(pt, path);
			}
			catch
			{
				return false;
			}
			Clear();
			if (!File.Exists(path))
				return true;
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

		public bool KeyExists(string key)
		{
			foreach (string e in this)
				if (e.IndexOf($"{key}>") == 0)
					return true;
			return false;
		}

	}
}
