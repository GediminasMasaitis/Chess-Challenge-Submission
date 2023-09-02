using System;
using System.Collections.Generic;
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
        Move rootBestMove = default;
        var (killers, inf, mate, allocatedTime, i) = (new Move[256], 2000000, 1000000, timer.MillisecondsRemaining / 8, 0);

        // Decay quiet history instead of clearing it
        for (; i < 4096; quietHistory[i++] /= 8) ;

        long nodes = 0; // #DEBUG

        int Search(int ply, int depth, int alpha, int beta, bool nullAllowed)
        {
            // Repetition detection
            if (ply > 0 && board.IsRepeatedPosition())
                return 0;
            
            bool inCheck = board.IsInCheck(), 
                 inZeroWindow = alpha == beta - 1;

            // If we are in check, we should search deeper
            if (inCheck)
                depth++;

            var (key, inQsearch, bestScore, doPruning, score) = (board.ZobristKey, depth <= 0, -inf, inZeroWindow && !inCheck, 15);

            // Evaluation inlined into search
            foreach (bool isWhite in new[] {!board.IsWhiteToMove, board.IsWhiteToMove})
            {
                score = -score;

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
                        if (!isWhite) sq ^= 56;

                        // Material and PSTs
                        score += material[pieceIndex]
                              + (Extract(pstRanks[pieceIndex], sq / 8)
                              +  Extract(pstFiles[pieceIndex], sq % 8)) * 2;
                    }
                }
            }

            // Local method for similar calls to Search, inspired by Tyrant7's approach.
            int defaultSearch(int beta, int reduction = 1, bool nullAllowed = true) => score = -Search(ply + 1, depth - reduction, -beta, -alpha, nullAllowed);


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
                    return beta;

                // Null move pruning
                if (nullAllowed && score >= beta && depth > 2)
                {
                    board.ForceSkipTurn();
                    defaultSearch(beta, 4 + depth / 6, false);
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
                ttMove = default;

                // Internal iterative reduction
                if (depth > 3)
                    depth--;
            }

            // Move generation, best-known move then MVV-LVA ordering then killers then quiet move history
            var (bestMove, moves, quietsEvaluated, movesEvaluated) = (ttMove,
                                                                      board.GetLegalMoves(inQsearch).OrderByDescending(move => move == ttMove ? 9000000000000000000
                                                                                                                     : move.IsCapture ? 8000000000000000000 + (long)move.CapturePieceType * 1000 - (long)move.MovePieceType
                                                                                                                     : move == killers[ply] ? 7000000000000000000
                                                                                                                     : quietHistory[move.RawValue & 4095]),
                                                                      new List<Move>(),
                                                                      0);

            byte flag = 0; // Upper

            // Loop over each legal move
            foreach (var move in moves)
            {
                board.MakeMove(move);
                nodes++; // #DEBUG

                bool isQuiet = !move.IsCapture;

                if (inQsearch || movesEvaluated == 0 // No PVS for first move or qsearch
                || (depth <= 2 || movesEvaluated <= 4 || !isQuiet // Conditions not to do LMR
                ||  defaultSearch(alpha + 1, 2 + depth / 8 + movesEvaluated / 16 + Convert.ToInt32(inZeroWindow) - Convert.ToInt32(quietHistory[move.RawValue & 4095] > 0)) > alpha) // LMR search raised alpha
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
                        bestMove = move;
                        if (ply == 0) rootBestMove = move;
                        alpha = score;
                        flag = 2; // Exact

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

                            flag = 1; // Lower

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
            if (movesEvaluated == 0)
                return inQsearch ? bestScore : inCheck ? ply - mate : 0;

            // Store the current position in the transposition table
            TT[key % TTSize] = (key, bestMove, inQsearch ? 0 : depth, bestScore, flag);

            return bestScore;
        }

        int score = 0,
            depth = 0;

        // Iterative deepening
        for (; timer.MillisecondsElapsedThisTurn <= allocatedTime / 5 /* Soft time limit */ && ++depth < 128;)
        {
            // Aspiration windows
            var window = 40;

            research:
            // Search with the current window
            var newScore = Search(0, depth, score - window, score + window, false);

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

            var elapsed = timer.MillisecondsElapsedThisTurn > 0 ? timer.MillisecondsElapsedThisTurn : 1; // #DEBUG
            Console.WriteLine($"info depth {depth} " + // #DEBUG
                              $"score cp {score} " + // #DEBUG
                              $"time {timer.MillisecondsElapsedThisTurn} " + // #DEBUG
                              $"nodes {nodes} " + // #DEBUG
                              $"nps {(nodes * 1000) / elapsed} " + // #DEBUG
                              $"pv {rootBestMove.ToString().Substring(7, rootBestMove.ToString().Length - 8)}"); // #DEBUG
        }

        return rootBestMove;
    }
}
