using System;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    int inf = 2000000;
    int mate = 1000000;

    const int TTSize = 1048576;

    // Key, move, depth, score, flag
    (ulong, Move, int, int, byte)[] TT = new (ulong, Move, int, int, byte)[TTSize];

    int[] material = { 0, 155, 444, 470, 755, 1454, 0 };

    // PSTs are encoded with the following format:
    // Every rank or file is encoded as a byte, with the first rank/file being the LSB and the last rank/file being the MSB.
    // For every value to fit inside a byte, the values are divided by 2, and multiplication inside evaluation is needed.
    ulong[] pstRanks = { 0, 31281101363607040, 16573830546891929838, 17870858404743019758, 1157719803347201780, 937325991850276595, 17729564580683643123 };
    ulong[] pstFiles = { 0, 18017215462567117565, 17870855106292153324, 18087582016362119672, 17653550905154667004, 578435479515560689, 17944029756714517748 };

    private int Evaluate(Board board)
    {
        int score = 0;
        for (var color = 0; color < 2; color++)
        {
            var isWhite = color == 0;

            if(BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Bishop, isWhite)) == 2)
                score += 44;

            for (var piece = PieceType.Pawn; piece <= PieceType.King; piece++)
            {
                var pieceIndex = (int)piece;
                var bitboard = board.GetPieceBitboard(piece, isWhite);

                while (bitboard != 0)
                {
                    var sq = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard);
                    sq ^= 56 * color;

                    var rank = sq >> 3;
                    var file = sq & 7;

                    // Material
                    score += material[pieceIndex];

                    // Rank PST
                    score += (sbyte)((pstRanks[pieceIndex] >> (rank * 8)) & 0xFF) * 2;

                    // File PST
                    score += (sbyte)((pstFiles[pieceIndex] >> (file * 8)) & 0xFF) * 2;
                }
            }

            score = -score;
        }

        return board.IsWhiteToMove ? score : -score;
    }

    private int Search(Board board, Timer timer, int totalTime, int ply, int depth, int alpha, int beta, long[,] quietHistory, bool nullAllowed, out Move bestMove)
    {
        ulong key = board.ZobristKey;
        bestMove = Move.NullMove;

        // Repetition detection
        if (ply > 0 && board.IsRepeatedPosition())
            return 0;

        // If we are in check, we should search deeper
        var inCheck = board.IsInCheck();
        if (inCheck)
            depth++;

        // Look up best move known so far if it is available
        var (ttKey, ttMove, ttDepth, ttScore, ttFlag) = TT[key % TTSize];

        if (ttKey == key)
        {
            // If conditions match, we can trust the table entry and return immediately
            if (ply > 0 && ttDepth >= depth && (ttFlag == 0 && ttScore <= alpha || ttFlag == 1 && ttScore >= beta || ttFlag == 2))
                return ttScore;
        }
        else
        {
            // If the table entry is not for this position, we can't trust the move to be the best known move
            ttMove = Move.NullMove;

            // Internal iterative reduction
            if(depth > 3)
                depth--;
        }

        var inQsearch = depth <= 0;

        var staticScore = Evaluate(board);
        var bestScore = -inf;
        if (inQsearch)
        {
            if (staticScore >= beta)
                return staticScore;

            if (staticScore > alpha)
                alpha = staticScore;

            bestScore = staticScore;
        }

        else if (ply > 0 && alpha == beta - 1 && !inCheck)
        {
            // Reverse futility pruning
            if (depth < 5 && staticScore - depth * 150 > beta)
                return beta;

            // Null move pruning
            if (nullAllowed && staticScore >= beta && depth > 2)
            {
                board.ForceSkipTurn();
                var score = -Search(board, timer, totalTime, ply + 1, depth - 4, -beta, -beta + 1, quietHistory, false, out _);
                board.UndoSkipTurn();
                if (score >= beta)
                    return beta;
            }
        }

        // Move generation, best-known move then MVV-LVA ordering then quiet move history
        var moves = board.GetLegalMoves(inQsearch).OrderByDescending(move => move == ttMove ? 9000000000000000000 : move.IsCapture ? 8000000000000000000 + (long)move.CapturePieceType * 1000 - (long)move.MovePieceType : quietHistory[move.StartSquare.Index, move.TargetSquare.Index]);

        var movesEvaluated = 0;
        byte flag = 0; // Upper

        // Loop over each legal move
        foreach (var move in moves)
        {
            board.MakeMove(move);

            // Principal variation search
            var childAlpha = -beta;
            var reduction = 1;
            if(inQsearch || movesEvaluated == 0)
                goto doSearch;
            childAlpha = -alpha - 1;

            // Late move reductions
            if(depth > 2 && movesEvaluated > 4 && !move.IsCapture)
                reduction = 2;

            doSearch:
            var score = -Search(board, timer, totalTime, ply + 1, depth - reduction, childAlpha, -alpha, quietHistory, true, out _);

            // If the result score is within the current bounds, we must research with a full window
            if (childAlpha != -beta && score > alpha && score < beta)
            {
                childAlpha = -beta;
                goto doSearch;
            }

            board.UndoMove(move);

            // If we are out of time, stop searching
            if (depth > 2 && timer.MillisecondsElapsedThisTurn * 30 > totalTime)
                return bestScore;

            // Count the number of moves we have evaluated for detecting mates and stalemates
            movesEvaluated++;

            // If the move is better than our current best, update our best move
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;

                // If the move is better than our current alpha, update alpha
                if (score > alpha)
                {
                    alpha = score;
                    flag = 2; // Exact

                    // If the move is better than our current beta, we can stop searching
                    if (score >= beta)
                    {
                        // If the move is not a capture, add a bonus to the quiets table
                        if (!move.IsCapture)
                            quietHistory[move.StartSquare.Index, move.TargetSquare.Index] += depth * depth;

                        flag = 1; // Lower

                        break;
                    }
                }
            }
        }

        if (movesEvaluated == 0)
            return inQsearch ? bestScore : inCheck ? ply - mate : 0;

        // Store the current position in the transposition table
        TT[key % TTSize] = (key, bestMove, depth, bestScore, flag);

        return bestScore;
    }

    public Move Think(Board board, Timer timer)
    {
        var totalTime = timer.MillisecondsRemaining;
        var quietHistory = new long[64, 64];
        var bestMove = Move.NullMove;
        // Iterative deepening
        for (var depth = 1; depth < 128; depth++)
        {
            var score = Search(board, timer, totalTime, 0, depth, -inf, inf, quietHistory, false, out var move);

            // If we are out of time, we cannot trust the move that was found during this iteration
            if (timer.MillisecondsElapsedThisTurn * 30 > totalTime)
                break;

            bestMove = move;

            // For debugging purposes, can be removed if lacking tokens
            // Move is not printed in the usual pv format, because the API does not support easy conversion to UCI notation
            Console.WriteLine($"info depth {depth} cp {score} time {timer.MillisecondsElapsedThisTurn} {bestMove}"); // #DEBUG
        }

        return bestMove;
    }
}
