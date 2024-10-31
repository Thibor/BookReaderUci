using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace NSProgram
{
    class CElementT
    {
        public string line = string.Empty;

        public string GetFen()
        {
            if (!Program.chess.SetFen(line))
                Program.chess.SetFen();
            return Program.chess.GetFen();
        }
    }

    internal class CTestList : List<CElementT>
    {
        public bool delete = false;
        int index = 0;
        public int resultOk = 0;
        public int resultFail = 0;
        public string line = String.Empty;

        public CElementT CurElement()
        {
            if ((index < 0) || (index >= Count))
                return null;
            return this[index];
        }

        public double GetProgress()
        {
            return (GetNumber() * 100.0) / Count;
        }

        public double GetTest()
        {
            return (resultOk * 100.0) / GetNumber();
        }

        public int GetNumber()
        {
            return resultOk + resultFail;
        }

        public void Reset()
        {
            resultOk = 0;
            resultFail = 0;
            index = Count;
            Next();
        }

        public void DeleteFen(string fen)
        {
            for (int n = Count - 1; n >= 0; n--)
                if (this[n].GetFen() == fen)
                {
                    RemoveAt(n);
                    if (index > n)
                        index--;
                }
        }

        public void LoadFromFile()
        {
            string fn = Constants.testEpd;
            Clear();
            if (!File.Exists(fn))
            {
                Console.WriteLine($"{fn} not exists");
                return;
            }
            using (FileStream fs = File.Open(fn, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader reader = new StreamReader(fs))
            {
                string line = String.Empty;
                while ((line = reader.ReadLine()) != null)
                    if (!String.IsNullOrEmpty(line))
                    {
                        CElementT t = new CElementT();
                        t.line = line;
                        Add(t);
                    }
            }
            Console.WriteLine($"info string test on fens {Count:N0}");
        }

        void SortFen()
        {
            Sort(delegate (CElementT l1, CElementT l2)
            {
                return String.Compare(l1.line, l2.line, StringComparison.Ordinal);
            });
        }

        public void SaveToFile()
        {
            SortFen();
            string lastFen=string.Empty;
            using (FileStream fs = File.Open(Constants.testEpd, FileMode.Create, FileAccess.Write, FileShare.None))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                foreach (CElementT e in this)
                {
                    string l = e.line;
                    string[] tokens = l.Split(' ');
                    string curFen = $"{tokens[0]} {tokens[1]}";
                    if (lastFen == curFen)
                        continue;
                    lastFen = curFen;
                    sw.WriteLine(e.line);
                }
            }
        }

        public bool Next()
        {
            index--;
            CElementT et = CurElement();
            if (et == null)
                return false;
            if (et.GetFen() == CChessExt.defFen)
            {
                if (delete)
                    RemoveAt(index);
                return Next();
            }
            return true;
        }

        public bool GetResult(string move)
        {
            Program.chess.SetFen(CurElement().GetFen());
            string san = Program.chess.UmoToSan(move);
            if (CurElement().line.Contains("bm "))
                if (CurElement().line.Contains($" {move}") || CurElement().line.Contains($" {san}"))
                    return true;
                else
                    return false;
            if (CurElement().line.Contains("am "))
                if (CurElement().line.Contains($" {move}") || CurElement().line.Contains($" {san}"))
                    return false;
                else
                    return true;
            return true;
        }

        public void SetResult(string move)
        {
            bool r = GetResult(move);
            if (r)
                resultOk++;
            else
            {
                if (delete)
                    RemoveAt(index);
                resultFail++;
            }
        }

    }
}
