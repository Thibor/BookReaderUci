using System;
using System.Collections.Generic;

namespace NSChess
{

	public enum CGameState { normal, mate, stalemate, repetition, move50, material, time, error, resignation }

	class CUndo
	{
		public int captured;
		public int passing;
		public int castle;
		public int move50;
		public int lastCastle;
		public ulong hash;
	}

	public class CChess
	{
		public static CChess This;
		public const string defFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
		public static Random random = new Random();
		public const int piecePawn = 0x01;
		public const int pieceKnight = 0x02;
		public const int pieceBishop = 0x03;
		public const int pieceRook = 0x04;
		public const int pieceQueen = 0x05;
		public const int pieceKing = 0x06;
		public const int colorBlack = 0x08;
		public const int colorWhite = 0x10;
		public const int colorEmpty = 0x20;
		const int moveflagPassing = 0x02 << 16;
		public const int moveflagCastleKing = 0x04 << 16;
		public const int moveflagCastleQueen = 0x08 << 16;
		const int moveflagPromotion = 0xf0 << 16;
		const int moveflagPromoteQueen = 0x10 << 16;
		const int moveflagPromoteRook = 0x20 << 16;
		const int moveflagPromoteBishop = 0x40 << 16;
		const int moveflagPromoteKnight = 0x80 << 16;
		const int maskCastle = moveflagCastleKing | moveflagCastleQueen;
		const int maskColor = colorBlack | colorWhite;
		public int g_castleRights = 0xf;
		ulong g_hash = 0;
		protected int g_passing = 0;
		public int g_move50 = 0;
		public int g_moveNumber = 0;
		public bool g_inCheck = false;
		int g_lastCastle = 0;
		bool adjInsufficient = false;
		int undoIndex = 0;
		readonly ulong[,] g_hashBoard = new ulong[256, 16];
		readonly int[] boardCheck = new int[256];
		readonly int[] boardCastle = new int[256];
		public bool whiteTurn = true;
		int usColor = 0;
		int enColor = 0;
		public static int[] arrField = new int[64];
		public int[] g_board = new int[256];
		readonly int[] arrDirKinght = { 14, -14, 18, -18, 31, -31, 33, -33 };
		readonly int[] arrDirBishop = { 15, -15, 17, -17 };
		readonly int[] arrDirRock = { 1, -1, 16, -16 };
		readonly int[] arrDirQueen = { 1, -1, 15, -15, 16, -16, 17, -17 };
		readonly CUndo[] undoStack = new CUndo[0xfff];

		#region initation

		public CChess()
		{
			This = this;
			Initialize();
		}

		public void Initialize()
		{
			g_hash = RAND_32();
			for (int n = 0; n < undoStack.Length; n++)
				undoStack[n] = new CUndo();
			for (int y = 0; y < 8; y++)
				for (int x = 0; x < 8; x++)
					arrField[y * 8 + x] = (y + 4) * 16 + x + 4;
			for (int n = 0; n < 256; n++)
			{
				boardCheck[n] = 0;
				boardCastle[n] = 15;
				g_board[n] = 0;
				for (int p = 0; p < 16; p++)
					g_hashBoard[n, p] = RAND_32();
			}
			int[] arrCastleI = { 68, 72, 75, 180, 184, 187 };
			int[] arrCasteleV = { 7, 3, 11, 13, 12, 14 };
			int[] arrCheckI = { 71, 72, 73, 183, 184, 185 };
			int[] arrCheckV = { colorBlack | moveflagCastleQueen, colorBlack | maskCastle, colorBlack | moveflagCastleKing, colorWhite | moveflagCastleQueen, colorWhite | maskCastle, colorWhite | moveflagCastleKing };
			for (int n = 0; n < 6; n++)
			{
				boardCastle[arrCastleI[n]] = arrCasteleV[n];
				boardCheck[arrCheckI[n]] = arrCheckV[n];
			}
		}
		#endregion

		#region conversion

		public static int Con256To64(int i)
		{
			int x = (i & 0xf) - 4;
			int y = (i >> 4) - 4;
			return y * 8 + x;
		}

		/// <summary>
		/// Engine MOve TO Uci MOve
		/// </summary>

