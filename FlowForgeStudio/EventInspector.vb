Imports System
Imports System.ComponentModel
Imports System.Drawing
Imports System.Linq
Imports System.Text.RegularExpressions
Imports System.Windows.Forms

Namespace FlowForgeStudio
    Friend Class EventInspector
        Inherits UserControl

        Public Event EventActivated(sender As Object, eventName As String)

        Private ReadOnly searchBox As New TextBox()
        Private ReadOnly list As New ListView()
        Private currentTarget As Object
        Private currentComponentName As String = ""
        Private currentCode As String = ""

        Public Sub New()
            Dock = DockStyle.Fill
            BackColor = SystemColors.Window

            searchBox.Dock = DockStyle.Top
            searchBox.Height = 26
            searchBox.BorderStyle = BorderStyle.FixedSingle
            searchBox.Tag = "Filtrar eventos..."

            list.Dock = DockStyle.Fill
            list.View = View.Details
            list.FullRowSelect = True
            list.HideSelection = False
            list.MultiSelect = False
            list.HeaderStyle = ColumnHeaderStyle.Nonclickable
            list.Columns.Add("Evento", 145)
            list.Columns.Add("Manipulador", 190)

            Controls.Add(list)
            Controls.Add(searchBox)

            AddHandler searchBox.TextChanged, Sub() RefreshList()
            AddHandler list.DoubleClick, AddressOf ActivateSelected
            AddHandler list.KeyDown, AddressOf ListKeyDown
            AddHandler Resize, AddressOf InspectorResize
        End Sub

        Public Sub SetTarget(target As Object, componentName As String, codeText As String)
            currentTarget = target
            currentComponentName = If(componentName, "")
            currentCode = If(codeText, "")
            RefreshList()
        End Sub

        Public Sub UpdateCode(codeText As String)
            currentCode = If(codeText, "")
            RefreshHandlersOnly()
        End Sub

        Public Sub ClearTarget()
            currentTarget = Nothing
            currentComponentName = ""
            currentCode = ""
            list.Items.Clear()
        End Sub

        Private Sub RefreshList()
            list.BeginUpdate()
            Try
                list.Items.Clear()
                If currentTarget Is Nothing Then Return

                Dim filter As String = searchBox.Text.Trim()
                Dim defaultEvent As EventDescriptor = TypeDescriptor.GetDefaultEvent(currentTarget)
                Dim events = TypeDescriptor.GetEvents(currentTarget).Cast(Of EventDescriptor)().OrderBy(Function(ev) ev.Name)

                For Each ev As EventDescriptor In events
                    If Not String.IsNullOrWhiteSpace(filter) AndAlso ev.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 Then Continue For
                    Dim handlerName As String = FindHandler(ev.Name)
                    Dim displayName As String = If(defaultEvent IsNot Nothing AndAlso ev.Name.Equals(defaultEvent.Name, StringComparison.OrdinalIgnoreCase), "★ " & ev.Name, ev.Name)
                    Dim item As New ListViewItem(displayName)
                    item.Tag = ev.Name
                    item.SubItems.Add(handlerName)
                    If handlerName <> "" Then item.Font = New Font(list.Font, FontStyle.Bold)
                    list.Items.Add(item)
                Next
            Finally
                list.EndUpdate()
            End Try
        End Sub

        Private Sub RefreshHandlersOnly()
            If list.Items.Count = 0 Then Return
            list.BeginUpdate()
            Try
                For Each item As ListViewItem In list.Items
                    Dim eventName As String = CStr(item.Tag)
                    Dim handlerName As String = FindHandler(eventName)
                    item.SubItems(1).Text = handlerName
                    item.Font = New Font(list.Font, If(handlerName <> "", FontStyle.Bold, FontStyle.Regular))
                Next
            Finally
                list.EndUpdate()
            End Try
        End Sub

        Private Function FindHandler(eventName As String) As String
            If String.IsNullOrWhiteSpace(currentCode) OrElse String.IsNullOrWhiteSpace(currentComponentName) Then Return ""
            Dim targetName As String = If(TypeOf currentTarget Is Form, "MyBase", currentComponentName)
            Dim pattern As String = "(?im)^\s*(?:Private|Public|Protected|Friend)?\s*(?:Async\s+)?Sub\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\([^\r\n]*\)\s*Handles\s*[^\r\n]*\b" & Regex.Escape(targetName) & "\." & Regex.Escape(eventName) & "\b"
            Dim match As Match = Regex.Match(currentCode, pattern)
            If match.Success Then Return match.Groups("name").Value
            Return ""
        End Function

        Private Sub ActivateSelected(sender As Object, e As EventArgs)
            If list.SelectedItems.Count = 0 Then Return
            Dim eventName As String = TryCast(list.SelectedItems(0).Tag, String)
            If String.IsNullOrWhiteSpace(eventName) Then Return
            RaiseEvent EventActivated(Me, eventName)
        End Sub

        Private Sub ListKeyDown(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Enter Then
                ActivateSelected(sender, EventArgs.Empty)
                e.Handled = True
                e.SuppressKeyPress = True
            End If
        End Sub

        Private Sub InspectorResize(sender As Object, e As EventArgs)
            If list.Columns.Count < 2 Then Return
            Dim available As Integer = Math.Max(120, list.ClientSize.Width - 6)
            list.Columns(0).Width = Math.Max(100, CInt(available * 0.43))
            list.Columns(1).Width = Math.Max(110, available - list.Columns(0).Width)
        End Sub
    End Class
End Namespace
