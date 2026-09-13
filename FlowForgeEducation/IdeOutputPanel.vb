Imports System
Imports System.Drawing
Imports System.Text.RegularExpressions
Imports System.Windows.Forms

Namespace FlowForgeStudio
    Public Class CompilerIssueEventArgs
        Inherits EventArgs

        Public ReadOnly FileName As String
        Public ReadOnly LineNumber As Integer
        Public ReadOnly ColumnNumber As Integer
        Public ReadOnly ErrorCode As String
        Public ReadOnly MessageText As String

        Public Sub New(fileNameValue As String, lineValue As Integer, columnValue As Integer, codeValue As String, messageValue As String)
            FileName = fileNameValue
            LineNumber = lineValue
            ColumnNumber = columnValue
            ErrorCode = codeValue
            MessageText = messageValue
        End Sub
    End Class

    Public Class IdeOutputPanel
        Inherits UserControl

        Private ReadOnly tabs As New TabControl()
        Private ReadOnly outputBox As New TextBox()
        Private ReadOnly errors As New ListView()
        Private ReadOnly header As New Panel()
        Private ReadOnly headerLabel As New Label()
        Private ReadOnly closeButton As New Button()

        Public Event IssueActivated As EventHandler(Of CompilerIssueEventArgs)
        Public Event CloseRequested As EventHandler

        Public Sub New()
            BackColor = Color.FromArgb(24, 26, 31)

            header.Dock = DockStyle.Top
            header.Height = 26
            header.BackColor = Color.FromArgb(31, 34, 40)
            header.Padding = New Padding(8, 0, 2, 0)

            headerLabel.Dock = DockStyle.Fill
            headerLabel.Text = "Saída / Lista de Erros"
            headerLabel.ForeColor = Color.Gainsboro
            headerLabel.TextAlign = ContentAlignment.MiddleLeft

            closeButton.Dock = DockStyle.Right
            closeButton.Width = 30
            closeButton.FlatStyle = FlatStyle.Flat
            closeButton.FlatAppearance.BorderSize = 0
            closeButton.Text = "×"
            closeButton.Font = New Font("Segoe UI", 11.0F, FontStyle.Regular)
            closeButton.ForeColor = Color.Gainsboro
            closeButton.BackColor = Color.FromArgb(31, 34, 40)
            closeButton.Cursor = Cursors.Hand
            closeButton.TabStop = False
            closeButton.AccessibleName = "Fechar painel de saída"
            AddHandler closeButton.Click, AddressOf CloseButtonClick

            header.Controls.Add(headerLabel)
            header.Controls.Add(closeButton)

            tabs.Dock = DockStyle.Fill

            outputBox.Dock = DockStyle.Fill
            outputBox.Multiline = True
            outputBox.ReadOnly = True
            outputBox.ScrollBars = ScrollBars.Both
            outputBox.WordWrap = False
            outputBox.Font = New Font("Consolas", 9.0F)
            outputBox.BackColor = Color.FromArgb(17, 19, 24)
            outputBox.ForeColor = Color.Gainsboro
            outputBox.BorderStyle = BorderStyle.None

            errors.Dock = DockStyle.Fill
            errors.View = View.Details
            errors.FullRowSelect = True
            errors.GridLines = False
            errors.HideSelection = False
            errors.MultiSelect = False
            errors.BackColor = Color.FromArgb(17, 19, 24)
            errors.ForeColor = Color.Gainsboro
            errors.BorderStyle = BorderStyle.None
            errors.Columns.Add("Arquivo", 180)
            errors.Columns.Add("Linha", 58)
            errors.Columns.Add("Col.", 48)
            errors.Columns.Add("Código", 72)
            errors.Columns.Add("Descrição", 520)
            AddHandler errors.DoubleClick, AddressOf ActivateSelectedIssue
            AddHandler errors.KeyDown, AddressOf ErrorsKeyDown

            Dim outputTab As New TabPage("Saída") With {.BackColor = Color.FromArgb(17, 19, 24)}
            outputTab.Controls.Add(outputBox)
            Dim errorsTab As New TabPage("Lista de Erros") With {.BackColor = Color.FromArgb(17, 19, 24)}
            errorsTab.Controls.Add(errors)
            tabs.TabPages.Add(outputTab)
            tabs.TabPages.Add(errorsTab)
            Controls.Add(tabs)
            Controls.Add(header)
        End Sub

        Public ReadOnly Property ErrorCount As Integer
            Get
                Return errors.Items.Count
            End Get
        End Property

        Public Sub ClearAll()
            outputBox.Clear()
            errors.Items.Clear()
        End Sub

        Public Sub ClearErrors()
            errors.Items.Clear()
        End Sub

        Public Sub SetOutput(text As String)
            outputBox.Text = If(text, "")
            outputBox.SelectionStart = outputBox.TextLength
            outputBox.SelectionLength = 0
        End Sub

        Public Sub SetCompilerOutput(text As String)
            SetOutput(text)
            errors.Items.Clear()
            If String.IsNullOrWhiteSpace(text) Then
                tabs.SelectedIndex = 0
                Return
            End If

            Dim pattern As String = "^(?<file>.+?)\s+[—-]\s+linha\s+(?<line>\d+),\s*coluna\s+(?<column>\d+):\s*(?<code>[A-Za-z]+\d+)\s+(?<message>.+)$"
            For Each rawLine As String In text.Replace(System.Environment.NewLine, System.Convert.ToChar(10).ToString()).Split(New Char() {System.Convert.ToChar(10)})
                Dim line As String = rawLine.Trim()
                Dim match As Match = Regex.Match(line, pattern, RegexOptions.IgnoreCase)
                If Not match.Success Then Continue For
                Dim lineNumber As Integer
                Dim columnNumber As Integer
                Integer.TryParse(match.Groups("line").Value, lineNumber)
                Integer.TryParse(match.Groups("column").Value, columnNumber)
                Dim values As String() = {
                    match.Groups("file").Value,
                    lineNumber.ToString(),
                    columnNumber.ToString(),
                    match.Groups("code").Value,
                    match.Groups("message").Value
                }
                Dim item As New ListViewItem(values)
                item.Tag = New CompilerIssueEventArgs(values(0), lineNumber, columnNumber, values(3), values(4))
                errors.Items.Add(item)
            Next
            tabs.SelectedIndex = If(errors.Items.Count > 0, 1, 0)
        End Sub

        Public Sub ShowOutputTab()
            tabs.SelectedIndex = 0
        End Sub

        Public Sub ShowErrorsTab()
            tabs.SelectedIndex = 1
        End Sub

        Private Sub CloseButtonClick(sender As Object, e As EventArgs)
            RaiseEvent CloseRequested(Me, EventArgs.Empty)
        End Sub

        Private Sub ErrorsKeyDown(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Enter Then
                ActivateSelectedIssue(sender, EventArgs.Empty)
                e.SuppressKeyPress = True
            End If
        End Sub

        Private Sub ActivateSelectedIssue(sender As Object, e As EventArgs)
            If errors.SelectedItems.Count = 0 Then Return
            Dim issue As CompilerIssueEventArgs = TryCast(errors.SelectedItems(0).Tag, CompilerIssueEventArgs)
            If issue IsNot Nothing Then RaiseEvent IssueActivated(Me, issue)
        End Sub
    End Class
End Namespace