		public string EmoToUmo(int emo)
		{
			string result = SquareToUmo(emo & 0xFF) + SquareToUmo((emo >> 8) & 0xFF);
			if ((emo & moveflagPromotion) > 0)
			{
				if ((emo & moveflagPromoteQueen) > 0) result += 'q';
				else if ((emo & moveflagPromoteRook) > 0) result += 'r';
				else if ((emo & moveflagPromoteBishop) > 0) result += 'b';
				else result += 'n';
			}
			return result;
		}

		/// <summary>
		/// Uci MOve TO Engine MOve
		/// </summary>

		public int UmoToEmo(string umo)
		{
			List<int> moves = GenerateAllMoves(whiteTurn, false);
			foreach (int m in moves)
				if (EmoToUmo(m) == umo)
					return m;
			return 0;
		}

		/// <summary>
		/// Uci MOve TO SAN move
		/// </summary>

		public string UmoToSan(string umo)
		{
			if (!MakeMove(umo, out int emo))
				return String.Empty;
			CGameState gs = GetGameState(out bool check);
			UnmakeMove(emo);
			string[] arrPiece = { "", "", "N", "B", "R", "Q", "K" };
			int fr = emo & 0xff;
			int to = (emo >> 8) & 0xff;
			int flags = emo & 0xff0000;
			int pieceFr = g_board[fr] & 7;
			int pieceTo = g_board[to] & 7;
			bool isAttack = (pieceTo > 0) || ((flags & moveflagPassing) > 0);
			if ((flags & moveflagCastleKing) > 0)
				return "O-O";
			if ((flags & moveflagCastleQueen) > 0)
				return "O-O-O";
			List<int> moves = GenerateValidMoves(out _);
			bool showRank = false;
			bool showFile = false;
			foreach (int m in moves)
			{
				int f = m & 0xff;
				if (f != fr)
				{
					int piece = g_board[f] & 7;
					if ((piece == pieceFr) && ((m & 0xff00) == (emo & 0xff00)))
					{
						if ((m & 0xf0) != (emo & 0xf0))
							showRank = true;
						if ((m & 0xf) != (emo & 0xf))
							showFile = true;
					}
				}
			}
			if (isAttack && (pieceFr == piecePawn))
				showFile = true;
			if (showFile && showRank)
				showRank = false;
			string faf = showFile ? umo.Substring(0, 1) : String.Empty;
			string far = showRank ? umo.Substring(1, 1) : String.Empty;
			string fb = umo.Substring(2, 2);
			string attack = isAttack ? "x" : "";
			string promo = "";
			if ((flags & moveflagPromoteKnight) > 0)
				promo = "=N";
			if ((flags & moveflagPromoteBishop) > 0)
				promo = "=B";
			if ((flags & moveflagPromoteRook) > 0)
				promo = "=R";
			if ((flags & moveflagPromoteQueen) > 0)
				promo = "=Q";
			string fin = check ? "+" : "";
			if (gs == CGameState.mate)
				fin = "#";
			return $"{arrPiece[pieceFr]}{faf}{far}{attack}{fb}{promo}{fin}";
		}

		/// <summary>
		/// SAN move TO Uci MOve
		/// </summary>

		public string SanToUmo(string san)
		{
			char[] charsToTrim = { '+', '#' };
			san = san.Trim(charsToTrim);
			List<int> moves = GenerateValidMoves(out _);
			foreach (int imo in moves)
			{
				string umo = EmoToUmo(imo);
				if (umo == san)
					return umo;
				if (UmoToSan(umo).Trim(charsToTrim) == san)
					return umo;
			}
			return "";
		}

		public static int UmoToIndex(string emo)
		{
			if (String.IsNullOrEmpty(emo))
				return -1;
			string file = "abcdefgh";
			string rank = "87654321";
			int x = file.IndexOf(emo[0]);
			int y = rank.IndexOf(emo[1]);
			return y * 8 + x;
		}

		public static string IndexToUmo(int index)
		{
			string file = "abcdefgh";
			string rank = "87654321";
			int x = index & 0x7;
			int y = index >> 3;
			return $"{file[x]}{rank[y]}";
		}

		string SquareToUmo(int square)
		{
			int x = (square & 0xf) - 4;
			int y = (square >> 4) - 4;
			if ((x < 0) || (y < 0) || (x > 7) || (y > 7))
				return String.Empty;
			string file = "abcdefgh";
			string rank = "87654321";
			return $"{file[x]}{rank[y]}";
		}

