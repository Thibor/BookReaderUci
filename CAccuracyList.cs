using RapLog;
using System;
using System.Collections.Generic;
using System.IO;

namespace NSProgram
{
    struct BadFen
    {
        public int bstScore;
        public int badScore;
        public double worstAccuracy;
        public string fen;
        public string bstMove;
        public string badMove;
    }

    internal class CAccuracyList : MSList
    {
        public bool check = false;
        public bool confirm = false;
        public int limit = 0;
        public int index = 0;
        int totalCount = 0;
        public double totalLoss = 0;
        double totalAccuracy = 0;
        public int inaccuracies = 0;
        public int mistakes = 0;
        public int blunders = 0;
        public int lastLoss = 0;
        public BadFen badFen;
        public CRapLog log = new CRapLog("accuracy.log");
        public CRapLog his = new CRapLog("accuracy.his");
        readonly static Random rnd = new Random();

        public int GetLimit()
        {
            if (!valid)
                return Count;
            if (confirm)
                return Count;
            if (check)
                return Count;
            return Constants.limit == 0 ? Count : Math.Min(Constants.limit, Count);
        }

        public double GetProgress()
        {
            return index * 100.0 / GetLimit();
        }

        public void Prolog()
        {
            LoadFromEpd();
            limit = GetLimit();
            index = 0;
            inaccuracies = 0;
            mistakes = 0;
            blunders = 0;
            totalCount = 0;
            totalLoss = 0;
            totalAccuracy = 0;
            badFen = default;
            badFen.worstAccuracy = 100;
        }

        public void LoadFromFile()
        {
            LoadFromEpd();
            int c = Check();
            if (c > 0)
            {
                Console.WriteLine($"{c} errors fixed.");
                SaveToEpd();
            }
            LoadFromEpd();
        }

        public void PrintInfo()
        {
            Console.WriteLine(GetInfo());
        }

        string GetInfo()
        {
            GetDepth(out int minD, out int maxD);
            GetMoves(out int minM, out int maxM);
            return $"fens {Count} fail {CountFail()} depth ({minD} - {maxD}) moves ({minM} - {maxM})";
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
            while ((val > 0) && (fl.Count > 0))
            {
                int i = rnd.Next(fl.Count);
                fen = fl[i].Trim();
                if (string.IsNullOrEmpty(fen))
                    continue;
                if (AddLine(fen))
                    val--;
            }
            for (int n = fl.Count - 1; n >= 0; n--)
            {
                fen = fl[n].Trim();
                if (string.IsNullOrEmpty(fen))
                    fl.RemoveAt(n);
            }
            File.WriteAllLines("accuracy.fen", fl);
            SaveToEpd();
            Console.WriteLine($"{fl.Count} fens left.");
        }

        public void AddScore(string fen, string curMove)
        {
            MSLine msl = GetLine(fen);
            int bstScore = msl.First().score;
            int curScore = msl.GetScore(curMove);
            double wcBest = MSLine.WiningChances(bstScore);
            double wcCurrent = MSLine.WiningChances(curScore);
            double curAccuracy = MSLine.GetAccuracy(wcBest, wcCurrent);
            double loss = wcBest - wcCurrent;
            msl.loss = loss;
            totalCount++;
            totalAccuracy += curAccuracy;
            totalLoss += loss;
            if (loss >= Constants.blunder)
            {
                blunders++;
                if (check)
                    DeleteFen(fen);
            }
            if (loss >= Constants.mistake)
            {
                mistakes++;
            }
            if (loss >= Constants.inaccuracy)
            {
                inaccuracies++;
            }
            if (badFen.worstAccuracy > curAccuracy)
            {
                badFen.worstAccuracy = curAccuracy;
                badFen.fen = fen;
                badFen.bstMove = msl.First().move;
                badFen.badMove = curMove;
                badFen.bstScore = bstScore;
                badFen.badScore = curScore;
            }
        }

        public double GetTotalGain()
        {
            if (totalCount == 0)
                return 0;
            return 100.0 - totalLoss / totalCount;
        }

        public MSLine GetLine(string fen)
        {
            foreach (MSLine msl in this)
                if (msl.fen == fen) return msl;
            return null;
        }

        public double GetTotalAccuracy()
        {
            if (totalCount == 0)
                return 0;
            return totalAccuracy / totalCount;
        }

        public bool NextLine(out MSLine line)
        {
            line = null;
            if (index >= limit)
                return false;
            line = this[index];
            index++;
            return true;
        }

    }
}
