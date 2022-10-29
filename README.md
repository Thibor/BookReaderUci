# BookReaderUci

BoookReaderUci can be used as normal UCI chess engine in chess GUI like Arena.<br/>
To use this program you need install  <a href="https://dotnet.microsoft.com/download/dotnet-framework/net48">.NET Framework 4.8</a>

## Parameters

**-bf** opening Book File name<br/>
**-ef** chess Engine File name<br/>
**-ea** chess Engine Arguments<br/>
**-w** add moves to the book<br/>
**-lr** Limit maximum ply depth when Read from book (default 0) 0 means no limit<br/>
**-lw** Limit maximum ply depth when Write to book (default 0) 0 means no limit<br/>
**-tf** Teacher File name<br/>
**-acd** Analysis Count Depth<br/>

## Console commands

**book load** [filename].[uci|pgn] - clear and add<br/>
**book save** [filename].[uci|png] - save book to the file<br/>
**book addfile** [filename].[uci|pgn] - adds moves from another book<br/>
**book delete** [number x] - delete x moves from the book<br/>
**book clear** - clear all moves from the book<br/>

### Examples

-bf book.uci -lw 10<br/>
book -lw 10

Opens a chess library of openings called "book.uci", and if the player gets a checkmate, the first 10 half moves of the game will be added to the library

-bf book.uci -ef stockfish.exe<br />
book -ef stockfish.exe

Opens a chess library of openings named book.uci, and if it doesn't find any move in it, it will run a chess engine called stockfish.exe 
