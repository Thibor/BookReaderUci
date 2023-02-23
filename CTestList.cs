using System;
using System.Collections.Generic;
using System.IO;

namespace NSProgram
{
	class CElementT
	{
		public string line = string.Empty;

		public string Fen{
			get
			{
				string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				List<string> sl = new List<string>(tokens);
				List<string> subList = sl.GetRange(0, 6);
				string fen = String.Join(" ", subList.ToArray()).Trim();
				Program.chess.SetFen(fen);
				return Program.chess.GetFen();
			}
		}
	}

	internal class CTestList:List<CElementT>
	{
		int index = -1;
		public int number = 1;
		public int resultOk = 0;
		public int resultFail = 0;
		public string line = String.Empty;

		public CElementT CurElement
		{
			get
			{
				return (Count <= 0) || (index >= Count) ? null : this[index];
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

		public void DeleteFen(string fen)
		{
			for(int n=Count-1;n>=0;n--)
				if(this[n].Fen == fen)
				{
					this.RemoveAt(n);
					if (index > n)
						index--;
				}
		}

		public void LoadFromFile()
		{
			string fn = Constants.testFen;
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
						CElementT t = new CElementT();
						t.line = line;
						Add(t);
					}
			}
			Console.WriteLine($"info string test on fens {Count:N0}");
		}

		public bool Next()
		{
			index++;
			if (CurElement == null)
				return false;
			if (CurElement.line.Substring(0, 3) == "t: ")
			{
				Console.WriteLine(CurElement.line.Substring(3));
				return Next();
			}
			if (CurElement.line.Substring(0,3)=="// ")
				return Next();
			if (CurElement.line == "finish")
				index = Count;
			number++;
			return !String.IsNullOrEmpty(CurElement.line);
		}

		public bool GetResult(string move)
		{
			Program.chess.SetFen(CurElement.Fen);
			string san = Program.chess.UmoToSan(move);
			if (CurElement.line.Contains("bm "))
				if (CurElement.line.Contains($" {move}")||CurElement.line.Contains($" {san}"))
					return true;
				else
					return false;
			if (CurElement.line.Contains("am "))
				if (CurElement.line.Contains($" {move}") ||CurElement.line.Contains($" {san}"))
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
