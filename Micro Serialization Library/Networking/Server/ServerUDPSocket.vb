Imports System.Net
Imports System.Net.Sockets
Imports MicroSerializationLibrary.Serialization

Namespace Networking.Server

    <Obsolete("Use TCP Server instead")>
    Public Class ServerUsingUDPClient
        Public Event OnReceivedMessage(sender As Object)
        Public Event OnSentMessage(sender As Object)
        Public Event OnShutdown(sender As Object, e As EventArgs)
        Public Event OnStartUp(sender As Object, e As EventArgs)
        Public Event OnInitialised(sender As Object, e As EventArgs)
        Public Event OnSendTimeout(sender As Object, e As EventArgs)
        Public Property Protocol As ISerializationProtocol
        Private _Port As Integer
        Private _Enabled As Boolean
        Private _Client As UdpClient
        Private Listener As Threading.Thread
        ''' <summary>
        ''' Returns the port the UDP Server is binded to
        ''' </summary>
        ''' <value></value>
        ''' <returns>Integer</returns>
        ''' <remarks></remarks>
        Public ReadOnly Property Port As Integer
            Get
                Return _Port
            End Get
        End Property
        ''' <summary>
        ''' Returns if the Server is listening to packets and can send packets.
        ''' </summary>
        ''' <value></value>
        ''' <returns>Boolean</returns>
        ''' <remarks></remarks>
        Public ReadOnly Property Enabled As Boolean
            Get
                Return _Enabled
            End Get
        End Property
        ''' <summary>
        ''' Make a new UDP server on the specified port.
        ''' </summary>
        ''' <param name="port">Port to listen on</param>
        ''' <param name="StartListening">(Optional) Start when initialised</param>
        ''' <remarks></remarks>
        Sub New(Protocol As ISerializationProtocol, Port As Integer, Optional StartListening As Boolean = False)
            Me.Protocol = Protocol
            Me._Port = Port
            _Client = New UdpClient(Me._Port)
            _Client.Client.SendBufferSize = 65527
            _Client.Client.ReceiveBufferSize = 65527
            If StartListening Then Start()
        End Sub
        ''' <summary>
        ''' Start listening to packets
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub [Start]()
            _Enabled = True
            Listener = New Threading.Thread(Sub() ListenerThread(_Client, New IPEndPoint(IPAddress.Parse("127.0.0.1"), Port)))
            Listener.Start()
            RaiseEvent OnStartUp(Me, Nothing)
        End Sub
        ''' <summary>
        ''' Stop listening
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub [Stop]()
            _Enabled = False
            Listener.Abort()
            RaiseEvent OnShutdown(Me, Nothing)
        End Sub
        Private Sub ListenerThread(Client As UdpClient, IPEndPoint As IPEndPoint)
            Dim Data As Object
            While Enabled
                Dim Bytes() As Byte = Client.Receive(IPEndPoint)
                Try
                    Data = Protocol.Deserialize(Bytes)
                    If Data <> Nothing Then
                        RaiseEvent OnReceivedMessage(Data)
                        Data = Nothing
                    End If
                Catch e As IO.FileNotFoundException
                    Throw New Exception("Add 'Core.AddResolver' to the program's initialiser.")
                Catch e As Exception
                    Debug.Print("Error in MattJamesLibrary.Networking.ServerUsingUDPClient.ListenerThread:- {0}", e.Message)
                End Try

            End While
        End Sub
        ''' <summary>
        ''' Send a serializable object with JSON to the address specified.
        ''' </summary>
        ''' <typeparam name="T">Any type (serializable only)</typeparam>
        ''' <param name="Message">The object you intend to send</param>
        ''' <param name="Address">The address to send the object to</param>
        ''' <remarks></remarks>
        Public Sub Send(Of T)(Message As T, Address As IPEndPoint)
            Try
                Send(Protocol.Serialize(Message), Address)
            Catch e As IO.FileNotFoundException
                Throw New Exception("Add 'Core.AddResolver' to the program's initialiser.")
            End Try
        End Sub
        ''' <summary>
        ''' Send a serialised object in bytes to the address specified
        ''' </summary>
        ''' <param name="Bytes">The data you intend to send</param>
        ''' <param name="Address">The address the data will be sent to</param>
        ''' <remarks></remarks>
        Public Sub Send(Bytes() As Byte, Address As IPEndPoint)
            _Client.Send(Bytes, Bytes.Length, Address)
            RaiseEvent OnSentMessage(Me)
        End Sub
    End Class
End Namespace