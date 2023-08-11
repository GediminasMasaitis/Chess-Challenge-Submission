Engines tested against:

| Name | Version | ELO |
| --- | --- | --- |
| Vice | 1.1 | 2060 |

### 1.0
Initial release - randoom legal mover

53 tokens

```
Score of ChessChallenge1.0 vs ChessChallengeEvil: 2 - 4211 - 787  [0.079] 5000
...      ChessChallenge1.0 playing White: 2 - 2081 - 416  [0.084] 2499
...      ChessChallenge1.0 playing Black: 0 - 2130 - 371  [0.074] 2501
...      White vs Black: 2132 - 2081 - 787  [0.505] 5000
Elo difference: -426.4 +/- 12.1, LOS: 0.0 %, DrawRatio: 15.7 %
```

### 1.1
Basics for a searching engine:
* Iterative deepening
* Brute-force search
* Basic time tracking
* Material-only evaluation
* Mate and stalemate detection

329 tokens

```
Score of ChessChallenge1.1 vs ChessChallenge1.0: 1783 - 0 - 217  [0.946] 2000
...      ChessChallenge1.1 playing White: 886 - 0 - 114  [0.943] 1000
...      ChessChallenge1.1 playing Black: 897 - 0 - 103  [0.949] 1000
...      White vs Black: 886 - 897 - 217  [0.497] 2000
Elo difference: 496.6 +/- 23.2, LOS: 100.0 %, DrawRatio: 10.8 %

Score of ChessChallenge1.1 vs ChessChallengeEvil: 1667 - 0 - 333  [0.917] 2000
...      ChessChallenge1.1 playing White: 828 - 0 - 171  [0.914] 999
...      ChessChallenge1.1 playing Black: 839 - 0 - 162  [0.919] 1001
...      White vs Black: 828 - 839 - 333  [0.497] 2000
Elo difference: 416.7 +/- 18.6, LOS: 100.0 %, DrawRatio: 16.7 %
```

### 1.2
Centrality evaluation

383 tokens

```
Score of ChessChallenge vs ChessChallengeDev: 338 - 81 - 1581  [0.564] 2000
...      ChessChallenge playing White: 189 - 33 - 778  [0.578] 1000
...      ChessChallenge playing Black: 149 - 48 - 803  [0.550] 1000
...      White vs Black: 237 - 182 - 1581  [0.514] 2000
Elo difference: 44.9 +/- 6.8, LOS: 100.0 %, DrawRatio: 79.0 %
```

### 1.3
Poor man's repetition detection

448 tokens

```
Score of ChessChallenge vs ChessChallengeDev: 272 - 143 - 585  [0.565] 1000
...      ChessChallenge playing White: 126 - 75 - 300  [0.551] 501
...      ChessChallenge playing Black: 146 - 68 - 285  [0.578] 499
...      White vs Black: 194 - 221 - 585  [0.486] 1000
Elo difference: 45.1 +/- 13.8, LOS: 100.0 %, DrawRatio: 58.5 %

Score of ChessChallenge vs ChessChallengeEvil: 1902 - 0 - 98  [0.976] 2000
...      ChessChallenge playing White: 932 - 0 - 68  [0.966] 1000
...      ChessChallenge playing Black: 970 - 0 - 30  [0.985] 1000
...      White vs Black: 932 - 970 - 98  [0.490] 2000
Elo difference: 640.0 +/- 34.8, LOS: 100.0 %, DrawRatio: 4.9 %
```

### 1.4
Alpha-beta pruning

506 tokens

```
Score of ChessChallenge vs ChessChallengeDev: 730 - 39 - 231  [0.846] 1000
...      ChessChallenge playing White: 353 - 14 - 133  [0.839] 500
...      ChessChallenge playing Black: 377 - 25 - 98  [0.852] 500
...      White vs Black: 378 - 391 - 231  [0.493] 1000
Elo difference: 295.3 +/- 22.3, LOS: 100.0 %, DrawRatio: 23.1 %
```

### 1.5
Order capture moves first

521 tokens

```
Score of ChessChallenge vs ChessChallengeDev: 549 - 104 - 347  [0.723] 1000
...      ChessChallenge playing White: 289 - 50 - 162  [0.739] 501
...      ChessChallenge playing Black: 260 - 54 - 185  [0.706] 499
...      White vs Black: 343 - 310 - 347  [0.516] 1000
Elo difference: 166.2 +/- 18.1, LOS: 100.0 %, DrawRatio: 34.7 %
```

