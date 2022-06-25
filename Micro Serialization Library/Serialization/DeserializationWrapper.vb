Imports System.Reflection

Namespace Serialization
    <Serializable>
    Public Class DeserializationWrapper
        Public Shared Property ReflectionFlags As BindingFlags = BindingFlags.Instance Or BindingFlags.[Public]
        Public Sub New()
        End Sub

        Public Sub New(Obj As Object, Protocol As ISerializationProtocol)
            Me.Type = Obj.GetType.AssemblyQualifiedName
            Me.Data = Obj
        End Sub

        Public Property Type As String
        Public Property Data As Object

        Public Function GetInitializedObject(sender As ISerializationProtocol) As Object
            ' Try
            Dim theType As Type = System.Type.GetType(Type)
                Dim instance As Object = Activator.CreateInstance(theType)
                GetPropertyReferences(instance).ForEach(Sub(r) SetProperty(instance, r, sender))
                Return instance
            'Catch ex As Exception
            '    Debug.WriteLine(ex.Message)
            '    Return Nothing
            'End Try
        End Function


        Public Sub SetProperty(ByRef Instance As Object, Reference As PropertyReference, Protocol As ISerializationProtocol)
            With Reference
                If .Info.CanWrite Then

                    Try
                        Dim v = Protocol.GetPropertyValue( .Info.Name, .Index, Data, Reference)
                        .Info.SetValue(Instance, v, Nothing)
                    Catch ex As Exception
                    End Try
                End If
            End With
        End Sub

#Region "GetEnumReferences"

#End Region

#Region "GetPropertyReferences"

        Public Shared ExcludedProperties As New List(Of String) From {"GRAPHICSDEVICE"}

        Public Shared Function GetPropertyReferences(target As Object) As List(Of PropertyReference)
            Return GetPropertyReferences(target.GetType, ReflectionFlags, False, target)
        End Function

        Public Shared Function GetPropertyReferences(target As Object, ReflectionFlags As BindingFlags) As List(Of PropertyReference)
            Return GetPropertyReferences(target.GetType, ReflectionFlags, False, target)
        End Function

        Public Shared Function GetPropertyReferences(Type As Type) As List(Of PropertyReference)
            Return GetPropertyReferences(Type, ReflectionFlags)
        End Function

        Public Shared Function GetPropertyReferences(Type As Type, ReflectionFlags As BindingFlags, Optional CatchAllProperties As Boolean = False, Optional CarryInstance As Object = Nothing) As List(Of PropertyReference)
            Dim objProperties As PropertyInfo() = Type.GetProperties(ReflectionFlags)
            Dim instance As Object = Activator.CreateInstance(Type)
            If CarryInstance IsNot Nothing Then instance = CarryInstance
            Dim index As Integer = 0
            Dim References As New List(Of PropertyReference)
            For Each p As PropertyInfo In objProperties
                If ((Not CatchAllProperties) AndAlso (ExcludedProperties.Contains(p.Name.ToUpper))) Then Continue For
                References.Add(New PropertyReference(instance, p, index))
                index += 1
            Next
            Return References
        End Function

        Public Shared Function GetFieldReferences(target As Object) As List(Of FieldReference)
            Return GetFieldReferences(target.GetType, ReflectionFlags)
        End Function

        Public Shared Function GetFieldReferences(target As Object, ReflectionFlags As BindingFlags) As List(Of FieldReference)
            Return GetFieldReferences(target.GetType, ReflectionFlags)
        End Function

        Public Shared Function GetFieldReferences(Type As Type) As List(Of FieldReference)
            Return GetFieldReferences(Type, ReflectionFlags)
        End Function

        Public Shared Function GetFieldReferences(Type As Type, ReflectionFlags As BindingFlags) As List(Of FieldReference)
            Dim objProperties As FieldInfo() = Type.GetFields(ReflectionFlags)
            Dim instance As Object = Activator.CreateInstance(Type)
            Dim index As Integer = 0
            Dim References As New List(Of FieldReference)
            For Each p As FieldInfo In objProperties
                References.Add(New FieldReference(instance, p, index))
                index += 1
            Next
            Return References
        End Function

        Public Shared Function GetEnumReferences(EnumType As Type, ReflectionFlags As BindingFlags) As List(Of EnumReference)
            Dim valuesAslist As New List(Of String)
            valuesAslist.AddRange([Enum].GetValues(GetType(Type)))
            Dim pRefs As New List(Of EnumReference)
            Dim i As Integer = 0
            valuesAslist.ForEach(Sub(s)
                                     pRefs.Add(New EnumReference(EnumType, s, i))
                                     i += 1
                                 End Sub)
            Return pRefs
        End Function

        Public Shared Function GetEnumReferences(EnumType As Type) As List(Of EnumReference)
            Return GetEnumReferences(EnumType, ReflectionFlags)
        End Function

#End Region

