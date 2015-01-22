namespace Gopher

open System

type TransferResult<'a> = OK of 'a | Closed

type TransferBuilder() = 
    member this.Bind (x, f) = 
        match x with
        | OK x -> f x
        | Closed -> Closed

    member this.Delay f = f ()

    member this.Return x = OK x

    member this.Zero () = OK ()


module Transfer = 

    let transfer = new TransferBuilder ()
