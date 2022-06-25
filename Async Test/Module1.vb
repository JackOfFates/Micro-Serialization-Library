Imports System.Net.Sockets
Imports System.Runtime.InteropServices
Imports System.Threading
Imports MicroSerializationLibrary
Imports MicroSerializationLibrary.Networking
Imports MicroSerializationLibrary.Serialization
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

<Serializable>
Public Class Message
    Public Property Name As String
    Public Property Message As String
    Public Property MethodReference As MethodReference
    Public Property MethodParameters As Object()
    Public Property InvokeMethodOnInstansiation As Boolean = False
    Sub New()
    End Sub
    Sub New(Name As String, Message As String, MethodReference As MethodReference, Optional MethodParameters As Object() = Nothing)
        Me.Name = Name
        Me.Message = Message
        If InvokeMethodOnInstansiation AndAlso MethodReference IsNot Nothing Then
            MethodReference.Info.Invoke(MethodReference.Instance, MethodParameters)
        End If
    End Sub
End Class

Module Module1

    Dim messageQueue As New List(Of Message)
    Dim SerializerEngine As MicroSerializationLibrary.Serialization.ISerializationProtocol = New MicroSerializationLibrary.Serialization.BinaryFormatterSerializer

    Public WithEvents client As New Networking.Client.TcpClient(SerializerEngine)
    Public WithEvents server As New Networking.Server.TcpServer(SerializerEngine, 4237)

    Dim isServer As Boolean = False

    Private Sub server_OnReceive(sender As Socket, obj As Object, BytesReceived As Integer) Handles server.OnReceive

        Select Case obj.GetType
            Case GetType(Message)
                Dim M As Message = DirectCast(obj, Message)

        End Select
    End Sub

    Sub Main()
        ' Start The Server
        If isServer Then
            server.Listen(1000)
        Else
            ' Connect to the server
            server.Listen(1000)
            client.Connect("127.0.0.1", 4237)

        End If

        Console.ReadLine()
    End Sub

    Public Function RequestTargetData(target As Object, methodName As String) As Message
        Dim m As New Message()
        For Each mr As MethodReference In DeserializationWrapper.GetMethodReferences(target)
            If mr.Info.Name = methodName Then

            End If
        Next

    End Function

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
        ' Ask the server if it wants any information.
        client.Send(RequestTargetData(My.Computer.Info), "ToString"))
    End Sub

    Private Sub client_OnReceive(sender As Socket, obj As Object, BytesReceived As Integer) Handles client.OnReceive

    End Sub
End Module