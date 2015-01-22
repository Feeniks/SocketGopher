namespace Gopher

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Threading

open Gopher.Transfer

module Tunnel = 

    let private guardConnected (s:Socket) = 
        match s.Connected with
        | true -> OK ()
        | false -> Closed

    let private guardTimeout (conn:Connection) = 
        match Connection.isTimedOut conn with
        | false -> OK ()
        | true -> Closed

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
        | _ -> Closed

    let rec private write (conn:Connection) (s:Socket) (data:byte array) = 
        try
            match s.Send data with
            | 0 -> OK ()
            | _ as l -> 
                updateLastActive conn
                data.[l..] |> write conn s
        with
        | _ -> Closed
    
    let rec private c2s (closefunc:unit -> unit) (conn:Connection) = async {
        let res = transfer {
            do! guardConnected conn.client
            let! data = read conn conn.client
            do! guardConnected conn.server
            do! write conn conn.server data
            do! guardTimeout conn
            return ()
        }
        
        match res with
        | OK _ -> return! c2s closefunc conn
        | Closed -> closefunc ()
    }

    let rec private s2c (closefunc:unit -> unit) (conn:Connection) = async {
        let res = transfer {
            do! guardConnected conn.server
            let! data = read conn conn.server
            do! guardConnected conn.client
            do! write conn conn.client data
            do! guardTimeout conn
            return ()
        }

        match res with
        | OK _ -> return! s2c closefunc conn
        | Closed -> closefunc ()
    }

    let start (conn:Connection) = 
        let cts = new CancellationTokenSource ()

        let closefunc () = 
            try
                cts.Cancel ()
                conn.client.Close ()
                conn.server.Close ()
            with
            | _ -> ()

        c2s closefunc conn |> Async.Start
        s2c closefunc conn |> Async.Start