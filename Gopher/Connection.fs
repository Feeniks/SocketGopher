namespace Gopher

open System
open System.Net.Sockets
open System.Threading

type Connection = 
    {
        client:Socket;
        server:Socket;
        timeout:int64;
        lastActive:int64 ref;
    }

    static member create (clientSocket:Socket) (serverSocket:Socket) (connectionTimeout:TimeSpan) = 
        let now = DateTime.Now.Ticks
        {
            client = clientSocket;
            server = serverSocket;
            timeout = connectionTimeout.Ticks;
            lastActive = ref now;
        }

    static member updateLastActive (conn:Connection) = 
        let v = DateTime.Now.Ticks
        Interlocked.Exchange (conn.lastActive, v) |> ignore

    static member isTimedOut (conn:Connection) = 
        let n = DateTime.Now.Ticks
        (n - !conn.lastActive) > conn.timeout