using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		public static string accuracyGo = "go nodes 1000000";
		public static string accuracyFen = "accuracy fen.txt";
		public static string testGo = "go nodes 1000000";
		public static string testFen = "test fen.txt";
		public static string teacher = "teacher.exe";
		public static string student = "student.exe";
		public static string command = String.Empty;
	}
}
