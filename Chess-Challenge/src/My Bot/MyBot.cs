// Project: smol.cs
// License: MIT
// Authors: Gediminas Masaitis, Goh CJ (cj5716)

using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    // Keeping track of which quiet move move is most likely to cause a beta cutoff.
    // The higher the score is, the more likely a beta cutoff is, so in move ordering we will put these moves first.
    long[] quietHistory = new long[4096];

    // Transposition table
    // We store the results of previous searches, keeping track of the score at that position,
    // as well as specific things how it was searched:
    // 1. Did it go through all the search and fail to find a better move? (Upper limit flag)
    // 2. Did it cause a beta cutoff and stopped searching early (Lower limit flag)
    // 3. Did it search through all moves and find a new best move for the currently searched position (Exact flag)
    // Read more about it here: https://www.chessprogramming.org/Transposition_Table
    // Format: Position key, move, depth, score, flag
    (ulong, Move, int, int, byte)[] TT = new (ulong, Move, int, int, byte)[2097152];


    // Due to the rules of the challenge and how token counting works, evaluation constants are packed into C# decimals,
    // as they allow the most efficient (12 usable bits per token).
    // The ordering is as follows: Midgame term 1, endgame term 1, midgame, term 2, endgame term 2...
    static sbyte[] extracted = new [] { 4835740172228143389605888m, 1862983114964290202813595648m, 6529489037797228073584297991m, 6818450810788061916507740187m, 7154536855449028663353021722m, 14899014974757699833696556826m, 25468819436707891759039590695m, 29180306561342183501734565961m, 944189991765834239743752701m, 4194697739m, 4340114601700738076711583744m, 3410436627687897068963695623m, 11182743911298765866015857947m, 10873240011723255639678263585m, 17684436730682332602697851426m, 17374951722591802467805509926m, 31068658689795177567161113954m, 1534136309681498319279645285m, 18014679997410182140m, 1208741569195510172352512m, 13789093343132567021105512448m, 6502873946609222871099113472m, 1250m }.SelectMany(x => decimal.GetBits(x).Take(3).SelectMany(y => (sbyte[])(Array)BitConverter.GetBytes(y))).ToArray();

    // After extracting the raw mindgame/endgame terms, we repack it into integers of midgame/endgame pairs.
    // The scheme in bytes (assuming little endian) is: 00 EG 00 MG
    // The idea of this is that we can do operations on both midgame and endgame values simultaneously, preventing the need
    // for evaluation for separate mid-game / end-game terms.
    int[] evalValues = Enumerable.Range(0, 138).Select(i => extracted[i * 2] | extracted[i * 2 + 1] << 16).ToArray();

    public Move Think(Board board, Timer timer)
    {
        Move rootBestMove = default;
        var (killers, allocatedTime, i, score, depth) = (new Move[256], timer.MillisecondsRemaining / 8, 0, 0, 1);

        // Decay quiet history instead of clearing it. 
        for (; i < 4096; quietHistory[i++] /= 8) ;

        long nodes = 0; // #DEBUG

        int Search(int ply, int depth, int alpha, int beta, bool nullAllowed)
        {
            // Repetition detection
            if (nullAllowed && board.IsRepeatedPosition())
                return 0;
            
            bool inCheck = board.IsInCheck();

            // If we are in check, we should search deeper
            if (inCheck)
                depth++;

            // -2000000 = -inf
            // Use 15 tempo for evaluation
            var (key, inQsearch, bestScore, doPruning, score, phase) = (board.ZobristKey, depth <= 0, -2_000_000, alpha == beta - 1 && !inCheck, 15, 0);

            // Evaluation inlined into search
            foreach (bool isWhite in new[] {!board.IsWhiteToMove, board.IsWhiteToMove})
            {
                score = -score;

                //       None (skipped)               King
                for (var pieceIndex = 0; ++pieceIndex <= 6;)
                {
                    var bitboard = board.GetPieceBitboard((PieceType)pieceIndex, isWhite);

                    while (bitboard != 0)
                    {
                        var sq = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard);

                        // Open files, doubled pawns
                        if ((0x101010101010101UL << sq % 8 & ~(1UL << sq) & board.GetPieceBitboard(PieceType.Pawn, isWhite)) == 0)
                            score += evalValues[126 + pieceIndex];

                        // For bishop, rook, queen and king
                        if (pieceIndex > 2)
                        {
                            // Mobility
                            var mobility = BitboardHelper.GetPieceAttacks((PieceType)pieceIndex, new Square(sq), board, isWhite) & ~(isWhite ? board.WhitePiecesBitboard : board.BlackPiecesBitboard);
                            score += evalValues[112 + pieceIndex] * BitboardHelper.GetNumberOfSetBits(mobility)
                                     // King attacks
                                   + evalValues[119 + pieceIndex] * BitboardHelper.GetNumberOfSetBits(mobility & BitboardHelper.GetKingAttacks(board.GetKingSquare(!isWhite)));
                        }

                        // Flip square if black
                        if (!isWhite) sq ^= 56;

                        phase += evalValues[pieceIndex];

                        // Material and PSTs
                        score += evalValues[pieceIndex * 8 + sq / 8]
                               + evalValues[56 + pieceIndex * 8 + sq % 8]
                               << 3;
                    }
                }
            }
            
            score = ((short)score * phase + (score + 0x8000 >> 16) * (24 - phase)) / 24;

            // Local method for similar calls to Search, inspired by Tyrant7's approach.
            int defaultSearch(int beta, int reduction = 1, bool nullAllowed = true) => score = -Search(ply + 1, depth - reduction, -beta, -alpha, nullAllowed);

            // Look up best move known so far if it is available
            var (ttKey, ttMove, ttDepth, ttScore, ttFlag) = TT[key % 2097152];

            if (ttKey == key)
            {
                // If conditions match, we can trust the table entry and return immediately
                if (alpha == beta - 1 && ttDepth >= depth && ttFlag != (ttScore >= beta ? 0 : 2))
                    return ttScore;

                // ttScore can be used as a better positional evaluation
                if (ttFlag != (ttScore > score ? 0 : 2))
                    score = ttScore;
            }
            else if (depth > 3)
                depth--;

            if (inQsearch)
            {
                if (score >= beta)
                    return score;

                if (score > alpha)
                    alpha = score;

                bestScore = score;
            }

            else if (doPruning)
            {
                // Reverse futility pruning
                if (depth < 7 && score - depth * 75 > beta)
                    return score;

                // Null move pruning
                if (nullAllowed && score >= beta && depth > 2 && phase != 0)
                {
                    board.ForceSkipTurn();
                    defaultSearch(beta, 4 + depth / 6, false);
                    board.UndoSkipTurn();
                    if (score >= beta)
                        return beta;
                }
            }

            // Move generation, best-known move then MVV-LVA ordering then killers then quiet move history
            var (moves, quietsEvaluated, movesEvaluated) = (board.GetLegalMoves(inQsearch).OrderByDescending(move => move == ttMove ? 9_000_000_000_000_000_000
                                                                                                                   : move.IsCapture ? 1_000_000_000_000_000_000 * (long)move.CapturePieceType - (long)move.MovePieceType
                                                                                                                   : move == killers[ply] ? 500_000_000_000_000_000
                                                                                                                   : quietHistory[move.RawValue & 4095]),
                                                            new List<Move>(),
                                                            0);

            ttFlag = 0; // Upper

            // Loop over each legal move
            foreach (var move in moves)
            {
                board.MakeMove(move);
                nodes++; // #DEBUG

                bool isQuiet = !move.IsCapture;

                if (inQsearch || movesEvaluated == 0 // No PVS for first move or qsearch
                || (depth <= 2 || movesEvaluated <= 4 || !isQuiet // Conditions not to do LMR
                ||  defaultSearch(alpha + 1, 2 + depth / 8 + movesEvaluated / 16 + Convert.ToInt32(doPruning) - quietHistory[move.RawValue & 4095].CompareTo(0)) > alpha) // LMR search raised alpha
                &&  alpha < defaultSearch(alpha + 1) && score < beta) // Full depth search failed high
                    defaultSearch(beta); // Do full window search

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
                        ttMove = move;
                        if (ply == 0) rootBestMove = move;
                        alpha = score;
                        ttFlag = 1; // Exact

                        // If the move is better than our current beta, we can stop searching
                        if (score >= beta)
                        {
                            // If the move is not a capture, add a bonus to the quiets table and save it as the current ply's killer move
                            if (isQuiet)
                            {
                                quietHistory[move.RawValue & 4095] += depth * depth;
                                foreach (var previousMove in quietsEvaluated)
                                    quietHistory[previousMove.RawValue & 4095] -= depth * depth;
                                killers[ply] = move;
                            }

                            ttFlag++; // Lower

                            break;
                        }
                    }
                }

                if (isQuiet)
                    quietsEvaluated.Add(move);

                // Late move pruning
                if (doPruning && quietsEvaluated.Count > 3 + depth * depth)
                    break;
            }

            // Checkmate / stalemate detection
            // 1000000 = mate score
            if (movesEvaluated == 0)
                return inQsearch ? bestScore : inCheck ? ply - 1_000_000 : 0;

            // Store the current position in the transposition table
            TT[key % 2097152] = (key, ttMove, inQsearch ? 0 : depth, bestScore, ttFlag);

            return bestScore;
        }

        // Iterative deepening
        for (; timer.MillisecondsElapsedThisTurn <= allocatedTime / 5 /* Soft time limit */; ++depth)
            // Aspiration windows
            for (int window = 40;;)
            {
                int alpha = score - window,
                    beta = score + window;
                // Search with the current window
                score = Search(0, depth, alpha, beta, false);

                // Hard time limit
                // If we are out of time, we stop searching and break.
                if (timer.MillisecondsElapsedThisTurn > allocatedTime)
                    break;

                // If the score is outside of the current window, we must research with a wider window.
                // Otherwise if we are in the window we can proceed to the next depth.
                if (alpha < score && score < beta)
                { // #DEBUG
                    var elapsed = timer.MillisecondsElapsedThisTurn > 0 ? timer.MillisecondsElapsedThisTurn : 1; // #DEBUG
                    Console.WriteLine($"info depth {depth} " + // #DEBUG
                                      $"score cp {score} " + // #DEBUG
                                      $"time {timer.MillisecondsElapsedThisTurn} " + // #DEBUG
                                      $"nodes {nodes} " + // #DEBUG
                                      $"nps {nodes * 1000 / elapsed} " + // #DEBUG
                                      $"pv {rootBestMove.ToString().Substring(7, rootBestMove.ToString().Length - 8)}"); // #DEBUG
                    break;
                } // #DEBUG

                window *= 2;
            }

        return rootBestMove;
    }
}