#Region "GetMethodReferences"

        Public Shared ExcludedMethods As New List(Of String) From {"GETTYPE", "TOSTRING", "GETHASHCODE", "EQUALS", "GETDBCONTEXT"}

        Public Shared Function GetMethodReferences(target As Object) As List(Of MethodReference)
            Return GetMethodReferences(target.GetType, ReflectionFlags)
        End Function

        Public Shared Function GetMethodReferences(target As Object, includeFunctions As Boolean) As List(Of MethodReference)
            Return GetMethodReferences(target.GetType, ReflectionFlags, includeFunctions)
        End Function

        Public Shared Function GetMethodReferences(target As Object, ReflectionFlags As BindingFlags) As List(Of MethodReference)
            Return GetMethodReferences(target.GetType, ReflectionFlags, False)
        End Function

        Public Shared Function GetMethodReferences(target As Object, ReflectionFlags As BindingFlags, includeFunctions As Boolean) As List(Of MethodReference)
            Return GetMethodReferences(target.GetType, ReflectionFlags, includeFunctions, False)
        End Function

        Public Shared Function GetMethodReferences(Type As Type) As List(Of MethodReference)
            Return GetMethodReferences(Type, ReflectionFlags)
        End Function

        Public Shared Function GetMethodReferences(Type As Type, includeFunctions As Boolean) As List(Of MethodReference)
            Return GetMethodReferences(Type, ReflectionFlags, includeFunctions)
        End Function

        Public Shared Function GetMethodReferences(Type As Type, ReflectionFlags As BindingFlags, includeFunctions As Boolean, Optional CatchAllMethods As Boolean = False) As List(Of MethodReference)
            Dim objMethods As MethodInfo() = Type.GetMethods(ReflectionFlags Or (Not BindingFlags.SetProperty) Or (Not BindingFlags.GetProperty))
            Dim instance As Object = Activator.CreateInstance(Type)
            Dim index As Integer = 0
            Dim References As New List(Of MethodReference)
            If Not includeFunctions Then
                For Each m As MethodInfo In objMethods
                    If m.ReturnType Is Nothing Then
#If NET20 Then
                        Dim all As String = Join(ExcludedMethods.ToArray, "").ToUpper
                        If Not CatchAllMethods AndAlso (all.IndexOf(m.Name.ToUpper) <> -1) Then Continue For
#Else
                        If ((Not CatchAllMethods) AndAlso (ExcludedMethods.Contains(m.Name.ToUpper))) Then Continue For
#End If
                        References.Add(New MethodReference(instance, m, index))
                        index += 1
                    End If
                Next
            Else
                For Each m As MethodInfo In objMethods
#If NET20 Then
                    Dim all As String = Join(ExcludedMethods.ToArray, "").ToUpper
                    If Not CatchAllMethods AndAlso (all.IndexOf(m.Name.ToUpper) <> -1) Then Continue For
#Else
                    If ((Not CatchAllMethods) AndAlso (ExcludedMethods.Contains(m.Name.ToUpper))) Then Continue For
#End If

                    References.Add(New MethodReference(instance, m, index))
                    index += 1
                Next
            End If

            Return References
        End Function

#End Region

#Region "GetEventReferences"
        Public Shared Function GetEventReferences(Type As Type) As List(Of EventReference)
            Dim objProperties As EventInfo() = Type.GetEvents(ReflectionFlags)
            Dim instance As Object = Activator.CreateInstance(Type)
            Dim index As Integer = 0
            Dim References As New List(Of EventReference)
            For Each p As EventInfo In objProperties
                References.Add(New EventReference(instance, p, index))
                index += 1
            Next
            Return References
        End Function
#End Region

    End Class

    Public MustInherit Class BaseReference(Of T)

        Public Sub New(Instance As Object, Info As T, Index As Integer)
            Me.Instance = Instance
            Me.Info = Info
            Me.Index = Index
        End Sub

        Public Property Instance As Object
        Public Property Info As T
        Public Property Index As Integer

    End Class

    Public Class EventReference
        Inherits BaseReference(Of EventInfo)

        Public Sub New(Instance As Object, EventInfo As EventInfo, Index As Integer)
            MyBase.New(Instance, EventInfo, Index)
        End Sub
    End Class

    Public Class PropertyReference
        Inherits BaseReference(Of PropertyInfo)

        Public Sub New(Instance As Object, PropertyInfo As PropertyInfo, Index As Integer)
            MyBase.New(Instance, PropertyInfo, Index)
        End Sub
    End Class

    Public Class MethodReference
        Inherits BaseReference(Of MethodInfo)

        Public Sub New(Instance As Object, MethodInfo As MethodInfo, Index As Integer)
            MyBase.New(Instance, MethodInfo, Index)
        End Sub
    End Class

    Public Class FieldReference
        Inherits BaseReference(Of FieldInfo)

        Public Sub New(Instance As Object, FieldInfo As FieldInfo, Index As Integer)
            MyBase.New(Instance, FieldInfo, Index)
        End Sub
    End Class

    Public Class EnumReference
        Inherits BaseReference(Of String)

        Public Sub New(Instance As Object, EnumName As String, Index As Integer)
            MyBase.New(Instance, EnumName, Index)
        End Sub
    End Class

End Namespace