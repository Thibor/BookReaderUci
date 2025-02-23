using System;
using System.Collections.Generic;

namespace NSChess
{

    public enum CGameState { wait, normal, mate, stalemate, repetition, move50, material, time, error, resignation }
    public enum Castle { WK = 1, WQ = 2, BK = 4, BQ = 8 };

    public class CastleRights
    {
        public int castleRights;

        public CastleRights()
        {
            castleRights = 0xf;
        }

        public void Set(Castle castle)
        {
            castleRights |= (int)castle;
        }

        public void Set(int i)
        {
            castleRights |= (1 << i);
        }

        public bool Get(Castle castle)
        {
            return (castleRights & (int)castle) > 0;
        }

        public bool Get(int i)
        {
            return (castleRights & (1 << i)) > 0;
        }

        public void Del(Castle castle)
        {
            castleRights &= ~(int)castle;
        }

        public void Del(int i)
        {
            castleRights &= ~(1 << i);
        }

        public void Switch(int i, bool b)
        {
            if (b)
                Set(i);
            else
                Del(i);
        }

        public void Clear()
        {
            castleRights = 0;
        }

        public void Full()
        {
            castleRights = 0xf;
        }

        public string ToStr()
        {
            if (castleRights == 0)
                return "-";
            string cr = string.Empty;
            if (Get(Castle.WK))
                cr += 'K';
            if (Get(Castle.WQ))
                cr += 'Q';
            if (Get(Castle.BK))
                cr += 'k';
            if (Get(Castle.BQ))
                cr += 'q';
            return cr;
        }

        public void FromStr(string s)
        {
            Clear();
            for (int n = 0; n < s.Length; n++)
                switch (s[n])
                {
                    case 'K':
                        Set(Castle.WK);
                        break;
                    case 'Q':
                        Set(Castle.WQ);
                        break;
                    case 'k':
                        Set(Castle.BK);
                        break;
                    case 'q':
                        Set(Castle.BQ);
                        break;
                }
        }

    }

    struct D2
    {
        public int x;
        public int y;

        public D2(int sx, int sy)
        {
            x = sx;
            y = sy;
        }
    }

    struct CUndo
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
        public const string defFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        public static Random rnd = new Random();
        public const int piecePawn = 0x01;
        public const int pieceKnight = 0x02;
        public const int pieceBishop = 0x03;
        public const int pieceRook = 0x04;
        public const int pieceQueen = 0x05;
        public const int pieceKing = 0x06;
        public const int colorBlack = 0x08;
        public const int colorWhite = 0x10;
        public const int colorEmpty = 0x20;
        public const int colorNone = 0x00;
        public const int maskRank = 7;
        const int moveflagPassing = 0x02 << 16;
        public const int moveflagCastleKing = 0x04 << 16;
        public const int moveflagCastleQueen = 0x08 << 16;
        internal const int moveflagPromoteQueen = pieceQueen << 24;
        internal const int moveflagPromoteRook = pieceRook << 24;
        internal const int moveflagPromoteBishop = pieceBishop << 24;
        internal const int moveflagPromoteKnight = pieceKnight << 24;
        internal const int maskPromotion = moveflagPromoteQueen | moveflagPromoteRook | moveflagPromoteBishop | moveflagPromoteKnight;
        const int maskCastle = moveflagCastleKing | moveflagCastleQueen;
        const int maskColor = colorBlack | colorWhite;
        public CastleRights castleRights = new CastleRights();
        ulong hash = 0;
        protected int passing = -1;
        public int move50 = 0;
        public int halfMove = 0;
        public bool inCheck = false;
        protected int lastCastle = 0;
        bool adjInsufficient = false;
        int undoIndex = 0;
        public int[] board = new int[64];
        readonly ulong[,] hashBoard = new ulong[64, 16];
        readonly int[] boardCheck = new int[64];
        readonly int[] boardCastle = new int[64];
        int usColor = 0;
        int enColor = 0;
        readonly D2[] arrDirKinght = { new D2(-2, -1), new D2(-2, 1), new D2(2, -1), new D2(2, 1), new D2(-1, -2), new D2(-1, 2), new D2(1, -2), new D2(1, 2) };
        readonly D2[] arrDirBishop = { new D2(-1, -1), new D2(-1, 1), new D2(1, -1), new D2(1, 1) };
        readonly D2[] arrDirRock = { new D2(-1, 0), new D2(1, 0), new D2(0, -1), new D2(0, 1) };
        readonly D2[] arrDirQueen = { new D2(-1, -1), new D2(-1, 1), new D2(1, -1), new D2(1, 1), new D2(-1, 0), new D2(1, 0), new D2(0, -1), new D2(0, 1) };
        readonly CUndo[] undoStack = new CUndo[0xfff];

