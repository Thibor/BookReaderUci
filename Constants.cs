using RapIni;
using System;
using System.IO;

namespace NSProgram
{
	internal class Constants
	{
		public static int minDepth = 16;
		public static int limit = 0;
		public static int inaccuracy = 10;
		public static int mistake = 20;
		public static int blunder = 30;
		public static double minAcc = 54;
		public static double maxAcc = 97;
		public static double maxElo = 3950;
		public static short CHECKMATE_MAX = short.MaxValue;
		public static short CHECKMATE_NEAR = 0x7000;
        public static string bookFile = "book.uci";
		public static string go = "go movetime 1000";
		public static string accuracyEpd = "accuracy.epd";
        public static string evalEpd = "evaluation.epd";
        public static string testEpd = "test.epd";
        public static string modEpd = "mod.epd";
        public static string evalGo = "eval";
		public static string testGo = "go movetime 1000";
		public static string teacher = "teacher.exe";
		public static string student = "student.exe";
        public static string studentArg = String.Empty;
        public static string command = String.Empty;

        public static void LoadFromIni(CRapIni ini)
        {
            ini.Load();
            go = ini.Read("go",go);
            accuracyEpd = ini.Read("accuracy>epd",accuracyEpd);
            bookFile = ini.Read("book", bookFile);
            student = ini.Read("student", student);
            studentArg = ini.Read("student>arg", studentArg);
            teacher = ini.Read("teacher", teacher);
            evalGo = ini.Read("eval>go", evalGo);
            evalEpd = ini.Read("eval>epd", evalEpd);
            testGo = ini.Read("test>go", testGo);
            testEpd = ini.Read("test>epd", testEpd);
            command = ini.Read("command",command);
            limit = ini.ReadInt("limit",limit);
            if (!File.Exists(student))
                studentArg = string.Empty;
        }

        public static void SavetoIni(CRapIni ini)
        {
            ini.Write("book", bookFile);
            ini.Write("go", go);
            ini.Write("teacher",teacher);
            ini.Write("student",student);
            ini.Save();
        }

    }

    internal class CHeader
    {
        public const string name = "BookReaderUci";
        public const string version = "2024-12-11";
        public const string extension = "uci";
    }

    }
