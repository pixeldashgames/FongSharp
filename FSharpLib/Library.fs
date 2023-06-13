namespace FSharpLib

open UnityEngine

type SimpleScript() =
    inherit MonoBehaviour()
   
    [<SerializeField>]
    let mutable f = 0.0
    
    member this.Start()= Debug.Log $"Hello World, f={f}"
        
