# BookReaderUci

BoookReaderUci can be used as normal UCI chess engine in chess GUI like Arena.<br/>
The program has an additional function implemented to evaluate the chess engine.<br/>
To use this program you need install  <a href="https://dotnet.microsoft.com/download/dotnet-framework/net48">.NET Framework 4.8</a>

## Parameters

**-bf** opening Book File name<br/>
**-ef** chess Engine File name<br/>
**-ea** chess Engine Arguments<br/>
**-w** Write new moves to the book<br/>
**-lr** Limit maximum ply depth when Read from book (default 0) 0 means no limit<br/>
**-lw** Limit maximum ply depth when Write to book (default 0) 0 means no limit<br/>
**-log** Create LOG file<br/>
**-info** show additional INFOrmation<br/>
**-tf** Teacher File name - teacher is a program used to update the accuracy.fen file<br/>
**-sf** Student File name - student is a program used to be tested<br/>
**-acd** Analysis Count Depth<br/>

## Console commands

**book** - operations on chess openings book in format uci<br />
	**book load** [filename].[uci|pgn] - clear and add moves from file<br/>
	**book save** [filename].[uci|png] - save book to the file<br/>
	**book addfile** [filename].[uci|pgn] - adds moves to the book from the file<br/>
	**book adduci** [uci moves] - add moves in uci format to the book<br />
	**book delete** [number x] - delete x games from the book<br/>
	**book clear** - remove all moves from the book<br/>
	**book getoption** - show options<br/>
	**book setoption name [option name] value [option value]** - set option<br/>
**accuracy** - evaluate accuracy and elo of chess engine<br />
	**accuracy start** - start test for the average centipawn loss, this command need file "accuracy.epd" and student file<br/>
	**accuracy update [depth]** - update file "accuracy.epd" this command need teacher file<br/>
	**accuracy delete** - delete positions from "accuracy.epd" where player cannot make blunder<br/>
**mod** - modify factors of chess engine, this command need file "mod.ini" and student file<br />
**evaluation** - evaluation chess positions by chess engine, this command nedd file "evaluation.epd" and student file<br />
**test** - test chess engine<br />
	**test start** - start test chess engine, this command need file "test.epd" and student file<br/>
**ini** - create configuration file<br />

### Examples of using parameters

-bf book.uci -lw 10<br/>
book -lw 10

Opens a chess library of openings called "book.uci", and if the player gets a checkmate, the first 10 half moves of the game will be added to the library

-bf book.uci -ef stockfish.exe<br />
book -ef stockfish.exe

Opens a chess library of openings named book.uci, and if it doesn't find any move in it, it will run a chess engine called stockfish.exe

### Examples of using console commands

accuracy start

This command requirest existence student file and "accuracy fen.txt". Student chess engine will be tested for the average centipawn loss, report will be generated in "accuracy report.log" file.

accuracy update 20

This command requirest existence teacher file and "accuracy fen.txt". File "accuracy fen.txt" will be udpated to depth 20.

test start

This command requirest existence student file and "test fen.txt". Student chess engine will be tested and report will be gnerated in "test report.log" file.

### Examples of using "bookreaderuci.ini" file

You can create file "bookreaderuci.ini" and add text lines.

teacher>stockfish.exe

Use file "stockfish.exe" as teacher.

student>rapchesscs.exe

Use file "rapchesscs.exe" as student.