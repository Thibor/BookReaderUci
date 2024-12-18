﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NSChess;

namespace NSProgram
{
    public class CBook
    {
        public const string defExt = ".uci";
        public string path = String.Empty;
        public readonly string name = "BookReaderUci";
        public readonly string version = "2022-03-19";
        public List<string> moves = new List<string>();
        readonly CChessExt chess = new CChessExt();
        public static Random rnd = new Random();

        public void AddUci(string uci)
        {
            int del = 0;
            for (int n = moves.Count - 1; n >= 0; n--)
                if (uci.IndexOf(moves[n]) == 0)
                {
                    del++;
                    moves.RemoveRange(n, 1);
                }
            moves.Add(uci);
            if ((del > 0) && Program.isLog)
                Program.log.Add($"delted {del} ({uci})");
        }

        public void AddUci(List<string> moves)
        {
            AddUci(String.Join(" ", moves));
        }

        int SelectDel()
        {
            if (moves.Count == 0)
                return -1;
            int bi = 0;
            double bv = moves[0].Length;
            for (int n = 1; n < moves.Count; n++)
            {
                double len = moves[n].Length;
                double cv = len - (len * n) / moves.Count;
                if (bv < cv)
                {
                    bv = cv;
                    bi = n;
                }
            }
            return bi;
        }

        public int DeleteMate(int lg)
        {
            int count = moves.Count - lg;
            if (count <= 0)
                return 0;
            int c = moves.Count;
            if (count >= moves.Count)
                moves.Clear();
            else
            {
                moves.RemoveRange(SelectDel(), 1);
                if (--count > 0)
                    moves.RemoveRange(0, count);
            }
            return c - moves.Count;
        }

        public int Delete(int count)
        {
            if (count <= 0)
                return 0;
            int c = moves.Count;
            if (count >= moves.Count)
                moves.Clear();
            else
                moves.RemoveRange(0, count);
            return c - moves.Count;
        }

        public string GetMove(string m)
        {
            if (moves.Count < 1)
                return String.Empty;
            string[] mo = m.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int bstC = 0;
            string bstM = String.Empty;
            int odd = Program.chess.halfMove & 1;
            for (int n = 0; n < moves.Count; n++)
            {
                string cm = moves[n];
                if (cm.IndexOf(m) == 0)
                {
                    string[] mr = cm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if ((mr.Length > mo.Length) && ((mr.Length & 1) != odd))
                        if (rnd.Next(++bstC) == 0)
                            bstM = mr[mo.Length];
                }
            }
            return bstM;
        }

        public bool LoadFromFile(string p)
        {
            moves.Clear();
            return AddFile(p);
        }

        public bool AddFile(string p)
        {
            bool result = false;
            if (File.Exists(p))
            {
                string ext = Path.GetExtension(p);
                if (ext == ".pgn")
                    result = AddFilePgn(p);
                if (ext == ".uci")
                    result = AddFileUci(p);
            }
            else
                return true;
            return result;
        }

        bool AddFileUci(string p)
        {
            path = p;
            using (FileStream fs = File.Open(p, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader reader = new StreamReader(fs))
            {
                string line = String.Empty;
                while ((line = reader.ReadLine()) != null)
                    if (!String.IsNullOrEmpty(line))
                        moves.Add(line);
            }
            return true;
        }

        bool AddFilePgn(string p)
        {
            List<string> listPgn = File.ReadAllLines(p).ToList();
            foreach (string m in listPgn)
            {
                string cm = m.Trim();
                if (cm.Length < 1)
                    continue;
                if (cm[0] == '[')
                    continue;
                cm = Regex.Replace(cm, @"\.(?! |$)", ". ");
                string[] arrMoves = cm.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                chess.SetFen();
                string movesUci = String.Empty;
                foreach (string san in arrMoves)
                {
                    if (Char.IsDigit(san[0]))
                        continue;
                    string umo = chess.SanToUmo(san);
                    if (umo == String.Empty)
                        break;
                    movesUci += $" {umo}";
                    int emo = chess.UmoToEmo(umo);
                    chess.MakeMove(emo);
                }
                moves.Add(movesUci.Trim());
            }
            return true;
        }

        public void Save()
        {
            Save(path);
        }

        public bool Save(string p)
        {
            path = p;
            string ext = Path.GetExtension(path);
            if (ext == String.Empty)
            {
                ext = defExt;
                path += defExt;
            }
            if (ext == ".uci")
                return SaveToUci(p);
            if (ext == ".pgn")
                return SavePgn();
            if (ext == ".fen")
                return SaveToFen(p);
            return false;
        }

        public bool SaveToUci(string p)
        {
            using (FileStream fs = File.Open(p, FileMode.Create, FileAccess.Write, FileShare.None))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                foreach (string uci in moves)
                {
                    string u = uci.Trim();
                    if (!string.IsNullOrEmpty(u))
                        sw.WriteLine(u);
                }
            }
            return true;
        }

        public bool SaveToFen(string p)
        {
            CChessExt chess = new CChessExt();
            using (FileStream fs = File.Open(p, FileMode.Create, FileAccess.Write, FileShare.None))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                foreach (string uci in moves)
                {
                    string u = uci.Trim();
                    if (!string.IsNullOrEmpty(u))
                    {
                        chess.SetFen();
                        chess.MakeMoves(u);
                        sw.WriteLine(chess.GetFen());
                    }
                }
            }
            return true;
        }

        public bool SavePgn(string p)
        {
            path = p;
            return SavePgn();
        }

        bool SavePgn()
        {
            List<string> listPgn = new List<string>();
            foreach (string m in moves)
            {
                string[] arrMoves = m.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                chess.SetFen();
                string png = String.Empty;
                foreach (string umo in arrMoves)
                {
                    string san = chess.UmoToSan(umo);
                    if (san == String.Empty)
                        break;
                    int number = (chess.halfMove >> 1) + 1;
                    if (chess.whiteTurn)
                        png += $" {number}. {san}";
                    else
                        png += $" {san}";
                    int emo = chess.UmoToEmo(umo);
                    chess.MakeMove(emo);
                }
                listPgn.Add(String.Empty);
                listPgn.Add("[White \"White\"]");
                listPgn.Add("[Black \"Black\"]");
                listPgn.Add(String.Empty);
                listPgn.Add(png.Trim());
            }
            File.WriteAllLines(path, listPgn);
            return true;
        }

        public void ShowInfo()
        {
            if (moves.Count == 0)
            {
                Console.WriteLine("no records");
                return;
            }
            int minL = int.MaxValue;
            int maxL = int.MinValue;
            int minM = 0;
            int maxM = 0;
            double sum = 0;
            foreach (string l in moves)
            {
                sum += l.Length + 1;
                if (minL > l.Length)
                {
                    minL = l.Length;
                    minM = l.Split().Length;
                }
                if (maxL < l.Length)
                {
                    maxL = l.Length;
                    maxM = l.Split().Length;
                }
            }
            sum /= (moves.Count * 5);
            string frm = "{0,8:N0}";
            Console.WriteLine();
            Console.WriteLine($"games     {frm}",moves.Count);
            Console.WriteLine($"depth avg {frm}",sum);
            Console.WriteLine($"depth min {frm}",minM);
            Console.WriteLine($"depth max {frm}",maxM);
        }

    }

}
