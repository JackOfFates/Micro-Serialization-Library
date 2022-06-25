Imports System.Linq

Namespace Serialization

    ''' <summary>
    ''' This protocol allow methods that deserialize and serialize object to use the same language. 
    ''' </summary>
    ''' <remarks></remarks>
    Public Interface ISerializationProtocol

        Function Serialize(Obj As Object) As Byte()
        Function Deserialize(Data As Byte()) As Object
        Function GetPropertyValue(PropertyName As String, PropertyIndex As Integer, ByRef ObjectData As Object, pr As PropertyReference) As Object

    End Interface

    ''' <summary>
    ''' The Default MS BinaryFormatter Serializer Engine. It's Slow and Picky.
    ''' </summary>
    Public Class BinaryFormatterSerializer
        Implements ISerializationProtocol

        Public Function Deserialize(Data() As Byte) As Object Implements ISerializationProtocol.Deserialize
            Dim bfTemp As New Runtime.Serialization.Formatters.Binary.BinaryFormatter
            Using sStream As New IO.MemoryStream(Data)
                Dim Obj As Object = bfTemp.Deserialize(sStream)
                Return Obj
            End Using
        End Function

        Public Function GetPropertyValue(PropertyName As String, PropertyIndex As Integer, ByRef ObjectData As Object, pr As PropertyReference) As Object Implements ISerializationProtocol.GetPropertyValue
            Return ObjectData(PropertyName)
        End Function

        Public Function Serialize(Obj As Object) As Byte() Implements ISerializationProtocol.Serialize
            Dim sStream As New IO.MemoryStream()
            Dim bfTemp As New Runtime.Serialization.Formatters.Binary.BinaryFormatter
            bfTemp.AssemblyFormat = Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
            bfTemp.Serialize(sStream, Obj)
            Return sStream.ToArray
        End Function
    End Class

End Namespace