        public bool WhiteTurn
        {
            get
            {
                return (halfMove & 1) == 0;
            }
        }

        public int MoveNumber
        {
            get
            {
                return ((halfMove >> 1) + 1);
            }
        }

        public string Passant
        {
            get
            {
                int myPiece = WhiteTurn ? piecePawn | colorWhite : piecePawn | colorBlack;
                int yb = WhiteTurn ? 2 : 5;
                int yc = passing >> 3;
                if (yc != yb)
                    return "-";
                int del = WhiteTurn ? 1 : -1;
                if ((board[passing + 7 * del] == myPiece) || (board[passing + 9 * del] == myPiece))
                    return IndexToSquare(passing);
                return "-";
            }
            set
            {
                passing = SquareToIndex(value);
            }
        }

        #region initation

        public CChess()
        {
            Initialize();
            SetFen();
        }

        public void Initialize()
        {
            hash = RAND_32();
            for (int n = 0; n < 64; n++)
            {
                boardCastle[n] = 15;
                boardCheck[n] = 0;
                board[n] = 0;
                for (int p = 0; p < 16; p++)
                    hashBoard[n, p] = RAND_32();
            }
            boardCastle[0] = 7;
            boardCastle[4] = 3;
            boardCastle[7] = 11;
            boardCastle[56] = 13;
            boardCastle[60] = 12;
            boardCastle[63] = 14;
            boardCheck[3] = colorBlack | moveflagCastleQueen;
            boardCheck[4] = colorBlack | maskCastle;
            boardCheck[5] = colorBlack | moveflagCastleKing;
            boardCheck[59] = colorWhite | moveflagCastleQueen;
            boardCheck[60] = colorWhite | maskCastle;
            boardCheck[61] = colorWhite | moveflagCastleKing;
        }
        #endregion

        #region info

        public int SquareX(int sq) => sq & 7;
        public int SquareY(int sq) => sq >> 3;
        public int MoveFr(int emo) => emo & 0x3f;
        public int MoveTo(int emo) => (emo >> 8) & 0x3f;
        public bool MoveWhite(int emo) => (board[MoveFr(emo)] & colorWhite) > 0;
        public bool MoveIsCastling(int emo) => (emo & maskCastle) > 0;
        public bool MoveIsCapture(int emo) => (board[MoveTo(emo)] & 0xf) > 0;

        public bool IsCheck(int emo)
        {
            MakeMove(emo);
            GenerateAllMoves(!WhiteTurn, true);
            UnmakeMove(emo);
            return inCheck;
        }

        #endregion info

        #region conversion

        /// <summary>
        /// Engine MOve TO Uci MOve
        /// </summary>

        public string EmoToUmo(int emo)
        {
            string result = IndexToSquare(emo & 0xFF) + IndexToSquare((emo >> 8) & 0xFF);
            int promotion = (emo >> 24) & maskRank;
            if (promotion > 0)
            {
                if (promotion == pieceKnight) result += 'n';
                else if (promotion == pieceBishop) result += 'b';
                else if (promotion == pieceRook) result += 'r';
                else result += 'q';
            }
            return result;
        }

        /// <summary>
        /// Uci MOve TO Engine MOve
        /// </summary>

