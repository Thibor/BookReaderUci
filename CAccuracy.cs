using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSProgram
{
	internal class CAccuracy : MSList
	{
		bool loaded = false;
		public int index = 0;
		long centyLoss = 0;
		long centyCount = 0;
		double bst = 0;
		public int inaccuracies = 0;
		public int mistakes = 0;
		public int blunders = 0;
		public int bstSb = 0;
		public int bstSc = 0;
		public string bstFen = String.Empty;
		public string bstMsg = String.Empty;


		public void LoadFromFile()
		{
			if (loaded)
				return;
			loaded = true;
			LoadFen();
			GetDepth(out int minD, out int maxD);
			GetMoves(out int minM, out int maxM);
			Console.WriteLine($"info string accuracy on fens {Count} fail {CountFail()} depth ({minD} - {maxD}) moves ({minM} - {maxM})");
		}

		public void Reset()
		{
			index = 0;
			inaccuracies = 0;
			mistakes = 0;
			blunders = 0;
			bst = 0;
			centyLoss = 0;
			centyCount = 0;
			bstSb = 0;
			bstSc = 0;
			bstFen = String.Empty;
			bstMsg = String.Empty;
		}

		public void AddScore(string fen, string msg, int best, int score)
		{
			int delta = Math.Abs(best - score);
			if (delta >= Constants.blunder)
			{
				delta = Constants.blunder;
				blunders++;
			}
			else if (delta > Constants.mistake)
				mistakes++;
			else if (delta > Constants.inaccuracy)
				inaccuracies++;
			double val = (double)delta / Math.Abs(best + score);
			centyCount++;
			centyLoss += delta;
			if (bst < val)
			{
				bst = val;
				bstSb = best;
				bstSc = score;
				bstFen = fen;
				bstMsg = msg;
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

		public double GetAccuracy(long cc,long cl)
		{
			if (cc == 0)
				return 0;
			double max = cc * Constants.blunder;
			return ((max - cl) * 100.0) / max;
		}

		public double GetAccuracy()
		{
			return GetAccuracy(centyCount,centyLoss);
		}

		public int GetElo(double accuracy)
		{
			double ratio = (Constants.maxElo - Constants.minElo) / (Constants.maxAcc - Constants.minAcc);
			double result = Constants.minElo + (accuracy - Constants.minAcc) * ratio;
			return Convert.ToInt32(result);
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
			line = this[Count - ++index];
			return true;
		}

	}
}
