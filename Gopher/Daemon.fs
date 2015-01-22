namespace Gopher

open System
open System.Net
open System.Net.Sockets

module Daemon = 

    let rec private listen (timeout:TimeSpan) (log:string -> unit) (endpoint:IPEndPoint) (server:Socket) = //TODO: Cleanup in case of error
        try
            let client = server.Accept ()

            let server = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            server.Connect endpoint

            let conn = Connection.create client server timeout

            Tunnel.start log conn
        with
        | _ as ex -> sprintf "<ERR> %s" ex.Message |> log

        listen timeout log endpoint server


    let start (timeout:TimeSpan) (log:string -> unit) (port:int) (endpoint:IPEndPoint) = 
        let localEndpoint = new IPEndPoint (IPAddress.Any, port)

        let server = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

        server.Bind localEndpoint
        server.Listen (100)

        listen timeout log endpoint server