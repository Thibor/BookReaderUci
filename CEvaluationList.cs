using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSProgram
{
	class CElementE
	{
		public string fen = string.Empty;
		public int eval = 0;

		public string Line
		{
			get
			{
				return $"{fen} {eval}";
			}
			set
			{
				string[] tokens = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (tokens.Length < 6)
					return;
				List<string> sl = new List<string>(tokens);
				List<string> subList = sl.GetRange(0, 6);
				fen = String.Join(" ", subList.ToArray()).Trim();
				eval = 0;
				if (tokens.Length > 6)
					if (int.TryParse(tokens[6], out int v))
						eval = v;
			}
		}
	}

	internal class CEvaluationList : List<CElementE>
	{
		int index = -1;

		public int CurIndex
		{
			get
			{
				return Count - 1 - index;
			}
		}

		public CElementE CurElement
		{
			get
			{
				if ((index < 0) || (index >= Count))
					return null;
				return this[CurIndex];
			}
		}

		public void Reset()
		{
			index = -1;
			Next();
		}

		public void Fill()
		{
			for (int n = 0; n < Count; n++)
				this[n].eval = 0;
			SaveToFile();
		}

		public void DeleteFen(string fen)
		{
			for (int n = Count - 1; n >= 0; n--)
				if (this[n].fen == fen)
				{
					RemoveAt(n);
					if (index >= Count-1-n)
						index--;
				}
		}

		public void LoadFromFile()
		{
			string fn = Constants.evalFen;
			Clear();
			if (!File.Exists(fn))
				return;
			using (FileStream fs = File.Open(fn, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (StreamReader reader = new StreamReader(fs))
			{
				string line = String.Empty;
				while ((line = reader.ReadLine()) != null)
					if (!String.IsNullOrEmpty(line))
					{
						CElementE e = new CElementE();
						e.Line = line;
						Add(e);
					}
			}
		}

		public void SaveToFile()
		{
			string last = String.Empty;
			using (FileStream fs = File.Open(Constants.evalFen, FileMode.Create, FileAccess.Write, FileShare.None))
			using (StreamWriter sw = new StreamWriter(fs))
			{
				foreach (CElementE e in this)
				{
					string l = e.Line;
					string[] tokens = l.Split(' ');
					if (last == tokens[0])
						continue;
					last = tokens[0];
					sw.WriteLine(l);
				}
			}
		}

		public bool Next()
		{
			do
			{
				if (index >= Count - 1)
					return false;
				index++;
				if (CurElement.fen == String.Empty)
				{
					RemoveAt(CurIndex);
					index--;
					SaveToFile();
					continue;
				}
			} while (CurElement.eval != 0);
			return true;
		}

		public int CountFail()
		{
			int result = 0;
			foreach (CElementE e in this)
				if (e.eval == 0)
					result++;
			return result;
		}

	}
}
