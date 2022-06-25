Public Class Tuple(Of T1, T2)
    Public Property Item1() As T1
        Get
            Return m_First
        End Get
        Private Set
            m_First = Value
        End Set
    End Property
    Private m_First As T1
    Public Property Item2() As T2
        Get
            Return m_Second
        End Get
        Private Set
            m_Second = Value
        End Set
    End Property
    Private m_Second As T2
    Friend Sub New(first__1 As T1, second__2 As T2)
        Item1 = first__1
        Item2 = second__2
    End Sub
End Class

Public NotInheritable Class Tuple
    Private Sub New()
    End Sub
    Public Shared Function [New](Of T1, T2)(Item1 As T1, Item2 As T2) As Tuple(Of T1, T2)
        Dim tuple = New Tuple(Of T1, T2)(Item1, Item2)
        Return tuple
    End Function
End Class