Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Windows.Forms

Namespace FlowForgeStudio
    Friend Class ProjectLibrariesForm
        Inherits Form

        Private ReadOnly project As FlowProject
        Private ReadOnly files As New ListView()
        Private ReadOnly info As New Label()

        Public Sub New(value As FlowProject)
            project = value
            If project.Libraries Is Nothing Then project.Libraries = New List(Of ProjectLibrary)()
            Text = "Bibliotecas do projeto"
            Width = 820
            Height = 480
            MinimumSize = New Size(680, 380)
            StartPosition = FormStartPosition.CenterParent
            Font = New Font("Segoe UI", 9.0F)
            BuildInterface()
            RefreshList()
        End Sub

        Private Sub BuildInterface()
            files.Dock = DockStyle.Fill
            files.View = View.Details
            files.FullRowSelect = True
            files.GridLines = True
            files.HideSelection = False
            files.Columns.Add("Arquivo", 230)
            files.Columns.Add("Destino", 220)
            files.Columns.Add("Uso", 150)
            files.Columns.Add("Empacotamento", 135)
            files.Columns.Add("Tamanho", 110)

            info.Dock = DockStyle.Top
            info.Height = 54
            info.Padding = New Padding(10, 8, 10, 4)
            info.Text = "Os arquivos ficam dentro do .flowapp. No modo Incorporar, também entram no EXE final; DLLs nativas são extraídas automaticamente durante a execução."

            Dim buttons As New FlowLayoutPanel()
            buttons.Dock = DockStyle.Bottom
            buttons.Height = 86
            buttons.FlowDirection = FlowDirection.LeftToRight
            buttons.Padding = New Padding(8)
            buttons.WrapContents = True

            buttons.Controls.Add(CreateButton("Adicionar referência .NET...", AddressOf AddManaged, 180))
            buttons.Controls.Add(CreateButton("Adicionar DLL nativa...", AddressOf AddNative, 165))
            buttons.Controls.Add(CreateButton("Adicionar recurso...", AddressOf AddResource, 135))
            buttons.Controls.Add(CreateButton("Alterar destino...", AddressOf ChangeDestination, 130))
            buttons.Controls.Add(CreateButton("Alternar modo", AddressOf ToggleMode, 105))
            buttons.Controls.Add(CreateButton("Remover", AddressOf RemoveSelected, 90))

            Controls.Add(files)
            Controls.Add(buttons)
            Controls.Add(info)
        End Sub

        Private Function CreateButton(textValue As String, action As Action, widthValue As Integer) As Button
            Dim result As New Button()
            result.Text = textValue
            result.Width = widthValue
            result.Height = 30
            AddHandler result.Click, Sub(sender, e) action()
            Return result
        End Function

        Private Sub AddManaged()
            Using dialog As New OpenFileDialog()
                dialog.Filter = "Bibliotecas .NET (*.dll)|*.dll|Todos os arquivos|*.*"
                dialog.Multiselect = True
                dialog.Title = "Incorporar referências .NET"
                If dialog.ShowDialog(Me) <> DialogResult.OK Then Return
                For Each filePath As String In dialog.FileNames
                    Try
                        AssemblyName.GetAssemblyName(filePath)
                        AddOrReplace(filePath, True)
                    Catch ex As Exception
                        MessageBox.Show(IO.Path.GetFileName(filePath) & " não é uma biblioteca .NET válida." & Environment.NewLine & ex.Message, "Biblioteca .NET", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End Try
                Next
            End Using
            RefreshList()
        End Sub

        Private Sub AddNative()
            Using dialog As New OpenFileDialog()
                dialog.Filter = "Bibliotecas e arquivos de execução (*.dll;*.dat;*.json)|*.dll;*.dat;*.json|Todos os arquivos|*.*"
                dialog.Multiselect = True
                dialog.Title = "Incorporar bibliotecas nativas"
                If dialog.ShowDialog(Me) <> DialogResult.OK Then Return
                For Each filePath As String In dialog.FileNames
                    AddOrReplace(filePath, False)
                Next
            End Using
            RefreshList()
        End Sub

        Private Sub AddResource()
            Using dialog As New OpenFileDialog()
                dialog.Filter = "Imagens e recursos|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.ico;*.txt;*.json;*.xml;*.dat|Todos os arquivos|*.*"
                dialog.Multiselect = True
                dialog.Title = "Incorporar imagens e recursos"
                If dialog.ShowDialog(Me) <> DialogResult.OK Then Return
                For Each filePath As String In dialog.FileNames
                    AddOrReplace(filePath, False)
                Next
            End Using
            RefreshList()
        End Sub

        Private Sub AddOrReplace(filePath As String, isReference As Boolean)
            Dim relative As String = SuggestRelativePath(filePath, isReference)
            Dim existing As ProjectLibrary = project.Libraries.FirstOrDefault(Function(x) x.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase))
            If existing Is Nothing Then
                existing = New ProjectLibrary()
                project.Libraries.Add(existing)
            End If
            Dim bytes As Byte() = File.ReadAllBytes(filePath)
            existing.FileName = IO.Path.GetFileName(filePath)
            existing.RelativePath = relative
            existing.DataBase64 = Convert.ToBase64String(bytes)
            existing.IsReference = isReference
            existing.EmbedInExecutable = True
            existing.FileSize = bytes.LongLength
        End Sub

        Private Function SuggestRelativePath(filePath As String, isReference As Boolean) As String
            If isReference Then Return IO.Path.GetFileName(filePath)
            Dim parent As String = IO.Path.GetFileName(IO.Path.GetDirectoryName(filePath))
            If parent.Equals("x86", StringComparison.OrdinalIgnoreCase) OrElse parent.Equals("x64", StringComparison.OrdinalIgnoreCase) Then
                Return parent.ToLowerInvariant() & IO.Path.DirectorySeparatorChar & IO.Path.GetFileName(filePath)
            End If
            Return IO.Path.GetFileName(filePath)
        End Function

        Private Sub ChangeDestination()
            If files.SelectedItems.Count = 0 Then Return
            Dim library As ProjectLibrary = DirectCast(files.SelectedItems(0).Tag, ProjectLibrary)
            Dim value As String = Microsoft.VisualBasic.Interaction.InputBox("Caminho relativo dentro da pasta do aplicativo:" & Environment.NewLine & "Exemplos: PdfiumViewer.dll, x64\pdfium.dll ou x86\pdfium.dll", "Destino da biblioteca", library.RelativePath)
            If String.IsNullOrWhiteSpace(value) Then Return
            value = value.Trim().Replace("/"c, Path.DirectorySeparatorChar)
            If Path.IsPathRooted(value) OrElse value.Contains("..") Then
                MessageBox.Show("Use somente um caminho relativo seguro.", "Destino inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            library.RelativePath = value
            RefreshList()
        End Sub

        Private Sub RemoveSelected()
            If files.SelectedItems.Count = 0 Then Return
            Dim selected As ProjectLibrary = DirectCast(files.SelectedItems(0).Tag, ProjectLibrary)
            If MessageBox.Show("Remover " & selected.RelativePath & " do projeto?", "Bibliotecas", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                project.Libraries.Remove(selected)
                RefreshList()
            End If
        End Sub

        Private Sub ToggleMode()
            If files.SelectedItems.Count = 0 Then Return
            For Each row As ListViewItem In files.SelectedItems
                Dim library As ProjectLibrary = DirectCast(row.Tag, ProjectLibrary)
                library.EmbedInExecutable = Not library.EmbedInExecutable
            Next
            RefreshList()
        End Sub

        Private Sub RefreshList()
            files.BeginUpdate()
            files.Items.Clear()
            For Each library As ProjectLibrary In project.Libraries.OrderBy(Function(x) x.RelativePath)
                Dim row As New ListViewItem(library.FileName)
                row.SubItems.Add(library.RelativePath)
                row.SubItems.Add(If(library.IsReference, "Referência .NET", "Arquivo nativo/runtime"))
                row.SubItems.Add(If(library.EmbedInExecutable, "Dentro do EXE", "Ao lado do EXE"))
                row.SubItems.Add(FormatSize(library.FileSize))
                row.Tag = library
                files.Items.Add(row)
            Next
            files.EndUpdate()
            info.Text = project.Libraries.Count.ToString() & " arquivo(s) anexado(s). Selecione um item e use Alternar modo para escolher entre EXE único e cópia externa."
        End Sub

        Private Function FormatSize(value As Long) As String
            If value >= 1048576 Then Return (value / 1048576.0R).ToString("0.0") & " MB"
            If value >= 1024 Then Return (value / 1024.0R).ToString("0.0") & " KB"
            Return value.ToString() & " bytes"
        End Function
    End Class
End Namespace
