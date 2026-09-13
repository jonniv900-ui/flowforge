Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Windows.Forms

Namespace FlowForgeStudio
    Public Class ResourceManagerForm
        Inherits Form
        Private ReadOnly _project As FlowProject
        Private ReadOnly _list As New ListView()
        Public Sub New(project As FlowProject)
            _project = project
            Text = "Recursos do Projeto"
            Width = 760 : Height = 470 : StartPosition = FormStartPosition.CenterParent
            _list.Dock = DockStyle.Fill : _list.View = View.Details : _list.FullRowSelect = True : _list.GridLines = True
            _list.Columns.Add("Nome", 190) : _list.Columns.Add("Caminho", 300) : _list.Columns.Add("Tamanho", 110)
            Dim bar As New FlowLayoutPanel With {.Dock = DockStyle.Top, .Height = 42, .Padding = New Padding(6)}
            Dim addButton As New Button With {.Text = "Adicionar arquivo...", .AutoSize = True}
            Dim removeButton As New Button With {.Text = "Remover", .AutoSize = True}
            Dim pathButton As New Button With {.Text = "Copiar GetPath", .AutoSize = True}
            Dim imageButton As New Button With {.Text = "Copiar GetImage", .AutoSize = True}
            Dim bytesButton As New Button With {.Text = "Copiar GetBytes", .AutoSize = True}
            AddHandler addButton.Click, AddressOf AddResource
            AddHandler removeButton.Click, AddressOf RemoveResource
            AddHandler pathButton.Click, Sub() CopySnippet("GetPath")
            AddHandler imageButton.Click, Sub() CopySnippet("GetImage")
            AddHandler bytesButton.Click, Sub() CopySnippet("GetBytes")
            bar.Controls.AddRange(New Control() {addButton, removeButton, pathButton, imageButton, bytesButton})
            Controls.Add(_list) : Controls.Add(bar)
            RefreshList()
        End Sub
        Private Sub RefreshList()
            _list.Items.Clear()
            If _project.Libraries Is Nothing Then Return
            For Each item As ProjectLibrary In _project.Libraries.Where(Function(x) Not x.IsReference AndAlso x.RelativePath.Replace("\"c, "/"c).StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
                Dim row As New ListViewItem(item.FileName)
                row.SubItems.Add(item.RelativePath)
                row.SubItems.Add(FormatBytes(item.FileSize))
                row.Tag = item
                _list.Items.Add(row)
            Next
        End Sub
        Private Sub AddResource(sender As Object, e As EventArgs)
            Using dialog As New OpenFileDialog With {.Title = "Adicionar recurso", .Filter = "Todos os arquivos (*.*)|*.*", .Multiselect = True}
                If dialog.ShowDialog(Me) <> DialogResult.OK Then Return
                If _project.Libraries Is Nothing Then _project.Libraries = New List(Of ProjectLibrary)()
                For Each filePath As String In dialog.FileNames
                    Dim relative As String = "Resources/" & Path.GetFileName(filePath)
                    Dim existing As ProjectLibrary = _project.Libraries.FirstOrDefault(Function(x) x.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase))
                    If existing Is Nothing Then
                        existing = New ProjectLibrary()
                        _project.Libraries.Add(existing)
                    End If
                    Dim data As Byte() = File.ReadAllBytes(filePath)
                    existing.FileName = Path.GetFileName(filePath)
                    existing.RelativePath = relative
                    existing.DataBase64 = Convert.ToBase64String(data)
                    existing.FileSize = data.LongLength
                    existing.IsReference = False
                    existing.EmbedInExecutable = True
                Next
            End Using
            RefreshList()
        End Sub
        Private Sub RemoveResource(sender As Object, e As EventArgs)
            If _list.SelectedItems.Count = 0 Then Return
            Dim item As ProjectLibrary = TryCast(_list.SelectedItems(0).Tag, ProjectLibrary)
            If item Is Nothing Then Return
            If MessageBox.Show("Remover o recurso '" & item.FileName & "'?", "Recursos", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            _project.Libraries.Remove(item)
            RefreshList()
        End Sub
        Private Sub CopySnippet(methodName As String)
            If _list.SelectedItems.Count = 0 Then
                MessageBox.Show("Selecione um recurso primeiro.", "Recursos", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Dim item As ProjectLibrary = DirectCast(_list.SelectedItems(0).Tag, ProjectLibrary)
            Dim quoteChar As String = System.Convert.ToChar(34).ToString()
            Clipboard.SetText("FlowForgeResources." & methodName & "(" & quoteChar & item.RelativePath.Replace(Path.DirectorySeparatorChar, "/"c) & quoteChar & ")")
        End Sub
        Private Shared Function FormatBytes(value As Long) As String
            If value >= 1048576 Then Return (value / 1048576.0R).ToString("0.00") & " MB"
            If value >= 1024 Then Return (value / 1024.0R).ToString("0.0") & " KB"
            Return value.ToString() & " B"
        End Function
    End Class
End Namespace
