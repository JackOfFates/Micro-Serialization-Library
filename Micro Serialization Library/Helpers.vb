Imports System.Runtime.CompilerServices
Imports System.Threading

Public Class Helpers

    Public Shared Sub IterateByteArray(target As Byte()(), operation As Action(Of Byte()))
        For Each ByteArray As Byte() In target
            operation.Invoke(ByteArray)
        Next
    End Sub

    Public Shared Function IterateByteArray(target As Byte()()) As Byte()()
        Dim BA As New List(Of Byte())
        For Each ByteArray As Byte() In target
            BA.Add(ByteArray)
        Next
        Return BA.ToArray
    End Function

    Public Shared Function GetByteArraySizes(target As Byte()()) As Long
        Dim Size As Long = 0
        ' IterateByteArray(target, Sub(ByteArray) Size += ByteArray.Length)
        For Each ByteArray As Byte() In IterateByteArray(target)
            Size += ByteArray.Length
        Next
        Return Size
    End Function

    Public Shared Function CombineByteArrays(target As Byte()()) As Byte()
        Dim Size As Long = GetByteArraySizes(target)
        Dim Merged As Byte() = New Byte(Size - 1) {}
        Dim Index As Integer = 0
        For Each ByteArray As Byte() In target
            ByteArray.CopyTo(Merged, Index)
            Index += ByteArray.Length
        Next
        Return Merged
    End Function

End Class