using NSUci;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using RapLog;
using System.Globalization;

namespace NSProgram
{
	class CTData
	{
		public bool ready = false;
		public bool stop = false;
		public bool done = false;
		public string moves = string.Empty;
		public int bestScore = 0;
		public string bestMove = string.Empty;
		public MSLine line = new MSLine();

		public CTData(bool finished)
		{
			this.ready = finished;
		}

		public void Assign(CTData td)
		{
			ready = td.ready;
			stop = td.stop;
			done = td.done;
			moves = td.moves;
			bestScore = td.bestScore;
			bestMove = td.bestMove;
			line.Assign(td.line);
		}
	}

	internal class CTeacher
	{
		public bool teacherEnabled = false;
		public bool studentEnabled = false;
		public bool stoped = false;
		readonly object locker = new object();
		readonly CTData tData = new CTData(true);
		Process teacherProcess = new Process();
		Process studentProcess = new Process();
		readonly CUci uci = new CUci();
		readonly CMod mod = new CMod();
		public List<string> students = new List<string>();
		public List<string> teachers = new List<string>();
		public List<string> history = new List<string>();
		public CRapLog accuracyReport = new CRapLog("accuracy report.log");
		public CRapLog evaluationReport = new CRapLog("evaluation report.log");
		public CRapLog testReport = new CRapLog("test report.log");

		public CTData GetTData()
		{
			CTData td = new CTData(false);
			lock (locker)
			{
				td.Assign(tData);
			}
			return td;
		}

		public void SetTData(CTData td)
		{
			lock (locker)
			{
				tData.Assign(td);
			}
		}

		void ConsoleWrite(string s)
		{
			Console.Write($"\r{s}{new string(' ', Console.WindowWidth - s.Length - 1)}");
		}

		void ConsoleWriteLine(string s)
		{
			Console.WriteLine($"\r{s}{new string(' ', Console.WindowWidth - s.Length - 1)}");
		}

		public void Quit()
		{
			TeacherWriteLine("quit");
			StudentWriteLine("quit");
		}

		bool PrepareTeachers()
		{
			teachers.Clear();
			if (File.Exists(Program.teacherFile))
			{
				teachers.Add(Program.teacherFile);
				return true;
			}
			if (File.Exists(Constants.teacher))
			{
				teachers.Add(Constants.teacher);
				return true;
			}
			FillTeachers();
			if (teachers.Count == 0)
			{
				Console.WriteLine("No teachers");
				return false;
			}
			return true;
		}

		bool PrepareStudents()
		{
			students.Clear();
			if (File.Exists(Program.studentFile))
			{
				students.Add(Program.studentFile);
				return true;
			}
			if (File.Exists(Constants.student))
			{
				students.Add(Constants.student);
				return true;
			}
			FillStudents();
			if (students.Count == 0)
			{
				Console.WriteLine("No students");
				return false;
			}
			return true;
		}

		public void FillTeachers()
		{
			if (Directory.Exists("Teachers"))
			{
				string[] filePaths = Directory.GetFiles("Teachers", "*.exe");
				for (int n = 0; n < filePaths.Length; n++)
				{
					string fn = Path.GetFileName(filePaths[n]);
					teachers.Add($@"Teachers\{fn}");
				}
			}
		}

		public void FillStudents()
		{
			if (Directory.Exists("Students"))
			{
				string[] filePaths = Directory.GetFiles("Students", "*.exe");
				Array.Sort(filePaths, StringComparer.CurrentCultureIgnoreCase);
				for (int n = 0; n < filePaths.Length; n++)
				{
					string fn = Path.GetFileName(filePaths[n]);
					students.Add($@"Students\{fn}");
				}
			}
		}

