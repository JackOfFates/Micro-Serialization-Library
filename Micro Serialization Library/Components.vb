Public Class ImprovedSpinWait

    Public Overloads Shared Sub SpinFor(millisecondsTimeout As Double)
        SpinFor(CLng(millisecondsTimeout * TimeSpan.TicksPerMillisecond))
    End Sub

    Public Overloads Shared Sub SpinFor(Ticks As Long)
        Dim s As New Stopwatch
        s.Start()

        Do Until s.Elapsed.Ticks >= Ticks
            Threading.Thread.SpinWait(1)
        Loop

        s.Stop()
    End Sub

End Class

Public Enum MouseButton As Short
    Up = 0
    Down = 1
End Enum