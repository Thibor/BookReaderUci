using System;

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
		public static string go = "go movetime 1000";
		public static string accuracyEpd = "accuracy.epd";
        public static string evalEpd = "evaluation.epd";
        public static string testEpd = "test.epd";
        public static string evalGo = "eval";
		public static string testGo = "go movetime 1000";
		public static string teacher = "teacher.exe";
		public static string student = "student.exe";
        public static string studentArg = String.Empty;
        public static string command = String.Empty;
	}
}