		void TeacherDataReceived(object sender, DataReceivedEventArgs e)
		{
			try
			{
				if (!String.IsNullOrEmpty(e.Data))
				{
					uci.SetMsg(e.Data);
					if (uci.command == "Final")
					{
						double d = 0;
						CTData td = GetTData();
						if (uci.GetValue("evaluation", out string eval))
							d = Convert.ToDouble(eval, CultureInfo.InvariantCulture.NumberFormat) * 100.0;
						td.done = true;
						td.bestScore = Convert.ToInt32(d);
						SetTData(td);
						return;
					}

					if (uci.command == "evaluation")
					{
						CTData td = GetTData();
						td.done = true;
						td.bestScore = uci.GetInt("evaluation");
						SetTData(td);
						return;
					}

					if (uci.command == "bestmove")
					{
						CTData td = GetTData();
						uci.GetValue("bestmove", out td.bestMove);
						td.ready = true;
						td.done = true;
						SetTData(td);
						return;
					}
					if (uci.GetValue("cp", out string value))
					{
						CTData td = GetTData();
						int v = Convert.ToInt32(value);
						if (v > Constants.CHECKMATE_NEAR)
							v = Constants.CHECKMATE_NEAR;
						if (v < -Constants.CHECKMATE_NEAR)
							v = -Constants.CHECKMATE_NEAR;
						td.bestScore = (short)v;
						SetTData(td);
						return;
					}
					if (uci.GetValue("mate", out string mate))
					{
						CTData td = GetTData();
						int v = Convert.ToInt32(mate);
						if (v > 0)
						{
							v = Constants.CHECKMATE_MAX - (v << 1);
							if (v <= Constants.CHECKMATE_NEAR)
								v = Constants.CHECKMATE_NEAR + 1;
						}
						if (v < 0)
						{
							v = -Constants.CHECKMATE_MAX - (v << 1);
							if (v >= -Constants.CHECKMATE_NEAR)
								v = -Constants.CHECKMATE_NEAR - 1;
						}
						td.bestScore = (short)v;
						SetTData(td);
						return;
					};
				}
			}
			catch { }
		}

		void TeacherWriteLine(string c)
		{
			if (teacherProcess.StartInfo.FileName != String.Empty)
				teacherProcess.StandardInput.WriteLine(c);
		}

		void StudentWriteLine(string c)
		{
			if (studentProcess.StartInfo.FileName != String.Empty)
				studentProcess.StandardInput.WriteLine(c);
		}

		public void TeacherTerminate()
		{
			try
			{
				if (teacherProcess.StartInfo.FileName != String.Empty)
				{
					teacherProcess.OutputDataReceived -= TeacherDataReceived;
					teacherProcess.Kill();
					teacherProcess.StartInfo.FileName = String.Empty;
				}
			}
			catch { }
		}

		public void StudentTerminate()
		{
			try
			{
				if (studentProcess.StartInfo.FileName != String.Empty)
				{
					studentProcess.OutputDataReceived -= TeacherDataReceived;
					studentProcess.Kill();
					studentProcess.StartInfo.FileName = String.Empty;
				}
			}
			catch { }
		}

		public void Terminate()
		{
			StudentTerminate();
			TeacherTerminate();
		}

		void WriteLine(string msg)
		{
			Console.WriteLine(msg);
			history.Add(msg);
		}

		MSLine TeacherStart(string fen)
		{
			MSLine line = new MSLine();
			line.fen = fen;
			line.depth = Constants.minDepth;
			CTData tds = new CTData(false);
			tds.line.Assign(line);
			SetTData(tds);
			TeacherWriteLine("ucinewgame");
			TeacherWriteLine($"position fen {line.fen}");
			TeacherWriteLine($"go depth {line.depth}");
			while (true)
			{
				CTData tdg = GetTData();
				if (!tdg.ready)
					continue;
				if (tdg.bestMove == String.Empty)
					return null;
				line.AddRec(new MSRec(tdg.bestMove, tdg.bestScore));
				int first = line.First().score;
				int last = line.Last().score;
				if (first - last < Constants.blunders)
				{
					Program.chess.SetFen(line.fen);
					List<int> listEmo = Program.chess.GenerateValidMoves(out bool mate);
					if (mate)
						Program.accuracy.fenList.DeleteFen(line.fen);
					else
					{
						List<string> listUci = new List<string>();
						foreach (int emo in listEmo)
						{
							string u = Program.chess.EmoToUmo(emo);
							if (!line.MoveExists(u))
								listUci.Add(u);
						}
						string moves = String.Join(" ", listUci.ToArray());
						if (!String.IsNullOrEmpty(moves))
						{
							tds.ready = false;
							tds.line.Assign(line);
							SetTData(tds);
							TeacherWriteLine($"position fen {line.fen}");
							TeacherWriteLine($"go depth {line.depth} searchmoves {moves}");
							continue;
						}
					}
				}
				Program.accuracy.fenList.AddLine(line);
				return line;
			}
		}

		public void Stop()
		{
			stoped = true;
			TeacherWriteLine("stop");
		}

