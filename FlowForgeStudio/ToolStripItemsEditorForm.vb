Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

Namespace FlowForgeStudio
    Public Class ToolStripItemsEditorForm
        Inherits Form

        Private ReadOnly tree As New TreeView()
        Private ReadOnly typeBox As New ComboBox()
        Private ReadOnly nameBox As New TextBox()
        Private ReadOnly textBoxValue As New TextBox()
        Private ReadOnly shortcutBox As New ComboBox()
        Private ReadOnly enabledBox As New CheckBox()
        Private ReadOnly visibleBox As New CheckBox()
        Private ReadOnly checkedBox As New CheckBox()
        Private ReadOnly addButton As New ToolStripDropDownButton("Adicionar")
        Private ReadOnly removeButton As New ToolStripButton("Remover")
        Private ReadOnly upButton As New ToolStripButton("Subir")
        Private ReadOnly downButton As New ToolStripButton("Descer")
        Private ReadOnly childButton As New ToolStripButton("Tornar submenu")
        Private ReadOnly rootButton As New ToolStripButton("Mover para raiz")
        Private ReadOnly okButton As New Button()
        Private ReadOnly cancelBtn As New Button()
        Private updatingFields As Boolean

        Private ReadOnly _items As List(Of ToolStripItemData)
        Public ReadOnly Property Items As List(Of ToolStripItemData)
            Get
                Return _items
            End Get
        End Property

        Public Sub New(source As List(Of ToolStripItemData))
            _items = CloneItems(source)
            Text = "Editor de MenuStrip / ToolStrip / StatusStrip"
            StartPosition = FormStartPosition.CenterParent
            MinimumSize = New Size(840, 560)
            Size = New Size(980, 650)
            Font = New Font("Segoe UI", 9.0F)
            BuildUi()
            RebuildTree(Nothing)
        End Sub

        Private Sub BuildUi()
            Dim toolbar As New ToolStrip() With {.Dock = DockStyle.Top, .GripStyle = ToolStripGripStyle.Hidden, .RenderMode = ToolStripRenderMode.System}
            For Each kind As String In {"ToolStripMenuItem", "ToolStripButton", "ToolStripSeparator", "ToolStripLabel", "ToolStripTextBox", "ToolStripComboBox", "ToolStripDropDownButton", "ToolStripSplitButton", "ToolStripStatusLabel", "ToolStripProgressBar"}
                Dim localKind As String = kind
                Dim item As New ToolStripMenuItem(DisplayType(kind))
                AddHandler item.Click, Sub() AddItem(localKind)
                addButton.DropDownItems.Add(item)
            Next
            toolbar.Items.Add(addButton)
            toolbar.Items.Add(New ToolStripSeparator())
            toolbar.Items.Add(removeButton)
            toolbar.Items.Add(upButton)
            toolbar.Items.Add(downButton)
            toolbar.Items.Add(New ToolStripSeparator())
            toolbar.Items.Add(childButton)
            toolbar.Items.Add(rootButton)

            AddHandler removeButton.Click, Sub() RemoveSelected()
            AddHandler upButton.Click, Sub() MoveSelected(-1)
            AddHandler downButton.Click, Sub() MoveSelected(1)
            AddHandler childButton.Click, Sub() MakeSubmenu()
            AddHandler rootButton.Click, Sub() MoveToRoot()

            tree.Dock = DockStyle.Fill
            tree.HideSelection = False
            AddHandler tree.AfterSelect, AddressOf TreeAfterSelect

            Dim left As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(8)}
            left.Controls.Add(tree)
            left.Controls.Add(toolbar)

            Dim props As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(12), .ColumnCount = 2, .RowCount = 9}
            props.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120.0F))
            props.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
            For i As Integer = 0 To 7
                props.RowStyles.Add(New RowStyle(SizeType.Absolute, 34.0F))
            Next
            props.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

            typeBox.DropDownStyle = ComboBoxStyle.DropDownList
            typeBox.Items.AddRange(New Object() {"ToolStripMenuItem", "ToolStripButton", "ToolStripSeparator", "ToolStripLabel", "ToolStripTextBox", "ToolStripComboBox", "ToolStripDropDownButton", "ToolStripSplitButton", "ToolStripStatusLabel", "ToolStripProgressBar"})
            shortcutBox.DropDownStyle = ComboBoxStyle.DropDown
            shortcutBox.Items.AddRange(New Object() {"None", "Ctrl+N", "Ctrl+O", "Ctrl+S", "Ctrl+Shift+S", "Ctrl+P", "Ctrl+Z", "Ctrl+Y", "Ctrl+X", "Ctrl+C", "Ctrl+V", "F1", "F5", "F6", "F7", "F11", "Alt+F4"})
            enabledBox.Text = "Enabled"
            visibleBox.Text = "Visible"
            checkedBox.Text = "Checked"

            AddRow(props, 0, "Tipo", typeBox)
            AddRow(props, 1, "Name", nameBox)
            AddRow(props, 2, "Text", textBoxValue)
            AddRow(props, 3, "Atalho", shortcutBox)
            AddRow(props, 4, "Ativado", enabledBox)
            AddRow(props, 5, "Visível", visibleBox)
            AddRow(props, 6, "Marcado", checkedBox)

            Dim help As New Label() With {
                .Dock = DockStyle.Fill,
                .AutoSize = False,
                .ForeColor = Color.DimGray,
                .Text = "Dica: selecione um Item de menu, DropDownButton ou SplitButton e use 'Tornar submenu' para criar hierarquia. Os eventos Click continuam sendo criados pela aba Eventos do FlowForge.",
                .Padding = New Padding(0, 10, 0, 0)
            }
            props.Controls.Add(help, 0, 7)
            props.SetColumnSpan(help, 2)

            AddHandler typeBox.SelectedIndexChanged, AddressOf FieldsChanged
            AddHandler nameBox.TextChanged, AddressOf FieldsChanged
            AddHandler textBoxValue.TextChanged, AddressOf FieldsChanged
            AddHandler shortcutBox.TextChanged, AddressOf FieldsChanged
            AddHandler enabledBox.CheckedChanged, AddressOf FieldsChanged
            AddHandler visibleBox.CheckedChanged, AddressOf FieldsChanged
            AddHandler checkedBox.CheckedChanged, AddressOf FieldsChanged

            Dim split As New SplitContainer() With {.Dock = DockStyle.Fill, .SplitterDistance = 470, .FixedPanel = FixedPanel.Panel2}
            split.Panel1.Controls.Add(left)
            split.Panel2.Controls.Add(props)

            okButton.Text = "OK"
            okButton.DialogResult = DialogResult.OK
            okButton.Size = New Size(100, 30)
            cancelBtn.Text = "Cancelar"
            cancelBtn.DialogResult = DialogResult.Cancel
            cancelBtn.Size = New Size(100, 30)

            Dim bottom As New FlowLayoutPanel() With {.Dock = DockStyle.Bottom, .Height = 46, .FlowDirection = FlowDirection.RightToLeft, .Padding = New Padding(8)}
            bottom.Controls.Add(cancelBtn)
            bottom.Controls.Add(okButton)

            Controls.Add(split)
            Controls.Add(bottom)
            AcceptButton = okButton
            Me.CancelButton = cancelBtn
        End Sub

        Private Shared Sub AddRow(panel As TableLayoutPanel, row As Integer, caption As String, editor As Control)
            Dim label As New Label() With {.Text = caption & ":", .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}
            editor.Dock = DockStyle.Fill
            panel.Controls.Add(label, 0, row)
            panel.Controls.Add(editor, 1, row)
        End Sub

        Private Sub RebuildTree(selectedData As ToolStripItemData)
            tree.BeginUpdate()
            tree.Nodes.Clear()
            AddNodes(tree.Nodes, Items)
            tree.ExpandAll()
            tree.EndUpdate()
            If selectedData IsNot Nothing Then SelectNodeForData(tree.Nodes, selectedData)
            If tree.SelectedNode Is Nothing AndAlso tree.Nodes.Count > 0 Then tree.SelectedNode = tree.Nodes(0)
            UpdateCommandState()
        End Sub

        Private Shared Sub AddNodes(nodes As TreeNodeCollection, items As List(Of ToolStripItemData))
            If items Is Nothing Then Return
            For Each data As ToolStripItemData In items
                Dim caption As String = If(data.TypeName = "ToolStripSeparator", "────────", If(String.IsNullOrWhiteSpace(data.Text), data.Name, data.Text))
                Dim node As New TreeNode(caption & "   [" & ShortType(data.TypeName) & "]") With {.Tag = data}
                nodes.Add(node)
                AddNodes(node.Nodes, data.DropDownItems)
            Next
        End Sub

        Private Shared Function SelectNodeForData(nodes As TreeNodeCollection, data As ToolStripItemData) As Boolean
            For Each node As TreeNode In nodes
                If Object.ReferenceEquals(node.Tag, data) Then
                    node.TreeView.SelectedNode = node
                    node.EnsureVisible()
                    Return True
                End If
                If SelectNodeForData(node.Nodes, data) Then Return True
            Next
            Return False
        End Function

        Private Sub TreeAfterSelect(sender As Object, e As TreeViewEventArgs)
            LoadFields(TryCast(e.Node.Tag, ToolStripItemData))
            UpdateCommandState()
        End Sub

        Private Sub LoadFields(data As ToolStripItemData)
            updatingFields = True
            Try
                Dim hasData As Boolean = data IsNot Nothing
                typeBox.Enabled = hasData
                nameBox.Enabled = hasData
                textBoxValue.Enabled = hasData
                shortcutBox.Enabled = hasData
                enabledBox.Enabled = hasData
                visibleBox.Enabled = hasData
                checkedBox.Enabled = hasData
                If Not hasData Then Return

                typeBox.SelectedItem = data.TypeName
                If typeBox.SelectedIndex < 0 Then typeBox.Text = data.TypeName
                nameBox.Text = data.Name
                textBoxValue.Text = data.Text
                shortcutBox.Text = ShortcutToFriendly(GetProp(data, "ShortcutKeys", "None"))
                enabledBox.Checked = ParseBool(GetProp(data, "Enabled", "True"), True)
                visibleBox.Checked = ParseBool(GetProp(data, "Visible", "True"), True)
                checkedBox.Checked = ParseBool(GetProp(data, "Checked", "False"), False)

                shortcutBox.Enabled = data.TypeName = "ToolStripMenuItem"
                checkedBox.Enabled = data.TypeName = "ToolStripMenuItem" OrElse data.TypeName = "ToolStripButton"
                textBoxValue.Enabled = data.TypeName <> "ToolStripSeparator" AndAlso data.TypeName <> "ToolStripProgressBar"
            Finally
                updatingFields = False
            End Try
        End Sub

        Private Sub FieldsChanged(sender As Object, e As EventArgs)
            If updatingFields OrElse tree.SelectedNode Is Nothing Then Return
            Dim data As ToolStripItemData = TryCast(tree.SelectedNode.Tag, ToolStripItemData)
            If data Is Nothing Then Return

            Dim oldType As String = data.TypeName
            Dim selectedType As String = TryCast(typeBox.SelectedItem, String)
            If Not String.IsNullOrWhiteSpace(selectedType) Then data.TypeName = selectedType
            data.Name = SanitizeName(nameBox.Text, data.Name)
            data.Text = If(data.TypeName = "ToolStripSeparator", "", textBoxValue.Text)
            SetProp(data, "Enabled", enabledBox.Checked.ToString())
            SetProp(data, "Visible", visibleBox.Checked.ToString())
            SetProp(data, "Checked", checkedBox.Checked.ToString())
            If data.TypeName = "ToolStripMenuItem" Then
                SetProp(data, "ShortcutKeys", FriendlyToShortcut(shortcutBox.Text))
            Else
                RemoveProp(data, "ShortcutKeys")
            End If

            If oldType <> data.TypeName AndAlso Not CanHaveChildren(data.TypeName) AndAlso data.DropDownItems IsNot Nothing AndAlso data.DropDownItems.Count > 0 Then
                Dim children As List(Of ToolStripItemData) = data.DropDownItems
                data.DropDownItems = New List(Of ToolStripItemData)()
                Items.AddRange(children)
            End If
            RebuildTree(data)
        End Sub

        Private Sub AddItem(kind As String)
            Dim data As New ToolStripItemData() With {
                .TypeName = kind,
                .Name = NextName(kind),
                .Text = If(kind = "ToolStripSeparator", "", FriendlyDefaultText(kind)),
                .Properties = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase),
                .DropDownItems = New List(Of ToolStripItemData)()
            }
            SetProp(data, "Enabled", "True")
            SetProp(data, "Visible", "True")

            Dim selected As ToolStripItemData = CurrentData()
            If selected IsNot Nothing AndAlso CanHaveChildren(selected.TypeName) AndAlso (kind = "ToolStripMenuItem" OrElse kind = "ToolStripSeparator") Then
                selected.DropDownItems.Add(data)
            Else
                Items.Add(data)
            End If
            RebuildTree(data)
        End Sub

        Private Sub RemoveSelected()
            Dim data As ToolStripItemData = CurrentData()
            If data Is Nothing Then Return
            If RemoveFrom(Items, data) Then RebuildTree(Nothing)
        End Sub

        Private Shared Function RemoveFrom(items As List(Of ToolStripItemData), target As ToolStripItemData) As Boolean
            If items Is Nothing Then Return False
            If items.Remove(target) Then Return True
            For Each item As ToolStripItemData In items
                If RemoveFrom(item.DropDownItems, target) Then Return True
            Next
            Return False
        End Function

        Private Sub MoveSelected(delta As Integer)
            Dim data As ToolStripItemData = CurrentData()
            If data Is Nothing Then Return
            Dim list As List(Of ToolStripItemData) = FindContainer(Items, data)
            If list Is Nothing Then Return
            Dim index As Integer = list.IndexOf(data)
            Dim destination As Integer = index + delta
            If destination < 0 OrElse destination >= list.Count Then Return
            list.RemoveAt(index)
            list.Insert(destination, data)
            RebuildTree(data)
        End Sub

        Private Sub MakeSubmenu()
            Dim data As ToolStripItemData = CurrentData()
            If data Is Nothing Then Return
            Dim node As TreeNode = tree.SelectedNode
            If node Is Nothing OrElse node.PrevNode Is Nothing Then
                MessageBox.Show("Para transformar um item em submenu, coloque-o logo abaixo do item pai e tente novamente.", "Editor de itens", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Dim parentData As ToolStripItemData = TryCast(node.PrevNode.Tag, ToolStripItemData)
            If parentData Is Nothing OrElse Not CanHaveChildren(parentData.TypeName) Then
                MessageBox.Show("O item anterior precisa ser Item de menu, DropDownButton ou SplitButton.", "Editor de itens", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Dim oldList As List(Of ToolStripItemData) = FindContainer(Items, data)
            If oldList Is Nothing Then Return
            oldList.Remove(data)
            parentData.DropDownItems.Add(data)
            RebuildTree(data)
        End Sub

        Private Sub MoveToRoot()
            Dim data As ToolStripItemData = CurrentData()
            If data Is Nothing Then Return
            Dim list As List(Of ToolStripItemData) = FindContainer(Items, data)
            If list Is Nothing OrElse Object.ReferenceEquals(list, Items) Then Return
            list.Remove(data)
            Items.Add(data)
            RebuildTree(data)
        End Sub

        Private Shared Function FindContainer(items As List(Of ToolStripItemData), target As ToolStripItemData) As List(Of ToolStripItemData)
            If items Is Nothing Then Return Nothing
            If items.Contains(target) Then Return items
            For Each item As ToolStripItemData In items
                Dim found As List(Of ToolStripItemData) = FindContainer(item.DropDownItems, target)
                If found IsNot Nothing Then Return found
            Next
            Return Nothing
        End Function

        Private Function CurrentData() As ToolStripItemData
            If tree.SelectedNode Is Nothing Then Return Nothing
            Return TryCast(tree.SelectedNode.Tag, ToolStripItemData)
        End Function

        Private Sub UpdateCommandState()
            Dim data As ToolStripItemData = CurrentData()
            removeButton.Enabled = data IsNot Nothing
            upButton.Enabled = data IsNot Nothing
            downButton.Enabled = data IsNot Nothing
            rootButton.Enabled = data IsNot Nothing AndAlso FindContainer(Items, data) IsNot Items
            childButton.Enabled = data IsNot Nothing
        End Sub

        Private Function NextName(kind As String) As String
            Dim baseName As String = kind
            Dim used As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            CollectNames(Items, used)
            Dim index As Integer = 1
            While used.Contains(baseName & index.ToString())
                index += 1
            End While
            Return baseName & index.ToString()
        End Function

        Private Shared Sub CollectNames(items As List(Of ToolStripItemData), names As HashSet(Of String))
            If items Is Nothing Then Return
            For Each item As ToolStripItemData In items
                If Not String.IsNullOrWhiteSpace(item.Name) Then names.Add(item.Name)
                CollectNames(item.DropDownItems, names)
            Next
        End Sub

        Private Shared Function CloneItems(source As List(Of ToolStripItemData)) As List(Of ToolStripItemData)
            Dim result As New List(Of ToolStripItemData)()
            If source Is Nothing Then Return result
            For Each item As ToolStripItemData In source
                Dim clone As New ToolStripItemData() With {
                    .TypeName = item.TypeName,
                    .Name = item.Name,
                    .Text = item.Text,
                    .Properties = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase),
                    .DropDownItems = CloneItems(item.DropDownItems)
                }
                If item.Properties IsNot Nothing Then
                    For Each pair As KeyValuePair(Of String, String) In item.Properties
                        clone.Properties(pair.Key) = pair.Value
                    Next
                End If
                result.Add(clone)
            Next
            Return result
        End Function

        Private Shared Function GetProp(data As ToolStripItemData, key As String, fallback As String) As String
            If data Is Nothing OrElse data.Properties Is Nothing Then Return fallback
            Dim value As String = Nothing
            If data.Properties.TryGetValue(key, value) Then Return value
            Return fallback
        End Function

        Private Shared Sub SetProp(data As ToolStripItemData, key As String, value As String)
            If data.Properties Is Nothing Then data.Properties = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            data.Properties(key) = value
        End Sub

        Private Shared Sub RemoveProp(data As ToolStripItemData, key As String)
            If data.Properties IsNot Nothing Then data.Properties.Remove(key)
        End Sub

        Private Shared Function ParseBool(value As String, fallback As Boolean) As Boolean
            Dim result As Boolean
            If Boolean.TryParse(value, result) Then Return result
            Return fallback
        End Function

        Private Shared Function CanHaveChildren(kind As String) As Boolean
            Return kind = "ToolStripMenuItem" OrElse kind = "ToolStripDropDownButton" OrElse kind = "ToolStripSplitButton"
        End Function

        Private Shared Function SanitizeName(value As String, fallback As String) As String
            Dim text As String = If(value, "").Trim()
            If text = "" Then Return fallback
            Dim chars As New List(Of Char)()
            For i As Integer = 0 To text.Length - 1
                Dim ch As Char = text(i)
                If Char.IsLetterOrDigit(ch) OrElse ch = "_"c Then chars.Add(ch)
            Next
            If chars.Count = 0 Then Return fallback
            If Char.IsDigit(chars(0)) Then chars.Insert(0, "_"c)
            Return New String(chars.ToArray())
        End Function

        Private Shared Function FriendlyDefaultText(kind As String) As String
            Select Case kind
                Case "ToolStripMenuItem" : Return "Novo item"
                Case "ToolStripButton" : Return "Botão"
                Case "ToolStripLabel", "ToolStripStatusLabel" : Return "Texto"
                Case "ToolStripDropDownButton" : Return "Menu"
                Case "ToolStripSplitButton" : Return "Ação"
                Case "ToolStripTextBox" : Return ""
                Case "ToolStripComboBox" : Return ""
                Case Else : Return ""
            End Select
        End Function

        Private Shared Function DisplayType(kind As String) As String
            Select Case kind
                Case "ToolStripMenuItem" : Return "Item de menu"
                Case "ToolStripButton" : Return "Botão"
                Case "ToolStripSeparator" : Return "Separador"
                Case "ToolStripLabel" : Return "Label"
                Case "ToolStripTextBox" : Return "Caixa de texto"
                Case "ToolStripComboBox" : Return "ComboBox"
                Case "ToolStripDropDownButton" : Return "DropDownButton"
                Case "ToolStripSplitButton" : Return "SplitButton"
                Case "ToolStripStatusLabel" : Return "StatusLabel"
                Case "ToolStripProgressBar" : Return "ProgressBar"
                Case Else : Return kind
            End Select
        End Function

        Private Shared Function ShortType(kind As String) As String
            Return kind.Replace("ToolStrip", "")
        End Function

        Private Shared Function ShortcutToFriendly(value As String) As String
            Dim text As String = If(value, "").Trim()
            If text = "" OrElse text.Equals("None", StringComparison.OrdinalIgnoreCase) Then Return "None"
            Return text.Replace("Control", "Ctrl").Replace(", ", "+").Replace(",", "+")
        End Function

        Private Shared Function FriendlyToShortcut(value As String) As String
            Dim text As String = If(value, "").Trim()
            If text = "" OrElse text.Equals("None", StringComparison.OrdinalIgnoreCase) Then Return "None"
            Dim parts As String() = text.Replace(" ", "").Split("+"c)
            Dim output As New List(Of String)()
            For Each part As String In parts
                Select Case part.ToUpperInvariant()
                    Case "CTRL", "CONTROL" : output.Add("Control")
                    Case "SHIFT" : output.Add("Shift")
                    Case "ALT" : output.Add("Alt")
                    Case Else : output.Add(part.ToUpperInvariant())
                End Select
            Next
            Return String.Join(", ", output.ToArray())
        End Function
    End Class
End Namespace
