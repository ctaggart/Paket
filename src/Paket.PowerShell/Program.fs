module Paket.PowerShell.Program

[<EntryPoint>]
let main argv = 
    let add = Add()
    for result in add.Invoke() do
        printfn "%A" result
    
    0