		public bool SetTeacher(string teacherFile)
		{
			TeacherTerminate();
			teacherEnabled = false;
			if (File.Exists(teacherFile))
			{
				teacherProcess = new Process();
				teacherProcess.StartInfo.FileName = teacherFile;
				teacherProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(teacherFile);
				teacherProcess.StartInfo.CreateNoWindow = true;
				teacherProcess.StartInfo.RedirectStandardInput = true;
				teacherProcess.StartInfo.RedirectStandardOutput = true;
				teacherProcess.StartInfo.RedirectStandardError = true;
				teacherProcess.StartInfo.UseShellExecute = false;
				teacherProcess.OutputDataReceived += TeacherDataReceived;
				teacherProcess.Start();
				teacherProcess.BeginOutputReadLine();
				teacherProcess.PriorityClass = ProcessPriorityClass.Idle;
				TeacherWriteLine("uci");
				TeacherWriteLine("isready");
				TeacherWriteLine("ucinewgame");
				teacherEnabled = true;
				stoped = false;
			}
			return teacherEnabled;
		}

		public bool SetStudent(string studentFile)
		{
			StudentTerminate();
			studentEnabled = false;
			if (File.Exists(studentFile))
			{
				studentProcess = new Process();
				studentProcess.StartInfo.FileName = studentFile;
				studentProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(studentFile);
				studentProcess.StartInfo.CreateNoWindow = true;
				studentProcess.StartInfo.RedirectStandardInput = true;
				studentProcess.StartInfo.RedirectStandardOutput = true;
				studentProcess.StartInfo.RedirectStandardError = true;
				studentProcess.StartInfo.UseShellExecute = false;
				studentProcess.OutputDataReceived += TeacherDataReceived;
				studentProcess.EnableRaisingEvents = true;
				studentProcess.Start();
				studentProcess.BeginOutputReadLine();
				StudentWriteLine("uci");
				StudentWriteLine("isready");
				StudentWriteLine("ucinewgame");
				studentEnabled = true;
				stoped = false;
			}
			return studentEnabled;
		}

		public bool SetTeacher()
		{
			FillTeachers();
			if (teachers.Count > 0)
				return SetTeacher(teachers[0]);
			return false;
		}

		#region accuracy

		public void AccuracyUpdate()
		{
			if (!PrepareTeachers())
				return;
			SetTeacher();
			int index = 0;
			while (true)
			{
				CTData tdg = GetTData();
				if (!tdg.ready)
					continue;
				if (tdg.bestMove != String.Empty)
				{
					tdg.line.AddRec(new MSRec(tdg.bestMove, tdg.bestScore));
					SetTData(tdg);
					if (tdg.line.GetLoss() < Constants.blunders)
					{
						Program.chess.SetFen(tdg.line.fen);
						List<int> listEmo = Program.chess.GenerateValidMoves(out bool mate);
						if (mate)
							Program.accuracy.fenList.DeleteFen(tdg.line.fen);
						else
						{
							List<string> listUci = new List<string>();
							foreach (int emo in listEmo)
							{
								string u = Program.chess.EmoToUmo(emo);
								if (!tdg.line.MoveExists(u))
									listUci.Add(u);
							}
							string moves = String.Join(" ", listUci.ToArray());
							if (!String.IsNullOrEmpty(moves))
							{
								TeacherWriteLine($"position fen {tdg.line.fen}");
								TeacherWriteLine($"go depth {tdg.line.depth} searchmoves {moves}");
								continue;
							}
						}
					}
					Program.accuracy.fenList.AddLine(tdg.line);
					Program.accuracy.fenList.SaveToFile();
				}
				Program.accuracy.fenList.SortDepth();
				MSLine line = Program.accuracy.fenList.GetShallowLine();
				if ((line == null) || (line.depth >= Constants.minDepth))
					break;
				CTData tds = new CTData(false);
				tds.line.fen = line.fen;
				tds.line.depth = Constants.minDepth;
				SetTData(tds);
				TeacherWriteLine("ucinewgame");
				TeacherWriteLine($"position fen {tds.line.fen}");
				TeacherWriteLine($"go depth {tds.line.depth}");
				Console.WriteLine($"{++index} depth {line.depth} fen {line.fen}");
			}
			Console.WriteLine("finish");
		}

		public void AccuracyStart()
		{
			if (!PrepareStudents())
				return;
			int count = Program.accuracy.fenList.Count;
			history.Clear();
			Program.accuracy.fenList.SortRandom();
			foreach (string student in students)
				AccuracyStart(student);
			int del = count - Program.accuracy.fenList.Count;
			WriteLine($"deleted {del}");
			WriteLine("finish");
			Console.Beep();
			File.WriteAllLines("accuracy history.txt", history);
		}

