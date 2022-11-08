using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSProgram
{
	internal class CAccuracy
	{
		int index = -1;
		public int number = 1;
		double centyPawns = 0;
		int count = 0;
		double bst = 0;
		public int inaccuracies = 0;
		public int mistakes = 0;
		public int blunders = 0;
		public int bstSb = 0;
		public int bstSc = 0;
		public string bstFen = String.Empty;
		public string bstMsg = String.Empty;
		public MSList fenList = new MSList();

		public CAccuracy()
		{
			fenList.LoadFromFile();
		}

		public void Reset()
		{
			index = -1;
			number = 1;
			inaccuracies = 0;
			mistakes = 0;
			blunders = 0;
			bst = 0;
			count = 0;
			centyPawns = 0;
			bstSb = 0;
			bstSc = 0;
			bstFen = String.Empty;
			bstMsg = String.Empty;
		}

		public void Add(string fen, string msg, int sb, int sc)
		{
			int delta = sb - sc;
			if (delta >= Constants.blunders)
			{
				delta = Constants.blunders;
				blunders++;
			}
			else if (delta > Constants.mistakes)
				mistakes++;
			else if (delta > Constants.inaccuracies)
				inaccuracies++;
			double val = (double)delta / Math.Abs(sb + sc);
			count++;
			centyPawns += delta;
			if (bst < val)
			{
				bst = val;
				bstSb = sb;
				bstSc = sc;
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
			return centyPawns / count;
		}

		public bool Next()
		{
			if (index >= fenList.Count)
				return false;
			index++;
			number++;
			return true;
		}
	}
}
