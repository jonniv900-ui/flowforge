Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq

Namespace FlowForgeStudio
    Friend NotInheritable Class RecentProjectStore
        Private Sub New()
        End Sub

        Private Shared Function ListFile(edition As String) As String
            Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), edition, "RecentProjects.txt")
        End Function

        Public Shared Function Load(edition As String) As List(Of String)
            Try
                If Not File.Exists(ListFile(edition)) Then Return New List(Of String)()
                Return File.ReadAllLines(ListFile(edition)).Where(Function(itemPath) Not String.IsNullOrWhiteSpace(itemPath) AndAlso File.Exists(itemPath)).Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToList()
            Catch
                Return New List(Of String)()
            End Try
        End Function

        Public Shared Sub Add(edition As String, filePath As String)
            Try
                Dim items As List(Of String) = Load(edition)
                items.RemoveAll(Function(itemPath) itemPath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                items.Insert(0, filePath)
                If items.Count > 10 Then items.RemoveRange(10, items.Count - 10)
                Directory.CreateDirectory(Path.GetDirectoryName(ListFile(edition)))
                File.WriteAllLines(ListFile(edition), items.ToArray())
            Catch
            End Try
        End Sub

        Public Shared Sub Clear(edition As String)
            Try
                If File.Exists(ListFile(edition)) Then File.Delete(ListFile(edition))
            Catch
            End Try
        End Sub
    End Class
End Namespace
