using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSProgram
{
	internal class CAccuracy
	{
		public int index = 0;
		double centyLoss = 0;
		int centyCount = 0;
		double bst = 0;
		public int inaccuracies = 0;
		public int mistakes = 0;
		public int blunders = 0;
		public int bstSb = 0;
		public int bstSc = 0;
		public string bstFen = String.Empty;
		public string bstMsg = String.Empty;
		public MSList fenList = new MSList();


		public void LoadFen()
		{
			fenList.LoadFen();
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
			if (delta >= Constants.blunders)
			{
				delta = Constants.blunders;
				blunders++;
			}
			else if (delta > Constants.mistakes)
				mistakes++;
			else if (delta > Constants.inaccuracies)
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

		public double GetAccuracy()
		{
			return centyLoss / (centyCount+1);
		}

		public bool NextLine(out MSLine line)
		{
			line = null;
			if (index >= fenList.Count)
				return false;
			line = fenList[fenList.Count - ++index];
			return true;
		}

	}
}
