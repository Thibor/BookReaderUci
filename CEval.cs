using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSProgram
{
	internal class CEval
	{
		int index = -1;
		public int number = 1;
		public int resultOk = 0;
		public int resultFail = 0;
		public string line = String.Empty;
		public List<string> fenList = new List<string>();

		string Line
		{
			get
			{
				return (fenList.Count <= 0) || (index >= fenList.Count) ? String.Empty : fenList[index];
			}
		}

		public string Fen
		{
			get
			{
				return LineToFen(Line);
			}
		}

		public void Reset()
		{
			index = -1;
			number = 1;
			resultOk = 0;
			resultFail = 0;
			Next();
		}

		string LineToFen(string line)
		{
			string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			List<string> sl = new List<string>(tokens);
			List<string> subList = sl.GetRange(0, 6);
			string fen = String.Join(" ", subList.ToArray()).Trim();
			Program.chess.SetFen(fen);
			return Program.chess.GetFen();
		}

		public List<string> GetFens()
		{
			List<string> sl = new List<string>();
			foreach (string l in fenList)
			{
				if( (l.Substring(0, 3) == "t: ")|| (l.Substring(0, 3) == "// "))
						continue;
				sl.Add(LineToFen(l));
			}
			return sl;
		}

		public void LoadFen()
		{
			string fn = Constants.testFen;
			fenList.Clear();
			if (!File.Exists(fn))
				return;
			using (FileStream fs = File.Open(fn, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (StreamReader reader = new StreamReader(fs))
			{
				string line = String.Empty;
				while ((line = reader.ReadLine()) != null)
					if (!String.IsNullOrEmpty(line))
						fenList.Add(line);
			}
		}

		public bool Next()
		{
			index++;
			if (Line == String.Empty)
				return false;
			if (Line.Substring(0, 3) == "t: ")
			{
				Console.WriteLine(Line.Substring(3));
				return Next();
			}
			if (Line.Substring(0,3)=="// ")
				return Next();
			if (Line == "finish")
				index = fenList.Count;
			number++;
			return !String.IsNullOrEmpty(Line);
		}

		public bool GetResult(string move)
		{
			Program.chess.SetFen(Fen);
			string san = Program.chess.UmoToSan(move);
			if (Line.Contains("bm "))
				if (Line.Contains($" {move}")||Line.Contains($" {san}"))
					return true;
				else
					return false;
			if (Line.Contains("am "))
				if (Line.Contains($" {move}") ||Line.Contains($" {san}"))
					return false;
				else
					return true;
			return true;
		}

		public void SetResult(bool r)
		{
			if (r)
				resultOk++;
			else
				resultFail++;
		}

	}
}
