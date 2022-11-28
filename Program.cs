using NSUci;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using RapIni;

namespace NSProgram
{
	static class Program
	{

		public static CChessExt chess = new CChessExt();
		public static CAccuracy accuracy = new CAccuracy();
		public static CTest test = new CTest();
		public static CTeacher teacher = new CTeacher();
		public static CBook book = new CBook();
		public static CUci uci = new CUci();
		public static CRapIni ini = new CRapIni();
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

		static void Main(string[] args)
		{
			_handler += new EventHandler(Handler);
			SetConsoleCtrlHandler(_handler, true);
			if (args.Length == 0)
			{
				Constants.accuracyGo = ini.Read("accurac>go", Constants.accuracyGo);
				Constants.accuracyFen = ini.Read("accuracy>fen", Constants.accuracyFen);
				Constants.testGo = ini.Read("test>go", Constants.testGo);
				Constants.testFen = ini.Read("test>fen", Constants.testFen);
				Constants.teacher = ini.Read("teacher", Constants.teacher);
				Constants.student = ini.Read("student", Constants.student);
				Constants.command = ini.Read("command", Constants.command);
			}
			accuracy.LoadFen();
			test.LoadFen();
			int missingIndex = 0;
			bool isW = false;
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
				int minD = accuracy.fenList.GetMinDepth();
				int proD = accuracy.fenList.GetProDepth(minD);
				int nb = accuracy.fenList.Count - accuracy.fenList.CountBlunders();
				Console.WriteLine($"info string accuracy on {accuracy.fenList.Count} fens depth {minD} ({proD}%) delete {nb} moves {accuracy.fenList.CountMoves():N0}");
			}
			if (test.fenList.Count > 0)
				Console.WriteLine($"info string test on {test.fenList.Count:N0} fens");
			if (!book.Load(bookFile))
				if (!book.Load($"{bookFile}.uci"))
					if (!book.Load($"{bookFile}.pgn"))
						book.Load($"{bookFile}{CBook.defExt}");
			if (File.Exists(Constants.teacher))
				Console.WriteLine("info string teacher on");
			if (File.Exists(Constants.student))
				Console.WriteLine("info string student on");
			if (book.moves.Count > 0)
				Console.WriteLine($"info string book on {book.moves.Count:N0} lines");
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
					continue;
				}
				uci.SetMsg(msg);
				int count = book.moves.Count;
				if (uci.command == "accuracy")
				{
					switch (uci.tokens[1])
					{
						case "start":
							if (accuracy.fenList.Count == 0)
							{
								Console.WriteLine("file \"accuracy fen.txt\" unavabile");
								break;
							}
							Constants.maxTest = uci.GetInt(2, accuracy.fenList.Count);
							teacher.AccuracyStart();
							break;
						case "update":
							Constants.minDepth = uci.GetInt(2, Constants.minDepth);
							teacher.AccuracyUpdate();
							break;
						case "delete":
							accuracy.fenList.DeleteNoBlunders();
							break;
					}
				}
				if (uci.command == "test")
				{
					switch (uci.tokens[1])
					{
						case "start":
							if (test.fenList.Count == 0)
							{
								Console.WriteLine("file \"test fen.txt\" unavabile");
								break;
							}
							Constants.maxTest = uci.GetInt(2, accuracy.fenList.Count);
							teacher.TestStart();
							break;
					}
				}
				if (uci.command == "book")
				{
					switch (uci.tokens[1])
					{
						case "addfile":
							if (!book.AddFile(uci.GetValue(2, 0)))
								Console.WriteLine("File not found");
							else
								Console.WriteLine($"{(book.moves.Count - count):N0} lines have been added");
							break;
						case "adduci":
							book.moves.Add(uci.GetValue(2, 0));
							Console.WriteLine($"{(book.moves.Count - count):N0} lines have been added");
							break;
						case "clear":
							book.Clear();
							Console.WriteLine("Book is empty");
							break;
						case "delete":
							int c = book.Delete(uci.GetInt(2));
							Console.WriteLine($"{c:N0} moves was deleted");
							break;
						case "load":
							if (!book.Load(uci.GetValue(2, 0)))
								Console.WriteLine("File not found");
							else
								Console.WriteLine($"{book.moves.Count:N0} lines in the book");
							break;
						case "save":
							book.Save(uci.GetValue(2, 0));
							Console.WriteLine("The book has been saved");
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
		}
	}
}