        public int UmoToEmo(string umo)
        {
            List<int> moves = GenerateAllMoves(WhiteTurn, false);
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
            int fr = MoveFr(emo);
            int to = MoveTo(emo);
            int pieceFr = board[fr] & 7;
            int pieceTo = board[to] & 7;
            bool isAttack = (pieceTo > 0) || ((emo & moveflagPassing) > 0);
            if ((emo & moveflagCastleKing) > 0)
                return "O-O";
            if ((emo & moveflagCastleQueen) > 0)
                return "O-O-O";
            List<int> moves = GenerateLegalMoves(out _);
            bool showRank = false;
            bool showFile = false;
            foreach (int m in moves)
            {
                int f = m & 0xff;
                if (f != fr)
                {
                    int piece = board[f] & 7;
                    if ((piece == pieceFr) && ((m & 0xff00) == (emo & 0xff00)))
                    {
                        if ((f / 8) != (fr / 8))
                            showRank = true;
                        if ((f % 8) != (fr % 8))
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
            string attack = isAttack ? "x" : String.Empty;
            int promotion = (emo >> 24) & maskRank;
            string promo = String.Empty;
            if (promotion == pieceKnight)
                promo = "=N";
            if (promotion == pieceBishop)
                promo = "=B";
            if (promotion == pieceRook)
                promo = "=R";
            if (promotion == pieceQueen)
                promo = "=Q";
            string fin = check ? "+" : String.Empty;
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
            san = san.Trim(charsToTrim).ToLower();
            List<int> moves = GenerateLegalMoves(out _);
            foreach (int emo in moves)
            {
                string umo = EmoToUmo(emo);
                int i = san.IndexOf(umo);
                if ((i == 0) || (i == 1))
                    return umo;
                if (UmoToSan(umo).Trim(charsToTrim).ToLower() == san)
                    return umo;
            }
            return String.Empty;
        }

        public static string IndexToSquare(int index)
        {
            int x = index & 7;
            int y = index >> 3;
            if ((x < 0) || (y < 0) || (x > 7) || (y > 7))
                return String.Empty;
            string file = "abcdefgh";
            string rank = "87654321";
            return $"{file[x]}{rank[y]}";
        }

        public static int SquareToIndex(string square)
        {
            if (square.Length < 2)
                return -1;
            int x = "abcdefgh".IndexOf(square[0]);
            int y = "87654321".IndexOf(square[1]);
            if ((x == -1) || (y == -1))
                return -1;
            return y * 8 + x;
        }

        #endregion

        #region fen

        public string PieceToStr(int p)
        {
            int w = p & 0x10;
            string[] arr = { " ", "p", "n", "b", "r", "q", "k", " " };
            string pt = arr[p & 7];
            if (w > 0)
                pt = pt.ToUpper();
            return pt;
        }

        public int CharToPiece(char c)
        {
            int piece = Char.IsUpper(c) ? colorWhite : colorBlack;
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
                    return 0;
            }
            return piece;
        }

        public bool SetFen(string fen = defFen)
        {
            if (String.IsNullOrEmpty(fen))
                fen = defFen;
            for (int n = 0; n < 64; n++)
                board[n] = colorEmpty;
            fen = fen.Trim();
            List<string> chunks= new List<string>(fen.Trim().Split());
            if (chunks.Count < 2)
                chunks.Add("w");
            if (chunks.Count < 3)
                chunks.Add("-");
            if (chunks.Count < 4)
                chunks.Add("-");
            if (chunks.Count < 5)
                chunks.Add("0");
            if (chunks.Count < 6)
                chunks.Add("1");
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
                    int piece = CharToPiece(c);
                    if (piece == 0)
                        return false;
                    int index = row * 8 + col;
                    board[index] = piece;
                    col++;
                }
            }
            if(row<7)
                return false;
            string s1 = chunks[1];
            if ((s1 != "w") && (s1 != "b"))
                return false;
            int wt = s1 == "w" ? 0 : 1;
            castleRights.FromStr(chunks[2]);
            Passant = chunks[3];
            int.TryParse(chunks[4], out move50);
            if (!int.TryParse(chunks[5], out int mn))
                mn = 1;
            halfMove = ((mn - 1) << 1) + wt;
            undoIndex = 0;
            CheckCastle();
            return true;
        }

        int BoardGetPiece(int i)
        {
            return board[i] & 0x1f;
        }

        int BoardGetPiece(int x, int y)
        {
            return BoardGetPiece(y * 8 + x);
        }

        void CheckCastle()
        {
            if (BoardGetPiece(4, 7) != (colorWhite | pieceKing))
            {
                castleRights.Del(Castle.WK);
                castleRights.Del(Castle.WQ);
            }
            if (BoardGetPiece(4, 0) != (colorBlack | pieceKing))
            {
                castleRights.Del(Castle.BK);
                castleRights.Del(Castle.BQ);
            }
            if (BoardGetPiece(0, 7) != (colorWhite | pieceRook))
                castleRights.Del(Castle.WQ);
            if (BoardGetPiece(7, 7) != (colorWhite | pieceRook))
                castleRights.Del(Castle.WK);
            if (BoardGetPiece(0, 0) != (colorBlack | pieceRook))
                castleRights.Del(Castle.BQ);
            if (BoardGetPiece(7, 0) != (colorBlack | pieceRook))
                castleRights.Del(Castle.BK);
        }

