﻿using System;
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

		public void Fens(int fens)
		{
			if (Count > fens)
				Console.WriteLine("To reduce fens use command \"accuracy delete\"");
			if (Count >= fens)
				return;
			if (!File.Exists("accuracy.fen"))
			{
				Console.WriteLine("File \"accuracy.fen\" is missing.");
				return;
			}
			string[] fa = File.ReadAllLines("accuracy.fen");
			List<string> fl = new List<string>(fa);
			while ((Count < fens) && (fl.Count > 0))
			{
				int i = rnd.Next(fl.Count);
				string fen = fl[i].Trim();
				fl.RemoveAt(i);
				if (string.IsNullOrEmpty(fen))
					continue;
				if (AddLine(fen))
					Info();
				else
					Console.WriteLine("Wrong fen");
			}
			File.WriteAllLines("accuracy.fen", fl);
			if (Count < fens)
				Console.WriteLine("No more fens.");
		}

		public void Reset()
		{
			index = 0;
			inaccuracies = 0;
			mistakes = 0;
			blunders = 0;
			centyLoss = 0;
			centyCount = 0;
			badFen = default;
		}

		public void AddScore(string fen, string bstMove, string curMove,int bstScore, int curScore)
		{
			int delta = Math.Abs(bstScore - curScore);
			if (delta >= Constants.blunder)
			{
				delta = Constants.blunder;
				blunders++;
			}
			else if (delta > Constants.mistake)
				mistakes++;
			else if (delta > Constants.inaccuracy)
				inaccuracies++;
			centyCount++;
			centyLoss += delta;
			if (badFen.worstDelta < delta)
			{
				badFen.worstDelta = delta;
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
			double proRatio = accuracy / 100;
			double x = 0.8;
			double y = 0.5;
			double p, r,l;
			double result = y;
			if (proRatio < x)
			{
				p = proRatio / x;
				r = p * y;
				result = p * y + (1-p)*r*y;
			}
			if (proRatio > x)
			{
				l = 1 - y;
				p = (proRatio - x) / (1 - x);
				r = p * (1 - y);
				result = y + p * l + (1-p) * r *l;
			}
			return Convert.ToInt32(result * Constants.maxElo);
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
			if (index > Count)
				return false;
			if (++index > Count)
				return false;
			line = this[Count - index];
			return true;
		}

	}
}
