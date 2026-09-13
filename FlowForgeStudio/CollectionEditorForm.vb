Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

Namespace FlowForgeStudio

    ' Editor visual para as coleções mais comuns do WinForms.
    ' A edição ocorre diretamente no componente selecionado do Designer.
    Public Class CollectionEditorForm
        Inherits Form

        Private ReadOnly target As Object
        Private ReadOnly titleLabel As New Label()
        Private ReadOnly content As New Panel()
        Private ReadOnly okButton As New Button()
        Private ReadOnly cancelBtn As New Button()

        ' Editor simples de Items.
        Private itemsText As TextBox

        ' Editor tabular para DataGridView.Columns e TabControl.TabPages.
        Private table As DataGridView
        Private addButton As Button
        Private deleteButton As Button
        Private upButton As Button
        Private downButton As Button

        ' Editor hierárquico para TreeView.Nodes.
        Private nodeTree As TreeView
        Private nodeText As TextBox
        Private nodeName As TextBox

        Public Sub New(component As Object)
            target = component

            Text = "Editor de Coleções"
            StartPosition = FormStartPosition.CenterParent
            MinimumSize = New Size(680, 460)
            Size = New Size(820, 560)
            Font = New Font("Segoe UI", 9.0F)
            ShowInTaskbar = False

            titleLabel.Dock = DockStyle.Top
            titleLabel.Height = 44
            titleLabel.Padding = New Padding(12, 0, 12, 0)
            titleLabel.TextAlign = ContentAlignment.MiddleLeft
            titleLabel.Font = New Font(Font, FontStyle.Bold)
            titleLabel.BackColor = Color.FromArgb(45, 48, 57)
            titleLabel.ForeColor = Color.White

            content.Dock = DockStyle.Fill
            content.Padding = New Padding(12)

            Dim footer As New FlowLayoutPanel With {
                .Dock = DockStyle.Bottom,
                .Height = 48,
                .FlowDirection = FlowDirection.RightToLeft,
                .Padding = New Padding(8)
            }

            okButton.Text = "OK"
            okButton.Width = 90
            okButton.DialogResult = DialogResult.OK

            cancelBtn.Text = "Cancelar"
            cancelBtn.Width = 90
            cancelBtn.DialogResult = DialogResult.Cancel

            footer.Controls.Add(okButton)
            footer.Controls.Add(cancelBtn)

            Controls.Add(content)
            Controls.Add(footer)
            Controls.Add(titleLabel)

            AcceptButton = okButton
            Me.CancelButton = cancelBtn

            BuildEditor()
        End Sub

        Private Sub BuildEditor()
            If TypeOf target Is ComboBox OrElse TypeOf target Is ListBox OrElse TypeOf target Is CheckedListBox Then
                BuildItemsEditor()
            ElseIf TypeOf target Is DataGridView Then
                BuildColumnsEditor()
            ElseIf TypeOf target Is TabControl Then
                BuildTabPagesEditor()
            ElseIf TypeOf target Is TreeView Then
                BuildTreeNodesEditor()
            Else
                titleLabel.Text = "Este componente não possui uma coleção editável suportada."
                okButton.Enabled = False
            End If
        End Sub

        Private Sub BuildItemsEditor()
            titleLabel.Text = target.GetType().Name & ".Items — um item por linha"

            itemsText = New TextBox With {
                .Dock = DockStyle.Fill,
                .Multiline = True,
                .ScrollBars = ScrollBars.Both,
                .AcceptsReturn = True,
                .AcceptsTab = True,
                .Font = New Font("Consolas", 10.0F)
            }

            Dim values As New List(Of String)()
            If TypeOf target Is ComboBox Then
                For Each value As Object In DirectCast(target, ComboBox).Items
                    values.Add(Convert.ToString(value))
                Next
            ElseIf TypeOf target Is CheckedListBox Then
                For Each value As Object In DirectCast(target, CheckedListBox).Items
                    values.Add(Convert.ToString(value))
                Next
            Else
                For Each value As Object In DirectCast(target, ListBox).Items
                    values.Add(Convert.ToString(value))
                Next
            End If

            itemsText.Lines = values.ToArray()
            content.Controls.Add(itemsText)
            AddHandler okButton.Click, AddressOf ApplyItems
        End Sub

        Private Sub ApplyItems(sender As Object, e As EventArgs)
            Dim values As String() = itemsText.Lines _
                .Select(Function(s) s.TrimEnd()) _
                .Where(Function(s) s.Length > 0) _
                .ToArray()

            If TypeOf target Is ComboBox Then
                Dim c As ComboBox = DirectCast(target, ComboBox)
                c.Items.Clear()
                c.Items.AddRange(values.Cast(Of Object).ToArray())
            ElseIf TypeOf target Is CheckedListBox Then
                Dim c As CheckedListBox = DirectCast(target, CheckedListBox)
                c.Items.Clear()
                c.Items.AddRange(values.Cast(Of Object).ToArray())
            Else
                Dim c As ListBox = DirectCast(target, ListBox)
                c.Items.Clear()
                c.Items.AddRange(values.Cast(Of Object).ToArray())
            End If
        End Sub

        Private Sub BuildColumnsEditor()
            titleLabel.Text = "DataGridView.Columns"

            BuildTableButtons()

            table = New DataGridView With {
                .Dock = DockStyle.Fill,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .MultiSelect = False,
                .RowHeadersVisible = False
            }

            table.Columns.Add("ColumnType", "Tipo")
            table.Columns.Add("ColumnName", "Name")
            table.Columns.Add("HeaderText", "Cabeçalho")
            table.Columns.Add("ColumnWidth", "Largura")
            Dim visibleColumn As New DataGridViewCheckBoxColumn With {.Name = "ColumnVisible", .HeaderText = "Visível"}
            table.Columns.Add(visibleColumn)

            Dim grid As DataGridView = DirectCast(target, DataGridView)
            For Each col As DataGridViewColumn In grid.Columns
                table.Rows.Add(col.GetType().Name, col.Name, col.HeaderText, col.Width, col.Visible)
            Next

            content.Controls.Add(table)
            content.Controls.Add(CreateButtonBar())
            AddHandler addButton.Click, Sub()
                                            table.Rows.Add("DataGridViewTextBoxColumn", NextColumnName(), "Coluna", 100, True)
                                        End Sub
            AddHandler deleteButton.Click, Sub() DeleteSelectedRow()
            AddHandler upButton.Click, Sub() MoveSelectedRow(-1)
            AddHandler downButton.Click, Sub() MoveSelectedRow(1)
            AddHandler okButton.Click, AddressOf ApplyColumns
        End Sub

        Private Function NextColumnName() As String
            Dim n As Integer = 1
            Do
                Dim candidate As String = "Column" & n.ToString()
                Dim exists As Boolean = table.Rows.Cast(Of DataGridViewRow)().Any(
                    Function(r) String.Equals(Convert.ToString(r.Cells("ColumnName").Value), candidate, StringComparison.OrdinalIgnoreCase))
                If Not exists Then Return candidate
                n += 1
            Loop
        End Function

        Private Sub ApplyColumns(sender As Object, e As EventArgs)
            Dim grid As DataGridView = DirectCast(target, DataGridView)
            grid.Columns.Clear()

            For Each row As DataGridViewRow In table.Rows
                Dim typeName As String = Convert.ToString(row.Cells("ColumnType").Value)
                Dim col As DataGridViewColumn

                Select Case typeName
                    Case "DataGridViewCheckBoxColumn"
                        col = New DataGridViewCheckBoxColumn()
                    Case "DataGridViewButtonColumn"
                        col = New DataGridViewButtonColumn()
                    Case "DataGridViewComboBoxColumn"
                        col = New DataGridViewComboBoxColumn()
                    Case "DataGridViewImageColumn"
                        col = New DataGridViewImageColumn()
                    Case "DataGridViewLinkColumn"
                        col = New DataGridViewLinkColumn()
                    Case Else
                        col = New DataGridViewTextBoxColumn()
                End Select

                col.Name = SafeName(Convert.ToString(row.Cells("ColumnName").Value), "Column" & (grid.Columns.Count + 1).ToString())
                col.HeaderText = Convert.ToString(row.Cells("HeaderText").Value)

                Dim width As Integer = 100
                Integer.TryParse(Convert.ToString(row.Cells("ColumnWidth").Value), width)
                col.Width = Math.Max(20, width)

                Dim visible As Boolean = True
                If row.Cells("ColumnVisible").Value IsNot Nothing Then
                    Boolean.TryParse(Convert.ToString(row.Cells("ColumnVisible").Value), visible)
                End If
                col.Visible = visible
                grid.Columns.Add(col)
            Next
        End Sub

        Private Sub BuildTabPagesEditor()
            titleLabel.Text = "TabControl.TabPages"

            BuildTableButtons()

            table = New DataGridView With {
                .Dock = DockStyle.Fill,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .MultiSelect = False,
                .RowHeadersVisible = False
            }
            table.Columns.Add("PageName", "Name")
            table.Columns.Add("PageText", "Texto")

            Dim tabs As TabControl = DirectCast(target, TabControl)
            For Each page As TabPage In tabs.TabPages
                table.Rows.Add(page.Name, page.Text)
            Next

            content.Controls.Add(table)
            content.Controls.Add(CreateButtonBar())

            AddHandler addButton.Click, Sub()
                                            Dim n As Integer = table.Rows.Count + 1
                                            table.Rows.Add("TabPage" & n.ToString(), "Página " & n.ToString())
                                        End Sub
            AddHandler deleteButton.Click, Sub() DeleteSelectedRow()
            AddHandler upButton.Click, Sub() MoveSelectedRow(-1)
            AddHandler downButton.Click, Sub() MoveSelectedRow(1)
            AddHandler okButton.Click, AddressOf ApplyTabPages
        End Sub

        Private Sub ApplyTabPages(sender As Object, e As EventArgs)
            Dim tabs As TabControl = DirectCast(target, TabControl)
            tabs.TabPages.Clear()

            For Each row As DataGridViewRow In table.Rows
                Dim page As New TabPage()
                page.Name = SafeName(Convert.ToString(row.Cells("PageName").Value), "TabPage" & (tabs.TabPages.Count + 1).ToString())
                page.Text = Convert.ToString(row.Cells("PageText").Value)
                tabs.TabPages.Add(page)
            Next
        End Sub

        Private Sub BuildTreeNodesEditor()
            titleLabel.Text = "TreeView.Nodes"

            Dim split As New SplitContainer With {.Dock = DockStyle.Fill, .SplitterDistance = 390}

            nodeTree = New TreeView With {.Dock = DockStyle.Fill, .HideSelection = False}
            CloneNodes(DirectCast(target, TreeView).Nodes, nodeTree.Nodes)

            Dim buttons As New FlowLayoutPanel With {
                .Dock = DockStyle.Top,
                .Height = 74,
                .AutoSize = False,
                .WrapContents = True
            }
            Dim addRoot As New Button With {.Text = "Adicionar raiz", .AutoSize = True}
            Dim addChild As New Button With {.Text = "Adicionar filho", .AutoSize = True}
            Dim deleteNode As New Button With {.Text = "Excluir", .AutoSize = True}
            Dim moveUp As New Button With {.Text = "Subir", .AutoSize = True}
            Dim moveDown As New Button With {.Text = "Descer", .AutoSize = True}

            buttons.Controls.AddRange(New Control() {addRoot, addChild, deleteNode, moveUp, moveDown})

            Dim editorPanel As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 5,
                .Padding = New Padding(10)
            }
            editorPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            editorPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            editorPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            editorPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            editorPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            editorPanel.Controls.Add(New Label With {.Text = "Name", .AutoSize = True})
            nodeName = New TextBox With {.Dock = DockStyle.Top}
            editorPanel.Controls.Add(nodeName)
            editorPanel.Controls.Add(New Label With {.Text = "Text", .AutoSize = True, .Margin = New Padding(3, 12, 3, 3)})
            nodeText = New TextBox With {.Dock = DockStyle.Top}
            editorPanel.Controls.Add(nodeText)

            split.Panel1.Controls.Add(nodeTree)
            split.Panel1.Controls.Add(buttons)
            split.Panel2.Controls.Add(editorPanel)
            content.Controls.Add(split)

            AddHandler nodeTree.AfterSelect, AddressOf SelectedNodeChanged
            AddHandler nodeName.TextChanged, AddressOf EditSelectedNode
            AddHandler nodeText.TextChanged, AddressOf EditSelectedNode

            AddHandler addRoot.Click, Sub()
                                           Dim node As New TreeNode("Novo nó") With {.Name = NextNodeName("Node")}
                                           nodeTree.Nodes.Add(node)
                                           nodeTree.SelectedNode = node
                                       End Sub
            AddHandler addChild.Click, Sub()
                                            If nodeTree.SelectedNode Is Nothing Then Return
                                            Dim node As New TreeNode("Novo nó") With {.Name = NextNodeName("Node")}
                                            nodeTree.SelectedNode.Nodes.Add(node)
                                            nodeTree.SelectedNode.Expand()
                                            nodeTree.SelectedNode = node
                                        End Sub
            AddHandler deleteNode.Click, Sub()
                                              If nodeTree.SelectedNode IsNot Nothing Then nodeTree.SelectedNode.Remove()
                                          End Sub
            AddHandler moveUp.Click, Sub() MoveSelectedNode(-1)
            AddHandler moveDown.Click, Sub() MoveSelectedNode(1)
            AddHandler okButton.Click, AddressOf ApplyTreeNodes

            If nodeTree.Nodes.Count > 0 Then nodeTree.SelectedNode = nodeTree.Nodes(0)
        End Sub

        Private Sub SelectedNodeChanged(sender As Object, e As TreeViewEventArgs)
            If e.Node Is Nothing Then Return
            nodeName.Text = e.Node.Name
            nodeText.Text = e.Node.Text
        End Sub

        Private Sub EditSelectedNode(sender As Object, e As EventArgs)
            If nodeTree Is Nothing OrElse nodeTree.SelectedNode Is Nothing Then Return
            nodeTree.SelectedNode.Name = SafeName(nodeName.Text, "Node")
            nodeTree.SelectedNode.Text = nodeText.Text
        End Sub

        Private Function NextNodeName(prefix As String) As String
            Dim n As Integer = 1
            Do
                Dim candidate As String = prefix & n.ToString()
                If FindNodeByName(nodeTree.Nodes, candidate) Is Nothing Then Return candidate
                n += 1
            Loop
        End Function

        Private Shared Function FindNodeByName(nodes As TreeNodeCollection, name As String) As TreeNode
            For Each node As TreeNode In nodes
                If String.Equals(node.Name, name, StringComparison.OrdinalIgnoreCase) Then Return node
                Dim child As TreeNode = FindNodeByName(node.Nodes, name)
                If child IsNot Nothing Then Return child
            Next
            Return Nothing
        End Function

        Private Sub MoveSelectedNode(direction As Integer)
            Dim node As TreeNode = nodeTree.SelectedNode
            If node Is Nothing Then Return

            Dim siblings As TreeNodeCollection = If(node.Parent Is Nothing, nodeTree.Nodes, node.Parent.Nodes)
            Dim oldIndex As Integer = node.Index
            Dim newIndex As Integer = oldIndex + direction
            If newIndex < 0 OrElse newIndex >= siblings.Count Then Return

            Dim clone As TreeNode = DirectCast(node.Clone(), TreeNode)
            siblings.RemoveAt(oldIndex)
            siblings.Insert(newIndex, clone)
            nodeTree.SelectedNode = clone
        End Sub

        Private Sub ApplyTreeNodes(sender As Object, e As EventArgs)
            Dim tree As TreeView = DirectCast(target, TreeView)
            tree.Nodes.Clear()
            CloneNodes(nodeTree.Nodes, tree.Nodes)
        End Sub

        Private Shared Sub CloneNodes(source As TreeNodeCollection, targetNodes As TreeNodeCollection)
            For Each sourceNode As TreeNode In source
                Dim clone As New TreeNode(sourceNode.Text) With {.Name = sourceNode.Name}
                targetNodes.Add(clone)
                CloneNodes(sourceNode.Nodes, clone.Nodes)
            Next
        End Sub

        Private Sub BuildTableButtons()
            addButton = New Button With {.Text = "Adicionar", .AutoSize = True}
            deleteButton = New Button With {.Text = "Excluir", .AutoSize = True}
            upButton = New Button With {.Text = "Subir", .AutoSize = True}
            downButton = New Button With {.Text = "Descer", .AutoSize = True}
        End Sub

        Private Function CreateButtonBar() As Control
            Dim bar As New FlowLayoutPanel With {
                .Dock = DockStyle.Top,
                .Height = 40,
                .Padding = New Padding(0, 4, 0, 4)
            }
            bar.Controls.AddRange(New Control() {addButton, deleteButton, upButton, downButton})
            Return bar
        End Function

        Private Sub DeleteSelectedRow()
            If table.CurrentRow IsNot Nothing AndAlso Not table.CurrentRow.IsNewRow Then
                table.Rows.Remove(table.CurrentRow)
            End If
        End Sub

        Private Sub MoveSelectedRow(direction As Integer)
            If table.CurrentRow Is Nothing Then Return
            Dim oldIndex As Integer = table.CurrentRow.Index
            Dim newIndex As Integer = oldIndex + direction
            If newIndex < 0 OrElse newIndex >= table.Rows.Count Then Return

            Dim values As Object() = table.CurrentRow.Cells.Cast(Of DataGridViewCell)().Select(Function(c) c.Value).ToArray()
            table.Rows.RemoveAt(oldIndex)
            Dim inserted As Integer = table.Rows.Add(values)
            Dim row As DataGridViewRow = table.Rows(inserted)
            table.Rows.Remove(row)
            table.Rows.Insert(newIndex, row)
            table.CurrentCell = row.Cells(0)
        End Sub

        Private Shared Function SafeName(value As String, fallback As String) As String
            If String.IsNullOrWhiteSpace(value) Then Return fallback
            Dim result As String = New String(value.Where(Function(ch) Char.IsLetterOrDigit(ch) OrElse ch = "_"c).ToArray())
            If result.Length = 0 Then Return fallback
            If Char.IsDigit(result(0)) Then result = "_" & result
            Return result
        End Function
    End Class
End Namespace
