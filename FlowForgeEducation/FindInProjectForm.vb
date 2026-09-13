Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

Namespace FlowForgeStudio
    Friend NotInheritable Class ProjectSearchResult
        Public Property Kind As String
        Public Property Index As Integer
        Public Property FileName As String
        Public Property LineNumber As Integer
        Public Property Preview As String
    End Class

    Friend Class FindInProjectForm
        Inherits Form

        Private ReadOnly resultsView As New ListView()
        Public Property SelectedResult As ProjectSearchResult

        Public Sub New(searchText As String, results As List(Of ProjectSearchResult))
            Text = "Localizar no projeto — " & searchText
            Width = 920
            Height = 520
            StartPosition = FormStartPosition.CenterParent
            MinimumSize = New Size(650, 350)
            BackColor = Color.FromArgb(32, 35, 42)

            Dim header As New Label With {.Dock = DockStyle.Top, .Height = 34, .Padding = New Padding(8, 7, 0, 0), .ForeColor = Color.WhiteSmoke, .Text = results.Count.ToString() & " resultado(s) para '" & searchText & "'"}
            resultsView.Dock = DockStyle.Fill
            resultsView.View = View.Details
            resultsView.FullRowSelect = True
            resultsView.GridLines = False
            resultsView.HideSelection = False
            resultsView.BackColor = Color.FromArgb(24, 27, 33)
            resultsView.ForeColor = Color.WhiteSmoke
            resultsView.Font = New Font("Segoe UI", 9.0F)
            resultsView.Columns.Add("Arquivo", 190)
            resultsView.Columns.Add("Linha", 65)
            resultsView.Columns.Add("Trecho", 610)

            For Each result As ProjectSearchResult In results
                Dim item As New ListViewItem(result.FileName)
                item.SubItems.Add(result.LineNumber.ToString())
                item.SubItems.Add(result.Preview)
                item.Tag = result
                resultsView.Items.Add(item)
            Next

            AddHandler resultsView.DoubleClick, AddressOf AcceptSelection
            AddHandler resultsView.KeyDown, AddressOf ResultsKeyDown
            Controls.Add(resultsView)
            Controls.Add(header)
        End Sub

        Private Sub ResultsKeyDown(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Enter Then
                AcceptSelection(sender, EventArgs.Empty)
                e.SuppressKeyPress = True
            ElseIf e.KeyCode = Keys.Escape Then
                DialogResult = DialogResult.Cancel
                Close()
            End If
        End Sub

        Private Sub AcceptSelection(sender As Object, e As EventArgs)
            If resultsView.SelectedItems.Count = 0 Then Return
            SelectedResult = TryCast(resultsView.SelectedItems(0).Tag, ProjectSearchResult)
            If SelectedResult Is Nothing Then Return
            DialogResult = DialogResult.OK
            Close()
        End Sub
    End Class
End Namespace
