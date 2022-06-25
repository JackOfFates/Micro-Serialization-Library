Imports MicroSerializationLibrary.Networking
Imports MicroSerializationLibrary.Serialization
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

''' <summary>
''' The JSON serialization engine is slower than MessagePack, but supports a wider range of objects.
''' </summary>
Public Class JsonSerializer
    Implements ISerializationProtocol

    Public Function Serialize(obj As Object) As Byte() Implements ISerializationProtocol.Serialize
        Dim JsonString As String = JsonConvert.SerializeObject(New DeserializationWrapper(obj, Me))
        Return System.Text.UnicodeEncoding.UTF8.GetBytes(JsonString)
    End Function

    Public Function Deserialize(Data As Byte()) As Object Implements ISerializationProtocol.Deserialize
        Dim JsonString As String = System.Text.UnicodeEncoding.UTF8.GetString(Data)
        Dim ObjWrapper As DeserializationWrapper = JsonConvert.DeserializeObject(Of DeserializationWrapper)(JsonString)
        Return ObjWrapper.GetInitializedObject(Me)
    End Function

    Public Function GetPropertyValue(PropertyName As String, PropertyIndex As Integer, ByRef ObjectData As Object, pr As PropertyReference) As Object Implements ISerializationProtocol.GetPropertyValue
        Try
            Dim v As JValue = DirectCast(ObjectData, JObject)(PropertyName)
            Return v.Value
        Catch ex As Exception
            Dim v As Object
            Dim objD As Object = ObjectData
            DeserializationWrapper.GetPropertyReferences(pr.Instance).ForEach(
                Sub(p)
                    If p.Info.Name = pr.Info.Name Then
                        v = DirectCast(objD, JObject)(PropertyName).ToObject(p.Info.GetValue(p.Instance, {0}).GetType)
                    End If
                End Sub)
            Return v
        End Try
    End Function

End Class

