using System;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    long[] quietHistory = new long[4096];

    const int TTSize = 1048576;
    // Key, move, depth, score, flag
    (ulong, Move, int, int, byte)[] TT = new (ulong, Move, int, int, byte)[TTSize];

    int[] material = { 0, 148, 456, 442, 727, 1434, 0 };

    // PSTs are encoded with the following format:
    // Every rank or file is encoded as a byte, with the first rank/file being the LSB and the last rank/file being the MSB.
    // For every value to fit inside a byte, the values are divided by 2, and multiplication inside evaluation is needed.
    ulong[] pstRanks = { 0, 32408100782142720, 16574112021868640239, 18014406223260090617, 796584101102809849, 70654625790754818, 17298066748544776942 },
            pstFiles = { 0, 18016651413102002942, 17654401953025031403, 18231695001086198523, 17653269425882988797, 145242196134722807, 17511685300639041005 };

    sbyte Extract(ulong term, int index) => (sbyte)(term >> (index * 8) & 0xFF);

    public Move Think(Board board, Timer timer)
    {
        // Decay quiet history instead of clearing it
        for (var i = 0; i < 4096; quietHistory[i++] /= 8) ;

        var killers = new Move[256];
        
        int inf = 2000000,
            mate = 1000000,
            allocatedTime = timer.MillisecondsRemaining / 8;

        long nodes = 0; // #DEBUG

        int Evaluate()
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
                        score += 52;

                    while (bitboard != 0)
                    {
                        var sq = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard);

                        // Open files, doubled pawns
                        if ((0x101010101010101UL << sq % 8 & ~(1UL << sq) & board.GetPieceBitboard(PieceType.Pawn, isWhite)) == 0)
                            score += Extract(69534330849924352, pieceIndex);

                        // For bishop, rook, queen and king
                        if (pieceIndex > 2)
                        {
                            // Mobility
                            var mobility = BitboardHelper.GetPieceAttacks((PieceType)pieceIndex, new Square(sq), board, isWhite) & ~(isWhite ? board.WhitePiecesBitboard : board.BlackPiecesBitboard);
                            score += Extract(70933906139906048, pieceIndex) * BitboardHelper.GetNumberOfSetBits(mobility)
                            // King attacks
                                  +  Extract(6221049792299008, pieceIndex) * BitboardHelper.GetNumberOfSetBits(mobility & BitboardHelper.GetKingAttacks(board.GetKingSquare(!isWhite)));
                        }

                        // Flip square if black
                        sq ^= 56 * color;

                        // Material and PSTs
                        score += material[pieceIndex]
                              +  Extract(pstRanks[pieceIndex], sq / 8) * 2
                              +  Extract(pstFiles[pieceIndex], sq % 8) * 2;
                    }
                }
            }

            return board.IsWhiteToMove ? -score : score;
        }

        int Search(int ply, int depth, int alpha, int beta, bool nullAllowed, out Move bestMove)
        {
            ulong key = board.ZobristKey;
            bestMove = Move.NullMove;

            // Repetition detection
            if (ply > 0 && board.IsRepeatedPosition())
                return 0;
            
            bool inCheck = board.IsInCheck(),
                 inZeroWindow = alpha == beta - 1;

            // If we are in check, we should search deeper
            if (inCheck)
                depth++;

            bool inQsearch = depth <= 0;
            int staticScore = Evaluate(),
                bestScore = -inf;

            if (inQsearch)
            {
                if (staticScore >= beta)
                    return staticScore;

                if (staticScore > alpha)
                    alpha = staticScore;

                bestScore = staticScore;
            }

            else if (inZeroWindow && !inCheck)
            {
                // Reverse futility pruning
                if (depth < 5 && staticScore - depth * 100 > beta)
                    return beta;

                // Null move pruning
                if (nullAllowed && staticScore >= beta && depth > 2)
                {
                    board.ForceSkipTurn();
                    var score = -Search(ply + 1, depth - 4, -beta, -beta + 1, false, out _);
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

            byte flag = 0, // Upper
                 movesEvaluated = 0,
                 quietsEvaluated = 0;

            nodes++; // #DEBUG

            // Loop over each legal move
            foreach (var move in moves)
            {
                board.MakeMove(move);

                bool isQuiet = !move.IsCapture;

                // Principal variation search
                int childAlpha = inQsearch || movesEvaluated == 0 ? beta : alpha + 1,

                // Late move reductions
                reduction = depth > 2 && movesEvaluated > 4 && isQuiet ? 
                            2 + movesEvaluated / 16 + Convert.ToInt32(inZeroWindow)
                          : 1;

                doSearch:
                var score = -Search(ply + 1, depth - reduction, -childAlpha, -alpha, true, out _);

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
                if (isQuiet)
                    quietsEvaluated++;

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
                            if (isQuiet)
                            {
                                quietHistory[move.RawValue & 4095] += depth * depth;
                                killers[ply] = move;
                            }

                            flag = 1; // Lower

                            break;
                        }
                    }
                }

                // Late move pruning
                if (!inCheck && inZeroWindow && quietsEvaluated > 3 + 2 * depth * depth)
                    break;
            }

            // Checkmate / stalemate detection
            if (movesEvaluated == 0)
                return inQsearch ? bestScore : inCheck ? ply - mate : 0;

            // Store the current position in the transposition table
            TT[key % TTSize] = (key, bestMove, inQsearch ? 0 : depth, bestScore, flag);

            return bestScore;
        }


        nodes = 0; // #DEBUG
        var bestMove = Move.NullMove;
        int score = 0,
            depth = 0;

        // Iterative deepening
        for (;timer.MillisecondsElapsedThisTurn <= allocatedTime / 5 /* Soft time limit */ && ++depth < 128;)
        {
            // Aspiration windows
            var window = 40;

            research:
            // Search with the current window
            var newScore = Search(0, depth, score - window, score + window, false, out var move);

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

            var elapsed = timer.MillisecondsElapsedThisTurn > 0 ? timer.MillisecondsElapsedThisTurn : 1; // #DEBUG
            Console.WriteLine($"info depth {depth} score cp {score} time {timer.MillisecondsElapsedThisTurn} nodes {nodes} nps {(nodes * 1000) / elapsed} pv {bestMove.ToString().Substring(7, bestMove.ToString().Length - 8)}"); // #DEBUG
        }

        return bestMove;
    }
}
