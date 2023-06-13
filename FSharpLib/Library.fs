namespace FSharpLib

open System

module Lib =
    type State =
        | Discovered of bool
        | Undiscovered of bool
        | Flagged of bool
    type Cell = {
        mutable hasMine: bool
        mutable adjacentMines: int
        state: State
    }

module Game =
    open Lib
    let createCell hasMine adjacentMines =
        { hasMine = hasMine; adjacentMines = adjacentMines; state = Undiscovered false}
    //Method to creat a bidimensional array of cells
    let createBoard rows cols =
        Array2D.init rows cols (fun i j -> createCell false 0)
    
    //Method to set randomly the mines in the board
    let rec setMinesPosition board minesCount=
        let random = new Random()
        if minesCount > 0 then
            let row = random.Next(0, Array2D.length1 board)
            let col = random.Next(0, Array2D.length2 board)
            if board[row, col].hasMine = false then
                board[row, col].hasMine <- true
                setMinesPosition board (minesCount - 1)
            else
                setMinesPosition board minesCount
        else
            board
    
    let isValidPosition board row col =
        row >= 0 && row < Array2D.length1 board && col >= 0 && col < Array2D.length2 board
        
    let d1 = [|1; -1; 0; 0; 1; -1; -1; 1|]
    let d2 = [|0; 0; 1; -1; -1; -1; 1; 1|]

    //Recursive method to count adjacent mines of a cell
    let rec countAdjacentMines (board: Cell [,]) row col i =
        let new_row = row + d1[i]
        let new_col = col + d2[i]
        if i = 0 then
            if isValidPosition board (row+d1[i]) (col+d2[i]) && board[row+d1[i],col+d2[i]].hasMine then 1 else 0
        else
            if isValidPosition board (row+d1[i]) (col+d2[i]) && board[row+d1[i],col+d2[i]].hasMine then 1 + countAdjacentMines board row col (i-1)
            else countAdjacentMines board row col (i-1)
        
       
    //Method to set the adjacent mines of a cell
    let rec setAdjacentMines board i =
        if i<(Array2D.length1 board)*(Array2D.length2 board) then
            let row = i/Array2D.length1 board
            let col = i%Array2D.length2 board
            board[row,col].adjacentMines <- countAdjacentMines board row col 7
            setAdjacentMines board (i+1)
        else
            board
            
    let startGame rows cols minesCount =
        let board = createBoard rows cols
        let boardWithMines = setMinesPosition board minesCount
        setAdjacentMines boardWithMines 0