using NSUci;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using RapLog;
using System.Globalization;
using System.Threading;
using System.Linq;

namespace NSProgram
{
    class CTData
    {
        public bool prepared = false;
        public bool stop = false;
        public bool done = false;
        public string moves = string.Empty;
        public int bestScore = 0;
        public string bestMove = string.Empty;
        public MSLine line = new MSLine();

        public void Assign(CTData td)
        {
            prepared = td.prepared;
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
        readonly CTData tData = new CTData();
        Process teacherProcess = new Process();
        Process studentProcess = new Process();
        readonly CUci uci = new CUci();
        public readonly CMod mod = new CMod();
        public List<string> students = new List<string>();
        public List<string> teachers = new List<string>();
        public CRapLog evaluationReport = new CRapLog("evaluation.log");
        public CRapLog testReport = new CRapLog("test.log");

        public CTData GetTData()
        {
            CTData td = new CTData();
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

                    if (uci.command == "score")
                    {
                        CTData td = GetTData();
                        td.done = true;
                        td.bestScore = uci.GetInt("score");
                        SetTData(td);
                        return;
                    }

                    if (uci.command == "bestmove")
                    {
                        CTData td = GetTData();
                        uci.GetValue("bestmove", out td.bestMove);
                        td.prepared = true;
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
            {
                teacherProcess.StandardInput.WriteLine(c);
                Thread.Sleep(10);
            }
        }

        void StudentWriteLine(string c)
        {
            if (studentProcess.StartInfo.FileName != String.Empty)
            {
                studentProcess.StandardInput.WriteLine(c);
                Thread.Sleep(10);
            }
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
                studentProcess.StartInfo.Arguments = Constants.studentArg;
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

        bool AccuracyUpdatePrepare(CTData tds)
        {
            Program.chess.SetFen(tds.line.fen);
            List<int> listEmo = Program.chess.GenerateLegalMoves(out bool mate);
            if (mate)
            {
                Program.accuracy.DeleteFen(tds.line.fen);
                return false;
            }
            List<string> listUci = new List<string>();
            foreach (int emo in listEmo)
            {
                string u = Program.chess.EmoToUmo(emo);
                if (!tds.line.MoveExists(u))
                    listUci.Add(u);
            }
            string moves = String.Join(" ", listUci.ToArray());
            if (!String.IsNullOrEmpty(moves))
            {
                tds.prepared = true;
                tds.done = false;
                SetTData(tds);
                TeacherWriteLine($"ucinewgame");
                TeacherWriteLine($"position fen {tds.line.fen}");
                TeacherWriteLine($"go depth {tds.line.depth} searchmoves {moves}");
                return true;
            }
            return false;
        }

        public void AccuracyUpdate()
        {
            int fail = Program.accuracy.CountFail();
            if (!PrepareTeachers())
                return;
            SetTeacher();
            while (true)
            {
                CTData tdg = GetTData();
                if (tdg.prepared && !tdg.done)
                    continue;
                if (tdg.done && !string.IsNullOrEmpty(tdg.bestMove))
                {
                    tdg.line.AddRec(new MSRec(tdg.bestMove, tdg.bestScore));
                    Console.WriteLine($"{tdg.bestMove} score {tdg.bestScore} loss {tdg.line.GetLoss():N2}");
                    SetTData(tdg);
                    if (tdg.line.Fail())
                        if (AccuracyUpdatePrepare(tdg))
                            continue;
                    if (tdg.line.Fail())
                    {
                        Program.accuracy.DeleteFen(tdg.line.fen);
                        Console.WriteLine($"fen deleted {tdg.line.GetAccuracy():N2} >> {Constants.blunder}");
                    }
                    else
                        Program.accuracy.ReplaceLine(tdg.line);
                    Program.accuracy.SaveToEpd();
                }
                Program.accuracy.SortDepth();
                MSLine msl = fail > 0 ? Program.accuracy.GetLineFail() : Program.accuracy.GetShallowLine();
                if ((msl == null) || ((fail == 0) && (msl.depth >= Constants.minDepth)))
                    break;
                CTData tds = new CTData() { prepared = true };
                tds.line.fen = msl.fen;
                tds.line.depth = Constants.minDepth;
                int c = fail > 0 ? Program.accuracy.CountFail() : Program.accuracy.CountShallowLine();
                Console.WriteLine($"{c} depth {msl.depth} >> {Constants.minDepth} fen {msl.fen}");
                AccuracyUpdatePrepare(tds);
            }
            Console.Beep();
            Console.WriteLine("finish");
        }

        void AccuracyLine()
        {
            double accuracy = Program.accuracy.GetAccuracy();
            double progress = Program.accuracy.GetProgress();
            ConsoleWrite($"\rprogress {progress:N2}% accuracy {accuracy:N2}% blunders {Program.accuracy.blunders} mistakes {Program.accuracy.mistakes} inaccuracies {Program.accuracy.inaccuracies} total {Program.accuracy.index}");
        }

        public double AccuracyStudent()
        {
            Program.accuracy.Prolog();
            SetTData(new CTData());
            while (true)
            {
                CTData tdg = GetTData();
                if (tdg.prepared && !tdg.done)
                    continue;
                if (tdg.prepared && tdg.done)
                {
                    Program.accuracy.AddScore(tdg.line.fen, tdg.bestMove);
                    AccuracyLine();
                }
                CTData tds = new CTData
                {
                    prepared = true
                };
                if (!Program.accuracy.NextLine(out tds.line))
                    break;
                if (tds.line.depth < Constants.minDepth)
                    continue;
                SetTData(tds);
                Program.accuracy.his.Add(tds.line.fen);
                StudentWriteLine("ucinewgame");
                StudentWriteLine($"position fen {tds.line.fen}");
                StudentWriteLine(Constants.go);
            }
            Console.WriteLine();
            return Program.accuracy.GetTotalGain();
        }

        void AccuracyStart(string student)
        {
            string name = Path.GetFileNameWithoutExtension(student);
            Console.WriteLine(name);
            if (!SetStudent(student))
            {
                Console.WriteLine($"{student} not avabile");
                return;
            }
            Program.accuracy.his.Add($"start {name}");
            AccuracyStudent();
            int winChanceSou = Convert.ToInt32(MSLine.WiningChances(Program.accuracy.badFen.bstScore) * 100.0);
            int winChanceDes = Convert.ToInt32(MSLine.WiningChances(Program.accuracy.badFen.badScore) * 100.0);
            double accuracy = Program.accuracy.GetAccuracy();
            Program.accuracy.log.Add($"{name} accuracy {accuracy:N2}% {Program.accuracy.blunders} {Program.accuracy.mistakes} {Program.accuracy.index} {Program.accuracy.Count} {Program.accuracy.badFen.fen} ({Program.accuracy.badFen.bstMove} => {Program.accuracy.badFen.badMove}) ({Program.accuracy.badFen.bstScore} => {Program.accuracy.badFen.badScore}) ({winChanceSou} => {winChanceDes})");
            StudentTerminate();
        }

        public void AccuracyStart()
        {
            if (!PrepareStudents())
                return;
            int count = Program.accuracy.Count;
            foreach (string student in students)
                AccuracyStart(student);
            int del = count - Program.accuracy.Count;
            if (del > 0)
            {
                Program.accuracy.SaveToEpd();
                Console.WriteLine($"deleted {del}");
            }
            List<string> list = Program.accuracy.log.List();
            count = 0;
            foreach (string l in list)
            {
                uci.SetMsg(l);
                Console.WriteLine(uci.GetValue(0, 8));
                if (++count > 8)
                    break;
            }
            Console.WriteLine("finish");
            Console.Beep();
        }

        #endregion accuracy

        #region mod

        void RenderLineMod()
        {
            double margin = Program.accuracy.GetMargin();
            double accuracy = Program.accuracy.GetAccuracy();
            double progress = Program.accuracy.GetProgress();
            ConsoleWrite($"\rprogress {progress:N2}% accuracy {accuracy:N2}% margin {margin:N2} blunders {Program.accuracy.blunders} mistakes {Program.accuracy.mistakes} inaccuracies {Program.accuracy.inaccuracies}");
        }

        public void ModStudent()
        {
            SetTData(new CTData());
            while (true)
            {
                CTData tdg = GetTData();
                if (tdg.prepared && !tdg.done)
                    continue;
                if (tdg.prepared && tdg.done)
                {
                    Program.accuracy.AddScore(tdg.line.fen, tdg.bestMove);
                    RenderLineMod();
                }
                if (Program.accuracy.valid && (mod.bstScore > 0) && !Program.accuracy.Procede() && (Program.accuracy.GetTotalGain() < mod.last.Min()))
                    break;
                CTData tds = new CTData
                {
                    prepared = true
                };
                if (!Program.accuracy.NextLine(out tds.line))
                    break;
                if (tds.line.depth < Constants.minDepth)
                    continue;
                SetTData(tds);
                Program.accuracy.his.Add(tds.line.fen);
                StudentWriteLine("ucinewgame");
                StudentWriteLine($"position fen {tds.line.fen}");
                StudentWriteLine(Constants.go);
            }
            Console.WriteLine();
        }

        public void ModStart()
        {
            if (!PrepareStudents())
                return;
            string student = students[0];
            if (!SetStudent(student))
            {
                Console.WriteLine($"{student} not avabile");
                return;
            }
            string name = Path.GetFileNameWithoutExtension(student);
            Console.WriteLine($"{name} ready");
            Console.WriteLine($"factors {mod.optionList.length} {mod.optionList.factor} fens {Program.accuracy.Count} gain {Program.teacher.mod.bstScore:N2}");
            while (true)
            {
                Program.accuracy.Prolog();
                if (Program.teacher.mod.bstScore == 0)
                    mod.optionList.BstToCur();
                SetStudent(student);
                foreach (COption opt in mod.optionList)
                    StudentWriteLine($"setoption name {opt.name} value {opt.cur}");
                Console.WriteLine(mod.optionList.OptionsCur());
                ModStudent();
                if (!mod.SetScore())
                    break;
            }
            Console.Beep();
            Console.WriteLine("finish");
        }

        #endregion mod

        #region test

        void TestLine()
        {
            double progress = Program.test.GetProgress();
            double test = Program.test.GetTest();
            int testOk = Program.test.resultOk;
            int testFail = Program.test.resultFail;
            ConsoleWrite($"\rprogress {progress:N2}% test {test:N2}% success {testOk} fail {testFail}");
        }

        public void TestStudent()
        {
            Program.test.Reset();
            while (true)
            {
                CTData tdg = GetTData();
                if (tdg.prepared && !tdg.done)
                    continue;
                if (tdg.prepared && tdg.done)
                {
                    Program.test.SetResult(tdg.bestMove);
                    TestLine();
                    if (!Program.test.Next())
                        return;
                    if ((Constants.limit > 0) && (Program.test.GetNumber() >= Constants.limit))
                        return;
                }
                CTData tds = new CTData() { prepared = true };
                tds.line.fen = Program.test.CurElement().GetFen();
                SetTData(tds);
                StudentWriteLine("ucinewgame");
                StudentWriteLine($"position fen {tds.line.fen}");
                StudentWriteLine(Constants.testGo);
            }
        }

        void TestStart(string student)
        {
            if (!SetStudent(student))
            {
                Console.WriteLine($"{student} not avabile");
                return;
            }
            string name = Path.GetFileNameWithoutExtension(student);
            Console.WriteLine($"{name} ready");
            Program.test.delete = student == Constants.teacher;
            TestStudent();
            int ok = Program.test.resultOk;
            int fail = Program.test.resultFail;
            double test = Program.test.GetTest();
            testReport.Add($"result {test:N2}% {name} ok {ok} fail {fail}");
            StudentTerminate();
            if (Program.test.delete)
                Program.test.SaveToFile();
        }

        public void TestStart()
        {
            if (!PrepareStudents())
                return;
            foreach (string student in students)
                TestStart(student);
            Console.WriteLine();
            Console.WriteLine("finish");
            Console.Beep();
        }

        #endregion test

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
                if (tdg.prepared && !tdg.done)
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
                CTData tds = new CTData();
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

        double EvaluationStudent()
        {
            Program.evaluation.Reset();
            while (true)
            {
                CTData tdg = GetTData();
                if (tdg.prepared && !tdg.done)
                    continue;
                if (tdg.done)
                {
                    Program.evaluation.AddScore(Program.evaluation.CurElement.eval, tdg.bestScore);
                    if (!Program.evaluation.Next())
                        break;

                }
                CTData tds = new CTData() { prepared = true };
                tds.line.fen = Program.evaluation.CurElement.fen;
                SetTData(tds);
                StudentWriteLine("ucinewgame");
                StudentWriteLine($"position fen {tds.line.fen}");
                StudentWriteLine(Constants.evalGo);
                ConsoleWrite($"progress {Program.evaluation.index * 100.0 / Program.evaluation.Limit:N2}% {Program.evaluation.GetAccuracy():N2}");
            }
            return Program.evaluation.GetAccuracy();
        }

        void EvaluationStart(string student)
        {
            if (!SetStudent(student))
            {
                Console.WriteLine($"{student} not avabile");
                return;
            }
            string name = Path.GetFileNameWithoutExtension(student);
            Console.WriteLine($"{name} ready");
            EvaluationStudent();
            evaluationReport.Add($"loss {Program.evaluation.GetAccuracy():N2} count {Program.evaluation.centyCount} {name}");
            StudentTerminate();
        }

        public void EvaluationStart()
        {
            if (!PrepareStudents())
                return;
            foreach (string student in students)
                EvaluationStart(student);
            Console.WriteLine("finish");
            Console.Beep();
        }

        #endregion evaluation

    }
}
