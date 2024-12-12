using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSProgram
{
    internal class CModList:CAccuracyList
    {
        public CModList()
        {
            fileName = Constants.modEpd;
        }

        public void AddEpd(int count)
        {
            Program.accuracy.LoadFromEpd();
            Program.accuracy.SortLoss();
            foreach(MSLine msl in Program.accuracy)
            {
                Add(msl);
                if (Count == count)
                    break;
            }
            SaveToEpd();
            Console.WriteLine($"{Count} fens");
        }

    }
}
