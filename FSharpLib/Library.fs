namespace FSharpLib

module Lib =
    type State =
        | Discovered of bool
        | Undiscovered of bool
        | Flagged of bool
    type Cell = {
        haveMine: bool
        adjacentMines: int
        state: State
    }