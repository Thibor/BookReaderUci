using NSUci;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NSProgram
{
	class CTData
	{
		public bool finished = true;
		public bool stop = false;
		public string moves = String.Empty;
		public short score = 0;
		public string best = String.Empty;
		public MSLine line = new MSLine();

		public CTData(bool finished)
		{
			this.finished = finished;
		}

		public void Assign(CTData td)
		{
			finished = td.finished;
			stop = td.stop;
			moves = td.moves;
			score = td.score;
			best = td.best;
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
		public List<string> students = new List<string>();
		public List<string> teachers = new List<string>();
		public List<string> history = new List<string>();
		public List<string> report = new List<string>();

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

		public void FillTeachers()
		{
			teachers.Clear();
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
			students.Clear();
			if (Directory.Exists("Students"))
			{
				string[] filePaths = Directory.GetFiles("Students", "*.exe");
				for (int n = 0; n < filePaths.Length; n++)
				{
					string fn = Path.GetFileName(filePaths[n]);
					students.Add(fn);
				}
			}
		}

		void TeacherDataReceived(object sender, DataReceivedEventArgs e)
		{
			try
			{
				if (!String.IsNullOrEmpty(e.Data))
				{
					//Console.WriteLine(e.Data);
					uci.SetMsg(e.Data);
					if (uci.command == "bestmove")
					{
						CTData td = GetTData();
						uci.GetValue("bestmove", out td.best);
						td.finished = true;
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
						td.score = (short)v;
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
						td.score = (short)v;
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

		void AccuracyStart(string student)
		{
			WriteLine(student);
			if (!SetStudent($@"Students\{student}"))
			{
				WriteLine($"{student} not avabile");
				return;
			}
			StudentAccuracyStart();
			report.Add($"{student} {Program.accuracy.GetAccuracy():N2}");
			report.Add(Program.accuracy.bstFen);
			report.Add($"{Program.accuracy.bstMsg} ({Program.accuracy.bstSb} => {Program.accuracy.bstSc})");
			StudentTerminate();
		}

		void TestStart(string student)
		{
			WriteLine(student);
			if (!SetStudent($@"Students\{student}"))
			{
				WriteLine($"{student} not avabile");
				return;
			}
			StudentTestStart();
			report.Add($"{student} ok {Program.test.resultOk} fail {Program.test.resultFail}");
			StudentTerminate();
		}

		bool PrepareStudents()
		{
			if (!Directory.Exists("Students"))
			{
				Console.WriteLine("Please create directory Students");
				return false;
			}
			FillStudents();
			if (students.Count == 0)
			{
				Console.WriteLine("No engines in Students directory");
				return false;
			}
			return true;
		}

		public void AccuracyStart()
		{
			if (!PrepareStudents())
				return;
			history.Clear();
			report.Clear();
			Program.accuracy.fenList.SortRandom();
			foreach (string student in students)
				AccuracyStart(student);
			WriteLine("finish");
			File.WriteAllLines("history.txt", history);
			File.WriteAllLines("report accuracy.txt", report);
		}

		public void TestStart()
		{
			File.WriteAllLines("fen list.txt", Program.test.GetFens());
			report.Clear();
			if (!PrepareStudents())
				return;
			foreach (string student in students)
				TestStart(student);
			WriteLine("finish");
			File.WriteAllLines("report test.txt", report);
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
				if (!tdg.finished)
					continue;
				if (tdg.best == String.Empty)
					return null;
				line.AddRec(new MSRec(tdg.best, tdg.score));
				int first = line.First().score;
				int last = line.Last().score;
				if (first - last < Constants.minScore)
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
							tds.finished = false;
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

		public void StudentAccuracyStart()
		{
			CTData tds = new CTData(true);
			SetTData(tds);
			int index = 0;
			Program.accuracy.Reset();
			while (true)
			{
				CTData tdg = GetTData();
				if (!tdg.finished)
					continue;
				if (tdg.best != String.Empty)
				{
					int best = tdg.line.First().score;
					int score = tdg.line.GetScore(tdg.best);
					int delta = best - score;
					string msg = $"move {tdg.best} best {tdg.line.First().move} delta {delta}";
					Program.accuracy.Add(tdg.line.fen, msg, best, score);
					WriteLine($"{msg} accuracy {Program.accuracy.GetAccuracy():N2}");
					if (index >= Constants.maxTest)
						return;
				}
				MSLine line = Program.accuracy.fenList[index];
				if ((line.depth < Constants.minDepth) && teacherEnabled)
					line = TeacherStart(line.fen);
				tds = new CTData(false);
				tds.line.Assign(line);
				SetTData(tds);
				StudentWriteLine("ucinewgame");
				StudentWriteLine($"position fen {tds.line.fen}");
				StudentWriteLine("go movetime 1000");
				WriteLine($"{++index} {tds.line.fen}");
			}
		}

		public void StudentTestStart()
		{
			CTData tds = new CTData(true);
			SetTData(tds);
			Program.test.Reset();
			while (true)
			{
				CTData tdg = GetTData();
				if (!tdg.finished)
					continue;
				if (tdg.best != String.Empty)
				{
					Program.test.SetResult(tdg.best);
					Console.WriteLine($"ok {Program.test.resultOk} fail {Program.test.resultFail}");
					if (!Program.test.Next())
						return;
				}
				tds = new CTData(false);
				tds.line.fen = Program.test.Fen;
				SetTData(tds);
				StudentWriteLine("ucinewgame");
				StudentWriteLine($"position fen {tds.line.fen}");
				StudentWriteLine("go movetime 1000");
				WriteLine($"{Program.test.number} {tds.line.fen}");
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

		public void UpdateStart()
		{
			int index = 0;
			while (true)
			{
				CTData tdg = GetTData();
				if (!tdg.finished)
					continue;
				if (tdg.best != String.Empty)
				{
					tdg.line.AddRec(new MSRec(tdg.best, tdg.score));
					SetTData(tdg);
					if ((tdg.line.First().score - tdg.line.Last().score < Constants.minScore))
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
				Console.WriteLine($"{++index} {tds.line.fen}");
			}
			Console.WriteLine("finish");
		}

	}

}
