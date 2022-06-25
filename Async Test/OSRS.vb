Imports System.Net.Sockets
Imports System.Runtime.InteropServices
Imports System.Threading
Imports MicroSerializationLibrary
Imports MicroSerializationLibrary.Networking
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

<Serializable>
Public Class iMessage
    Public Property Name As String
    Public Property Message As String
    Sub New()
    End Sub
    Sub New(Name As String, Message As String)
        Me.Name = Name
        Me.Message = Message
    End Sub
End Class

Module OSRS

    Dim messageQueue As New List(Of Message)
    Dim SerializerEngine As MicroSerializationLibrary.Serialization.ISerializationProtocol = New MicroSerializationLibrary.Serialization.BinaryFormatterSerializer

    Public WithEvents client As New Networking.Client.TcpClient(SerializerEngine)
    Public WithEvents server As New Networking.Server.TcpServer(SerializerEngine, 4237)
    Dim cIndex As Integer = 0

    Private Sub server_OnReceive(sender As Socket, obj As Object, BytesReceived As Integer) Handles server.OnReceive
        If Not sw.IsRunning Then
            sw.Start()
        End If
        Select Case obj.GetType
            Case GetType(Message)
                Dim M As Message = DirectCast(obj, Message)
                cIndex += 1
                If cIndex = SendAmount - 1 Then
                    sw.Stop()

                    Console.WriteLine(sw.Elapsed.TotalSeconds)
                End If
        End Select
    End Sub
    Private sw As New Stopwatch
    Dim isServer As Boolean = False
    Sub Main()
        ' Start The Server
        If isServer Then
            server.Listen(1000)
        Else
            ' Connect to the server
            server.Listen(1000)
            client.Connect("127.0.0.1", 4237)
            WorkLoop()
        End If

        Console.ReadLine()
    End Sub
    Dim SendAmount As Integer = 33333
    Public Sub WorkLoop()
        Dim MsgData As String = DuplicateString(GenerateCode(16.7), 3)


        messageQueue.Clear()

        For i As Integer = 0 To SendAmount - 1
            Dim MessageID As String = Guid.NewGuid.ToString
            Dim MSG As New Message(MessageID, i)
            Dim start As Long = Stopwatch.GetTimestamp
            Dim List As List(Of String) = New List(Of String)
            messageQueue.Add(MSG)
        Next

        messageQueue.ForEach(
            Sub(m)
                client.Send(m)
                'Console.WriteLine(String.Format("Sent Message"))
                ImprovedSpinWait.SpinFor(20)
            End Sub)
        sw.Stop()
        'Console.WriteLine(sw.Elapsed.TotalSeconds)

    End Sub

    Public Sub DoUntilWorked(ByVal action As Action)
        Dim sw As New SpinWait
        Dim worked As Boolean = False
        Do Until worked
            Try
                action.Invoke()
                worked = True
            Catch ex As Exception
                sw.SpinOnce()
            End Try
        Loop
    End Sub

    Public Function DuplicateString(Input As String, Multiples As Integer) As String
        Dim output As String = Nothing
        For i As Integer = 0 To Multiples - 1
            output = Input & output
        Next
        Return output
    End Function

    Public Function GenerateCode(Optional ByVal intnamelength As Integer = 10) As String
        Dim intrnd As Object
        Dim intstep As Object
        Dim strname As Object
        Dim intlength As Object
        Dim strinputstring As Object
        strinputstring = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
        intlength = Len(strinputstring)
        Randomize()
        strname = ""
        For intstep = 1 To intnamelength
            intrnd = Int((intlength * Rnd()) + 1)
            strname = strname & Mid(strinputstring, intrnd, 1)
        Next
        Return strname
    End Function

    Private Sub client_OnConnected(Sender As Socket) Handles client.OnConnected
        sw.Start()
    End Sub
End Module