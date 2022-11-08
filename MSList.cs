using System;
using System.Collections.Generic;
using System.IO;
using NSChess;

namespace NSProgram
{
	class MSRec
	{
		public string move;
		public int score;

		public MSRec(MSRec rec)
		{
			move = rec.move;
			score = rec.score;
		}

		public MSRec(string m,int s)
		{
			move = m;
			score = s;
		}
	}

	internal class MSLine : List<MSRec>
	{
		public string fen = String.Empty;
		public int depth = 0;

		public void Assign(MSLine line)
		{
			Clear();
			fen = line.fen;
			depth = line.depth;
			foreach (MSRec rec in line)
				Add(new MSRec(rec));
		}

		public void DeleteMove(string move)
		{
			for (int n = Count - 1; n >= 0; n--)
				if (this[n].move == move)
					RemoveAt(n);
		}

		public void AddRec(MSRec rec)
		{
			DeleteMove(rec.move);
			Add(rec);
			SortScore();
		}

		public void SortScore()
		{
			Sort(delegate (MSRec r1, MSRec r2)
			{
				return r2.score - r1.score;
			});
		}

		public MSRec First()
		{
			if(Count>0)
			return this[0];
			return null;
		}

		public MSRec Last()
		{
			if (Count > 0)
				return this[Count-1];
			return null;
		}

		public bool MoveExists(string move)
		{
			foreach (MSRec rec in this)
				if (rec.move == move)
					return true;
			return false;
		}

		void Reset()
		{
			fen = String.Empty;
			depth = 0;
			Clear();
		}

		string GetMoves()
		{
			string moves = String.Empty;
			foreach(MSRec rec in this)
			{
				moves = $"{moves} bm {rec.move} ce {rec.score}";
			}
			return moves.Trim();
		}

		public int GetScore(string move)
		{
			foreach (MSRec rec in this)
				if (rec.move == move)
					return rec.score;
			return Last().score;
		}

		public string SaveToStr()
		{
			string moves = GetMoves();
			return $"{fen} acd {depth} {moves}".Trim();
		}

		public bool LoadFromStr(string line)
		{
			Reset();
			string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (tokens.Length < 6)
				return false;
			List<string> sl = new List<string>(tokens);
			string last = String.Empty;
			string move = String.Empty;
			for(int n = 0; n < sl.Count; n++)
			{
				string s = sl[n];
				if ((s == "bm") || (s == "ce") || (s == "acd"))
				{
					last = s;
					continue;
				}
				if (n < 6)
				{
					fen += ' ' + s;
					continue;
				}
				switch (last)
				{
					case "acd":
						depth = Convert.ToInt32(s);
						break;
					case "bm":
						move = s;
						break;
					case "ce":
						if (!String.IsNullOrEmpty(move))
						{
							MSRec rec = new MSRec(move, Convert.ToInt32(s));
							AddRec(rec);
						}
						break;
				}
			}
			fen = fen.Trim();
			return true;
		}

	}

	internal class MSList:List<MSLine>
	{
		public string path = "accuracy fen.txt";

		public void DeleteFen(string fen)
		{
			for (int n = Count - 1; n >= 0; n--)
				if (this[n].fen == fen)
					RemoveAt(n);
		}

		public int GetDepth()
		{
			if (Count == 0)
				return 0;
			int result = int.MaxValue;
			foreach (MSLine msl in this)
				if (result > msl.depth)
					result = msl.depth;
			return result;
		}

		public void SaveToFile()
		{
			string last = String.Empty;
			SortFen();
			using (FileStream fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None))
			using (StreamWriter sw = new StreamWriter(fs))
			{
				foreach (MSLine msl in this)
				{
					string l = msl.SaveToStr();
					string[] tokens = l.Split(' ');
					if (last == tokens[0])
						continue;
					last = tokens[0];
					sw.WriteLine(l);
				}
			}
		}

		public bool LoadFromFile()
		{
			Clear();
			if (!File.Exists(path))
				return false;
			using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (StreamReader reader = new StreamReader(fs))
			{
				string line = String.Empty;
				while ((line = reader.ReadLine()) != null)
				{
					MSLine msl = new MSLine();
					if (msl.LoadFromStr(line))
						Add(msl);
				}
			}
			return Count > 0;
		}
		void SortFen()
		{
			Sort(delegate (MSLine l1, MSLine l2)
			{
				return String.Compare(l1.fen, l2.fen, StringComparison.Ordinal);
			});
		}

		public void SortAccuracy()
		{
			Sort(delegate (MSLine l1, MSLine l2)
			{
				int d1 = l1.depth;
				int d2 = l2.depth;
				if (d1 > Constants.minDepth)
					d1 = Constants.minDepth;
				if (d2 > Constants.minDepth)
					d2 = Constants.minDepth;
				return d2 - d1;
			});
		}

		public void SortDepth()
		{
			Sort(delegate (MSLine l1, MSLine l2)
			{
				int d1 = l1.depth;
				int d2 = l2.depth;
				return d1 - d2;
			});
		}

		public void SortRandom()
		{
			for (int n = 0; n < Count; n++)
			{
				int r = CChess.rnd.Next(Count);
				(this[n],this[r]) = (this[r],this[n]);
			}
		}

		public int GetFenIndex(string fen)
		{
			for (int n = 0; n < Count; n++)
				if (this[n].fen == fen)
					return n;
			return -1;
		}

		public void AddLine(MSLine line) {
			int index = GetFenIndex(line.fen);
			if (index >= 0)
				this[index].Assign(line);
			else
				Add(line);
		}

		public MSLine GetShallowLine()
		{
			foreach (MSLine line in this)
				if (line.depth < Constants.minDepth)
					return line;
			return null;
		}

		public MSLine GetRandomLine()
		{
			if (Count == 0)
				return null;
			int index = CChess.rnd.Next(Count);
			return this[index];
		}

	}

}