		void AccuracyStart(string student)
		{
			string name = Path.GetFileNameWithoutExtension(student);
			WriteLine(name);
			if (!SetStudent(student))
			{
				WriteLine($"{student} not avabile");
				return;
			}
			AccuracyStudent();
			int winChanceSou = Convert.ToInt32(Program.accuracy.WinningChances(Program.accuracy.bstSb) * 100.0);
			int winChanceDes = Convert.ToInt32(Program.accuracy.WinningChances(Program.accuracy.bstSc) * 100.0);
			accuracyReport.Add($"loss {Program.accuracy.GetAccuracy():N2} count {Program.accuracy.index} {name} blunders {Program.accuracy.blunders} mistakes {Program.accuracy.mistakes} inaccuracies {Program.accuracy.inaccuracies} {Program.accuracy.bstFen} {Program.accuracy.bstMsg} ({Program.accuracy.bstSb} => {Program.accuracy.bstSc}) ({winChanceSou} => {winChanceDes})");
			StudentTerminate();
		}

		public double AccuracyStudent()
		{
			CTData tds = new CTData(true);
			SetTData(tds);
			Program.accuracy.Reset();
			while (true)
			{
				CTData tdg = GetTData();
				if (!tdg.ready)
					continue;
				if (tdg.bestMove != String.Empty)
				{
					int best = tdg.line.First().score;
					int score = tdg.line.GetScore(tdg.bestMove);
					int delta = best - score;
					string msg = $"move {tdg.bestMove} best {tdg.line.First().move} delta {delta}";
					Program.accuracy.AddScore(tdg.line.fen, msg, best, score);
					if ((Constants.teacher == Constants.student) && ((delta > Constants.mistakes) || (tdg.line.GetLoss() < Constants.blunders)))
					{
						Program.accuracy.index--;
						Program.accuracy.fenList.DeleteFen(tdg.line.fen);
						Program.accuracy.fenList.SaveToFile();
					}
					if ((Constants.limit > 0) && (Program.accuracy.index >= Constants.limit))
						break;
				}
				if (!Program.accuracy.NextLine(out MSLine line))
					break;
				if ((line.depth < Constants.minDepth) && teacherEnabled)
					line = TeacherStart(line.fen);
				tds = new CTData(false);
				tds.line.Assign(line);
				SetTData(tds);
				StudentWriteLine("ucinewgame");
				StudentWriteLine($"position fen {tds.line.fen}");
				StudentWriteLine(Constants.accuracyGo);
				ConsoleWrite($"\rprogress {Program.accuracy.index * 100.0 / Program.accuracy.fenList.Count:N2}% ({Program.accuracy.GetAccuracy():N2})");
			}
			return Program.accuracy.GetAccuracy();
		}

		public void AccuracyMod()
		{
			if (!PrepareStudents())
				return;
			string student = students[0];
			if (!SetStudent(student))
			{
				WriteLine($"{student} not avabile");
				return;
			}
			string mn = string.Empty;
			string name = Path.GetFileNameWithoutExtension(student);
			WriteLine($"{name} ready");
			mod.SetMode("accuracy");
			while (true)
			{
				mod.Start();
				if (mn != mod.ModName)
				{
					mn = mod.ModName;
					int lc = mod.CurList.Count;
					ConsoleWriteLine($"{mn} {lc}");
				}
				double score = AccuracyStudent();
				double result = mod.bstScore - score;
				int del = mod.Del;
				int index = mod.index + 1;
				if (mod.bstScore == 0)
					ConsoleWriteLine($"score {score:N2}");
				else
					ConsoleWriteLine($"score {score:N2} best {mod.bstScore:N2} index {index} delta {del} result {result:N2}");
				if (!mod.SetScore(score))
					break;
			}
			Console.Beep();
			Console.WriteLine("finish");
		}

		#endregion accuracy

		#region evaluation

		public void EvaluationUpdate()
		{
			if (!PrepareTeachers())
				return;
			SetTeacher();
			Program.evaluation.Reset();
			int index = 0;
			while (true)
			{
				CTData tdg = GetTData();
				if (!tdg.ready && !tdg.done)
					continue;
				if (tdg.done)
				{
					if (tdg.bestScore == 0)
						Program.evaluation.DeleteFen(tdg.line.fen);
					else
						Program.evaluation.CurElement.eval = tdg.bestScore;
					if (!Program.evaluation.Next())
						break;
				}
				CElementE e = Program.evaluation.CurElement;
				if (e == null)
					break;
				CTData tds = new CTData(false);
				tds.line.fen = e.fen;
				SetTData(tds);
				TeacherWriteLine("ucinewgame");
				TeacherWriteLine($"position fen {tds.line.fen}");
				TeacherWriteLine(Constants.evalGo);
				Console.WriteLine($"{++index} fen {tds.line.fen}");
				if (index % 10000 == 0)
					Program.evaluation.SaveToFile();
			}
			Program.evaluation.SaveToFile();
			Console.WriteLine("finish");
		}

