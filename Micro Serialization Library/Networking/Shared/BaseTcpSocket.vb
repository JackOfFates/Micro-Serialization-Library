Imports System.Net
Imports System.Net.Sockets
Imports MicroSerializationLibrary.Serialization

Namespace Networking

    <Serializable>
    Public Class ObjectTicketRequest

        Public Property ID As String
        Sub New()

        End Sub
        Sub New(ID As String)
            Me.ID = ID
        End Sub
    End Class

    ''' <summary>
    ''' BaseTCPSocket used for ServerTCPSocket and ClientTCPSocket
    ''' </summary>
    ''' <remarks></remarks>
    Public MustInherit Class BaseTCPSocket

        Public Event OnConnectionInterrupt(sender As Socket)
        Public Event OnReceive(sender As Socket, obj As Object, BytesReceived As Integer)
        Public Event OnSent(sender As Socket, BytesSent As Integer)
        Public Event OnError(sender As Socket, e As Exception)
        Friend Event OnRelease(sender As Socket, senderIP As IPEndPoint)

        Public Property isPacketFailureRecoveryEnabled As Boolean = True
        ''' <summary>
        ''' How many different messages should be cached for packet failure. Recommended at around 20-40.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Property PacketFailureCacheSize As Integer = 20
        'Private SendCache As Object() = New Object(10) {}
        'Private CacheIndex As Integer = -1

        Private _Port As Integer

        Friend Class ObjectTicket
            Public Property ResendRequests As Integer = 0
            Public Data() As Byte
            Sub New(Data() As Byte)
                Me.Data = Data
            End Sub
        End Class
        Private TicketCache As New Dictionary(Of Tuple(Of Socket, String), ObjectTicket)

        ''' <summary>
        ''' The main socket that listens to all requests. Using the TCP protocol.
        ''' </summary>
        ''' <remarks>Uses AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp</remarks>
        Public BaseSocket As Socket
        ''' <summary>
        ''' Make a new TCP socket and bind it instantly.
        ''' </summary>
        ''' <param name="Port">The port you wish to bind to</param>
        ''' <param name="Protocol">The protocol to use for serializing and deserializing information</param>
        ''' <remarks></remarks>
        Public Sub New(Protocol As ISerializationProtocol, Optional Port As Integer = 0)
            _Port = Port
            BaseSocket = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            BaseSocket.ReceiveTimeout = -1
            BaseSocket.SendTimeout = -1
            Try
                Bind(Port)
            Catch ex As SocketException
                Throw New Exception("Cannot bind to port!")
            Catch ex As Exception
                Throw ex
            End Try
            Me.Protocol = Protocol

        End Sub
        Private Sub Bind(Port As Integer)
            BaseSocket.Bind(New IPEndPoint(IPAddress.Any, Port))
        End Sub
        Public Property Protocol As ISerializationProtocol
        ''' <summary>
        ''' Returns the binded port number
        ''' </summary>
        ''' <value>Integer</value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property Port As Integer
            Get
                Return _Port
            End Get
        End Property
        ''' <summary>
        ''' Returns the binded IPEndPoint
        ''' </summary>
        ''' <value>IPEndPoint</value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property LocalIPEndPoint As IPEndPoint
            Get
                Return CType(BaseSocket.LocalEndPoint, IPEndPoint)
            End Get
        End Property
        ''' <summary>
        ''' Returns a boolean value if the socket's connected to a remote host
        ''' </summary>
        ''' <value>Boolean</value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property Connected As Boolean
            Get
                Return If(Not IsNothing(BaseSocket), BaseSocket.Connected, False)
            End Get
        End Property

        Friend Sub Receive(sender As Socket)
            Try
                ' Create the state object.
                Dim state As New StateObject
                state.workSocket = sender

                ' Get the length of the body data transfer. 
                sender.BeginReceive(state.PaddingBuffer, 0, StateObject.PaddingBufferSize, 0, New AsyncCallback(AddressOf PaddingCallback), state)
            Catch ex As SocketException
                RaiseEvent OnConnectionInterrupt(sender)
            Catch ex As Exception
                RaiseEvent OnError(sender, ex)
            End Try

        End Sub 'Receive
        Private Sub PaddingCallback(ar As IAsyncResult)

            ' Retrieve the state object and the client socket 
            ' from the asynchronous state object.
            Dim state As StateObject = CType(ar.AsyncState, StateObject)
            Dim client As Socket = state.workSocket

            Try
                ' Read the length of the body.
                Dim bytesRead As Integer = client.EndReceive(ar)
                state.TotalBytesRead += bytesRead

                Dim StringData As String = Text.Encoding.UTF8.GetString(state.PaddingBuffer)
                Dim BodyLength As Integer = CInt(StringData.Remove(10))
                Dim MessageID As String = StringData.Remove(0, 10)
                state.TotalBytesToRead = BodyLength
                state.ID = MessageID

                ' Begin receiving the data from the remote device.
                DoRecursiveReceive(state)
            Catch ex As Exception
                RaiseEvent OnError(client, ex)
            End Try
        End Sub
        Private Sub ReceiveCallback(ar As IAsyncResult)
            ' Retrieve the state object and the client socket 
            ' from the asynchronous state object.
            Dim state As StateObject = CType(ar.AsyncState, StateObject)
            Dim client As Socket = state.workSocket

            Try
                ' Read data from the remote device.
                Dim bytesRead As Integer = client.EndReceive(ar)

                ' Add temporary buffer data to final output buffer.
                state.ObjectData.AddRange(state.buffer)
                state.TotalBytesRead += bytesRead

                ' Begin receiving the data from the remote device.
                DoRecursiveReceive(state)
            Catch ex As Exception
                RaiseEvent OnError(client, ex)
            End Try

        End Sub 'ReceiveCallback

        Private Sub DoRecursiveReceive(state As StateObject)
            Dim client As Socket = state.workSocket
            ' Check if we need more data or have finished the transfer.
            Dim Difference As Integer = state.TotalBytesToRead - state.TotalBytesRead

            If Difference < 0 Then
                ' Message corrupt
                If isPacketFailureRecoveryEnabled Then
                    ' Attempt To Recover
                    Send(client, New ObjectTicketRequest(state.ID))
                Else
                    RaiseEvent OnError(client, New SocketException(SocketError.NoRecovery))
                End If
            ElseIf Difference <= StateObject.BufferSize Then
                ' Done After Next Receive.
                client.BeginReceive(state.buffer, 0, Difference, 0, New AsyncCallback(AddressOf FinishReceiveCallback), state)
            Else
                ' Get the rest of the data.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, New AsyncCallback(AddressOf ReceiveCallback), state)
            End If
        End Sub

        Private Sub FinishReceiveCallback(ByVal ar As IAsyncResult)

            ' Retrieve the state object and the client socket 
            ' from the asynchronous state object.
            Dim state As StateObject = CType(ar.AsyncState, StateObject)
            Dim client As Socket = state.workSocket

            ' Read data from the remote device.
            Dim bytesRead As Integer = client.EndReceive(ar)

            ' Add temporary buffer data to final output buffer.
            Dim BufferArrayList As New List(Of Byte)(state.buffer)
            state.ObjectData.AddRange(BufferArrayList.GetRange(0, bytesRead))
            state.TotalBytesRead += bytesRead
            Try
                Dim data As Object = Protocol.Deserialize(state.ObjectData.ToArray)
                If Not IsNothing(data) And Not TypeOf (data) Is ObjectTicketRequest Then

                    RaiseEvent OnReceive(client, data, state.TotalBytesRead)

                ElseIf Not IsNothing(data) And TypeOf (data) Is ObjectTicketRequest Then
                    ResendData(client, data)
                End If
            Catch ex As Runtime.Serialization.SerializationException
                Send(client, New ObjectTicketRequest(state.ID))
            End Try
            FinishReceiveClean(state)
        End Sub
        Private Sub ResendData(Sender As Socket, Obj As ObjectTicketRequest)
            'If we receive a TicketRequest (data failure) 
            If TicketCache.ContainsKey(New Tuple(Of Socket, String)(Sender, Obj.ID)) Then
                'If we still have the ticket for this client
                Dim Ticket As ObjectTicket = TicketCache(New Tuple(Of Socket, String)(Sender, Obj.ID))
                'and the client has only been resent it less than 3 times
                If Ticket.ResendRequests < 3 Then
                    'Resend the data
                    Ticket.ResendRequests += 1
                    Send(Sender, Ticket.Data)
                End If
                'If the ticket requests are 3 or over, the ticket expires
                If Ticket.ResendRequests >= 3 Then
                    TicketCache.Remove(New Tuple(Of Socket, String)(Sender, Obj.ID))
                End If
            End If
        End Sub
        Private Sub FinishReceiveClean(ByRef State As StateObject)
            ' Clean Up
            Dim client As Socket = State.workSocket
            State.TotalBytesToRead = 0
            State.TotalBytesRead = 0
            State.buffer = New Byte(StateObject.BufferSize - 1) {}
            State.PaddingBuffer = New Byte(StateObject.PaddingBufferSize - 1) {}
            State.ObjectData.Clear()
            State = Nothing

            Receive(client)
        End Sub
        Private Sub UpdateRecoveryCache(Client As Socket, MessageID As String, Data As Byte())
            TicketCache.Add(New Tuple(Of Socket, String)(Client, MessageID), New ObjectTicket(Data))
            'Add our new ticket
            Dim DifferentMessageIDs As New List(Of String)
            For Each item In TicketCache
                'If the id has not been checked before, add it to our list
                If Not DifferentMessageIDs.Contains(item.Key.Item2) Then
                    DifferentMessageIDs.Add(item.Key.Item2)
                End If
            Next
            'If we contain over 10 different messages
            If DifferentMessageIDs.Count > PacketFailureCacheSize Then
                For i = 0 To DifferentMessageIDs.Count - PacketFailureCacheSize