		int UmoToSquare(string s)
		{
			if (s.Length < 2)
				return 0;
			string xs = "abcdefgh";
			string ys = "87654321";
			int x = xs.IndexOf(s[0]);
			int y = ys.IndexOf(s[1]);
			return ((y + 4) << 4) | (x + 4);
		}

		#endregion

		#region fen

		public bool SetFen(string fen = defFen)
		{
			if (fen == "")
				fen = defFen;
			string[] chunks = fen.Split(' ');
			if (chunks.Length < 4)
				return false;
			for (int n = 0; n < 64; n++)
				g_board[arrField[n]] = colorEmpty;
			int row = 0;
			int col = 0;
			string pieces = chunks[0];
			for (int i = 0; i < pieces.Length; i++)
			{
				char c = pieces[i];
				if (c == '/')
				{
					row++;
					col = 0;
				}
				else if (c >= '0' && c <= '9')
				{
					for (int j = 0; j < Int32.Parse(c.ToString()); j++)
						col++;
				}
				else
				{
					int piece = Char.IsUpper(c) ? colorWhite : colorBlack;
					int index = (row + 4) * 16 + col + 4;
					switch (Char.ToLower(c))
					{
						case 'p':
							piece |= piecePawn;
							break;
						case 'n':
							piece |= pieceKnight;
							break;
						case 'b':
							piece |= pieceBishop;
							break;
						case 'r':
							piece |= pieceRook;
							break;
						case 'q':
							piece |= pieceQueen;
							break;
						case 'k':
							piece |= pieceKing;
							break;
						default:
							return false;
					}
					g_board[index] = piece;
					col++;
				}
			}
			if (chunks[1] == "w")
				whiteTurn = true;
			else if (chunks[1] == "b")
				whiteTurn = false;
			else
				return false;
			g_castleRights = 0;
			if (chunks[2].IndexOf('K') != -1)
				g_castleRights |= 1;
			if (chunks[2].IndexOf('Q') != -1)
				g_castleRights |= 2;
			if (chunks[2].IndexOf('k') != -1)
				g_castleRights |= 4;
			if (chunks[2].IndexOf('q') != -1)
				g_castleRights |= 8;
			g_passing = UmoToSquare(chunks[3]);
			g_move50 = chunks.Length < 5 ? 0 : Int32.Parse(chunks[4]);
			g_moveNumber = chunks.Length < 6 ? 1 : Int32.Parse(chunks[5]);
			if (g_moveNumber > 0)
				g_moveNumber--;
			g_moveNumber <<= 1;
			if (!whiteTurn)
				g_moveNumber++;
			undoIndex = g_move50;
			return true;
		}

		public string GetFenBase()
		{
			string result = "";
			string[] arr = { " ", "p", "n", "b", "r", "q", "k", " " };
			for (int y = 0; y < 8; y++)
			{
				if (y != 0)
					result += '/';
				int empty = 0;
				for (int x = 0; x < 8; x++)
				{
					int piece = g_board[((y + 4) << 4) + x + 4];
					if (piece == colorEmpty)
						empty++;
					else
					{
						if (empty != 0)
							result += empty;
						empty = 0;
						string pieceChar = arr[(piece & 0x7)];
						result += ((piece & colorWhite) != 0) ? pieceChar.ToUpper() : pieceChar;
					}
				}
				if (empty != 0)
				{
					result += empty;
				}
			}
			return result;
		}

		public string GetEpd()
		{
			string result = GetFenBase();
			result += whiteTurn ? " w " : " b ";
			if (g_castleRights == 0)
				result += "-";
			else
			{
				if ((g_castleRights & 1) != 0)
					result += 'K';
				if ((g_castleRights & 2) != 0)
					result += 'Q';
				if ((g_castleRights & 4) != 0)
					result += 'k';
				if ((g_castleRights & 8) != 0)
					result += 'q';
			}
			result += ' ';
			if (g_passing == 0)
				result += '-';
			else
			{
				if (whiteTurn)
				{
					if ((g_board[g_passing + 15] == (piecePawn | colorWhite)) || (g_board[g_passing + 17] == (piecePawn | colorWhite)))
						result += SquareToUmo(g_passing);
					else
						result += '-';
				}
				else
				{
					if ((g_board[g_passing - 15] == (piecePawn | colorBlack)) || (g_board[g_passing - 17] == (piecePawn | colorBlack)))
						result += SquareToUmo(g_passing);
					else
						result += '-';
				}
			}
			return result;
		}

