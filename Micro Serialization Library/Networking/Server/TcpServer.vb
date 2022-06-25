Imports System.Net
Imports System.Net.Sockets
Imports MicroSerializationLibrary.Serialization

Namespace Networking.Server
    ''' <summary>
    ''' Creates a multithreaded TCP server with concurrent connections with clients. 
    ''' </summary>
    ''' <remarks></remarks>
    Public Class TcpServer
        Inherits BaseTCPSocket
        Public Event OnConnected(Sender As Socket)
        Public Event OnNewWait()
        Public Event AcceptThreadedConnection()
        Public ConnectedSockets As New Dictionary(Of IPEndPoint, ConnectedSocket)
        Private ListenerThread As Threading.Thread

        Sub New(Protocol As ISerializationProtocol, Port As Integer)
            MyBase.New(Protocol, Port)
        End Sub
        Public Sub Listen(backlog As Integer)
            ListenerThread = New Threading.Thread(Sub() Listen(backlog, BaseSocket))
            ListenerThread.Start()
        End Sub
        Private Sub Listen(backlog As Integer, Sender As Socket)
            MyBase.BaseSocket.Listen(backlog)
            Do
                RaiseEvent OnNewWait()
                Dim handler As Socket = Sender.Accept
                RaiseEvent AcceptThreadedConnection()
                Dim RemoteIPEndPoint As IPEndPoint = handler.RemoteEndPoint
                ConnectedSockets.Add(RemoteIPEndPoint, New ConnectedSocket(handler))
                Receive(handler)
                RaiseEvent OnConnected(handler)
            Loop
        End Sub
        Private Sub Release(sender As Socket, senderIP As IPEndPoint) Handles MyBase.OnRelease
            ConnectedSockets.Remove(senderIP)
        End Sub
        ''' <summary>
        ''' Send a serializable object with Protocol using an IPEndPoint
        ''' </summary>
        ''' <param name="Sender">The IPAddress/Port (IPEndPoint) of a connected socket</param>
        ''' <param name="Obj">The object you intend to send</param>
        ''' <remarks></remarks>
        Public Overloads Sub Send(Sender As IPEndPoint, Obj As Object)
            Send(ConnectedSockets(Sender).CurrentSocket, Obj)
        End Sub
        ''' <summary>
        ''' Send a serializable object with Protocol to an array of connected sockets
        ''' </summary>
        ''' <param name="Sender">An array of IPAddresses/Ports (IPEndPoints) from the connected sockets dictionary</param>
        ''' <param name="Obj">The object you intend to send</param>
        ''' <remarks></remarks>
        Public Sub SendBroadcast(Sender() As IPEndPoint, Obj As Object)
            For Each sock In Sender
                Send(ConnectedSockets(sock).CurrentSocket, Obj)
            Next
        End Sub
        Public Sub SendBroadcast(Obj As Object)
            ' TODO:
            ' Needs to be changed to cached method to save processing time on large scale servers.
#If NET20 Then
            Dim Keys As New List(Of IPEndPoint)
            For Each Key As IPEndPoint In Keys
                Keys.Add(Key)
            Next
            SendBroadcast(Keys.ToArray, Obj)
#Else
            SendBroadcast(ConnectedSockets.Keys.ToArray, Obj)
#End If
        End Sub
        ''' <summary>
        ''' Close all connected sockets and close the main listening socket. ConnectedSockets will be cleared.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub [Close]()
            For Each ConnectedSocketThread In ConnectedSockets
                ConnectedSocketThread.Value.CurrentSocket.Close()
            Next
            ConnectedSockets.Clear()
            ListenerThread.Abort()
            BaseSocket.Close()
        End Sub
    End Class
End Namespace