		public void EvaluationStart()
		{
			if (!PrepareStudents())
				return;
			history.Clear();
			foreach (string student in students)
				EvaluationStart(student);
			WriteLine("finish");
			Console.Beep();
			File.WriteAllLines("evaluation history.txt", history);
		}

		void EvaluationStart(string student)
		{
			string name = Path.GetFileNameWithoutExtension(student);
			WriteLine(student);
			if (!SetStudent(student))
			{
				WriteLine($"{student} not avabile");
				return;
			}
			EvaluationStudent();
			evaluationReport.Add($"loss {Program.evaluation.GetAccuracy():N2} count {Program.evaluation.centyCount} {name}");
			StudentTerminate();
		}

		double EvaluationStudent()
		{
			CTData tds = new CTData(true);
			SetTData(tds);
			Program.evaluation.Reset();
			while (true)
			{
				CTData tdg = GetTData();
				if (!tdg.ready && !tdg.done)
					continue;
				if (tdg.done)
				{
					Program.evaluation.AddScore(Program.evaluation.CurElement.eval, tdg.bestScore);
					if (!Program.evaluation.Next())
						break;

				}
				tds = new CTData(false);
				tds.line.fen = Program.evaluation.CurElement.fen;
				SetTData(tds);
				StudentWriteLine("ucinewgame");
				StudentWriteLine($"position fen {tds.line.fen}");
				StudentWriteLine(Constants.evalGo);
				ConsoleWrite($"progress {Program.evaluation.index * 100.0 / Program.evaluation.Limit:N2}% {Program.evaluation.GetAccuracy():N2}");
			}
			return Program.evaluation.GetAccuracy();
		}

		public void EvaluationMod()
		{
			if (!PrepareStudents())
				return;
			string student = students[0];
			if (!SetStudent(student))
			{
				WriteLine($"{student} not avabile");
				return;
			}
			string mn = string.Empty;
			string name = Path.GetFileNameWithoutExtension(student);
			WriteLine($"{name} ready");
			mod.SetMode("evaluation");
			while (true)
			{
				mod.Start();
				if (mn != mod.ModName)
				{
					mn = mod.ModName;
					int lc = mod.CurList.Count;
					ConsoleWriteLine($"{mn} {lc}");
				}
				double score = EvaluationStudent();
				double result = mod.bstScore - score;
				int del = mod.Del;
				int index = mod.index + 1;
				if (mod.bstScore == 0)
					ConsoleWriteLine($"score {score:N2}");
				else
					ConsoleWriteLine($"score {score:N2} best {mod.bstScore:N2} index {index} delta {del} result {result:N2}");
				if (!mod.SetScore(score))
					break;
			}
			WriteLine("finish");
			Console.Beep();
		}

		#endregion evaluation

		#region test

		public void TestStart()
		{
			if (!PrepareStudents())
				return;
			history.Clear();
			foreach (string student in students)
				TestStart(student);
			WriteLine("finish");
			Console.Beep();
			File.WriteAllLines("test history.txt", history);
		}

		void TestStart(string student)
		{
			string name = Path.GetFileNameWithoutExtension(student);
			WriteLine(student);
			if (!SetStudent(student))
			{
				WriteLine($"{student} not avabile");
				return;
			}
			TestStudent();
			int ok = Program.test.resultOk;
			int fail = Program.test.resultFail;
			double pro = (ok * 100.0) / (ok + fail);
			testReport.Add($"result {pro:N2}% count {Program.test.number:0000} {name} ok {ok} fail {fail}");
			StudentTerminate();
		}

		public void TestStudent()
		{
			CTData tds = new CTData(true);
			SetTData(tds);
			Program.test.Reset();
			while (true)
			{
				CTData tdg = GetTData();
				if (!tdg.ready)
					continue;
				if (tdg.bestMove != String.Empty)
				{
					bool r = Program.test.GetResult(tdg.bestMove);
					Program.test.SetResult(r);
					string sr = r ? "ok" : "fail";
					WriteLine($"{sr} ({Program.test.resultOk} : {Program.test.resultFail})");
					if (!Program.test.Next())
						return;
					if ((Constants.limit > 0) && (Program.test.number >= Constants.limit))
						return;
				}
				tds = new CTData(false);
				tds.line.fen = Program.test.CurElement.Fen;
				SetTData(tds);
				StudentWriteLine("ucinewgame");
				StudentWriteLine($"position fen {tds.line.fen}");
				StudentWriteLine(Constants.testGo);
				WriteLine($"{Program.test.number} {tds.line.fen}");
			}
		}

		#endregion test
	}
}
