using System;

namespace NSProgram
{
	internal class Constants
	{
		public static int minDepth = 16;
		public static int limit = 0;
		public static int inaccuracy = 50;
		public static int mistake = 100;
		public static int blunder = 300;
		public static double minAcc = 54;
		public static double maxAcc = 97;
		public static double minElo = 1200;
		public static double maxElo = 3500;
		public static short CHECKMATE_MAX = short.MaxValue;
		public static short CHECKMATE_NEAR = 0x7000;
		public static string accuracyGo = "go movetime 1000";
		public static string accuracyFen = "accuracy.fen";
		public static string evalGo = "eval";
		public static string evalFen = "evaluation.fen";
		public static string testGo = "go movetime 1000";
		public static string testFen = "test.fen";
		public static string teacher = "teacher.exe";
		public static string student = "student.exe";
		public static string command = String.Empty;
	}
}
