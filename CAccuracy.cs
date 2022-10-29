using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSProgram
{
	internal class CAccuracy
	{
		double centyPawns = 0;
		int count = 0;
		double bst = 0;
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
			double val = (double)delta / Math.Abs(sb + sc);
			count++;
			centyPawns += delta;
			if (bst<val)
			{
				bst = val;
				bstSb = sb;
				bstSc = sc;
				bstFen = fen;
				bstMsg = msg;
			}
		}
		public double GetAccuracy()
		{
			return centyPawns / count;
		}

	}
}
