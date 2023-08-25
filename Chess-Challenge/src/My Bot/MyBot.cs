using System;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    int inf = 2000000;
    int mate = 1000000;

    long nodes = 0; // #DEBUG
    long[] quietHistory = new long[4096];
    Move[] killers = new Move[256];

    const int TTSize = 1048576;
    // Key, move, depth, score, flag
    (ulong, Move, int, int, byte)[] TT = new (ulong, Move, int, int, byte)[TTSize];


    int[] material = { 0, 158, 450, 433, 716, 1422, 0 };

    // PSTs are encoded with the following format:
    // Every rank or file is encoded as a byte, with the first rank/file being the LSB and the last rank/file being the MSB.
    // For every value to fit inside a byte, the values are divided by 2, and multiplication inside evaluation is needed.
    ulong[] pstRanks = { 0, 32125526293804288, 16501772952854001903, 18086463821593051128, 796865576079520248, 936477147365309181, 17370124342582573551 };
    ulong[] pstFiles = { 0, 18016932892373680894, 17654403052536658923, 18304034070100836859, 17653270525394616572, 722549563767978482, 17583742894660126190 };

    int[] mobilities = { 6, 5, 3, -5 };

    private int Evaluate(Board board)
    {
        int score = 0;
        for (var color = 2; --color >= 0; score = -score)
        {
            var isWhite = color == 0;

            //       None (skipped)               King
            for (var pieceIndex = 0; ++pieceIndex <= 6;)
            {
                var bitboard = board.GetPieceBitboard((PieceType)pieceIndex, isWhite);

                if (pieceIndex == 3 && BitboardHelper.GetNumberOfSetBits(bitboard) == 2) // Bishop pair
                    score += 47;

                while (bitboard != 0)
                {
                    var sq = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard);

                    // Mobility
                    if (pieceIndex > 2)
                        score += mobilities[pieceIndex - 3] * BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks((PieceType)pieceIndex, new Square(sq), board, isWhite) & ~(isWhite ? board.WhitePiecesBitboard : board.BlackPiecesBitboard));

                    // Flip square if black
                    sq ^= 56 * color;

                    // Material
                    score += material[pieceIndex]
                          +  (sbyte)(pstRanks[pieceIndex] >> (sq / 8 * 8) & 0xFF) * 2  // Rank PST
                          +  (sbyte)(pstFiles[pieceIndex] >> (sq % 8 * 8) & 0xFF) * 2; // File PST
                }
            }
        }

        return board.IsWhiteToMove ? -score : score;
    }

    private int Search(Board board, Timer timer, int allocatedTime, int ply, int depth, int alpha, int beta, bool nullAllowed, out Move bestMove)
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

        var inQsearch = depth <= 0;
        var inZeroWindow = alpha == beta - 1;

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

        else if (ply > 0 && inZeroWindow && !inCheck)
        {
            // Reverse futility pruning
            if (depth < 5 && staticScore - depth * 100 > beta)
                return beta;

            // Null move pruning
            if (nullAllowed && staticScore >= beta && depth > 2)
            {
                board.ForceSkipTurn();
                var score = -Search(board, timer, allocatedTime, ply + 1, depth - 4, -beta, -beta + 1, false, out _);
                board.UndoSkipTurn();
                if (score >= beta)
                    return beta;
            }
        }

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
            if (depth > 3)
                depth--;
        }

        bestMove = ttMove;

        // Move generation, best-known move then MVV-LVA ordering then killers then quiet move history
        var moves = board.GetLegalMoves(inQsearch).OrderByDescending(move => move == ttMove ? 9000000000000000000
                                                                           : move.IsCapture ? 8000000000000000000 + (long)move.CapturePieceType * 1000 - (long)move.MovePieceType
                                                                           : move == killers[ply] ? 7000000000000000000
                                                                           : quietHistory[move.RawValue & 4095]);

        var movesEvaluated = 0;
        byte flag = 0; // Upper
        nodes++; // #DEBUG

        // Loop over each legal move
        foreach (var move in moves)
        {
            board.MakeMove(move);

            // Principal variation search
            var childAlpha = inQsearch || movesEvaluated == 0 ? beta : alpha + 1;

            // Late move reductions
            var reduction = depth > 2 && movesEvaluated > 4 && !move.IsCapture ? 
                            2 + movesEvaluated / 16 + Convert.ToInt32(inZeroWindow)
                          : 1;

            doSearch:
            var score = -Search(board, timer, allocatedTime, ply + 1, depth - reduction, -childAlpha, -alpha, true, out _);

            // If score raises alpha, we see if we should do a re-search
            if (score > alpha)
            {
                // If we reduced the search previously, we research without a reduction, using the same window as before
                if (reduction > 1)
                {
                    reduction = 1;
                    goto doSearch;
                }

                // If the result score is within the current bounds, we must research with a full window
                if (childAlpha != beta && score < beta)
                {
                    childAlpha = beta;
                    goto doSearch;
                }
            }

            board.UndoMove(move);

            // If we are out of time, stop searching
            if (depth > 2 && timer.MillisecondsElapsedThisTurn > allocatedTime)
                return bestScore;

            // Count the number of moves we have evaluated for detecting mates and stalemates
            movesEvaluated++;

            // If the move is better than our current best, update our best move
            if (score > bestScore)
            {
                bestScore = score;

                // If the move is better than our current alpha, update alpha
                if (score > alpha)
                {
                    bestMove = move;
                    alpha = score;
                    flag = 2; // Exact

                    // If the move is better than our current beta, we can stop searching
                    if (score >= beta)
                    {
                        // If the move is not a capture, add a bonus to the quiets table and save it as the current ply's killer move
                        if (!move.IsCapture)
                        {
                            quietHistory[move.RawValue & 4095] += depth * depth;
                            killers[ply] = move;
                        }

                        flag = 1; // Lower

                        break;
                    }
                }
            }
        }

        // Checkmate / stalemate detection
        if (movesEvaluated == 0)
            return inQsearch ? bestScore : inCheck ? ply - mate : 0;

        // Store the current position in the transposition table
        TT[key % TTSize] = (key, bestMove, inQsearch ? 0 : depth, bestScore, flag);

        return bestScore;
    }

    public Move Think(Board board, Timer timer)
    {
        var allocatedTime = timer.MillisecondsRemaining / 8;
        
        // Decay quiet history instead of clearing it
        for (var i = 0; i < 4096; quietHistory[i++] /= 8) ;

        Array.Clear(killers);
        var bestMove = Move.NullMove;
        var score = 0;
        nodes = 0; // #DEBUG
        // Iterative deepening
        for (var depth = 0; ++depth < 128;)
        {
            // Aspiration windows
            var window = 40;
            research:

            // Search with the current window
            var newScore = Search(board, timer, allocatedTime, 0, depth, score - window, score + window, false, out var move);

            // Hard time limit
            // If we are out of time, we cannot trust the move that was found
            // during this iteration, so we break without setting bestMove
            if (timer.MillisecondsElapsedThisTurn > allocatedTime)
                break;

            // If the score is outside of the current window, we must research with a wider window
            if (newScore >= score + window || newScore <= score - window)
            {
                window *= 2;
                score = newScore;
                goto research;
            }

            score = newScore;
            bestMove = move;

            // Move is not printed in the usual pv format, because the API does not support easy conversion to UCI notation
            var elapsed = timer.MillisecondsElapsedThisTurn > 0 ? timer.MillisecondsElapsedThisTurn : 1; // #DEBUG
            Console.WriteLine($"info depth {depth} cp {score} time {timer.MillisecondsElapsedThisTurn} nodes {nodes} nps {(nodes * 1000) / elapsed} {bestMove}"); // #DEBUG

            // Soft time limit
            if (timer.MillisecondsElapsedThisTurn > allocatedTime / 5)
                break;
        }

        return bestMove;
    }
}
