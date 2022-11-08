using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

namespace RapLog
{
	public class CRapLog
	{
		readonly bool addDate;
		readonly int max;
		readonly string path;

		public CRapLog(string path = "",int max = 100,bool addDate = true)
		{
			this.path = path;
			this.max = max;
			this.addDate = addDate;
			if(String.IsNullOrEmpty(path)) {
				string name = Assembly.GetExecutingAssembly().GetName().Name + ".log";
				this.path = new FileInfo(name).FullName.ToString();
			}
		}

		public void Add(string m)
		{
			List<string> list = new List<string>();
			if (File.Exists(path))
				list = File.ReadAllLines(path).ToList();
			if (addDate)
				list.Insert(0, $"{DateTime.Now} {m}");
			else
				list.Add(m);
			int count = list.Count - max;
			if ((count > 0) && (max > 0))
				list.RemoveRange(100, count);
			File.WriteAllLines(path, list);
		}

		public List<string> List()
		{
			List<string> list = new List<string>();
			if (File.Exists(path))
				list = File.ReadAllLines(path).ToList();
			return list;
		}

	}
}
