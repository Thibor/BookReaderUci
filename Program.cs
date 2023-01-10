using NSUci;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using RapIni;
using RapLog;

namespace NSProgram
{
	static class Program
	{
		public static bool isLog = false;
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
				Console.Write($"{ title } [y/n] ");
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
			{
				Constants.accuracyGo = ini.Read("accurac>go", Constants.accuracyGo);
				Constants.accuracyFen = ini.Read("accuracy>fen", Constants.accuracyFen);
				Constants.evalGo = ini.Read("test>go", Constants.evalGo);
				Constants.evalFen = ini.Read("test>fen", Constants.evalFen);
				Constants.testGo = ini.Read("test>go", Constants.testGo);
				Constants.testFen = ini.Read("test>fen", Constants.testFen);
				Constants.teacher = ini.Read("teacher", Constants.teacher);
				Constants.student = ini.Read("student", Constants.student);
				Constants.command = ini.Read("command", Constants.command);
				Constants.limit = ini.ReadInt("limit", Constants.limit);
			}
			accuracy.LoadFen();
			evaluation.LoadFromFile();
			test.LoadFromFile();
			int missingIndex = 0;
			bool isW = false;
			bool isInfo = false;
			bool bookRead = false;
			int bookLimitW = 0;
			int bookLimitR = 0;
			string ax = "-bf";
			List<string> movesUci = new List<string>();
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
					case "-tf"://teacher file
					case "-sf"://student file
					case "-acd"://analysis count depth
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
							case "-w":
								ac = ac.Replace("K", "000").Replace("M", "000000");
								book.maxRecords = int.TryParse(ac, out int m) ? m : 0;
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
			Console.WriteLine($"info string {book.name} ver {book.version}");
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
			if (accuracy.fenList.Count > 0)
			{
				accuracy.fenList.GetDepth(out int minD,out int maxD);
				accuracy.fenList.GetMoves(out int minM, out int maxM);
				Console.WriteLine($"info string accuracy on fens {accuracy.fenList.Count} fail {accuracy.fenList.CountFail()} depth ({minD} - {maxD}) moves ({minM} - {maxM})");
			}

			if (evaluation.Count > 0)
				Console.WriteLine($"info string evaluation on fens {evaluation.Count:N0} fail {evaluation.CountFail()}");
			if (test.Count > 0)
				Console.WriteLine($"info string test on fens {test.Count:N0}");
			bool bookLoaded = SetBookFile(bookFile);
			if (File.Exists(Constants.teacher))
				Console.WriteLine("info string teacher on");
			if (File.Exists(Constants.student))
				Console.WriteLine("info string student on");
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
				if (uci.command == "accuracy")
				{
					switch (uci.tokens[1])
					{
						case "update":
							accuracy.fenList.GetDepth(out int minD, out _);
							minD = uci.GetInt("update", ++minD);
							if (Constants.minDepth < minD)
								Constants.minDepth = minD;
							teacher.AccuracyUpdate();
							break;
						case "delete":
							int cm = accuracy.fenList.CountMoves(out int minM);
							if (Confirm($"Delete {cm} fens"))
							{
								cm = accuracy.fenList.DeleteMoves(minM);
								Console.WriteLine($"{cm} fens deleted");
							}
							break;
						case "start":
							if (accuracy.fenList.Count == 0)
							{
								Console.WriteLine("file \"accuracy fen.txt\" unavabile");
								break;
							}
							Constants.limit = uci.GetInt("start", accuracy.fenList.Count);
							teacher.AccuracyStart();
							break;
						case "mod":
							teacher.AccuracyMod();
							break;
					}
				}
				if (uci.command == "evaluation")
				{
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
						case "mod":
							teacher.EvaluationMod();
							break;
					}
				}
				if (uci.command == "test")
				{
					switch (uci.tokens[1])
					{
						case "start":
							if (test.Count == 0)
							{
								Console.WriteLine("file \"test fen.txt\" unavabile");
								break;
							}
							Constants.limit = uci.GetInt("test", accuracy.fenList.Count);
							teacher.TestStart();
							break;
					}
				}
				if (uci.command == "book")
				{
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
							Console.WriteLine($"option name Book file type string default book{CBook.defExt}");
							Console.WriteLine($"option name Write type check default false");
							Console.WriteLine($"option name Log type check default false");
							Console.WriteLine($"option name Limit read moves type spin default {bookLimitR} min 0 max 100");
							Console.WriteLine($"option name Limit write moves type spin default {bookLimitW} min 0 max 100");
							Console.WriteLine("optionend");
							break;
						case "setoption":
							switch (uci.GetValue("name", "value").ToLower())
							{
								case "book file":
									SetBookFile(uci.GetValue("value"));
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
							}
							break;
						default:
							Console.WriteLine($"Unknown command [{uci.tokens[1]}]");
							break;
					}
					continue;
				}
				if ((uci.command != "go") && (engineFile != String.Empty))
					myProcess.StandardInput.WriteLine(msg);
				switch (uci.command)
				{
					case "position":
						bookRead = false;
						movesUci.Clear();
						chess.SetFen();
						if (uci.GetIndex("fen") < 0)
						{
							bookRead = true;
							int m = uci.GetIndex("moves", uci.tokens.Length);
							for (int n = m + 1; n < uci.tokens.Length; n++)
							{
								string umo = uci.tokens[n];
								movesUci.Add(umo);
								int emo = chess.UmoToEmo(umo);
								chess.MakeMove(emo);
							}
							if (chess.halfMove < 2)
								missingIndex = 0;
							if (isW && chess.Is2ToEnd(out string mm, out string em))
							{
								movesUci.Add(mm);
								movesUci.Add(em);
								int c = bookLimitW > 0 ? bookLimitW : movesUci.Count;
								if (missingIndex <= c)
									book.AddMate(movesUci.GetRange(0, missingIndex));
								book.Delete();
							}
						}
						break;
					case "go":
						string move = String.Empty;
						if (bookRead && ((chess.halfMove < bookLimitR) || (bookLimitR == 0)))
						{
							string moves = String.Join(" ", movesUci);
							move = book.GetMove(moves);
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

			bool SetBookFile(string bn)
			{
				bookFile = bn;
				bookLoaded = book.LoadFromFile(bookFile);
				if (bookLoaded)
				{
					if ((book.moves.Count > 0) && File.Exists(book.path))
					{
						FileInfo fi = new FileInfo(book.path);
						long mpl = (fi.Length / 5) / book.moves.Count;
						Console.WriteLine($"info string book on {book.moves.Count:N0} games {mpl} mpg");
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
