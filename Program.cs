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
        public static bool isSmart = true;
        public static CChessExt chess = new CChessExt();
        public static CAccuracyList accuracy = new CAccuracyList();
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
                Constants.LoadFromIni(ini);
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
            if (args.Length == 0)
                bookFile = Constants.bookFile;
            Console.WriteLine($"idbook name {CHeader.name}");
            Console.WriteLine($"idbook version {CHeader.version}");
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
            bool help = false;

            do
            {
                string msg = String.IsNullOrEmpty(Constants.command) ? Console.ReadLine().Trim() : Constants.command;
                Constants.command = String.Empty;
                uci.SetMsg(msg);
                int count = book.Count;
                bool done = true;
                if (help || String.IsNullOrEmpty(msg) || (msg == "help") || (msg == "book"))
                {
                    Console.WriteLine("book       - operations on chess openings book in format uci");
                    Console.WriteLine("accuracy   - evaluate accuracy and elo of chess engine");
                    Console.WriteLine("mod        - modify factors of chess engine");
                    Console.WriteLine("evaluation - evaluation chess positions by chess engine");
                    Console.WriteLine("test       - test chess engine");
                    Console.WriteLine("ini        - create configuration file");
                    help = false;
                    continue;
                }
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
                                        if (Confirm($"Delete {cf} fens?"))
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
                                    teacher.AccuracyStart();
                                    break;
                                case "check":
                                    accuracy.check = true;
                                    teacher.AccuracyStart();
                                    accuracy.check = false;
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
                                case "prepare":
                                    teacher.ModStart(true,false);
                                    break;
                                case "start":
                                    teacher.ModStart(false,false);
                                    break;
                                case "reset":
                                    teacher.mod.Reset();
                                    break;
                                case "best":
                                    teacher.mod.ShowBest();
                                    break;
                                case "confirm":
                                    teacher.ModStart(false,true);
                                    break;
                                case "enabled":
                                    teacher.mod.Enabled(uci.GetValue("enabled") == "on");
                                    Console.WriteLine($"enabled {teacher.mod.optionList.CountEnabled()}");
                                    break;
                                default:
                                    Console.WriteLine($"Unknown command [{uci.tokens[1]}]");
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
                    case "ini":
                        Constants.SavetoIni(ini);
                        break;
                    case "book":
                        help = uci.tokens.Length < 2;
                        if (!help)
                            switch (uci.tokens[1])
                            {
                                case "addfile":
                                    if (!book.AddFile(uci.GetValue("addfile")))
                                        Console.WriteLine("File not found");
                                    else
                                        Console.WriteLine($"{book.Count - count:N0} lines have been added");
                                    break;
                                case "adduci":
                                    book.Add(uci.GetValue("adduci"));
                                    Console.WriteLine($"{book.Count - count:N0} lines have been added");
                                    break;
                                case "clear":
                                    book.Clear();
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
                                        Console.WriteLine($"{book.Count:N0} lines in the book");
                                    break;
                                case "save":
                                    book.Save(uci.GetValue("save"));
                                    Console.WriteLine("The book has been saved");
                                    break;
                                case "sort":
                                    book.Sort();
                                    book.Save();
                                    Console.WriteLine("The book has been sorted");
                                    break;
                                case "moves":
                                    book.InfoMoves(uci.GetValue("moves"));
                                    break;
                                case "info":
                                    book.ShowInfo();
                                    break;
                                case "getoption":
                                    Console.WriteLine($"option name book_file type string default book{CBook.defExt}");
                                    Console.WriteLine($"option name write type check default false");
                                    Console.WriteLine($"option name smart type check default true");
                                    Console.WriteLine($"option name log type check default false");
                                    Console.WriteLine($"option name ply_read type spin default {bookLimitR} min 0 max 100");
                                    Console.WriteLine($"option name ply_write type spin default {bookLimitW} min 0 max 100");
                                    Console.WriteLine($"option name limit_games type string default 1k");
                                    Console.WriteLine($"option name random type spin default {random} min 0 max 10");
                                    Console.WriteLine("optionend");
                                    break;
                                case "setoption":
                                    switch (uci.GetValue("name", "value").ToLower())
                                    {
                                        case "book_file":
                                            SetBookFile(uci.GetValue("value"));
                                            break;
                                        case "write":
                                            isW = uci.GetValue("value") == "true";
                                            break;
                                        case "log":
                                            isLog = uci.GetValue("value") == "true";
                                            break;
                                        case "smart":
                                            isSmart = uci.GetValue("value") == "true";
                                            break;
                                        case "ply_read":
                                            bookLimitR = uci.GetInt("value");
                                            break;
                                        case "ply_write":
                                            bookLimitW = uci.GetInt("value");
                                            break;
                                        case "limit_games":
                                            string limit = uci.GetValue("value");
                                            limit = limit.Replace("k", "000").Replace("m", "000000");
                                            limitGames = int.TryParse(limit, out int lg) ? lg : 0;
                                            break;
                                        case "random":
                                            random = uci.GetInt("value");
                                            break;
                                    }
                                    break;
                                case "help":
                                    Console.WriteLine("book load [filename].[uci|png]    - clear and add moves from file");
                                    Console.WriteLine("book save [filename].[uci|png]    - save book to the file");
                                    Console.WriteLine("book addfile [filename].[uci|png] - add moves to the book from file");
                                    Console.WriteLine("book adduci [uci moves]           - add moves in uci format to the book");
                                    Console.WriteLine("book delete [number x]            - delete x games from the book");
                                    Console.WriteLine("book clear                        - remove all moves from the book");
                                    Console.WriteLine("book sort                         - sort games");
                                    Console.WriteLine("book moves [uci]                  - show possible continuations");
                                    Console.WriteLine("book info                         - show extra informations of current book");
                                    Console.WriteLine("book getoption                    - show options");
                                    Console.WriteLine("book setoption name [option name] value [option value] - set option");
                                    break;
                                default:
                                    Console.WriteLine($"unknown command [{uci.tokens[1]}]");
                                    Console.WriteLine($"book help - show console commands");
                                    break;
                            }
                        break;
                    case "help":
                        help = true;
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
                        //chess.SetFen("rnbq1bnr/pppp1kpp/4pp2/8/3N2P1/8/PPPPPP1P/RNBQKB1R w KQ - 2 4");
                        //Console.WriteLine(chess.GetUmo());
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
                        if (random >= chess.MoveNumber)
                        {
                            if (isSmart)
                                move = chess.GetUmo();
                            else
                            {
                                List<int> lm = chess.GenerateLegalMoves(out _);
                                if (lm.Count > 0)
                                {
                                    int r = CChess.rnd.Next(lm.Count);
                                    move = chess.EmoToUmo(lm[r]);
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(move))
                            if ((bookLimitR == 0) || (bookLimitR >= chess.MoveNumber))
                                if (bookRead)
                                {
                                    move = book.GetMove(lastMoves);
                                    if ((String.IsNullOrEmpty(move)) && (missingIndex == 0))
                                        missingIndex = chess.halfMove + 1;
                                }

                        if (!string.IsNullOrEmpty(move))
                            Console.WriteLine($"bestmove {move}");
                        else if (engineFile == String.Empty)
                            Console.WriteLine("enginemove");
                        else
                            myProcess.StandardInput.WriteLine(msg);
                        break;
                }
            } while (uci.command != "quit");
            teacher.Terminate();

            bool SetBookFile(string bn)
            {
                bookFile = bn;
                bookLoaded = book.LoadFromFile(bookFile);
                if (bookLoaded)
                {
                    if (File.Exists(book.path))
                        Console.WriteLine("info string book on");
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
