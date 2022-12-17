using System;

namespace NSProgram
{
	internal class Constants
	{
		public static int minDepth = 16;
		public static int maxTest = 0;
		public static int inaccuracies = 50;
		public static int mistakes = 100;
		public static int blunders = 300;
		public static short CHECKMATE_MAX = short.MaxValue;
		public static short CHECKMATE_NEAR = 0x7000;
		public static string accuracyGo = "go movetime 1000";
		public static string accuracyFen = "accuracy fen.txt";
		public static string evalGo = "eval";
		public static string evalFen = "evaluation fen.txt";
		public static string testGo = "go movetime 1000";
		public static string testFen = "test fen.txt";
		public static string teacher = "teacher.exe";
		public static string student = "student.exe";
		public static string command = String.Empty;
	}
}