### 1.5.1
Remove unused usings

503 tokens

### 1.6
Sort by most valuable victim - least valuable attacker

516 tokens

```
Score of ChessChallenge vs ChessChallengeDev: 539 - 364 - 97
Elo difference: 61 +/- 21
```

### 1.7
Evaluation changes:
* Evaluation texel tuning using https://github.com/GediminasMasaitis/texel-tuner
* Remove centrality evaluation
* Split PSTs by rank and file
566 tokens

```
Score of ChessChallenge vs ChessChallengeDev: 811 - 424 - 265  [0.629] 1500
...      ChessChallenge playing White: 395 - 211 - 144  [0.623] 750
...      ChessChallenge playing Black: 416 - 213 - 121  [0.635] 750
...      White vs Black: 608 - 627 - 265  [0.494] 1500
Elo difference: 91.7 +/- 16.4, LOS: 100.0 %, DrawRatio: 17.7 %

Score of ChessChallenge vs ChessChallengeEvil: 4891 - 0 - 109  [0.989] 5000
...      ChessChallenge playing White: 2432 - 0 - 69  [0.986] 2501
...      ChessChallenge playing Black: 2459 - 0 - 40  [0.992] 2499
...      White vs Black: 2432 - 2459 - 109  [0.497] 5000
Elo difference: 783.1 +/- 32.9, LOS: 100.0 %, DrawRatio: 2.2 %
```

### 1.7.1
Format output to be semi UCI compatible, add more info to it

593 tokens

```
info depth 1 cp 56 time 31 Move: 'g1f3'
info depth 2 cp 0 time 32 Move: 'g1f3'
info depth 3 cp 50 time 34 Move: 'b1c3'
info depth 4 cp 0 time 62 Move: 'b1c3'
info depth 5 cp 89 time 187 Move: 'b2b3'
info depth 6 cp -39 time 1381 Move: 'b1c3'
info depth 7 cp 117 time 7861 Move: 'e2e3'
```

### 1.8
Add qsearch when depth <= 0

614 tokens

```
info depth 1 cp 56 time 32 Move: 'g1f3'
info depth 2 cp 0 time 34 Move: 'g1f3'
info depth 3 cp 50 time 38 Move: 'b1c3'
info depth 4 cp 0 time 101 Move: 'b1c3'
info depth 5 cp 12 time 1008 Move: 'b1a3'
info depth 6 cp 0 time 8181 Move: 'b1c3'
```

```
Score of ChessChallenge vs ChessChallengeDev: 1215 - 138 - 147  [0.859] 1500
...      ChessChallenge playing White: 628 - 52 - 71  [0.883] 751
...      ChessChallenge playing Black: 587 - 86 - 76  [0.834] 749
...      White vs Black: 714 - 639 - 147  [0.525] 1500
Elo difference: 313.9 +/- 22.6, LOS: 100.0 %, DrawRatio: 9.8 %

Score of ChessChallenge vs ChessChallengeEvil: 4924 - 0 - 76  [0.992] 5000
...      ChessChallenge playing White: 2458 - 0 - 43  [0.991] 2501
...      ChessChallenge playing Black: 2466 - 0 - 33  [0.993] 2499
...      White vs Black: 2458 - 2466 - 76  [0.499] 5000
Elo difference: 846.3 +/- 39.7, LOS: 100.0 %, DrawRatio: 1.5 %
```

### 1.9
Faster and smaller MVV-LVA ordering

602 tokens

```
info depth 1 cp 56 time 0 Move: 'g1f3'
info depth 2 cp 0 time 0 Move: 'g1f3'
info depth 3 cp 50 time 3 Move: 'b1c3'
info depth 4 cp 0 time 55 Move: 'b1c3'
info depth 5 cp 12 time 846 Move: 'b1a3'
info depth 6 cp 0 time 6837 Move: 'b1c3'
```

```
Score of ChessChallenge vs ChessChallengeDev: 934 - 680 - 386  [0.564] 2000
...      ChessChallenge playing White: 519 - 298 - 183  [0.611] 1000
...      ChessChallenge playing Black: 415 - 382 - 203  [0.516] 1000
...      White vs Black: 901 - 713 - 386  [0.547] 2000
Elo difference: 44.4 +/- 13.8, LOS: 100.0 %, DrawRatio: 19.3 %
```

### 1.10
In-check extension

610 tokens

