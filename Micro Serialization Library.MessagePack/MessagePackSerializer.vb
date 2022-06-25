Imports MicroSerializationLibrary.Networking
Imports MsgPack.Serialization

''' <summary>
''' The Message Pack Serializer is faster and slimmer than JSON, but not without limitations. 
''' https://github.com/msgpack/msgpack/blob/master/spec.md#limitation
''' </summary>
Public Class MessagePackSerializer
        Implements ISerializationProtocol

        Dim serializer As IMessagePackSingleObjectSerializer = SerializationContext.Default.GetSerializer(Of DeserializationWrapper)

        Private Function ISerializationProtocol_Serialize(Obj As Object) As Byte() Implements ISerializationProtocol.Serialize
        Dim WrapperObject As New DeserializationWrapper(Obj, Me)
        Return serializer.PackSingleObject(WrapperObject)
        End Function

        Public Function Deserialize(Data As Byte()) As Object Implements ISerializationProtocol.Deserialize
            Dim jsonstr As String = System.Text.UnicodeEncoding.UTF8.GetString(Data)
            Dim ObjWrapper As DeserializationWrapper = serializer.UnpackSingleObject(Data)
            Return ObjWrapper.GetInitializedObject(Me)
        End Function

    Public Function GetPropertyValue(PropertyName As String, PropertyIndex As Integer, ByRef ObjectData As Object, pr As PropertyReference) As Object Implements ISerializationProtocol.GetPropertyValue
        Dim MsgPackObj As IList(Of MsgPack.MessagePackObject) = DirectCast(ObjectData, MsgPack.MessagePackObject).AsList
        Return MsgPackObj(MsgPackObj.Count - PropertyIndex).ToObject
    End Function

End Class
