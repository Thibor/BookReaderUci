using NSChess;
using System;
using System.Collections.Generic;

namespace NSProgram
{
    class CChessExt : CChess
    {
        int[] material = { 0, 100, 320, 330, 500, 900, 0 };

        public bool Is2ToEnd(out string myMov, out string enMov)
        {
            myMov = "";
            enMov = "";
            List<int> mu1 = GenerateLegalMoves(out _);//my last move
            foreach (int myMove in mu1)
            {
                bool myEscape = true;
                MakeMove(myMove);
                List<int> mu2 = GenerateLegalMoves(out _);//enemy mat move
                foreach (int enMove in mu2)
                {
                    bool enAttack = false;
                    MakeMove(enMove);
                    List<int> mu3 = GenerateLegalMoves(out bool mate);//my illegal move
                    if (mate)
                    {
                        myEscape = false;
                        enAttack = true;
                        myMov = EmoToUmo(myMove);
                        enMov = EmoToUmo(enMove);
                    }
                    UnmakeMove(enMove);
                    if (enAttack)
                        continue;
                }
                UnmakeMove(myMove);
                if (myEscape)
                    return false;
            }
            return true;
        }

        int Center(int r, int f)
        {
            return 3 - Math.Abs(r * 2 - 7) / 2 - Math.Abs(f * 2 - 7) / 2;
        }

        int Eval()
        {
            int score = 0;
            for (int r = 0; r < 8; r++)
                for (int f = 0; f < 8; f++)
                {
                    int sq = r * 8 + f;
                    int piece = board[sq];
                    int pt = PieceType(piece);
                    if (pt == 0)
                        continue;
                    int m = material[pt] + Center(r, f);
                    if (PieceWhite(piece))
                        score += m;
                    else
                        score -= m;
                }
            return WhiteTurn ? score : -score;
        }

        int QSearch(int alpha, int beta)
        {
            List<int> moves = GenerateAllMoves(WhiteTurn, true);
            if (inCheck)
                return 0xffff;
            int score = Eval();
            if (score >= beta)
                return beta;
            if (score > alpha)
                alpha = score;
            foreach (int move in moves)
            {
                MakeMove(move);
                score = -QSearch(-beta, -alpha);
                UnmakeMove(move);
                if (score >= beta)
                    return beta;
                if (score > alpha)
                    alpha = score;
            }
            return alpha;
        }

        int Search(int depth, int alpha, int beta)
        {
            GenerateAllMoves(!WhiteTurn, true);
            if (inCheck)
                depth++;
            if (depth <= 0)
                return QSearch(alpha, beta);
            int bestScore = -0xffff;
            List<int> moves = GenerateLegalMoves(out _);
            foreach (int move in moves)
            {
                MakeMove(move);
                int score = -Search(depth - 1, -beta, -alpha);
                UnmakeMove(move);
                if (bestScore < score)
                    bestScore = score;
                if (score >= beta)
                    break;
                if (score > alpha)
                    alpha = score;
            }
            return bestScore;
        }

        public string GetUmo(int r)
        {
            int bestScore = -0xffff;
            List<int> best = new List<int>();
            List<int> moves = GenerateLegalMoves(out _);
            List<int> scores = new List<int>(moves.Count);
            if (moves.Count == 0)
                return string.Empty;
            foreach (int move in moves)
            {
                MakeMove(move);
                int score = -Search(3, -0xffff, 0xffff);
                UnmakeMove(move);
                scores.Add(score);
                if (bestScore < score)
                    bestScore = score;
            }
            for (int n = 0; n < moves.Count; n++)
            {
                if ((scores[n] >= bestScore - r) || (r == 10))
                    best.Add(moves[n]);
            }
            if (best.Count == 0)
                return string.Empty;
            return EmoToUmo(best[rnd.Next(best.Count)]);
        }

    }
}