```
info depth 1 cp 56 time 32 Move: 'g1f3'
info depth 2 cp 0 time 34 Move: 'g1f3'
info depth 3 cp 50 time 38 Move: 'b1c3'
info depth 4 cp 0 time 96 Move: 'b1c3'
info depth 5 cp 12 time 916 Move: 'b1a3'
bestmove b1a3
```

```
Score of ChessChallenge vs ChessChallengeDev: 771 - 405 - 324  [0.622] 1500
...      ChessChallenge playing White: 408 - 169 - 172  [0.660] 749
...      ChessChallenge playing Black: 363 - 236 - 152  [0.585] 751
...      White vs Black: 644 - 532 - 324  [0.537] 1500
Elo difference: 86.5 +/- 15.9, LOS: 100.0 %, DrawRatio: 21.6 %
```


### 1.11
Use built-in API for repetition detections

570 tokens

```
info depth 1 cp 56 time 28 Move: 'g1f3'
info depth 2 cp 0 time 29 Move: 'g1f3'
info depth 3 cp 50 time 33 Move: 'b1c3'
info depth 4 cp 0 time 88 Move: 'b1c3'
info depth 5 cp 12 time 878 Move: 'b1a3'
bestmove b1a3
```

```
Score of ChessChallenge vs ChessChallengeDev: 1113 - 1074 - 813  [0.506] 3000
...      ChessChallenge playing White: 660 - 447 - 393  [0.571] 1500
...      ChessChallenge playing Black: 453 - 627 - 420  [0.442] 1500
...      White vs Black: 1287 - 900 - 813  [0.565] 3000
Elo difference: 4.5 +/- 10.6, LOS: 79.8 %, DrawRatio: 27.1 %
```

### 1.12
Primitive transposition table, only used for move ordering best known move first

631 tokens

```
info depth 1 cp 56 time 32 Move: 'g1f3'
info depth 2 cp 0 time 34 Move: 'g1f3'
info depth 3 cp 50 time 39 Move: 'g1f3'
info depth 4 cp 0 time 45 Move: 'g1f3'
info depth 5 cp 12 time 171 Move: 'g1f3'
info depth 6 cp 0 time 254 Move: 'g1f3'
bestmove g1f3
```

```
Score of ChessChallenge vs ChessChallengeDev: 738 - 375 - 387  [0.621] 1500
...      ChessChallenge playing White: 425 - 147 - 179  [0.685] 751
...      ChessChallenge playing Black: 313 - 228 - 208  [0.557] 749
...      White vs Black: 653 - 460 - 387  [0.564] 1500
Elo difference: 85.8 +/- 15.4, LOS: 100.0 %, DrawRatio: 25.8 %
```

### 1.12.1
Mark #DEBUG code

593 tokens

### 1.13
Faster move ordering

593 tokens

```
info depth 1 cp 56 time 29 Move: 'g1f3'
info depth 2 cp 0 time 31 Move: 'g1f3'
info depth 3 cp 50 time 36 Move: 'g1f3'
info depth 4 cp 0 time 40 Move: 'g1f3'
info depth 5 cp 12 time 142 Move: 'g1f3'
info depth 6 cp 0 time 203 Move: 'g1f3'
bestmove g1f3
```

```
Score of ChessChallenge vs ChessChallengeDev: 929 - 660 - 411  [0.567] 2000
...      ChessChallenge playing White: 530 - 252 - 219  [0.639] 1001
...      ChessChallenge playing Black: 399 - 408 - 192  [0.495] 999
...      White vs Black: 938 - 651 - 411  [0.572] 2000
Elo difference: 47.0 +/- 13.7, LOS: 100.0 %, DrawRatio: 20.5 %
```

### 1.14
Move generation and ordering after qsearch checks

593 tokens

```
info depth 1 cp 56 time 30 Move: 'g1f3'
info depth 2 cp 0 time 32 Move: 'g1f3'
info depth 3 cp 50 time 34 Move: 'g1f3'
info depth 4 cp 0 time 37 Move: 'g1f3'
info depth 5 cp 12 time 114 Move: 'g1f3'
info depth 6 cp 0 time 167 Move: 'g1f3'
info depth 7 cp 30 time 1754 Move: 'g1f3'
bestmove g1f3
```

```
Score of ChessChallenge vs ChessChallengeDev: 931 - 632 - 437  [0.575] 2000
...      ChessChallenge playing White: 540 - 269 - 192  [0.635] 1001
...      ChessChallenge playing Black: 391 - 363 - 245  [0.514] 999
...      White vs Black: 903 - 660 - 437  [0.561] 2000
Elo difference: 52.3 +/- 13.6, LOS: 100.0 %, DrawRatio: 21.9 %
```

