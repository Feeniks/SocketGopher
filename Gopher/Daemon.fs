namespace Gopher

open System
open System.Net
open System.Net.Sockets

module Daemon = 

    let rec private listen (timeout:TimeSpan) (endpoint:IPEndPoint) (server:Socket) = //TODO: Cleanup in case of error
        let client = server.Accept ()

        let srv = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        srv.Connect endpoint

        let conn = Connection.create client srv timeout

        Tunnel.start conn

        listen timeout endpoint server


    let start (timeout:TimeSpan) (port:int) (endpoint:IPEndPoint) = 
        let localEndpoint = new IPEndPoint (IPAddress.Any, port)

        let server = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

        server.Bind localEndpoint
        server.Listen (100)

        listen timeout endpoint server