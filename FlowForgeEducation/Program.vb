Imports System
Imports System.Windows.Forms

Namespace FlowForgeStudio
    Friend Module Program
        <STAThread>
        Public Sub Main()
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)

            Dim initialProject As String = Nothing
            Dim args As String() = Environment.GetCommandLineArgs()
            If args IsNot Nothing AndAlso args.Length > 1 Then
                Dim candidate As String = args(1)
                If Not String.IsNullOrWhiteSpace(candidate) AndAlso
                   IO.File.Exists(candidate) AndAlso
                   IO.Path.GetExtension(candidate).Equals(".flowapp", StringComparison.OrdinalIgnoreCase) Then
                    initialProject = IO.Path.GetFullPath(candidate)
                End If
            End If

            Application.Run(New MainForm(initialProject))
        End Sub
    End Module
End Namespace
