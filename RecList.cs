using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSProgram
{
    public class ERec
    {
        public int games = 1;
        public string move =string.Empty;
    }

    public class RecList:List<ERec>
    {

        public int FindRec(ERec r)
        {
            int first = -1;
            int last = Count;
            while (true)
            {
                if (last - first == 1)
                    return last;
                int middle = (first + last) >> 1;
                ERec rec = this[middle];
                  if(String.Compare(r.move,rec.move, comparisonType: StringComparison.OrdinalIgnoreCase)<=0)
                last = middle;
                else
                    first = middle;
            }
        }


        public bool AddRec(ERec rec)
        {
            int index = FindRec(rec);
            if (index == Count)
                Add(rec);
            else
            {
                ERec r = this[index];
                if (r.move == rec.move)
                {
                    this[index].games++;
                    return false;
                }
                else
                    Insert(index, rec);
            }
            return true;
        }

        public void SortGames()
        {
            Sort(delegate (ERec r1, ERec r2)
            {
                return r2.games - r1.games;
            });
        }


    }
}
