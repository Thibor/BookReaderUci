using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RapIni;

namespace NSProgram
{
	internal class CMod
	{
		public readonly CRapIni ini;
		public double bstScore = 0;
		bool modified = false;
		bool plus = false;
		bool second = false;
		bool modAuto = false;
		bool reset = false;
		int modIndex = 0;
		public int index = 0;
		public int shift = 0;
		int bonShift = 0;
		readonly List<string> modList = new List<string>();

		public CMod()
		{
			ini = new CRapIni(@"Students\mod.ini");
			LoadFromIni();
			modList = ini.ReadKeyList("cur");
			foreach (string key in modList)
				if (!ini.KeyExists($"bst>{key}"))
				{
					string s = ini.Read($"cur>{key}");
					ini.Write($"bst>{key}", s);
				}
			modList = ini.ReadKeyList("bst");
			BstToCur();
			ini.Save();
		}

		public int Del
		{
			get
			{
				int d = 1 << (shift + bonShift);
				return plus ? d : -d;
			}
		}

		public int ValBst
		{
			get
			{
				if (BstList.Count == 0)
					return 0;
				return BstList[index % BstList.Count];
			}
		}

		public int ValCur
		{
			get
			{
				List<int> list = CurList;
				if (list.Count == 0)
					return 0;
				return list[index % list.Count];
			}
			set
			{
				List<int> list = CurList;
				if (list.Count > 0)
					list[index % list.Count] = value;
				ini.Write($"cur>{ModName}",list);
			}
		}

		public string ModName
		{
			get
			{
				if (modList.Count == 0)
					return string.Empty;
				return modList[modIndex % modList.Count];
			}
		}


		public List<int> BstList
		{
			get
			{
				return ini.ReadListInt("bst>" + ModName);
			}
		}

		public List<int> CurList
		{
			get
			{
				return ini.ReadListInt("cur>" + ModName);
			}
		}

		public void BstToCur()
		{
			foreach (string key in modList)
			{
				string s = ini.Read($"bst>{key}");
				ini.Write($"cur>{key}", s);
			}
		}

		public void SetMode(string mode)
		{
			string m = ini.Read("mode");
			if (m != mode)
			{
				ini.Write("reset", true);
				ini.Write("mode", mode);
				ini.Save();
			}
		}

		void LoadFromIni()
		{
			ini.Load();
			modified = ini.ReadBool("modified");
			plus = ini.ReadBool("plus");
			reset = ini.ReadBool("reset");
			second = ini.ReadBool("second");
			modAuto = ini.ReadBool("auto", true);
			index = ini.ReadInt("index");
			shift = ini.ReadInt("shift");
			modIndex = ini.ReadInt("modIndex");
			bstScore = ini.ReadDouble("score");
		}

		void SaveToIni()
		{
			ini.Write("modified", modified);
			ini.Write("plus", plus);
			ini.Write("reset", reset);
			ini.Write("second", second);
			ini.Write("auto", modAuto);
			ini.Write("index", index);
			ini.Write("shift", shift);
			ini.Write("modIndex", modIndex);
			ini.Write("score", bstScore);
			ini.Save();
		}

		void ModCur()
		{
			ValCur += Del;
			ini.Write("cur>" + ModName, CurList);
		}

		public bool SetScore(double s)
		{
			if (bstScore == 0)
				bstScore = s;
			else if (bstScore > s)
			{
				bstScore = s;
				modified = true;
				second = false;
				bonShift++;
				ini.Load();
				List<int> list = CurList;
				ini.Write("bst>" + ModName, list);
				string mod = String.Join(",", list.ToArray());
				Program.log.Add($"({s:N2}) {mod}");
			}
			else
			{
				bonShift = 0;
				bool stop = false;
				do
				{
					if (++index >= CurList.Count)
					{
						index = 0;
						if (modAuto && (modIndex < modList.Count - 1))
							modIndex++;
						else
						{
							if (stop)
								return false;
							stop = true;
							plus = !plus;
							if (second)
								shift++;
							second = !second;
							if (modified)
							{
								modified = false;
								second = false;
								shift = 0;
							}
							if (modAuto)
								modIndex = 0;
						}
					}
				} while (Math.Abs(Del) >> 1 > Math.Abs(ValBst));
			}
			SaveToIni();
			return true;
		}

		public void Start()
		{
			LoadFromIni();
			if (reset)
			{
				reset = false;
				plus = false;
				second = false;
				bstScore = 0;
				index = 0;
				shift = 0;
			}
			else
			{
				BstToCur();
				ModCur();
			}
		}

	}
}