		public string GetFen()
		{
			return GetEpd() + ' ' + g_move50 + ' ' + ((g_moveNumber >> 1) + 1);
		}

		#endregion

		#region move generator

		void GenerateMove(List<int> moves, int fr, int to, bool add, int flag)
		{
			if (((g_board[to] & 7) == pieceKing) || (((boardCheck[to] & g_lastCastle) == g_lastCastle) && ((g_lastCastle & maskCastle) > 0)))
				g_inCheck = true;
			if (add)
				moves.Add(fr | (to << 8) | flag);
		}

		public List<int> GenerateValidMoves(out bool mate, bool norep = false)
		{
			mate = false;
			int count = 0;
			List<int> moves = new List<int>(64);
			List<int> am = GenerateAllMoves(whiteTurn, false);
			if (!g_inCheck)
				foreach (int m in am)
				{
					MakeMove(m);
					GenerateAllMoves(whiteTurn, true);
					if (!g_inCheck)
					{
						count++;
						if (!norep || !IsRepetition())
							moves.Add(m);
					}
					UnmakeMove(m);
				}
			if (count == 0)
			{
				GenerateAllMoves(!whiteTurn, true);
				mate = g_inCheck;
			}
			return moves;
		}

		public List<int> GenerateAllMoves(bool wt, bool onlyAattack)
		{
			g_inCheck = false;
			usColor = wt ? colorWhite : colorBlack;
			enColor = wt ? colorBlack : colorWhite;
			int pieceM = 0;
			int pieceN = 0;
			int pieceB = 0;
			List<int> moves = new List<int>();
			for (int n = 0; n < 64; n++)
			{
				int fr = arrField[n];
				int f = g_board[fr];
				if ((f & usColor) > 0) f &= 7;
				else continue;
				switch (f)
				{
					case 1:
						pieceM++;
						int del = wt ? -16 : 16;
						int to = fr + del;
						if (((g_board[to] & colorEmpty) > 0) && !onlyAattack)
						{
							GeneratePwnMoves(moves, fr, to, !onlyAattack, 0);
							if ((g_board[fr - del - del] == 0) && (g_board[to + del] & colorEmpty) > 0)
								GeneratePwnMoves(moves, fr, to + del, !onlyAattack, 0);
						}
						if ((g_board[to - 1] & enColor) > 0)
							GeneratePwnMoves(moves, fr, to - 1, true, 0);
						else if ((to - 1) == g_passing)
							GeneratePwnMoves(moves, fr, g_passing, true, moveflagPassing);
						else if ((g_board[to - 1] & colorEmpty) > 0)
							GeneratePwnMoves(moves, fr, to - 1, false, 0);
						if ((g_board[to + 1] & enColor) > 0)
							GeneratePwnMoves(moves, fr, to + 1, true, 0);
						else if ((to + 1) == g_passing)
							GeneratePwnMoves(moves, fr, g_passing, true, moveflagPassing);
						else if ((g_board[to + 1] & colorEmpty) > 0)
							GeneratePwnMoves(moves, fr, to + 1, false, 0);
						break;
					case 2:
						pieceN++;
						GenerateUniMoves(moves, onlyAattack, fr, arrDirKinght, 1);
						break;
					case 3:
						pieceB++;
						GenerateUniMoves(moves, onlyAattack, fr, arrDirBishop, 7);
						break;
					case 4:
						pieceM++;
						GenerateUniMoves(moves, onlyAattack, fr, arrDirRock, 7);
						break;
					case 5:
						pieceM++;
						GenerateUniMoves(moves, onlyAattack, fr, arrDirQueen, 7);
						break;
					case 6:
						GenerateUniMoves(moves, onlyAattack, fr, arrDirQueen, 1);
						int cr = wt ? g_castleRights : g_castleRights >> 2;
						if ((cr & 1) > 0)
							if (((g_board[fr + 1] & colorEmpty) > 0) && ((g_board[fr + 2] & colorEmpty) > 0))
								GenerateMove(moves, fr, fr + 2, true, moveflagCastleKing);
						if ((cr & 2) > 0)
							if (((g_board[fr - 1] & colorEmpty) > 0) && ((g_board[fr - 2] & colorEmpty) > 0) && ((g_board[fr - 3] & colorEmpty) > 0))
								GenerateMove(moves, fr, fr - 2, true, moveflagCastleQueen);
						break;
				}
			}
			adjInsufficient = (pieceM == 0) && (pieceN + (pieceB << 1) < 3);
			return moves;
		}

