using System;
using System.IO;
using System.Collections.Generic;
using RapLog;

namespace NSProgram
{
	struct BadFen
	{
		public int bstScore;
		public int badScore;
		public int worstDelta;
		public string fen;
		public string bstMove;
		public string badMove;
	}

	internal class CAccuracy : MSList
	{
		bool loaded = false;
		public int index = 0;
		long centyLoss = 0;
		long centyCount = 0;
		public int inaccuracies = 0;
		public int mistakes = 0;
		public int blunders = 0;
		public int lastLoss = 0;
		public BadFen badFen;
		public CRapLog log = new CRapLog("accuracy.log");
		public CRapLog his = new CRapLog("accuracy.his");
		readonly static Random rnd = new Random();

		public void LoadFromFile()
		{
			if (loaded)
				return;
			loaded = true;
			LoadFromEpd();
			Check();
			GetDepth(out int minD,out int maxD);
			GetMoves(out int minM,out int maxM);
			Console.WriteLine($"info string accuracy on fens {Count} fail {CountFail()} depth ({minD} - {maxD}) moves ({minM} - {maxM})");
		}

		public void Info()
		{
			GetDepth(out int minD,out _);
			GetMoves(out int minM,out _);
			int fail = CountFail();
			Console.WriteLine($"fens {Count} moves {minM} depth {minD} fail {fail}");
		}

		public void Add(int val)
		{
			if (!File.Exists("accuracy.fen"))
			{
				Console.WriteLine("File \"accuracy.fen\" is missing.");
				return;
			}
			string fen;
			string[] fa = File.ReadAllLines("accuracy.fen");
			List<string> fl = new List<string>(fa);
			while ((val>0) && (fl.Count > 0))
			{
				int i = rnd.Next(fl.Count);
				fen = fl[i].Trim();
				if (string.IsNullOrEmpty(fen))
					continue;
				if (AddLine(fen))
					val--;
			}
			for(int n = fl.Count - 1; n >= 0; n--)
			{
                fen = fl[n].Trim();
				if(string.IsNullOrEmpty(fen))
                    fl.RemoveAt(n);
            }
            File.WriteAllLines("accuracy.fen", fl);
            SaveToEpd();
            Console.WriteLine($"{fl.Count} fens left.");
		}

		public void Reset()
		{
			index = 0;
			inaccuracies = 0;
			mistakes = 0;
			blunders = 0;
			centyLoss = 0;
			centyCount = 0;
			lastLoss = 0;
			badFen = default;
		}

		public void AddScore(string fen, string bstMove, string curMove,int bstScore, int curScore)
		{
			lastLoss = Math.Abs(bstScore - curScore);
			if (lastLoss >= Constants.blunder)
			{
                lastLoss = Constants.blunder;
				blunders++;
			}
			else if (lastLoss >= Constants.mistake)
				mistakes++;
			else if (lastLoss >= Constants.inaccuracy)
				inaccuracies++;
			centyCount++;
			centyLoss += lastLoss;
			if (badFen.worstDelta < lastLoss)
			{
				badFen.worstDelta = lastLoss;
				badFen.fen = fen;
				badFen.bstMove = bstMove;
				badFen.badMove = curMove;
				badFen.bstScore = bstScore;
				badFen.badScore = curScore;
			}
		}

		public double WinningChances(int eval)
		{
			double result = 2.0 / (1 + Math.Exp(-0.00368208 * eval)) - 1;
			if (result > 1)
				return 1;
			if (result < -1)
				return -1;
			return result;
		}

		public double GetAccuracy(long cc, long cl)
		{
			if (cc == 0)
				return 0;
			double max = cc * Constants.blunder;
			return ((max - cl) * 100.0) / max;
		}

		public double GetAccuracy()
		{
			return GetAccuracy(centyCount, centyLoss);
		}

		public int GetElo(double accuracy)
		{
            accuracy /= 100.0;
            accuracy = (accuracy - 0.6) / 0.4;
            if (accuracy < 0)
                accuracy = 0;
            return Convert.ToInt32(accuracy * Constants.maxElo);
		}

		public int GetElo(double accuracy, out int del)
		{
			double minAcc = GetAccuracy(centyCount + 1, centyLoss + Constants.blunder);
			double maxAcc = GetAccuracy(centyCount + 1, centyLoss);
			del = GetElo(maxAcc) - GetElo(minAcc);
			return GetElo(accuracy);
		}

		public bool NextLine(out MSLine line)
		{
			line = null;
			if (index >= Count)
				return false;
			line = this[index++];
			return true;
		}

	}
}
