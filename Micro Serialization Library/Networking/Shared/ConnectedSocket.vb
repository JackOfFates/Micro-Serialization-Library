Imports System.Net.Sockets

Namespace Networking
    ''' <summary>
    ''' This class will contain a connected socket and a running thread that checks for recieved messages
    ''' </summary>
    ''' <remarks></remarks>
    Public Class ConnectedSocket
        Public Property CurrentSocket As Socket
        Sub New(CurrentSocket As Socket)
            Me.CurrentSocket = CurrentSocket
        End Sub
    End Class
End Namespace