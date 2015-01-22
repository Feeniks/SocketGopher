
open System
open System.Net
open System.Net.Sockets

open Gopher
open Gopher.Transfer

let remoteIP = Dns.GetHostAddresses "www.google.com" |> Seq.head
let remotePort = 80

let remoteEP = new IPEndPoint (remoteIP, remotePort)
let localPort = 15001

let timeout = new TimeSpan (0, 1, 0)

let log s = printfn "%s" s

Daemon.start timeout log localPort remoteEP