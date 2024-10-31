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
        public double worstAccuracy;
        public string fen;
        public string bstMove;
        public string badMove;
    }

    internal class CAccuracy : MSList
    {
        bool loaded = false;
        public int index = 0;
        int totalCount = 0;
        public double totalLoss = 0;
        public double totalLossBst = 0;
        double totalAccuracy = 0;
        double totalWeight = 0;
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

        public void Reset()
        {
            index = 0;
            inaccuracies = 0;
            mistakes = 0;
            blunders = 0;
            totalCount = 0;
            totalLoss = 0;
            totalLossBst = 0;
            totalAccuracy = 0;
            totalWeight = 0;
            badFen = default;
            badFen.worstAccuracy = 100;
            LoadFromEpd();
            SortLoss();
        }

        public void AddScore(string fen, string bstMove, string curMove, int bstScore, int curScore)
        {
            MSLine msl = GetLine(fen);
            double bstWC = MSLine.WiningChances(bstScore);
            double curWC = MSLine.WiningChances(curScore);
            double curAccuracy = MSLine.GetAccuracy(bstWC, curWC);
            double loss = bstWC - curWC;
            totalLossBst += msl.loss;
            msl.loss = loss;
            totalCount++;
            totalAccuracy += curAccuracy;
            totalLoss += loss;
            totalWeight += (loss + 1) * curAccuracy;
            if (loss >= Constants.blunder)
                blunders++;
            if (loss >= Constants.mistake)
                mistakes++;
            if (loss >= Constants.inaccuracy)
                inaccuracies++;
            if (badFen.worstAccuracy > curAccuracy)
            {
                badFen.worstAccuracy = curAccuracy;
                badFen.fen = fen;
                badFen.bstMove = bstMove;
                badFen.badMove = curMove;
                badFen.bstScore = bstScore;
                badFen.badScore = curScore;
            }
        }

        public double GetMargin()
        {
            return totalLossBst - totalLoss;
        }

        public double GetAccuracy()
        {
            if (totalCount == 0)
                return 0;
            return totalAccuracy / totalCount;
        }

        public double GetGain()
        {
            if (totalCount == 0)
                return 0;
            return 100.0 - totalLoss / totalCount;
        }

        public double GetWeight()
        {
            if (totalCount == 0)
                return 0;
            return totalWeight / (totalLoss + totalCount);
        }

        int GetLimit()
        {
            return Constants.limit < 1 ? Count : Math.Min(Constants.limit, Count);
        }

        public double GetProgress()
        {
            return index * 100.0 / GetLimit();
        }

        public MSLine GetLine(string fen)
        {
            foreach (MSLine msl in this)
                if (msl.fen == fen) return msl;
            return null;
        }

        public double GetLoss()
        {
            if (totalCount == 0)
                return 0;
            return totalLoss / totalCount;
        }

        public int GetEloAccuracy(double accuracy)
        {
            accuracy /= 100.0;
            return Convert.ToInt32(accuracy * Constants.maxElo);
        }

        public int GetEloWeight(double weight)
        {
            weight /= 100.0;
            return Convert.ToInt32(weight * Constants.maxElo);
        }

        public int GetEloAccuracy(double accuracy, out int del)
        {
            int eloMax = GetEloAccuracy(accuracy);
            int eloMin = GetEloAccuracy(totalAccuracy / (totalCount + 1));
            del = eloMax - eloMin;
            return eloMax;
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
