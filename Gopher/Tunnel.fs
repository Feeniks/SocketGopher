namespace Gopher

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Threading

open Gopher.Transfer

module Tunnel = 

    let rec private guardTimeout (conn:Connection) = 
        match Connection.isTimedOut conn with
        | false -> OK ()
        | true -> Error "timeout"

    let rec private updateLastActive (conn:Connection) = Connection.updateLastActive conn

    let private read (conn:Connection) (s:Socket) = 
        try
            match s.Available with
            | 0 -> 
                Thread.Sleep 50
                OK [||]
            | _ -> 
                updateLastActive conn
                let buf = Array.init s.Available (fun _ -> 0uy)
                match s.Receive buf with
                | l when l = 0 -> OK [||]
                | _ as l -> OK buf.[..(l-1)]
        with
        | _ as ex -> Error ex.Message

    let rec private write (conn:Connection) (s:Socket) (data:byte array) = 
        try
            match s.Send data with
            | 0 -> OK ()
            | _ as l -> 
                updateLastActive conn
                data.[l..] |> write conn s
        with
        | _ as ex -> Error ex.Message
    
    let rec private c2s (errfunc:string -> unit) (conn:Connection) = async {
        let res = transfer {
            let! data = read conn conn.client
            do! write conn conn.server data
            do! guardTimeout conn
            return ()
        }
        
        match res with
        | OK _ -> return! c2s errfunc conn
        | Error ex -> errfunc ex
    }

    let rec private s2c (errfunc:string -> unit) (conn:Connection) = async {
        let res = transfer {
            let! data = read conn conn.server
            do! write conn conn.client data
            do! guardTimeout conn
            return ()
        }

        match res with
        | OK _ -> return! s2c errfunc conn
        | Error ex -> errfunc ex
    }

    let start (log:string -> unit) (conn:Connection) = 
        let cts = new CancellationTokenSource ()

        let errfunc e = 
            try
                sprintf "<ERR>: %s" e |> log
                cts.Cancel ()
                conn.client.Close ()
                conn.server.Close ()
            with
            | _ -> ()

        c2s errfunc conn |> Async.Start
        s2c errfunc conn |> Async.Start