		void GeneratePwnMoves(List<int> moves, int fr, int to, bool add, int flag)
		{
			int y = to >> 4;
			if (((y == 4) || (y == 11)) && add)
			{
				GenerateMove(moves, fr, to, add, moveflagPromoteQueen);
				GenerateMove(moves, fr, to, add, moveflagPromoteRook);
				GenerateMove(moves, fr, to, add, moveflagPromoteBishop);
				GenerateMove(moves, fr, to, add, moveflagPromoteKnight);
			}
			else
				GenerateMove(moves, fr, to, add, flag);
		}

		void GenerateUniMoves(List<int> moves, bool attack, int fr, int[] dir, int count)
		{
			for (int n = 0; n < dir.Length; n++)
			{
				int to = fr;
				int c = count;
				while (c-- > 0)
				{
					to += dir[n];
					if ((g_board[to] & colorEmpty) > 0)
						GenerateMove(moves, fr, to, !attack, 0);
					else if ((g_board[to] & enColor) > 0)
					{
						GenerateMove(moves, fr, to, true, 0);
						break;
					}
					else
						break;
				}
			}
		}

		#endregion

		#region make move

		public void UnmakeMove(int move)
		{
			int fr = move & 0xFF;
			int to = (move >> 8) & 0xFF;
			int flags = move & 0xFF0000;
			int capi = to;
			CUndo undo = undoStack[--undoIndex];
			g_passing = undo.passing;
			g_castleRights = undo.castle;
			g_move50 = undo.move50;
			g_lastCastle = undo.lastCastle;
			g_hash = undo.hash;
			int captured = undo.captured;
			if ((flags & moveflagCastleKing) > 0)
			{
				g_board[to + 1] = g_board[to - 1];
				g_board[to - 1] = colorEmpty;
			}
			else if ((flags & moveflagCastleQueen) > 0)
			{
				g_board[to - 2] = g_board[to + 1];
				g_board[to + 1] = colorEmpty;
			}
			if ((flags & moveflagPromotion) > 0)
			{
				int piece = (g_board[to] & (~0x7)) | piecePawn;
				g_board[fr] = piece;
			}
			else g_board[fr] = g_board[to];
			if ((flags & moveflagPassing) > 0)
			{
				capi = whiteTurn ? to - 16 : to + 16;
				g_board[to] = colorEmpty;
			}
			g_board[capi] = captured;
			whiteTurn ^= true;
			g_moveNumber--;
		}

		public void MakeMove(int emo)
		{
			CUndo undo = undoStack[undoIndex++];
			undo.hash = g_hash;
			undo.passing = g_passing;
			undo.castle = g_castleRights;
			undo.move50 = g_move50;
			undo.lastCastle = g_lastCastle;
			int fr = emo & 0xff;
			int to = (emo >> 8) & 0xff;
			int flags = emo & 0xFF0000;
			int piecefr = g_board[fr];
			int piece = piecefr & 0xf;
			int captured = g_board[to];
			g_lastCastle = (emo & maskCastle) | (piecefr & maskColor);
			if ((flags & moveflagCastleKing) > 0)
			{
				g_board[to - 1] = g_board[to + 1];
				g_board[to + 1] = colorEmpty;
			}
			else if ((flags & moveflagCastleQueen) > 0)
			{
				g_board[to + 1] = g_board[to - 2];
				g_board[to - 2] = colorEmpty;
			}
			else if ((flags & moveflagPassing) > 0)
			{
				int capi = whiteTurn ? to + 16 : to - 16;
				captured = g_board[capi];
				g_board[capi] = colorEmpty;
			}
			undo.captured = captured;
			g_hash ^= g_hashBoard[fr, piece];
			g_passing = 0;
			if ((captured & 0xF) > 0)
				g_move50 = 0;
			else if ((piece & 7) == piecePawn)
			{
				if (to == (fr + 32)) g_passing = (fr + 16);
				if (to == (fr - 32)) g_passing = (fr - 16);
				g_move50 = 0;
			}
			else
				g_move50++;
			if ((flags & moveflagPromotion) > 0)
			{
				int newPiece = piecefr & (~0x7);
				if ((flags & moveflagPromoteKnight) > 0)
					newPiece |= pieceKnight;
				else if ((flags & moveflagPromoteQueen) > 0)
					newPiece |= pieceQueen;
				else if ((flags & moveflagPromoteBishop) > 0)
					newPiece |= pieceBishop;
				else
					newPiece |= pieceRook;
				g_board[to] = newPiece;
				g_hash ^= g_hashBoard[to, newPiece & 0xf];
			}
			else
			{
				g_board[to] = g_board[fr];
				g_hash ^= g_hashBoard[to, piece];
			}
			g_board[fr] = colorEmpty;
			g_castleRights &= boardCastle[fr] & boardCastle[to];
			whiteTurn ^= true;
			g_moveNumber++;
		}

