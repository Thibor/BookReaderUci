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
        long totalCount = 0;
        double totalAccuracy = 0;
        public double lastAccuracy = 0;
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
            totalAccuracy = 0;
            lastAccuracy = 0;
            badFen = default;
            badFen.worstAccuracy = 100;
        }

        public void AddScore(string fen, string bstMove, string curMove, int bstScore, int curScore)
        {
            double bstWC = MSLine.WiningChances(bstScore);
            double curWC = MSLine.WiningChances(curScore);
            double del = bstWC - curWC;
            lastAccuracy = MSLine.GetAccuracy(bstWC, curWC);
            totalCount++;
            totalAccuracy += lastAccuracy;
            if (del >= Constants.blunder)
                blunders++;
            else if (del >= Constants.mistake)
                mistakes++;
            else if (del >= Constants.inaccuracy)
                inaccuracies++;
            if (badFen.worstAccuracy > lastAccuracy)
            {
                badFen.worstAccuracy = lastAccuracy;
                badFen.fen = fen;
                badFen.bstMove = bstMove;
                badFen.badMove = curMove;
                badFen.bstScore = bstScore;
                badFen.badScore = curScore;
            }
        }

        public static double DeltaWC(int before ,int after)
        {
            double winPercentBefore = MSLine.WiningChances(before);
            double winPercentAfter = MSLine.WiningChances(after);
            return winPercentBefore - winPercentAfter;
        }

        public double GetAccuracy()
        {
            return totalAccuracy / totalCount;
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
            int eloMax = GetElo(accuracy);
            int eloMin = GetElo(totalAccuracy / (totalCount + 1));
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
