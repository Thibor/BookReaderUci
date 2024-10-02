using NSChess;
using NSUci;
using RapIni;
using RapLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace NSProgram
{
    static class Program
    {
        public static bool isLog = false;
        public static bool isW = false;
        public static CChessExt chess = new CChessExt();
        public static CAccuracy accuracy = new CAccuracy();
        public static CEvaluationList evaluation = new CEvaluationList();
        public static CTestList test = new CTestList();
        public static CTeacher teacher = new CTeacher();
        public static CBook book = new CBook();
        public static CUci uci = new CUci();
        public static CRapIni ini = new CRapIni();
        public static CRapLog log = new CRapLog();
        public static string teacherFile = String.Empty;
        public static string studentFile = String.Empty;

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        private static bool Handler(CtrlType signal)
        {
            switch (signal)
            {
                case CtrlType.CTRL_BREAK_EVENT:
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                    teacher.Terminate();
                    Environment.Exit(0);
                    return false;

                default:
                    return false;
            }
        }

        private enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }
        public static bool Confirm(string title)
        {
            ConsoleKey response;
            do
            {
                Console.Write($"{title} [y/n] ");
                response = Console.ReadKey(false).Key;
                if (response != ConsoleKey.Enter)
                {
                    Console.WriteLine();
                }
            } while (response != ConsoleKey.Y && response != ConsoleKey.N);

            return (response == ConsoleKey.Y);
        }

        static void Main(string[] args)
        {
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);
            if (args.Length == 0)
                LoadFromIni();
            int missingIndex = 0;
            bool isInfo = false;
            bool bookRead = false;
            int bookLimitW = 0;
            int bookLimitR = 0;
            int limitGames = 1000;
            int random = 0;
            string lastFen = String.Empty;
            string lastMoves = String.Empty;
            string ax = "-bf";
            List<string> listBf = new List<string>();
            List<string> listEf = new List<string>();
            List<string> listEa = new List<string>();
            List<string> listTf = new List<string>();
            List<string> listSf = new List<string>();
            for (int n = 0; n < args.Length; n++)
            {
                string ac = args[n];
                switch (ac)
                {
                    case "-bf"://book file
                    case "-ef"://engine file
                    case "-ea"://engine arguments
                    case "-lr"://limit ply read
                    case "-lw"://limit ply write
                    case "-lg"://limit games
                    case "-tf"://teacher file
                    case "-sf"://student file
                    case "-acd"://analysis count depth
                    case "-rnd"://random moves
                        ax = ac;
                        break;
                    case "-w":
                        ax = ac;
                        isW = true;
                        break;
                    case "-log"://add log
                        ax = ac;
                        isLog = true;
                        break;
                    case "-info":
                        ax = ac;
                        isInfo = true;
                        break;
                    default:
                        switch (ax)
                        {
                            case "-bf":
                                listBf.Add(ac);
                                break;
                            case "-ef":
                                listEf.Add(ac);
                                break;
                            case "-ea":
                                listEa.Add(ac);
                                break;
                            case "-tf":
                                listTf.Add(ac);
                                break;
                            case "-sf":
                                listSf.Add(ac);
                                break;
                            case "-acd":
                                Constants.minDepth = int.TryParse(ac, out int acd) ? acd : Constants.minDepth;
                                break;
                            case "-lr":
                                bookLimitR = int.TryParse(ac, out int lr) ? lr : 0;
                                break;
                            case "-lw":
                                bookLimitW = int.TryParse(ac, out int lw) ? lw : 0;
                                break;
                            case "-lg":
                                ac = ac.Replace("K", "000").Replace("M", "000000");
                                limitGames = int.TryParse(ac, out int lg) ? lg : 0;
                                break;
                            case "-rnd":
                                random = int.TryParse(ac, out int rnd) ? rnd : 0;
                                break;
                        }
                        break;
                }
            }
            string bookFile = String.Join(" ", listBf);
            string engineFile = String.Join(" ", listEf);
            string engineArguments = String.Join(" ", listEa);
            teacherFile = String.Join(" ", listTf);
            studentFile = String.Join(" ", listSf);
            Console.WriteLine($"idbook name {book.name} ver {book.version}");
            Console.WriteLine($"idbook extension {CBook.defExt}");
            Process myProcess = new Process();
            if (File.Exists(engineFile))
            {
                myProcess.StartInfo.FileName = engineFile;
                myProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(engineFile);
                myProcess.StartInfo.UseShellExecute = false;
                myProcess.StartInfo.RedirectStandardInput = true;
                myProcess.StartInfo.Arguments = engineArguments;
                myProcess.Start();
            }
            else
            {
                if (engineFile != String.Empty)
                    Console.WriteLine("info string missing engine");
                engineFile = String.Empty;
            }
            if (File.Exists(Constants.teacher))
                Console.WriteLine("info string teacher on");
            if (File.Exists(Constants.student))
                Console.WriteLine("info string student on");
            if (File.Exists("accuracy.epd"))
                Console.WriteLine("info string accuracy on");
            if (File.Exists("test.epd"))
                Console.WriteLine("info string test on");
            if (File.Exists("evaluation.epd"))
                Console.WriteLine("info string evaluation on");
            if (File.Exists("mod.ini"))
                Console.WriteLine("info string mod on");
            if (File.Exists("accuracy.fen"))
                Console.WriteLine("info string fen on");
            bool bookLoaded = SetBookFile(bookFile);
            do
            {
                string msg = String.IsNullOrEmpty(Constants.command) ? Console.ReadLine().Trim() : Constants.command;
                Constants.command = String.Empty;
                if (String.IsNullOrEmpty(msg) || (msg == "help") || (msg == "book"))
                {
                    Console.WriteLine("book load [filename].[umo|uci|png] - clear and add moves from file");
                    Console.WriteLine("book save [filename].[umo|uci|png] - save book to the file");
                    Console.WriteLine("book addfile [filename].[umo|uci|png] - add moves to the book from file");
                    Console.WriteLine("book adduci [uci] - add moves in uci format to the book");
                    Console.WriteLine("book clear - clear all moves from the book");
                    Console.WriteLine("book getoption - show options");
                    Console.WriteLine("book setoption name [option name] value [option value] - set option");
                    continue;
                }
                uci.SetMsg(msg);
                int count = book.moves.Count;
                bool done = true;
                switch (uci.command)
                {
                    case "accuracy":
                        accuracy.LoadFromFile();
                        if (accuracy.Count == 0)
                            Console.WriteLine($"file \"{Constants.accuracyEpd}\" unavabile");
                        if (uci.tokens.Length > 1)
                            switch (uci.tokens[1])
                            {
                                case "update":
                                    accuracy.GetDepth(out int minD, out _);
                                    minD = uci.GetInt("update", ++minD);
                                    if (Constants.minDepth < minD)
                                        Constants.minDepth = minD;
                                    teacher.AccuracyUpdate();
                                    break;
                                case "delete":
                                    int cf = accuracy.CountFail();
                                    if (cf > 0)
                                    {
                                        if (Confirm($"Delete {cf} fens"))
                                        {
                                            cf = accuracy.DeleteFail();
                                            Console.WriteLine($"{cf} fens deleted");
                                        }
                                        break;
                                    }
                                    if (uci.GetValue("delete") == "min")
                                    {
                                        int cm = accuracy.CountMovesMin(out int minM);
                                        if (Confirm($"Delete {cm} fens"))
                                        {
                                            cm = accuracy.DeleteMoves(minM);
                                            Console.WriteLine($"{cm} fens deleted");
                                        }
                                    }
                                    if (uci.GetValue("delete") == "max")
                                    {
                                        int cm = accuracy.CountMovesMax(out int maxM);
                                        if (Confirm($"Delete {cm} fens"))
                                        {
                                            cm = accuracy.DeleteMoves(maxM);
                                            Console.WriteLine($"{cm} fens deleted");
                                        }
                                    }
                                    break;
                                case "add":
                                    accuracy.Add(uci.GetInt("add"));
                                    break;
                                case "depth":
                                    accuracy.SetDepth(uci.GetInt("depth"));
                                    break;
                                case "start":
                                    Constants.limit = uci.GetInt("start", accuracy.Count);
                                    teacher.AccuracyStart();
                                    break;
                                default:
                                    Console.WriteLine($"Unknown command [{uci.tokens[1]}]");
                                    break;
                            }
                        accuracy.PrintInfo();
                        break;
                    case "mod":
                        accuracy.LoadFromFile();
                        if (accuracy.Count == 0)
                            Console.WriteLine($"file \"{Constants.accuracyEpd}\" unavabile");
                        if (uci.tokens.Length > 1)
                            switch (uci.tokens[1])
                            {
                                case "start":
                                    teacher.ModStart();
                                    break;
                                case "reset":
                                    teacher.mod.Reset();
                                    break;
                                default:
                                    Console.WriteLine($"Unknown command [{uci.tokens[1]}]");
                                    break;
                            }
                        break;
                    case "evaluation":
                        evaluation.LoadFromFile();
                        if (evaluation.Count == 0)
                            Console.WriteLine("file \"evaluation.fen\" unavabile");
                        if (uci.tokens.Length > 1)
                            switch (uci.tokens[1])
                            {
                                case "reset":
                                    evaluation.Fill();
                                    break;
                                case "update":
                                    teacher.EvaluationUpdate();
                                    break;
                                case "start":
                                    teacher.EvaluationStart();
                                    break;
                            }
                        break;

                    case "test":
                        test.LoadFromFile();
                        if (test.Count == 0)
                            Console.WriteLine("file \"test.fen\" unavabile");
                        if (uci.tokens.Length > 1)
                            switch (uci.tokens[1])
                            {
                                case "start":
                                    Constants.limit = uci.GetInt("test", accuracy.Count);
                                    teacher.TestStart();
                                    break;
                            }
                        break;
                    case "ini":
                        SavetoIni();
                        break;
                    case "book":
                        switch (uci.tokens[1])
                        {
                            case "addfile":
                                if (!book.AddFile(uci.GetValue("addfile")))
                                    Console.WriteLine("File not found");
                                else
                                    Console.WriteLine($"{book.moves.Count - count:N0} lines have been added");
                                break;
                            case "adduci":
                                book.moves.Add(uci.GetValue("adduci"));
                                Console.WriteLine($"{book.moves.Count - count:N0} lines have been added");
                                break;
                            case "clear":
                                book.moves.Clear();
                                Console.WriteLine("Book is empty");
                                break;
                            case "delete":
                                int c = book.Delete(uci.GetInt("delete"));
                                Console.WriteLine($"{c:N0} moves was deleted");
                                break;
                            case "load":
                                if (!book.LoadFromFile(uci.GetValue("load")))
                                    Console.WriteLine("File not found");
                                else
                                    Console.WriteLine($"{book.moves.Count:N0} lines in the book");
                                break;
                            case "save":
                                book.Save(uci.GetValue("save"));
                                Console.WriteLine("The book has been saved");
                                break;
                            case "info":
                                book.ShowInfo();
                                break;
                            case "getoption":
                                Console.WriteLine($"option name book_file type string default book{CBook.defExt}");
                                Console.WriteLine($"option name Write type check default false");
                                Console.WriteLine($"option name Log type check default false");
                                Console.WriteLine($"option name Limit read moves type spin default {bookLimitR} min 0 max 100");
                                Console.WriteLine($"option name Limit write moves type spin default {bookLimitW} min 0 max 100");
                                Console.WriteLine($"option name Limit games type string default 1k");
                                Console.WriteLine($"option name Random type spin default {random} min 0 max 10");
                                Console.WriteLine("optionend");
                                break;
                            case "setoption":
                                switch (uci.GetValue("name", "value").ToLower())
                                {
                                    case "book_file":
                                        bookFile = uci.GetValue("value");
                                        break;
                                    case "write":
                                        isW = uci.GetValue("value") == "true";
                                        break;
                                    case "log":
                                        isLog = uci.GetValue("value") == "true";
                                        break;
                                    case "limit read":
                                        bookLimitR = uci.GetInt("value");
                                        break;
                                    case "limit write":
                                        bookLimitW = uci.GetInt("value");
                                        break;
                                    case "limit games":
                                        string limit = uci.GetValue("value");
                                        limit = limit.Replace("k", "000").Replace("m", "000000");
                                        limitGames = int.TryParse(limit, out int lg) ? lg : 0;
                                        break;
                                    case "random":
                                        random = uci.GetInt("value");
                                        break;
                                }
                                break;
                            case "optionend":
                                SetBookFile(bookFile);
                                break;
                            default:
                                Console.WriteLine($"Unknown command [{uci.tokens[1]}]");
                                break;
                        }
                        break;
                    default:
                        done = false;
                        break;
                }
                if (done)
                    continue;
                if ((uci.command != "go") && (engineFile != String.Empty))
                    myProcess.StandardInput.WriteLine(msg);
                switch (uci.command)
                {
                    case "position":
                        lastFen = uci.GetValue("fen", "moves");
                        lastMoves = uci.GetValue("moves", "fen");
                        chess.SetFen(lastFen);
                        chess.MakeMoves(lastMoves);
                        bookRead = false;
                        if (String.IsNullOrEmpty(lastFen))
                        {
                            bookRead = true;
                            if (chess.halfMove < 2)
                                missingIndex = 0;
                            if (isW && chess.Is2ToEnd(out string myMove, out string enMove))
                            {
                                string moves = $"{lastMoves} {myMove} {enMove}";
                                string[] am = moves.Trim().Split();
                                List<string> movesUci = new List<string>(am);
                                int c = bookLimitW > 0 ? bookLimitW : movesUci.Count;
                                if ((movesUci.Count & 1) != (missingIndex & 1))
                                    if ((missingIndex & 1) == 1)
                                        missingIndex++;
                                    else
                                        missingIndex--;
                                if (missingIndex <= c)
                                {
                                    book.AddUci(movesUci.GetRange(0, missingIndex));
                                    book.DeleteMate(limitGames);
                                    book.Save(bookFile);
                                }
                            }
                        }
                        break;
                    case "go":
                        string move = String.Empty;
                        if ((random - 1) >= (chess.halfMove >> 1))
                        {
                            List<int> lm = chess.GenerateValidMoves(out _);
                            int m = lm[CChess.rnd.Next(lm.Count)];
                            move = chess.EmoToUmo(m);
                        }
                        else if (bookRead && ((chess.halfMove < bookLimitR) || (bookLimitR == 0)))
                        {
                            move = book.GetMove(lastMoves);
                            if ((String.IsNullOrEmpty(move)) && (missingIndex == 0))
                                missingIndex = chess.halfMove + 1;
                        }
                        if (move != String.Empty)
                            Console.WriteLine($"bestmove {move}");
                        else if (engineFile == String.Empty)
                            Console.WriteLine("enginemove");
                        else
                            myProcess.StandardInput.WriteLine(msg);
                        break;
                }
            } while (uci.command != "quit");
            teacher.Terminate();

            void LoadFromIni()
            {
                ini.Load();
                Constants.go = ini.Read("go", Constants.go);
                Constants.accuracyEpd = ini.Read("fen", Constants.accuracyEpd);
                Constants.student = ini.Read("student", Constants.student);
                Constants.studentArg = ini.Read("student>arg", Constants.studentArg);
                Constants.teacher = ini.Read("teacher", Constants.teacher);
                Constants.evalGo = ini.Read("test>go", Constants.evalGo);
                Constants.evalEpd = ini.Read("test>fen", Constants.evalEpd);
                Constants.testGo = ini.Read("test>go", Constants.testGo);
                Constants.testEpd = ini.Read("test>fen", Constants.testEpd);
                Constants.command = ini.Read("command", Constants.command);
                Constants.limit = ini.ReadInt("limit", Constants.limit);
                if (!File.Exists(Constants.student))
                    Constants.studentArg=string.Empty;
            }

            void SavetoIni()
            {
                ini.Write("go", Constants.go);
                ini.Save();
            }

            bool SetBookFile(string bn)
            {
                bookFile = bn;
                bookLoaded = book.LoadFromFile(bookFile);
                if (bookLoaded)
                {
                    if ((book.moves.Count > 0) && File.Exists(book.path))
                    {
                        FileInfo fi = new FileInfo(book.path);
                        long mpg = (fi.Length / 5) / book.moves.Count;
                        Console.WriteLine($"info string book on {book.moves.Count:N0} games ({mpg} moves per game)");
                    }
                    if (isW)
                        Console.WriteLine($"info string write on");
                    if (isInfo)
                        book.ShowInfo();
                }
                else
                    isW = false;
                return bookLoaded;
            }
        }

    }
}
