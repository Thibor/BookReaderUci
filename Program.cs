using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NSUci;

namespace NSProgram
{
	static class Program
	{
		static void Main(string[] args)
		{
			int missingIndex = 0;
			CBookUci book = new CBookUci();
			CUci Uci = new CUci();
			CChessExt chess = new CChessExt();
			bool isW = false;
			bool bookRead = false;
			int bookLimitW = 0;
			int bookLimitR = 0;
			string ax = "-bn";
			List<string> movesUci = new List<string>();
			List<string> listBn = new List<string>();
			List<string> listEf = new List<string>();
			List<string> listEa = new List<string>();
			for (int n = 0; n < args.Length; n++)
			{
				string ac = args[n];
				switch (ac)
				{
					case "-bn":
					case "-ef":
					case "-ea":
					case "-lr":
					case "-lw":
						ax = ac;
						break;
					case "-w":
						ax = ac;
						isW = true;
						break;
					default:
						switch (ax)
						{
							case "-bn":
								listBn.Add(ac);
								break;
							case "-ef":
								listEf.Add(ac);
								break;
							case "-ea":
								listEa.Add(ac);
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
			string bookName = String.Join(" ", listBn);
			string engineName = String.Join(" ", listEf);
			string arguments = String.Join(" ", listEa);
			Process myProcess = new Process();
			if (File.Exists(engineName))
			{
				myProcess.StartInfo.FileName = engineName;
				myProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(engineName);
				myProcess.StartInfo.UseShellExecute = false;
				myProcess.StartInfo.RedirectStandardInput = true;
				myProcess.StartInfo.Arguments = arguments;
				myProcess.Start();
			}
			else
			{
				if (engineName != String.Empty)
					Console.WriteLine("info string missing engine");
				engineName = String.Empty;
			}
			if (!book.Load(bookName))
				if (!book.Load($"{bookName}.uci"))
					if (!book.Load($"{bookName}.pgn"))
						book.Load($"{bookName}{CBookUci.defExt}");
			Console.WriteLine($"info string book {book.moves.Count:N0} lines");
			Console.WriteLine($"info string book {book.name} ver {book.version} moves {book.moves.Count:N0}");
			do
			{
				string msg = Console.ReadLine().Trim();
				if (String.IsNullOrEmpty(msg) || (msg == "help") || (msg == "book"))
				{
					Console.WriteLine("book load [filename].[umo|uci|png] - clear and add moves from file");
					Console.WriteLine("book save [filename].[umo|uci|png] - save book to the file");
					Console.WriteLine("book addfile [filename].[umo|uci|png] - add moves to the book from file");
					Console.WriteLine("book adduci [uci] - add moves in uci format to the book");
					Console.WriteLine("book clear - clear all moves from the book");
					continue;
				}
				Uci.SetMsg(msg);
				int count = book.moves.Count;
				if (Uci.command == "book")
				{
					switch (Uci.tokens[1])
					{
						case "addfile":
							if (!book.AddFile(Uci.GetValue(2, 0)))
								Console.WriteLine("File not found");
							else
								Console.WriteLine($"{(book.moves.Count - count):N0} lines have been added");
							break;
						case "adduci":
							book.moves.Add(Uci.GetValue(2, 0));
							Console.WriteLine($"{(book.moves.Count - count):N0} lines have been added");
							break;
						case "clear":
							book.Clear();
							Console.WriteLine("Book is empty");
							break;
						case "delete":
							int c = book.Delete(Uci.GetInt(2));
							Console.WriteLine($"{c:N0} moves was deleted");
							break;
						case "load":
							if (!book.Load(Uci.GetValue(2, 0)))
								Console.WriteLine("File not found");
							else
								Console.WriteLine($"{book.moves.Count:N0} lines in the book");
							break;
						case "save":
							book.Save(Uci.GetValue(2, 0));
							Console.WriteLine("The book has been saved");
							break;
						default:
							Console.WriteLine($"Unknown command [{Uci.tokens[1]}]");
							break;
					}
					continue;
				}
				if ((Uci.command != "go") && (engineName != String.Empty))
					myProcess.StandardInput.WriteLine(msg);
				switch (Uci.command)
				{
					case "position":
						bookRead = false;
						movesUci.Clear();
						chess.SetFen();
						if (Uci.GetIndex("fen") < 0)
						{
							bookRead = true;
							int m = Uci.GetIndex("moves", Uci.tokens.Length);
							for (int n = m + 1; n < Uci.tokens.Length; n++)
							{
								string umo = Uci.tokens[n];
								movesUci.Add(umo);
								int emo = chess.UmoToEmo(umo);
								chess.MakeMove(emo);
							}
							if (chess.g_moveNumber < 2)
								missingIndex = 0;
							if (isW && chess.Is2ToEnd(out string mm, out string em))
							{
								movesUci.Add(mm);
								movesUci.Add(em);
								int c = bookLimitW > 0 ? bookLimitW : movesUci.Count;
								if (missingIndex <= c)
								{
									book.AddUci(movesUci.GetRange(0, missingIndex));
									book.Delete();
									book.Save();
								}
							}
						}
						break;
					case "go":
						string move = String.Empty;
						if (bookRead && ((chess.g_moveNumber < bookLimitR) || (bookLimitR == 0)))
						{
							string moves = String.Join(" ", movesUci);
							move = book.GetMove(moves);
							if ((String.IsNullOrEmpty(move)) && (missingIndex == 0))
								missingIndex = chess.g_moveNumber + 1;
						}
						if (move != String.Empty)
							Console.WriteLine($"bestmove {move}");
						else if (engineName == String.Empty)
							Console.WriteLine("enginemove");
						else
							myProcess.StandardInput.WriteLine(msg);
						break;
				}
			} while (Uci.command != "quit");

		}
	}
}