        public string GetFenBase()
        {
            string result = String.Empty;
            string[] arr = { " ", "p", "n", "b", "r", "q", "k", " " };
            for (int y = 0; y < 8; y++)
            {
                if (y != 0)
                    result += '/';
                int empty = 0;
                for (int x = 0; x < 8; x++)
                {
                    int piece = board[y * 8 + x];
                    int rank = piece & 7;
                    if (rank == 0)
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

        public string ColorToStr()
        {
            return WhiteTurn ? "w" : "b";
        }

        public string GetEpd()
        {
            return $"{GetFenBase()} {ColorToStr()} {castleRights.ToStr()} {Passant}";
        }

        public string GetFen()
        {
            return $"{GetEpd()} {move50} {MoveNumber}";
        }

        #endregion

        #region move generator

        void GenerateMove(List<int> moves, int fr, int to, bool add, int flag)
        {
            if (((board[to] & 7) == pieceKing) || (((boardCheck[to] & lastCastle) == lastCastle) && ((lastCastle & maskCastle) > 0)))
                inCheck = true;
            if (add)
                moves.Add(MakeEmo(fr,to,flag));
        }

        public List<int> GenerateLegalMoves(out bool mate, bool repetytion = true)
        {
            mate = false;
            int count = 0;
            List<int> moves = new List<int>(64);
            List<int> am = GenerateAllMoves(WhiteTurn, false);
            if (!inCheck)
                foreach (int m in am)
                {
                    MakeMove(m);
                    GenerateAllMoves(WhiteTurn, true);
                    if (!inCheck)
                    {
                        count++;
                        if (repetytion || !IsRepetition())
                            moves.Add(m);
                    }
                    UnmakeMove(m);
                }
            if (count == 0)
            {
                GenerateAllMoves(!WhiteTurn, true);
                mate = inCheck;
            }
            return moves;
        }

        public List<int> GenerateAllMoves(bool wt, bool onlyAattack)
        {
            inCheck = false;
            usColor = wt ? colorWhite : colorBlack;
            enColor = wt ? colorBlack : colorWhite;
            int pieceM = 0;
            int pieceN = 0;
            int pieceB = 0;
            int sq;
            List<int> moves = new List<int>();
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    int fr = y * 8 + x;
                    int f = board[fr];
                    if ((f & usColor) > 0) f &= 7;
                    else continue;
                    switch (f)
                    {
                        case 1:
                            pieceM++;
                            int del = wt ? -1 : 1;
                            int to = fr + del * 8;
                            if (((board[to] & colorEmpty) > 0) && !onlyAattack)
                            {
                                GeneratePwnMoves(moves, fr, to, !onlyAattack, 0);
                                int d = wt ? 6 : 1;
                                if ((y == d) && (board[to + del * 8] & colorEmpty) > 0)
                                    GeneratePwnMoves(moves, fr, to + del * 8, !onlyAattack, 0);
                            }
                            if (GetBoard(x - 1, y + del, out sq))
                            {
                                if ((sq & enColor) > 0)
                                    GeneratePwnMoves(moves, fr, to - 1, true, 0);
                                else if ((to - 1) == passing)
                                    GeneratePwnMoves(moves, fr, passing, true, moveflagPassing);
                                else if ((sq & colorEmpty) > 0)
                                    GeneratePwnMoves(moves, fr, to - 1, false, 0);
                            }
                            if (GetBoard(x + 1, y + del, out sq))
                            {
                                if ((sq & enColor) > 0)
                                    GeneratePwnMoves(moves, fr, to + 1, true, 0);
                                else if ((to + 1) == passing)
                                    GeneratePwnMoves(moves, fr, passing, true, moveflagPassing);
                                else if ((sq & colorEmpty) > 0)
                                    GeneratePwnMoves(moves, fr, to + 1, false, 0);
                            }
                            break;
                        case 2:
                            pieceN++;
                            GenerateUniMoves(moves, onlyAattack, x, y, arrDirKinght, 1);
                            break;
                        case 3:
                            pieceB++;
                            GenerateUniMoves(moves, onlyAattack, x, y, arrDirBishop, 7);
                            break;
                        case 4:
                            pieceM++;
                            GenerateUniMoves(moves, onlyAattack, x, y, arrDirRock, 7);
                            break;
                        case 5:
                            pieceM++;
                            GenerateUniMoves(moves, onlyAattack, x, y, arrDirQueen, 7);
                            break;
                        case 6:
                            GenerateUniMoves(moves, onlyAattack, x, y, arrDirQueen, 1);
                            if (x == 4)
                            {
                                int cr = wt ? castleRights.castleRights : castleRights.castleRights >> 2;
                                if ((cr & 1) > 0)
                                    if (((board[fr + 1] & colorEmpty) > 0) && ((board[fr + 2] & colorEmpty) > 0))
                                        GenerateMove(moves, fr, fr + 2, true, moveflagCastleKing);
                                if ((cr & 2) > 0)
                                    if (((board[fr - 1] & colorEmpty) > 0) && ((board[fr - 2] & colorEmpty) > 0) && ((board[fr - 3] & colorEmpty) > 0))
                                        GenerateMove(moves, fr, fr - 2, true, moveflagCastleQueen);
                            }
                            break;
                    }
                }
            adjInsufficient = (pieceM == 0) && (pieceN + (pieceB << 1) < 3);
            return moves;
        }

        void GeneratePwnMoves(List<int> moves, int fr, int to, bool add, int flag)
        {
            int y = to >> 3;
            if (((y == 0) || (y == 7)) && add)
            {
                GenerateMove(moves, fr, to, add, moveflagPromoteQueen);
                GenerateMove(moves, fr, to, add, moveflagPromoteRook);
                GenerateMove(moves, fr, to, add, moveflagPromoteBishop);
                GenerateMove(moves, fr, to, add, moveflagPromoteKnight);
            }
            else
                GenerateMove(moves, fr, to, add, flag);
        }

        void GenerateUniMoves(List<int> moves, bool attack, int fx, int fy, D2[] dir, int count)
        {
            for (int n = 0; n < dir.Length; n++)
            {
                int fr = fy * 8 + fx;
                int dx = fx;
                int dy = fy;
                int c = count;
                while (c-- > 0)
                {
                    D2 d = dir[n];
                    dx += d.x;
                    dy += d.y;
                    if (!GetBoard(dx, dy, out int sq))
                        break;
                    int to = dy * 8 + dx;
                    if ((sq & colorEmpty) > 0)
                        GenerateMove(moves, fr, to, !attack, 0);
                    else if ((sq & enColor) > 0)
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
            int capi = to;
            CUndo undo = undoStack[--undoIndex];
            passing = undo.passing;
            castleRights.castleRights = undo.castle;
            move50 = undo.move50;
            lastCastle = undo.lastCastle;
            hash = undo.hash;
            int captured = undo.captured;
            if ((move & moveflagCastleKing) > 0)
            {
                board[to + 1] = board[to - 1];
                board[to - 1] = colorEmpty;
            }
            else if ((move & moveflagCastleQueen) > 0)
            {
                board[to - 2] = board[to + 1];
                board[to + 1] = colorEmpty;
            }
            if ((move & maskPromotion) > 0)
            {
                int piece = (board[to] & (~0x7)) | piecePawn;
                board[fr] = piece;
            }
            else board[fr] = board[to];
            if ((move & moveflagPassing) > 0)
            {
                capi = WhiteTurn ? to - 8 : to + 8;
                board[to] = colorEmpty;
            }
            board[capi] = captured;
            halfMove--;
        }

        public void MakeMove(int move)
        {
            ref CUndo undo = ref undoStack[undoIndex++];
            undo.hash = hash;
            undo.passing = passing;
            undo.castle = castleRights.castleRights;
            undo.move50 = move50;
            undo.lastCastle = lastCastle;
            int fr = move & 0xff;
            int to = (move >> 8) & 0xff;
            int piecefr = board[fr];
            int piece = piecefr & 0xf;
            int captured = board[to];
            lastCastle = (move & maskCastle) | (piecefr & maskColor);
            if ((move & moveflagCastleKing) > 0)
            {
                board[to - 1] = board[to + 1];
                board[to + 1] = colorEmpty;
            }
            else if ((move & moveflagCastleQueen) > 0)
            {
                board[to + 1] = board[to - 2];
                board[to - 2] = colorEmpty;
            }
            else if ((move & moveflagPassing) > 0)
            {
                int capi = WhiteTurn ? to + 8 : to - 8;
                captured = board[capi];
                board[capi] = colorEmpty;
            }
            undo.captured = captured;
            hash ^= hashBoard[fr, piece];
            passing = -1;
            if ((captured & maskRank) > 0)
                move50 = 0;
            else if ((piece & 7) == piecePawn)
            {
                if (to == (fr + 16))
                    passing = fr + 8;
                if (to == (fr - 16))
                    passing = fr - 8;
                move50 = 0;
            }
            else
                move50++;
            int newPiece = ((move & maskPromotion) > 0) ? (piecefr & maskColor) | ((move >> 24) & maskRank) : piecefr;
            board[fr] = colorEmpty;
            board[to] = newPiece;
            hash ^= hashBoard[to, newPiece & 0xf];
            castleRights.castleRights &= boardCastle[fr] & boardCastle[to];
            halfMove++;
        }

        public int GetPieceType(int emo)
        {
            return board[emo & 0xff] & 7;
        }

        public bool MakeMove(string umo, out int emo, out int piece)
        {
            piece = 0;
            emo = UmoToEmo(umo);
            if (emo > 0)
            {
                piece = GetPieceType(emo);
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
            List<int> moves = GenerateLegalMoves(out _);
            foreach (int m in moves)
                if (m == emo)
                    return true;
            return false;
        }

        public bool IsValidMove(string umo, out int emo)
        {
            emo = 0;
            List<int> moves = GenerateLegalMoves(out _);
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
            move = move.ToLower();
            emo = 0;
            umo = String.Empty;
            List<int> moves = GenerateLegalMoves(out _);
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
            move = move.ToLower();
            emo = 0;
            umo = String.Empty;
            san = String.Empty;
            List<int> moves = GenerateLegalMoves(out _);
            foreach (int m in moves)
            {
                emo = m;
                umo = EmoToUmo(emo);
                san = UmoToSan(umo);
                if ((umo == move) || (umo == $"{move}q") || (san.ToLower() == move))
                    return true;
            }
            return false;
        }

        #endregion

        #region helpers

        public CGameState GetGameState(out bool check)
        {
            GenerateAllMoves(!WhiteTurn, true);
            bool enInsufficient = adjInsufficient;
            check = inCheck;
            GenerateAllMoves(WhiteTurn, false);
            bool myInsufficient = adjInsufficient;
            if (move50 >= 100)
                return CGameState.move50;
            if (IsRepetition())
                return CGameState.repetition;
            if (enInsufficient && myInsufficient)
                return CGameState.material;
            List<int> moves = GenerateLegalMoves(out _);
            if (moves.Count > 0)
                return CGameState.normal;
            return check ? CGameState.mate : CGameState.stalemate;
        }

        public CGameState GetGameState()
        {
            return GetGameState(out _);
        }

        public bool IsRepetition(int count = 3)
        {
            int pos = undoIndex - 2;
            while (pos >= 0)
            {
                if (undoStack[pos].hash == hash)
                    if (--count <= 1)
                        return true;
                pos -= 2;
                if (pos < undoIndex - move50)
                    return false;
            }
            return false;
        }

        ulong RAND_32()
        {
            return ((ulong)rnd.Next() << 32) | ((ulong)rnd.Next() << 0);
        }

        public static void UmoToSD(string umo, out int s, out int d)
        {
            if (umo.Length < 4)
            {
                s = -1;
                d = -1;
            }
            else
            {
                s = SquareToIndex(umo.Substring(0, 2));
                d = SquareToIndex(umo.Substring(2, 2));
            }
        }

        bool GetBoard(int x, int y, out int v)
        {
            v = 0;
            if ((x < 0) || (y < 0) || (x > 7) || (y > 7))
                return false;
            v = board[y * 8 + x];
            return true;
        }

        public static int MakeEmo(int fr,int to,int flag)
        {
            return fr | (to << 8) | flag;
        }

        public static int EmoFrom(int emo)
        {
            return emo & 0xFF;
        }

        public static int EmoTo(int emo)
        {
            return (emo >> 8) & 0xFF;
        }

        public int GetCapturedPiece(int emo)
        {
            return board[EmoTo(emo)];   
        }

        public int GetMovingPiece(int emo)
        {
            return board[EmoFrom(emo)];
        }

        public bool PieceWhite(int p)
        {
            return (p & colorWhite) > 0;
        }
        public bool PieceBlack(int p)
        {
            return (p & colorBlack) > 0;
        }

        public int PieceType(int p)
        {
            return p & 0x7;
        }

        #endregion helpers

    }
}