#If NET20 Then
                    TicketCache.Remove(GetFirstTicketKey)
#Else
                    TicketCache.Remove(TicketCache.Keys(0))
#End If
                    ' Remove the amount we need to achieve our count back to 10 again from top, as top is the oldest
                Next
            End If
        End Sub
#If NET20 Then
        Private Function GetFirstTicketKey() As Tuple(Of Socket, String)
            For Each Key As Tuple(Of Socket, String) In TicketCache.Keys
                Return Key
            Next
            Return Nothing
        End Function
#End If
        Private Sub SendCallBack(ByVal ar As IAsyncResult)
            ' Retrieve the socket from the state object.
            Dim client As Socket = CType(ar.AsyncState, Socket)
            ' Complete sending the data to the remote device.
            Dim bytesSent As Integer = client.EndSend(ar)
            'Console.WriteLine("Sent {0} bytes to server.", bytesSent)
            ' Signal that all bytes have been sent.

            RaiseEvent OnSent(client, bytesSent)
        End Sub
        Friend Sub Send(sender As Socket, Obj As Object)
            Send(sender, Protocol.Serialize(Obj))
        End Sub
        Friend Sub Send(sender As Socket, Bytes() As Byte)
            Try
                Dim RAW As Object() = PrepareSend(Bytes)
                Dim ObjectData As Byte() = RAW(0)
                Dim MessageID As String = RAW(1)
                If isPacketFailureRecoveryEnabled Then UpdateRecoveryCache(sender, MessageID, ObjectData)
                sender.BeginSend(ObjectData, 0, ObjectData.Length, SocketFlags.None, AddressOf SendCallBack, sender)
            Catch e As Exception
                If Not IsNothing(sender) And sender.Connected Then
                    RaiseEvent OnConnectionInterrupt(sender)
                    sender.Close()
                    Exit Sub
                End If
            End Try
        End Sub

        Private Function PrepareSend(Data As Byte()) As Object()
            Dim PaddingInfo As String = GetPaddingInformation(Data)
            Dim MessageID As String = PaddingInfo.Substring(10, 32)
            Dim DataLength As String = PaddingInfo.Substring(0, 10)
            Dim LengthData As Byte() = Text.Encoding.UTF8.GetBytes(PaddingInfo)
            Return {Helpers.CombineByteArrays({LengthData, Data}), MessageID}
        End Function

        Private Function GetPaddingInformation(Data As Byte()) As String
            Return CInt(Data.Length + StateObject.PaddingBufferSize).ToString("D10") +
                   (Guid.NewGuid.ToString.Replace("-", ""))
        End Function

        Private Sub CloseSocket(s As Socket)
            RaiseEvent OnRelease(s, s.RemoteEndPoint)
            s.Close()
        End Sub

        'Private Sub BaseTCPSocket_OnError(sender As Socket, e As Exception) Handles Me.OnError

        'End Sub
    End Class

    Public Class StateObject
        ' Client socket.
        Public workSocket As Socket = Nothing
        ' Size of receive buffer.
        Public Const BufferSize As Integer = 4096
        Public Const PaddingBufferSize As Integer = 42 ' 32 + 10 (GUID + int)
        Public TotalBytesRead As Integer = 0
        Public TotalBytesToRead As Integer = 0
        ' Receive buffer.
        Public buffer(BufferSize - 1) As Byte
        Public PaddingBuffer(PaddingBufferSize - 1) As Byte
        Public ObjectData As New List(Of Byte)
        'The GUID of the data we're getting
        Public ID As String
    End Class 'StateObject

End Namespace