### 1.15
Reverse futility pruning

623 tokens

```
info depth 1 cp 56 time 30 Move: 'g1f3'
info depth 2 cp 0 time 32 Move: 'g1f3'
info depth 3 cp 50 time 35 Move: 'g1f3'
info depth 4 cp 0 time 38 Move: 'g1f3'
info depth 5 cp 12 time 114 Move: 'g1f3'
info depth 6 cp 0 time 165 Move: 'g1f3'
info depth 7 cp 30 time 1641 Move: 'g1f3'
```

```
Score of ChessChallenge vs ChessChallengeDev: 953 - 580 - 467  [0.593] 2000
...      ChessChallenge playing White: 551 - 246 - 203  [0.652] 1000
...      ChessChallenge playing Black: 402 - 334 - 264  [0.534] 1000
...      White vs Black: 885 - 648 - 467  [0.559] 2000
Elo difference: 65.6 +/- 13.5, LOS: 100.0 %, DrawRatio: 23.4 %
```

### 1.16
Non-capture move score history table

679 tokens

```
info depth 1 cp 56 time 30 Move: 'g1f3'
info depth 2 cp 0 time 31 Move: 'g1f3'
info depth 3 cp 50 time 31 Move: 'g1f3'
info depth 4 cp 0 time 34 Move: 'g1f3'
info depth 5 cp 12 time 39 Move: 'g1f3'
info depth 6 cp 0 time 78 Move: 'g1f3'
info depth 7 cp 30 time 210 Move: 'g1f3'
info depth 8 cp 0 time 1389 Move: 'g1f3'
bestmove g1f3
```

```
Score of ChessChallenge vs ChessChallengeDev: 994 - 559 - 447  [0.609] 2000
...      ChessChallenge playing White: 553 - 245 - 203  [0.654] 1001
...      ChessChallenge playing Black: 441 - 314 - 244  [0.564] 999
...      White vs Black: 867 - 686 - 447  [0.545] 2000
Elo difference: 76.8 +/- 13.6, LOS: 100.0 %, DrawRatio: 22.4 %
```

### 1.17
Full transposition table and transposition table cutoffs

738 tokens

```
info depth 1 cp 56 time 30 Move: 'g1f3'
info depth 2 cp 0 time 31 Move: 'g1f3'
info depth 3 cp 50 time 32 Move: 'g1f3'
info depth 4 cp 0 time 34 Move: 'g1f3'
info depth 5 cp 12 time 41 Move: 'g1f3'
info depth 6 cp 0 time 66 Move: 'g1f3'
info depth 7 cp 30 time 137 Move: 'g1f3'
info depth 8 cp 0 time 720 Move: 'g1f3'
bestmove g1f3
```

```
Score of ChessChallenge vs ChessChallengeDev: 954 - 584 - 462  [0.593] 2000
...      ChessChallenge playing White: 544 - 227 - 230  [0.658] 1001
...      ChessChallenge playing Black: 410 - 357 - 232  [0.527] 999
...      White vs Black: 901 - 637 - 462  [0.566] 2000
Elo difference: 65.0 +/- 13.5, LOS: 100.0 %, DrawRatio: 23.1 %
```

### 1.17.1
Reduce token count

699 tokens

### 1.18
Null-move pruning

757 tokens

```
info depth 1 cp 56 time 30 Move: 'g1f3'
info depth 2 cp 0 time 31 Move: 'g1f3'
info depth 3 cp 50 time 32 Move: 'g1f3'
info depth 4 cp 0 time 33 Move: 'g1f3'
info depth 5 cp 12 time 35 Move: 'g1f3'
info depth 6 cp 0 time 40 Move: 'g1f3'
info depth 7 cp 30 time 45 Move: 'g1f3'
info depth 8 cp 0 time 144 Move: 'g1f3'
info depth 9 cp 18 time 230 Move: 'g1f3'
info depth 10 cp 2 time 742 Move: 'g1f3'
bestmove g1f3
```

```
Score of ChessChallenge vs ChessChallengeDev: 973 - 554 - 473  [0.605] 2000
...      ChessChallenge playing White: 550 - 228 - 223  [0.661] 1001
...      ChessChallenge playing Black: 423 - 326 - 250  [0.549] 999
...      White vs Black: 876 - 651 - 473  [0.556] 2000
Elo difference: 73.9 +/- 13.5, LOS: 100.0 %, DrawRatio: 23.6 %
```