		public bool MakeMove(string umo, out int emo, out int piece)
		{
			piece = 0;
			emo = UmoToEmo(umo);
			if (emo > 0)
			{
				piece = g_board[emo & 0xff] & 7;
				MakeMove(emo);
				return true;
			}
			return false;
		}

		public bool MakeMove(string umo, out int emo)
		{
			emo = UmoToEmo(umo);
			if (emo > 0)
			{
				MakeMove(emo);
				return true;
			}
			return false;
		}

		public bool MakeMoves(string[] moves)
		{
			foreach (string m in moves)
				if (!MakeMove(m, out _))
					return false;
			return true;
		}

		public bool MakeMoves(string moves)
		{
			string[] am = moves.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			return MakeMoves(am);
		}

		public bool IsValidMove(int emo)
		{
			List<int> moves = GenerateValidMoves(out _);
			foreach (int m in moves)
				if (m == emo)
					return true;
			return false;
		}

		public bool IsValidMove(string umo, out int emo)
		{
			emo = 0;
			List<int> moves = GenerateValidMoves(out _);
			foreach (int m in moves)
				if (EmoToUmo(m) == umo)
				{
					emo = m;
					return true;
				}
			return false;
		}

		public bool IsValidMove(string move, out string umo, out int emo)
		{
			umo = move;
			emo = 0;
			move = move.ToLower();
			List<int> moves = GenerateValidMoves(out _);
			foreach (int m in moves)
			{
				string u = EmoToUmo(m);
				if ((u == move) || (u == $"{move}q"))
				{
					umo = u;
					emo = m;
					return true;
				}
			}
			return false;
		}

		public bool IsValidMove(string move, out string umo, out string san, out int emo)
		{
			emo = 0;
			umo = "";
			san = "";
			List<int> moves = GenerateValidMoves(out _);
			foreach (int m in moves)
			{
				emo = m;
				umo = EmoToUmo(emo);
				san = UmoToSan(umo);
				if ((umo == move) || (san == move))
					return true;
			}
			return false;
		}

		#endregion

		#region utils

		public CGameState GetGameState(out bool check)
		{
			GenerateAllMoves(!whiteTurn, true);
			bool enInsufficient = adjInsufficient;
			check = g_inCheck;
			GenerateAllMoves(whiteTurn, false);
			bool myInsufficient = adjInsufficient;
			if (g_move50 >= 100)
				return CGameState.move50;
			if (IsRepetition())
				return CGameState.repetition;
			if (enInsufficient && myInsufficient)
				return CGameState.material;
			List<int> moves = GenerateValidMoves(out _);
			if (moves.Count > 0)
				return (int)CGameState.normal;
			return check ? CGameState.mate : CGameState.stalemate;
		}

		public CGameState GetGameState()
		{
			return GetGameState(out _);
		}

		bool IsRepetition(int count = 3)
		{
			int min =  undoIndex - g_move50;
			if (min < 0)
				min = 0;
			for (int n = undoIndex - 4; n >= min; n -= 2)
				if (undoStack[n].hash == g_hash)
					if (--count <= 1)
						return true;
			return false;
		}

		ulong RAND_32()
		{
			return ((ulong)random.Next() << 32) | ((ulong)random.Next() << 0);
		}

		public static void UmoToSD(string umo, out int s, out int d)
		{
			if (umo == "")
			{
				s = -1;
				d = -1;
			}
			else
			{
				s = UmoToIndex(umo.Substring(0, 2));
				d = UmoToIndex(umo.Substring(2, 2));
			}
		}

		public int MakeSquare(int x, int y)
		{
			return ((y + 4) << 4) | (x + 4);
		}

		#endregion

	}
}

