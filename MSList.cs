using NSChess;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace NSProgram
{
    class MSRec
    {
        public string move;
        public int score;

        public MSRec(MSRec rec)
        {
            move = rec.move;
            score = rec.score;
        }

        public MSRec(string m, int s)
        {
            move = m;
            score = s;
        }
    }

    internal class MSLine : List<MSRec>
    {
        public string fen = String.Empty;
        public int depth = 0;
        public double loss = 100;

        public bool Valid()
        {
            return loss < 100;
        }

        public void Assign(MSLine line)
        {
            Clear();
            fen = line.fen;
            depth = line.depth;
            foreach (MSRec rec in line)
                Add(new MSRec(rec));
        }

        public void DeleteMove(string move)
        {
            for (int n = Count - 1; n >= 0; n--)
                if (this[n].move == move)
                    RemoveAt(n);
        }

        public void AddRec(MSRec rec)
        {
            DeleteMove(rec.move);
            Add(rec);
            SortScore();
        }

        public void SortScore()
        {
            Sort(delegate (MSRec r1, MSRec r2)
            {
                return r2.score - r1.score;
            });
        }

        public string ShortFen()
        {
            return fen.Split(' ')[0];
        }

        public MSRec First()
        {
            if (Count > 0)
                return this[0];
            return null;
        }

        public MSRec Last()
        {
            if (Count > 0)
                return this[Count - 1];
            return null;
        }

        public static double WiningChances(int centipawns)
        {
            return 50 + 50 * (2 / (1 + Math.Exp(-0.00368208 * centipawns)) - 1);
        }

        public static double GetAccuracy(double winPercentBefore, double winPercentAfter)
        {
            return 103.1668 * Math.Exp(-0.04354 * (winPercentBefore - winPercentAfter)) - 3.1669;
        }

        public static double GetAccuracy(int scoreBefore, int scoreAfter)
        {
            double winPercentBefore = WiningChances(scoreBefore);
            double winPercentAfter = WiningChances(scoreAfter);
            return GetAccuracy(winPercentBefore, winPercentAfter);
        }

        public static double GetLoss(int before, int after)
        {
            double winPercentBefore = WiningChances(before);
            double winPercentAfter = WiningChances(after);
            return winPercentBefore - winPercentAfter;
        }

        public double GetAccuracy()
        {
            MSRec f = First();
            MSRec l = Last();
            if ((f == null) || (l == null))
                return 0;
            return GetAccuracy(First().score, Last().score);
        }

        public double GetLoss()
        {
            if(Count < 2)
                return 0;
            return GetLoss(First().score, Last().score);
        }

        public bool Fail()
        {
            if (Count<2)
                return false;
            return GetLoss() < Constants.blunder;
        }

        public bool MoveExists(string move)
        {
            foreach (MSRec rec in this)
                if (rec.move == move)
                    return true;
            return false;
        }

        void Reset()
        {
            fen = String.Empty;
            depth = 0;
            loss = 0;
            Clear();
        }

        string GetMoves()
        {
            string moves = String.Empty;
            foreach (MSRec rec in this)
            {
                moves = $"{moves} bm {rec.move} ce {rec.score}";
            }
            return moves.Trim();
        }

        public int GetScore(string move)
        {
            foreach (MSRec rec in this)
                if (rec.move == move)
                    return rec.score;
            return Last().score;
        }

        public string SaveToStr()
        {
            string moves = GetMoves();
            string l = Convert.ToString(loss, CultureInfo.InvariantCulture.NumberFormat);
            return $"{fen} acd {depth} loss {l} {moves}".Trim();
        }

        public bool LoadFromStr(string line)
        {
            Reset();
            string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 6)
                return false;
            List<string> sl = new List<string>(tokens);
            string last = String.Empty;
            string move = String.Empty;
            for (int n = 0; n < sl.Count; n++)
            {
                string s = sl[n];
                if ((s == "bm") || (s == "ce") || (s == "acd") || (s == "loss"))
                {
                    last = s;
                    continue;
                }
                if (n < 6)
                {
                    fen += ' ' + s;
                    continue;
                }
                switch (last)
                {
                    case "acd":
                        depth = Convert.ToInt32(s);
                        break;
                    case "loss":
                        double.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out loss);
                        break;
                    case "bm":
                        move = s;
                        break;
                    case "ce":
                        if (!String.IsNullOrEmpty(move))
                        {
                            MSRec rec = new MSRec(move, Convert.ToInt32(s));
                            AddRec(rec);
                        }
                        break;
                }
            }
            fen = fen.Trim();
            return true;
        }

    }

    internal class MSList : List<MSLine>
    {
        public bool valid = true;

        public int Check()
        {
            if (Count == 0)
                return 0;
            SortFen();
            MSLine last = this[0];
            CChess chess = new CChess();
            int result = 0;
            for (int n = Count - 1; n >= 0; n--)
            {
                MSLine msl = this[n];
                if (chess.SetFen(msl.fen))
                {
                    string fen = chess.GetFen();
                    if (msl.fen != fen)
                        result++;
                    msl.fen = fen;
                }
                else
                {
                    result++;
                    RemoveAt(n);
                }
                if ((msl.ShortFen() == last.ShortFen()) && (Count > 1))
                {
                    result++;
                    if (msl.depth < last.depth)
                        RemoveAt(n);
                    else
                        RemoveAt(n + 1);
                }
                last = msl;
            }
            return result;
        }

        public void GetDepth(out int min, out int max)
        {
            min = int.MaxValue;
            max = 0;
            foreach (MSLine msl in this)
            {
                if (min > msl.depth)
                    min = msl.depth;
                if (max < msl.depth)
                    max = msl.depth;
            }
            if (min > max)
                min = 0;
        }

        public void SetDepth(int depth)
        {
            foreach (MSLine msl in this)
                if (msl.depth > depth)
                    msl.depth = depth;
        }

        public void GetMoves(out int min, out int max)
        {
            min = int.MaxValue;
            max = 0;
            foreach (MSLine msl in this)
            {
                if (min > msl.Count)
                    min = msl.Count;
                if (max < msl.Count)
                    max = msl.Count;
            }
            if (min > max)
                min = 0;
        }

        public int CountMovesMin(out int min)
        {
            int result = 0;
            min = int.MaxValue;
            foreach (MSLine msl in this)
            {
                if (min > msl.Count)
                {
                    min = msl.Count;
                    result = 1;
                }
                else if (min == msl.Count)
                    result++;
            }
            return result;
        }

        public int CountMovesMax(out int max)
        {
            int result = 0;
            max = 0;
            foreach (MSLine msl in this)
            {
                if (max < msl.Count)
                {
                    max = msl.Count;
                    result = 1;
                }
                else if (max == msl.Count)
                    result++;
            }
            return result;
        }

        public int CountFail()
        {
            int result = 0;
            foreach (MSLine msl in this)
                if (msl.Fail())
                    result++;
            return result;
        }

        public int DeleteFail()
        {
            int result = 0;
            for (int n = Count - 1; n >= 0; n--)
            {
                MSLine msl = this[n];
                if (msl.Fail())
                {
                    result++;
                    RemoveAt(n);
                }
            }
            if (result > 0)
                SaveToEpd();
            return result;
        }

        public void DeleteFen(string fen)
        {
            for (int n = Count - 1; n >= 0; n--)
                if (this[n].fen == fen)
                    RemoveAt(n);
        }

        public int DeleteMoves(int moves)
        {
            int result = 0;
            for (int n = Count - 1; n >= 0; n--)
                if (this[n].Count == moves)
                {
                    result++;
                    RemoveAt(n);
                }
            if (result > 0)
                SaveToEpd();
            return result;
        }

        public void SaveToEpd()
        {
            SortFen();
            using (FileStream fs = File.Open(Constants.accuracyEpd, FileMode.Create, FileAccess.Write, FileShare.None))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                foreach (MSLine msl in this)
                {
                    string l = msl.SaveToStr();
                    sw.WriteLine(l);
                }
            }
        }

        public bool LoadFromEpd()
        {
            valid = true;
            Clear();
            if (!File.Exists(Constants.accuracyEpd))
                return false;
            using (FileStream fs = File.Open(Constants.accuracyEpd, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader reader = new StreamReader(fs))
            {
                string line = String.Empty;
                while ((line = reader.ReadLine()) != null)
                {
                    MSLine msl = new MSLine();
                    if (msl.LoadFromStr(line))
                        Add(msl);
                    if(!msl.Valid())
                        valid= false;
                }
            }
            return Count > 0;
        }

        void SortFen()
        {
            Sort(delegate (MSLine l1, MSLine l2)
            {
                return String.Compare(l1.fen, l2.fen, StringComparison.Ordinal);
            });
        }

        public void SortDepth()
        {
            Sort(delegate (MSLine l1, MSLine l2)
            {
                int d1 = l1.depth;
                int d2 = l2.depth;
                return d1 - d2;
            });
        }

        public void SortLoss()
        {
            Sort(delegate (MSLine l1, MSLine l2)
            {
                double d1 = l1.loss;
                double d2 = l2.loss;
                if (d1 == d2) return 0;
                return d1 < d2 ? 1 : -1;
            });
        }

        public void SortRandom()
        {
            for (int n = 0; n < Count; n++)
            {
                int r = CChess.rnd.Next(Count);
                (this[n], this[r]) = (this[r], this[n]);
            }
        }

        public int GetFenIndex(string fen)
        {
            for (int n = 0; n < Count; n++)
                if (this[n].fen == fen)
                    return n;
            return -1;
        }

        public bool AddLine(MSLine line)
        {
            int index = GetFenIndex(line.fen);
            if (index < 0)
                Add(line);
            return index < 0;
        }

        public bool AddLine(string line)
        {
            MSLine msl = new MSLine();
            if (msl.LoadFromStr(line))
                return AddLine(msl);
            return false;
        }

        public void ReplaceLine(MSLine line)
        {
            int index = GetFenIndex(line.fen);
            if (index >= 0)
                this[index].Assign(line);
            else
                Add(line);
        }
        public int CountShallowLine()
        {
            int result = 0;
            foreach (MSLine line in this)
                if (line.depth < Constants.minDepth)
                    result++;
            return result;
        }

        public MSLine GetShallowLine()
        {
            if (Count == 0)
                return null;
            MSLine bst = this[0];
            foreach (MSLine line in this)
                if (bst.depth > line.depth)
                    bst = line;
            return bst.depth < Constants.minDepth ? bst : null;
        }

        public MSLine GetLineFail()
        {
            foreach (MSLine line in this)
                if (line.Fail())
                    return line;
            return null;
        }

        public MSLine GetRandomLine()
        {
            if (Count == 0)
                return null;
            int index = CChess.rnd.Next(Count);
            return this[index];
        }

        public void ResetLoss()
        {
            foreach (MSLine line in this)
                line.loss = 100;
            SaveToEpd();
        }

    }

}
