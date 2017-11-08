Module Module1

    Sub Main()

    End Sub

End Module

Class A

    Public value As Integer = 1
    Sub Action1(Optional val As Integer = value) ' BC30369 - WEIRD COMPILER ERROR 
    End Sub

    Public Shared sharedValue As Integer = 2
    Sub Action2(Optional val As Integer = sharedValue) 'BC30059 - CORRECT COMPILER ERROR 
    End Sub

End Class