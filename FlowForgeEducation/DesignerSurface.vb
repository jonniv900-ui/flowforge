Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.IO
Imports System.Runtime.Serialization.Json
Imports System.Text
Imports System.Linq
Imports System.Reflection
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports Microsoft.VisualBasic

Namespace FlowForgeStudio

    Friend Class ProjectUserControlPlaceholder
        Inherits Panel
        <Browsable(False)> Public Property ProjectTypeName As String = "UserControl"

        Public Sub New(typeName As String)
            ProjectTypeName = typeName
            BackColor = Color.FromArgb(238, 242, 248)
            BorderStyle = BorderStyle.FixedSingle
            Size = New Size(180, 100)
            Dim caption As New Label With {.Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleCenter, .Text = typeName & System.Environment.NewLine & "UserControl do projeto", .ForeColor = Color.FromArgb(55, 65, 80), .BackColor = Color.Transparent}
            Controls.Add(caption)
        End Sub
    End Class

    Public Class DesignerSurface
        Inherits Panel
        Public Event SelectionChanged(sender As Object, selected As Object)
        Public Event ComponentDoubleClick(sender As Object, component As Object)
        Public Property GridSize As Integer = 10
        Public Property SnapToGrid As Boolean = True
        Public Property ShowGrid As Boolean = True
        Public Property Document As FormData
        Public Property FormTemplate As Form
        Public Property UserControlDocument As UserControlData
        Private _selected As Control
        Private _selectedComponent As Object
        Private dragging As Boolean
        Private dragStart As Point
        Private originalLocation As Point
        Private ReadOnly resizeHandles As New List(Of Panel)()
        Private ReadOnly knownNames As New Dictionary(Of Control, String)()
        Private formGrip As Panel
        Private ReadOnly selectedControls As New List(Of Control)()
        Private ReadOnly dragOriginalLocations As New Dictionary(Of Control, Point)()
        Private selectingRectangle As Boolean
        Private selectionStart As Point
        Private selectionRectangle As Rectangle = Rectangle.Empty
        Private snapGuideX As Integer = Integer.MinValue
        Private snapGuideY As Integer = Integer.MinValue
        Private ReadOnly undoHistory As New List(Of String)()
        Private ReadOnly redoHistory As New List(Of String)()
        Private restoringHistory As Boolean
        Private lastHistoryState As String = ""
        Private formChrome As Panel
        Private formChromeTitle As Label
        Private formChromeIcon As PictureBox
        Private formChromeMin As Label
        Private formChromeMax As Label
        Private formChromeClose As Label
        Private Shared ReadOnly GlyphMinimize As String = ChrW(&HE921)
        Private Shared ReadOnly GlyphMaximize As String = ChrW(&HE922)
        Private Shared ReadOnly GlyphRestore As String = ChrW(&HE923)
        Private Shared ReadOnly GlyphClose As String = ChrW(&HE8BB)
        Private Shared ReadOnly ChromeActiveColor As Color = Color.FromArgb(32, 100, 170)
        Private Shared ReadOnly ChromeInactiveColor As Color = Color.FromArgb(120, 120, 120)
        Private Shared ReadOnly ChromeCloseHoverColor As Color = Color.FromArgb(232, 17, 35)
        Public Property ShowFormChrome As Boolean = True

        Private Function ChromeHeightFor(borderStyle As FormBorderStyle) As Integer
            If borderStyle = FormBorderStyle.FixedToolWindow OrElse borderStyle = FormBorderStyle.SizableToolWindow Then Return 24
            Return 32
        End Function

        Public Sub New()
            DoubleBuffered = True : AutoScroll = True : BackColor = Color.FromArgb(35, 38, 46)
            AllowDrop = True : AddHandler DragEnter, AddressOf SurfaceDragEnter : AddHandler DragDrop, AddressOf SurfaceDragDrop
            SetStyle(ControlStyles.ResizeRedraw, True)
        End Sub

        Public ReadOnly Property SelectedControl As Control
            Get
                Return _selected
            End Get
        End Property
        Public ReadOnly Property SelectedComponent As Object
            Get
                If _selectedComponent IsNot Nothing Then Return _selectedComponent
                If _selected IsNot Nothing Then Return _selected
                Return FormTemplate
            End Get
        End Property
        Public ReadOnly Property SelectedComponentName As String
            Get
                Dim item As ToolStripItem = TryCast(_selectedComponent, ToolStripItem)
                If item IsNot Nothing Then Return item.Name
                If _selected IsNot Nothing Then Return _selected.Name
                Return If(Document Is Nothing, "", Document.Name)
            End Get
        End Property
        Public ReadOnly Property FormCanvas As Control
            Get
                Return FindCanvas()
            End Get
        End Property

        Public Function NudgeSelected(deltaX As Integer, deltaY As Integer, resize As Boolean) As Boolean
            If _selected Is Nothing OrElse _selected.Name = "__FORM__" OrElse TypeOf _selected Is ComponentTile Then Return False
            If resize Then
                _selected.Size = New Size(Math.Max(10, _selected.Width + deltaX), Math.Max(10, _selected.Height + deltaY))
            Else
                _selected.Location = New Point(Math.Max(0, _selected.Left + deltaX), Math.Max(0, _selected.Top + deltaY))
            End If
            PositionHandles() : Snapshot() : Return True
        End Function

        Public Function CenterSelected(horizontal As Boolean) As Boolean
            Dim canvas As Control = FindCanvas()
            If canvas Is Nothing OrElse _selected Is Nothing OrElse _selected.Name = "__FORM__" OrElse TypeOf _selected Is ComponentTile Then Return False
            If horizontal Then
                _selected.Left = Math.Max(0, (canvas.ClientSize.Width - _selected.Width) \ 2)
            Else
                _selected.Top = Math.Max(0, (canvas.ClientSize.Height - _selected.Height) \ 2)
            End If
            PositionHandles() : Snapshot() : Return True
        End Function

        Public Function ChangeSelectedZOrder(bringToFront As Boolean) As Boolean
            If _selected Is Nothing OrElse _selected.Name = "__FORM__" OrElse TypeOf _selected Is ComponentTile Then Return False
            If bringToFront Then _selected.BringToFront() Else _selected.SendToBack()
            PositionHandles() : Snapshot() : Return True
        End Function

        Public Sub LoadDocument(data As FormData)
            UserControlDocument = Nothing
            If FormTemplate IsNot Nothing Then FormTemplate.Dispose()
            Controls.Clear() : resizeHandles.Clear() : knownNames.Clear() : selectedControls.Clear() : _selected = Nothing : _selectedComponent = Nothing : Document = data
            FormTemplate = New Form() : ApplyProperties(FormTemplate, data.Properties) : FormTemplate.Name = data.Name
            Dim size As Size = FormTemplate.ClientSize : If size.Width < 100 Then size = ParseSize(GetValue(data.Properties, "ClientSize", "800, 500"), New Size(800, 500))
            Dim canvasTop As Integer = 30
            If ShowFormChrome AndAlso FormTemplate.FormBorderStyle <> FormBorderStyle.None Then canvasTop += ChromeHeightFor(FormTemplate.FormBorderStyle)
            Dim canvas As New Panel With {.Name = "__FORM__", .Location = New Point(30, canvasTop), .Size = size, .BackColor = ParseColor(GetValue(data.Properties, "BackColor", "White"), Color.White), .BorderStyle = BorderStyle.FixedSingle, .AutoScroll = False}
            If ShowFormChrome AndAlso FormTemplate.FormBorderStyle <> FormBorderStyle.None Then CreateFormChrome(canvas)
            AddHandler canvas.MouseDown, AddressOf CanvasMouseDown
            AddHandler canvas.MouseMove, AddressOf CanvasMouseMove
            AddHandler canvas.MouseUp, AddressOf CanvasMouseUp
            AddHandler canvas.DoubleClick, Sub(sender, e) RaiseEvent ComponentDoubleClick(Me, FormTemplate)
            Controls.Add(canvas)
            For Each item As ControlData In data.Controls
                Dim control As Control = CreateDesignItem(item.TypeName, item.AssemblyPath, item.IsNonVisual)
                ApplyItemProperties(control, item.Properties) : control.Name = item.Name
                Dim strip As ToolStrip = TryCast(control, ToolStrip)
                If strip IsNot Nothing Then RestoreToolStripItems(strip.Items, item.Items)
                AttachControl(control) : canvas.Controls.Add(control)
            Next
            AutoScrollMinSize = New Size(size.Width + 80, size.Height + 110) : UpdateFormChrome() : Invalidate()
            ResetDesignerHistory()
        End Sub

        Public Sub LoadUserControl(data As UserControlData)
            If data Is Nothing Then Return
            If data.Properties Is Nothing Then data.Properties = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            If data.Controls Is Nothing Then data.Controls = New List(Of ControlData)()
            Dim proxy As New FormData With {.Name = data.Name, .Code = data.Code, .FolderPath = data.FolderPath}
            proxy.Properties = New Dictionary(Of String, String)(data.Properties, StringComparer.OrdinalIgnoreCase)
            proxy.Controls = data.Controls
            LoadDocument(proxy)
            UserControlDocument = data
        End Sub

        Public Sub AddComponent(entry As ToolboxEntry)
            AddComponentAt(entry, New Point(20, 20))
        End Sub
        Public Sub AddComponentAt(entry As ToolboxEntry, location As Point)
            Dim canvas As Control = FindCanvas() : If canvas Is Nothing Then Return
            Try
                Dim control As Control = CreateDesignItem(entry.TypeName, entry.AssemblyPath, entry.IsNonVisual)
                control.Name = NextName(entry.TypeName) : control.Text = entry.DisplayName
                control.Location = Snap(location)
                If TypeOf control Is ComponentTile Then
                    control.Size = New Size(150, 42)
                ElseIf TypeOf control Is Label Then
                    control.AutoSize = True
                Else
                    control.Size = DefaultSize(entry.TypeName)
                End If
                AttachControl(control) : canvas.Controls.Add(control) : Snapshot() : SelectControl(control)
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Adicionar componente", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Public Sub Snapshot()
            If Document Is Nothing Then Return
            Dim canvas As Control = FindCanvas() : If canvas Is Nothing Then Return
            If FormTemplate IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(FormTemplate.Name) AndAlso Not FormTemplate.Name.Equals(Document.Name, StringComparison.OrdinalIgnoreCase) Then
                Dim oldName = Document.Name : Document.Name = FormTemplate.Name : Document.Code = Regex.Replace(Document.Code, "(?i)(Partial\s+Public\s+Class\s+)" & Regex.Escape(oldName) & "\b", "$1" & Document.Name)
            End If
            FormTemplate.ClientSize = canvas.Size : FormTemplate.BackColor = canvas.BackColor : Document.Properties = CaptureProperties(FormTemplate) : UpdateFormChrome()
            Document.Controls.Clear()
            For Each c As Control In canvas.Controls
                If Not resizeHandles.Contains(TryCast(c, Panel)) Then
                    SynchronizeControlName(c)
                    Dim tile As ComponentTile = TryCast(c, ComponentTile)
                    If tile IsNot Nothing Then
                        Dim values As Dictionary(Of String, String) = CaptureProperties(tile.ComponentInstance)
                        values("__TrayLocation") = tile.Location.X & ", " & tile.Location.Y
                        Document.Controls.Add(New ControlData With {.TypeName = tile.ComponentTypeName, .Name = c.Name, .AssemblyPath = tile.AssemblyPath, .IsNonVisual = True, .Properties = values})
                    Else
                        Dim projectPlaceholder As ProjectUserControlPlaceholder = TryCast(c, ProjectUserControlPlaceholder)
                        Dim externalAssembly As String = CStr(If(c.Tag, ""))
                        Dim storedTypeName As String
                        If projectPlaceholder IsNot Nothing Then
                            storedTypeName = projectPlaceholder.ProjectTypeName
                        Else
                            storedTypeName = If(String.IsNullOrWhiteSpace(externalAssembly), FriendlyType(c), c.GetType().FullName)
                        End If
                        Dim data As New ControlData With {.TypeName = storedTypeName, .Name = c.Name, .AssemblyPath = externalAssembly, .Properties = CaptureProperties(c)}
                        Dim strip As ToolStrip = TryCast(c, ToolStrip)
                        If strip IsNot Nothing Then data.Items = CaptureToolStripItems(strip.Items)
                        Document.Controls.Add(data)
                    End If
                End If
            Next
            If UserControlDocument IsNot Nothing Then
                Dim oldName As String = UserControlDocument.Name
                UserControlDocument.Name = Document.Name
                UserControlDocument.Properties = New Dictionary(Of String, String)(Document.Properties, StringComparer.OrdinalIgnoreCase)
                UserControlDocument.Controls = Document.Controls
                If Not oldName.Equals(UserControlDocument.Name, StringComparison.OrdinalIgnoreCase) Then
                    UserControlDocument.Code = Regex.Replace(UserControlDocument.Code, "(?i)(Partial\s+Public\s+Class\s+)" & Regex.Escape(oldName) & "\b", "$1" & UserControlDocument.Name)
                End If
            End If
            RecordDesignerHistory()
        End Sub

        Public Sub DeleteSelected()
            Dim selectedItem As ToolStripItem = TryCast(_selectedComponent, ToolStripItem)
            If selectedItem IsNot Nothing Then
                selectedItem.Dispose() : _selectedComponent = _selected
                Snapshot()
                If _selected IsNot Nothing Then RaiseEvent SelectionChanged(Me, _selected)
                Return
            End If
            If selectedControls.Count > 1 Then
                Dim targets As Control() = selectedControls.ToArray()
                selectedControls.Clear()
                For Each target As Control In targets
                    If target.Parent IsNot Nothing Then target.Parent.Controls.Remove(target)
                    target.Dispose()
                Next
                SelectForm()
                Snapshot()
                Return
            End If
            If _selected Is Nothing Then Return
            Dim singleTarget As Control = _selected
            SelectForm()
            If singleTarget.Parent IsNot Nothing Then singleTarget.Parent.Controls.Remove(singleTarget)
            singleTarget.Dispose()
            Snapshot()
        End Sub

        Private Sub AttachControl(c As Control)
            knownNames(c) = c.Name
            WireSelectionEvents(c, c)
            Dim strip As ToolStrip = TryCast(c, ToolStrip)
            If strip IsNot Nothing Then
                AddHandler strip.ItemClicked, Sub(sender, e) SelectToolStripItem(strip, e.ClickedItem)
                AddHandler strip.MouseDoubleClick, Sub(sender, e)
                                                       Dim clicked As ToolStripItem = strip.GetItemAt(e.Location)
                                                       If clicked IsNot Nothing Then RaiseEvent ComponentDoubleClick(Me, clicked)
                                                   End Sub
                For Each stripItem As ToolStripItem In strip.Items : AttachToolStripItemEvents(strip, stripItem) : Next
            End If
        End Sub
        ' Controles compostos (ex.: RichTextEditor, SyntaxCodeEditor) tem filhos internos
        ' (RichTextBox, ToolStrip, régua de linhas) com Dock=Fill cobrindo toda a área.
        ' Como eventos de mouse do WinForms nao "sobem" do filho pro pai, o MouseDown do
        ' item nunca disparava e ele nao podia ser selecionado/redimensionado no designer.
        ' Aqui propagamos os handlers recursivamente para todos os descendentes, sempre
        ' redirecionando a selecao/arraste para o controle de nivel superior ("owner"),
        ' que e o item de fato colocado no canvas.
        Private Sub WireSelectionEvents(c As Control, owner As Control)
            If ReferenceEquals(c, owner) Then
                AddHandler c.MouseDown, AddressOf ItemMouseDown
                AddHandler c.MouseMove, AddressOf ItemMouseMove
                AddHandler c.MouseUp, AddressOf ItemMouseUp
                AddHandler c.DoubleClick, Sub(sender, e) RaiseEvent ComponentDoubleClick(Me, InspectableObject(DirectCast(sender, Control)))
            Else
                AddHandler c.MouseDown, Sub(sender, e) ItemMouseDown(owner, TranslateArgs(owner, DirectCast(sender, Control), e))
                AddHandler c.MouseMove, Sub(sender, e) ItemMouseMove(owner, TranslateArgs(owner, DirectCast(sender, Control), e))
                AddHandler c.MouseUp, Sub(sender, e) ItemMouseUp(owner, TranslateArgs(owner, DirectCast(sender, Control), e))
                AddHandler c.DoubleClick, Sub(sender, e) RaiseEvent ComponentDoubleClick(Me, InspectableObject(owner))
            End If
            For Each child As Control In c.Controls
                WireSelectionEvents(child, owner)
            Next
        End Sub
        Private Function TranslateArgs(owner As Control, source As Control, e As MouseEventArgs) As MouseEventArgs
            Dim pt As Point = owner.PointToClient(source.PointToScreen(e.Location))
            Return New MouseEventArgs(e.Button, e.Clicks, pt.X, pt.Y, e.Delta)
        End Function
        Private Sub AttachToolStripItemEvents(strip As ToolStrip, item As ToolStripItem)
            Dim parent As ToolStripDropDownItem = TryCast(item, ToolStripDropDownItem)
            If parent Is Nothing Then Return
            AddHandler parent.DropDownItemClicked, Sub(sender, e) SelectToolStripItem(strip, e.ClickedItem)
            For Each child As ToolStripItem In parent.DropDownItems : AttachToolStripItemEvents(strip, child) : Next
        End Sub
        Private Sub SynchronizeControlName(c As Control)
            Dim oldName As String = Nothing
            If knownNames.TryGetValue(c, oldName) AndAlso oldName <> c.Name AndAlso Not String.IsNullOrWhiteSpace(c.Name) AndAlso Document IsNot Nothing Then
                Document.Code = Regex.Replace(Document.Code, "\b" & Regex.Escape(oldName) & "\b", c.Name)
                knownNames(c) = c.Name
            End If
        End Sub
        Private Sub ItemMouseDown(sender As Object, e As MouseEventArgs)
            Dim c As Control = DirectCast(sender, Control)
            If (ModifierKeys And (Keys.Control Or Keys.Shift)) <> Keys.None Then
                ToggleControlSelection(c)
            ElseIf Not selectedControls.Contains(c) OrElse selectedControls.Count <= 1 Then
                SelectControl(c)
            End If
            If e.Button <> MouseButtons.Left Then Return
            If e.Clicks >= 2 Then RaiseEvent ComponentDoubleClick(Me, c) : Return
            dragging = True
            dragStart = c.PointToScreen(e.Location)
            originalLocation = c.Location
            dragOriginalLocations.Clear()
            For Each selected As Control In selectedControls
                dragOriginalLocations(selected) = selected.Location
            Next
            If dragOriginalLocations.Count = 0 Then dragOriginalLocations(c) = c.Location
            c.Capture = True
        End Sub
        Private Sub SurfaceDragEnter(sender As Object, e As DragEventArgs)
            If e.Data.GetDataPresent(GetType(ToolboxEntry)) Then e.Effect = DragDropEffects.Copy
        End Sub
        Private Sub SurfaceDragDrop(sender As Object, e As DragEventArgs)
            Dim entry = TryCast(e.Data.GetData(GetType(ToolboxEntry)), ToolboxEntry) : If entry Is Nothing Then Return
            Dim canvas = FindCanvas(), p = canvas.PointToClient(New Point(e.X, e.Y)) : AddComponentAt(entry, p)
        End Sub
        Private Sub ItemMouseMove(sender As Object, e As MouseEventArgs)
            If Not dragging Then Return
            Dim c As Control = DirectCast(sender, Control)
            Dim now As Point = c.PointToScreen(e.Location)
            Dim dx As Integer = now.X - dragStart.X
            Dim dy As Integer = now.Y - dragStart.Y
            Dim target As Point = SmartSnap(c, New Point(originalLocation.X + dx, originalLocation.Y + dy))
            dx = target.X - originalLocation.X
            dy = target.Y - originalLocation.Y
            For Each pair In dragOriginalLocations
                pair.Key.Location = New Point(Math.Max(0, pair.Value.X + dx), Math.Max(0, pair.Value.Y + dy))
            Next
            PositionHandles()
            Invalidate()
        End Sub
        Private Sub ItemMouseUp(sender As Object, e As MouseEventArgs)
            dragging = False
            snapGuideX = Integer.MinValue : snapGuideY = Integer.MinValue
            DirectCast(sender, Control).Capture = False
            Snapshot()
            Invalidate()
        End Sub

        Public Sub SelectControl(c As Control)
            selectedControls.Clear()
            If c IsNot Nothing AndAlso Not TypeOf c Is ComponentTile Then selectedControls.Add(c)
            _selected = c : _selectedComponent = InspectableObject(c) : CreateHandles() : RaiseEvent SelectionChanged(Me, _selectedComponent) : Invalidate()
        End Sub

        Private Sub ToggleControlSelection(c As Control)
            If c Is Nothing OrElse TypeOf c Is ComponentTile Then SelectControl(c) : Return
            If selectedControls.Contains(c) Then
                selectedControls.Remove(c)
            Else
                selectedControls.Add(c)
            End If
            If selectedControls.Count = 0 Then
                SelectForm()
                Return
            End If
            _selected = selectedControls(selectedControls.Count - 1)
            _selectedComponent = InspectableObject(_selected)
            CreateHandles()
            RaiseEvent SelectionChanged(Me, _selectedComponent)
            Invalidate()
        End Sub
        Private Sub SelectToolStripItem(strip As ToolStrip, item As ToolStripItem)
            _selected = strip : _selectedComponent = item : RemoveHandles() : RaiseEvent SelectionChanged(Me, item)
            Dim menuItem As ToolStripDropDownItem = TryCast(item, ToolStripDropDownItem)
            If menuItem IsNot Nothing AndAlso menuItem.HasDropDownItems Then menuItem.ShowDropDown()
        End Sub
        Public Sub SelectForm()
            selectedControls.Clear()
            _selected = Nothing : _selectedComponent = FormTemplate : RemoveHandles() : CreateFormGrip() : RaiseEvent SelectionChanged(Me, FormTemplate) : Invalidate()
        End Sub

        Public Sub AddToolStripItem(itemType As String)
            Dim strip As ToolStrip = TryCast(_selected, ToolStrip)
            If strip Is Nothing Then
                MessageBox.Show("Selecione primeiro um MenuStrip, ToolStrip ou StatusStrip no designer.", "Adicionar item", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Dim item As ToolStripItem = CreateToolStripItem(itemType)
            item.Name = NextToolStripItemName(itemType)
            item.Text = If(itemType = "ToolStripSeparator", "", item.Name)
            Dim selectedParent As ToolStripDropDownItem = TryCast(_selectedComponent, ToolStripDropDownItem)
            If selectedParent IsNot Nothing AndAlso (itemType = "ToolStripMenuItem" OrElse itemType = "ToolStripSeparator") Then
                selectedParent.DropDownItems.Add(item)
            Else
                strip.Items.Add(item)
            End If
            AttachToolStripItemEvents(strip, item)
            Snapshot() : SelectToolStripItem(strip, item)
        End Sub

        Public Function CanEditToolStripItems() As Boolean
            Return ResolveSelectedToolStrip() IsNot Nothing
        End Function


        Public Function CanEditSelectedCollection() As Boolean
            Dim component As Object = SelectedComponent
            Return TypeOf component Is ComboBox OrElse
                   TypeOf component Is ListBox OrElse
                   TypeOf component Is CheckedListBox OrElse
                   TypeOf component Is DataGridView OrElse
                   TypeOf component Is TabControl OrElse
                   TypeOf component Is TreeView
        End Function

        Public Sub EditSelectedCollection(owner As IWin32Window)
            Dim component As Object = SelectedComponent
            If Not CanEditSelectedCollection() Then
                MessageBox.Show("Selecione um ComboBox, ListBox, CheckedListBox, DataGridView, TabControl ou TreeView.",
                                "Editor de Coleções",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Return
            End If

            Using editor As New CollectionEditorForm(component)
                If editor.ShowDialog(owner) <> DialogResult.OK Then Return
            End Using

            Snapshot()
            RaiseEvent SelectionChanged(Me, component)
            Invalidate()
        End Sub

        Public Sub EditSelectedToolStripItems(owner As IWin32Window)
            Dim strip As ToolStrip = ResolveSelectedToolStrip()
            If strip Is Nothing Then
                MessageBox.Show("Selecione um MenuStrip, ToolStrip, StatusStrip ou um de seus itens.", "Editor de itens", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim original As List(Of ToolStripItemData) = CaptureToolStripItems(strip.Items)
            Using editor As New ToolStripItemsEditorForm(original)
                If editor.ShowDialog(owner) <> DialogResult.OK Then Return

                strip.Items.Clear()
                RestoreToolStripItems(strip.Items, editor.Items)
                For Each stripItem As ToolStripItem In strip.Items
                    AttachToolStripItemEvents(strip, stripItem)
                Next
                Snapshot()
                _selected = strip
                _selectedComponent = strip
                RemoveHandles()
                RaiseEvent SelectionChanged(Me, strip)
                Invalidate()
            End Using
        End Sub

        Private Function ResolveSelectedToolStrip() As ToolStrip
            Dim strip As ToolStrip = TryCast(_selected, ToolStrip)
            If strip IsNot Nothing Then Return strip

            Dim selectedItem As ToolStripItem = TryCast(_selectedComponent, ToolStripItem)
            If selectedItem Is Nothing Then Return Nothing

            Dim ownerStrip As ToolStrip = selectedItem.Owner
            While TypeOf ownerStrip Is ToolStripDropDown
                Dim dropDown As ToolStripDropDown = DirectCast(ownerStrip, ToolStripDropDown)
                If dropDown.OwnerItem Is Nothing Then Exit While
                ownerStrip = dropDown.OwnerItem.Owner
            End While
            Return ownerStrip
        End Function

        Public Sub RefreshFromPropertyGrid()
            Dim canvas As Control = FindCanvas() : If canvas Is Nothing Then Return
            If _selected Is Nothing AndAlso FormTemplate IsNot Nothing Then canvas.Size = FormTemplate.ClientSize : canvas.BackColor = FormTemplate.BackColor : AutoScrollMinSize = New Size(canvas.Width + 80, canvas.Height + 80)
            Snapshot() : PositionHandles() : Invalidate()
        End Sub

        Private Sub CreateHandles()
            RemoveHandles() : If _selected Is Nothing OrElse _selected.Parent Is Nothing Then Return
            For Each edge As String In {"NW", "N", "NE", "E", "SE", "S", "SW", "W"}
                Dim h As New Panel With {.Size = New Size(8, 8), .BackColor = Color.White, .BorderStyle = BorderStyle.FixedSingle, .Tag = edge, .Cursor = ResizeCursor(edge)}
                AddHandler h.MouseDown, AddressOf HandleDown : AddHandler h.MouseMove, AddressOf HandleMove : AddHandler h.MouseUp, AddressOf HandleUp
                _selected.Parent.Controls.Add(h) : h.BringToFront() : resizeHandles.Add(h)
            Next
            PositionHandles()
        End Sub
        Private resizeStartMouse As Point, resizeStartBounds As Rectangle
        Private Sub HandleDown(sender As Object, e As MouseEventArgs)
            resizeStartMouse = Cursor.Position : resizeStartBounds = _selected.Bounds : DirectCast(sender, Control).Capture = True
        End Sub
        Private Sub HandleMove(sender As Object, e As MouseEventArgs)
            Dim h As Control = DirectCast(sender, Control) : If Not h.Capture OrElse _selected Is Nothing Then Return
            Dim dx As Integer = Cursor.Position.X - resizeStartMouse.X, dy As Integer = Cursor.Position.Y - resizeStartMouse.Y, edge As String = CStr(h.Tag), r As Rectangle = resizeStartBounds
            If edge.Contains("E") Then r.Width = Math.Max(12, resizeStartBounds.Width + dx)
            If edge.Contains("S") Then r.Height = Math.Max(12, resizeStartBounds.Height + dy)
            If edge.Contains("W") Then r.X = resizeStartBounds.X + dx : r.Width = Math.Max(12, resizeStartBounds.Width - dx)
            If edge.Contains("N") Then r.Y = resizeStartBounds.Y + dy : r.Height = Math.Max(12, resizeStartBounds.Height - dy)
            r.Location = Snap(r.Location) : If SnapToGrid Then r.Size = New Size(Math.Max(12, CInt(Math.Round(r.Width / GridSize)) * GridSize), Math.Max(12, CInt(Math.Round(r.Height / GridSize)) * GridSize))
            _selected.Bounds = r : PositionHandles()
        End Sub
        Private Sub HandleUp(sender As Object, e As MouseEventArgs)
            DirectCast(sender, Control).Capture = False : Snapshot() : RaiseEvent SelectionChanged(Me, _selected)
        End Sub
        Private Sub RemoveHandles()
            For Each h As Panel In resizeHandles.ToArray()
                If h.Parent IsNot Nothing Then h.Parent.Controls.Remove(h) : h.Dispose()
            Next
            resizeHandles.Clear()
            If formGrip IsNot Nothing Then
                If formGrip.Parent IsNot Nothing Then formGrip.Parent.Controls.Remove(formGrip)
                formGrip.Dispose() : formGrip = Nothing
            End If
        End Sub
        Private formResizeStart As Size, formMouseStart As Point

        Public Sub SetShowFormChrome(value As Boolean)
            If ShowFormChrome = value Then Return
            ShowFormChrome = value
            If Document IsNot Nothing Then LoadDocument(Document)
        End Sub

        Public Sub UpdateFormChrome()
            If FormTemplate Is Nothing Then Return
            Dim canvas As Control = FindCanvas()
            If canvas Is Nothing Then Return
            If Not ShowFormChrome OrElse FormTemplate.FormBorderStyle = FormBorderStyle.None Then
                If formChrome IsNot Nothing Then formChrome.Visible = False
                canvas.Top = 30
                Return
            End If
            If formChrome Is Nothing OrElse formChrome.IsDisposed Then CreateFormChrome(canvas)
            Dim isToolWindow As Boolean = FormTemplate.FormBorderStyle = FormBorderStyle.FixedToolWindow OrElse FormTemplate.FormBorderStyle = FormBorderStyle.SizableToolWindow
            Dim chromeHeight As Integer = ChromeHeightFor(FormTemplate.FormBorderStyle)
            formChrome.Visible = True
            formChrome.Location = New Point(canvas.Left, canvas.Top - chromeHeight)
            formChrome.Size = New Size(canvas.Width, chromeHeight)
            formChrome.BackColor = If(FormTemplate.Enabled, ChromeActiveColor, ChromeInactiveColor)
            ApplyChromeRegion(formChrome)
            Dim showIcon As Boolean = FormTemplate.ShowIcon AndAlso Not isToolWindow
            If formChromeTitle IsNot Nothing Then
                formChromeTitle.Font = New Font("Segoe UI", If(isToolWindow, 8.0F, 9.0F), If(isToolWindow, FontStyle.Bold, FontStyle.Regular))
                formChromeTitle.Height = chromeHeight
                formChromeTitle.Text = If(String.IsNullOrWhiteSpace(FormTemplate.Text), FormTemplate.Name, FormTemplate.Text)
                formChromeTitle.Left = If(showIcon, 30, 10)
            End If
            If formChromeIcon IsNot Nothing Then
                formChromeIcon.Visible = showIcon
                formChromeIcon.Top = (chromeHeight - formChromeIcon.Height) \ 2
                If FormTemplate.Icon IsNot Nothing Then formChromeIcon.Image = FormTemplate.Icon.ToBitmap()
            End If
            Dim showBox As Boolean = FormTemplate.ControlBox
            Dim buttonWidth As Integer = If(isToolWindow, 30, 46)
            Dim isMaximized As Boolean = FormTemplate.WindowState = FormWindowState.Maximized
            If formChromeClose IsNot Nothing Then
                formChromeClose.Visible = showBox
                formChromeClose.Size = New Size(buttonWidth, chromeHeight)
            End If
            If formChromeMax IsNot Nothing Then
                formChromeMax.Visible = showBox AndAlso Not isToolWindow
                formChromeMax.Enabled = FormTemplate.MaximizeBox
                formChromeMax.ForeColor = If(FormTemplate.MaximizeBox, Color.White, Color.FromArgb(255, 190, 210, 230))
                formChromeMax.Size = New Size(buttonWidth, chromeHeight)
                formChromeMax.Text = If(isMaximized, GlyphRestore, GlyphMaximize)
            End If
            If formChromeMin IsNot Nothing Then
                formChromeMin.Visible = showBox AndAlso Not isToolWindow
                formChromeMin.Enabled = FormTemplate.MinimizeBox
                formChromeMin.ForeColor = If(FormTemplate.MinimizeBox, Color.White, Color.FromArgb(255, 190, 210, 230))
                formChromeMin.Size = New Size(buttonWidth, chromeHeight)
            End If
            formChromeClose.Location = New Point(Math.Max(0, formChrome.Width - buttonWidth), 0)
            formChromeMax.Location = New Point(Math.Max(0, formChrome.Width - buttonWidth * 2), 0)
            formChromeMin.Location = New Point(Math.Max(0, formChrome.Width - buttonWidth * 3), 0)
            formChromeTitle.Width = Math.Max(40, formChrome.Width - formChromeTitle.Left - (buttonWidth * If(isToolWindow, 1, 3)) - 6)
            Dim canvasPanel As Panel = TryCast(canvas, Panel)
            If canvasPanel IsNot Nothing Then
                canvasPanel.BorderStyle = If(FormTemplate.FormBorderStyle = FormBorderStyle.None, BorderStyle.None, BorderStyle.FixedSingle)
            End If
            PositionFormGrip()
        End Sub

        Private Sub ApplyChromeRegion(panel As Panel)
            If panel.Width <= 0 OrElse panel.Height <= 0 Then Return
            Const radius As Integer = 6
            Dim path As New GraphicsPath()
            Dim rect As New Rectangle(0, 0, panel.Width, panel.Height)
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90)
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90)
            path.AddLine(rect.Right, rect.Y + radius, rect.Right, rect.Bottom)
            path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom)
            path.AddLine(rect.X, rect.Bottom, rect.X, rect.Y + radius)
            path.CloseFigure()
            If panel.Region IsNot Nothing Then panel.Region.Dispose()
            panel.Region = New Region(path)
        End Sub

        Private Sub CreateFormChrome(canvas As Control)
            If formChrome IsNot Nothing Then
                Controls.Remove(formChrome)
                formChrome.Dispose()
            End If
            formChrome = New Panel With {.Name = "__FORM_CHROME__", .Height = 32, .BackColor = ChromeActiveColor}
            formChromeIcon = New PictureBox With {.Location = New Point(8, 7), .Size = New Size(18, 18), .SizeMode = PictureBoxSizeMode.StretchImage, .BackColor = Color.Transparent}
            formChromeTitle = New Label With {.Location = New Point(30, 0), .Height = 32, .TextAlign = ContentAlignment.MiddleLeft, .ForeColor = Color.White, .BackColor = Color.Transparent, .Font = New Font("Segoe UI", 9.0F), .AutoEllipsis = True}
            formChromeMin = ChromeButton(GlyphMinimize, False)
            formChromeMax = ChromeButton(GlyphMaximize, False)
            formChromeClose = ChromeButton(GlyphClose, True)
            formChrome.Controls.Add(formChromeIcon)
            formChrome.Controls.Add(formChromeTitle)
            formChrome.Controls.Add(formChromeMin)
            formChrome.Controls.Add(formChromeMax)
            formChrome.Controls.Add(formChromeClose)
            Controls.Add(formChrome)
            AddHandler formChrome.MouseDown, Sub(sender, e) SelectForm()
            AddHandler formChrome.DoubleClick, Sub(sender, e) RaiseEvent ComponentDoubleClick(Me, FormTemplate)
            AddHandler formChromeTitle.MouseDown, Sub(sender, e) SelectForm()
            AddHandler formChromeTitle.DoubleClick, Sub(sender, e) RaiseEvent ComponentDoubleClick(Me, FormTemplate)
            AddHandler formChromeIcon.MouseDown, Sub(sender, e) SelectForm()
            formChrome.BringToFront()
            UpdateFormChrome()
        End Sub

        Private Function ChromeButton(caption As String, isClose As Boolean) As Label
            Dim btn As New Label With {.Text = caption, .Size = New Size(46, 32), .TextAlign = ContentAlignment.MiddleCenter, .ForeColor = Color.White, .BackColor = Color.Transparent, .Font = New Font("Segoe MDL2 Assets", 8.5F)}
            AddHandler btn.MouseDown, Sub(sender, e) SelectForm()
            AddHandler btn.MouseEnter, Sub(sender, e)
                                            If Not btn.Enabled Then Return
                                            Dim baseColor As Color = If(btn.Parent IsNot Nothing, btn.Parent.BackColor, ChromeActiveColor)
                                            btn.BackColor = If(isClose, ChromeCloseHoverColor, ControlPaint.Light(baseColor, 0.4F))
                                        End Sub
            AddHandler btn.MouseLeave, Sub(sender, e) btn.BackColor = Color.Transparent
            AddHandler btn.EnabledChanged, Sub(sender, e)
                                                If Not btn.Enabled Then btn.BackColor = Color.Transparent
                                            End Sub
            Return btn
        End Function

        Private Sub CreateFormGrip()
            Dim canvas = FindCanvas() : If canvas Is Nothing Then Return
            formGrip = New Panel With {.Size = New Size(14, 14), .BackColor = Color.White, .BorderStyle = BorderStyle.FixedSingle, .Cursor = Cursors.SizeNWSE}
            Controls.Add(formGrip) : PositionFormGrip()
            AddHandler formGrip.MouseDown, AddressOf FormGripMouseDown : AddHandler formGrip.MouseMove, AddressOf FormGripMouseMove : AddHandler formGrip.MouseUp, AddressOf FormGripMouseUp
        End Sub
        Private Sub FormGripMouseDown(sender As Object, e As MouseEventArgs)
            If e.Button <> MouseButtons.Left Then Return
            Dim canvas = FindCanvas() : formResizeStart = canvas.Size : formMouseStart = Cursor.Position : formGrip.Capture = True
        End Sub
        Private Sub FormGripMouseMove(sender As Object, e As MouseEventArgs)
            If formGrip Is Nothing OrElse Not formGrip.Capture Then Return
            Dim canvas = FindCanvas(), dx = Cursor.Position.X - formMouseStart.X, dy = Cursor.Position.Y - formMouseStart.Y
            Dim w = Math.Max(200, formResizeStart.Width + dx), h = Math.Max(120, formResizeStart.Height + dy)
            If SnapToGrid Then w = CInt(Math.Round(w / GridSize)) * GridSize : h = CInt(Math.Round(h / GridSize)) * GridSize
            canvas.Size = New Size(w, h) : FormTemplate.ClientSize = canvas.Size : UpdateFormChrome() : PositionFormGrip()
        End Sub
        Private Sub FormGripMouseUp(sender As Object, e As MouseEventArgs)
            formGrip.Capture = False : Snapshot() : RaiseEvent SelectionChanged(Me, FormTemplate)
        End Sub
        Private Sub PositionFormGrip()
            Dim canvas = FindCanvas()
            If formGrip IsNot Nothing AndAlso canvas IsNot Nothing Then formGrip.Location = New Point(canvas.Right - 7, canvas.Bottom - 7) : formGrip.BringToFront()
        End Sub
        Private Sub PositionHandles()
            If _selected Is Nothing Then Return
            Dim x As Integer = _selected.Left, y As Integer = _selected.Top, w As Integer = _selected.Width, h As Integer = _selected.Height
            Dim points() As Point = {New Point(x - 4, y - 4), New Point(x + w \ 2 - 4, y - 4), New Point(x + w - 4, y - 4), New Point(x + w - 4, y + h \ 2 - 4), New Point(x + w - 4, y + h - 4), New Point(x + w \ 2 - 4, y + h - 4), New Point(x - 4, y + h - 4), New Point(x - 4, y + h \ 2 - 4)}
            For i As Integer = 0 To Math.Min(resizeHandles.Count, points.Length) - 1
                resizeHandles(i).Location = points(i)
                resizeHandles(i).BringToFront()
            Next
        End Sub

        Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
            MyBase.OnPaintBackground(e)
            If ShowGrid Then
                Using p As New Pen(Color.FromArgb(45, 120, 130, 150))
                    For x As Integer = 30 To Width Step Math.Max(2, GridSize)
                        e.Graphics.DrawLine(p, x, 0, x, Height)
                    Next
                    For y As Integer = 30 To Height Step Math.Max(2, GridSize)
                        e.Graphics.DrawLine(p, 0, y, Width, y)
                    Next
                End Using
            End If
            Using selectionPen As New Pen(Color.DodgerBlue, 1.0F)
                selectionPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
                For Each selected As Control In selectedControls
                    If selected.Parent IsNot Nothing Then
                        Dim rect As Rectangle = New Rectangle(selected.Parent.PointToScreen(selected.Location), selected.Size)
                        rect.Location = PointToClient(rect.Location)
                        e.Graphics.DrawRectangle(selectionPen, rect)
                    End If
                Next
                If selectingRectangle AndAlso Not selectionRectangle.IsEmpty Then e.Graphics.DrawRectangle(selectionPen, selectionRectangle)
            End Using
            Using guidePen As New Pen(Color.DeepSkyBlue, 1.0F)
                If snapGuideX <> Integer.MinValue Then e.Graphics.DrawLine(guidePen, snapGuideX, 0, snapGuideX, Height)
                If snapGuideY <> Integer.MinValue Then e.Graphics.DrawLine(guidePen, 0, snapGuideY, Width, snapGuideY)
            End Using
        End Sub

        Public ReadOnly Property CanUndoDesigner As Boolean
            Get
                Return undoHistory.Count > 1
            End Get
        End Property
        Public ReadOnly Property CanRedoDesigner As Boolean
            Get
                Return redoHistory.Count > 0
            End Get
        End Property
        Public Function UndoDesigner() As Boolean
            If undoHistory.Count <= 1 Then Return False
            Dim current As String = undoHistory(undoHistory.Count - 1)
            undoHistory.RemoveAt(undoHistory.Count - 1)
            redoHistory.Add(current)
            RestoreDesignerState(undoHistory(undoHistory.Count - 1))
            Return True
        End Function
        Public Function RedoDesigner() As Boolean
            If redoHistory.Count = 0 Then Return False
            Dim value As String = redoHistory(redoHistory.Count - 1)
            redoHistory.RemoveAt(redoHistory.Count - 1)
            undoHistory.Add(value)
            RestoreDesignerState(value)
            Return True
        End Function
        Private Sub ResetDesignerHistory()
            undoHistory.Clear() : redoHistory.Clear()
            lastHistoryState = SerializeDocument(Document)
            If lastHistoryState <> "" Then undoHistory.Add(lastHistoryState)
        End Sub
        Private Sub RecordDesignerHistory()
            If restoringHistory OrElse Document Is Nothing Then Return
            Dim value As String = SerializeDocument(Document)
            If value = "" OrElse value = lastHistoryState Then Return
            undoHistory.Add(value)
            If undoHistory.Count > 80 Then undoHistory.RemoveAt(0)
            redoHistory.Clear()
            lastHistoryState = value
        End Sub
        Private Shared Function SerializeDocument(value As FormData) As String
            If value Is Nothing Then Return ""
            Dim serializer As New DataContractJsonSerializer(GetType(FormData))
            Using stream As New MemoryStream()
                serializer.WriteObject(stream, value)
                Return Convert.ToBase64String(stream.ToArray())
            End Using
        End Function
        Private Shared Function DeserializeDocument(value As String) As FormData
            Dim serializer As New DataContractJsonSerializer(GetType(FormData))
            Using stream As New MemoryStream(Convert.FromBase64String(value))
                Return DirectCast(serializer.ReadObject(stream), FormData)
            End Using
        End Function
        Private Sub RestoreDesignerState(value As String)
            Dim restored As FormData = DeserializeDocument(value)
            Dim savedUndo As New List(Of String)(undoHistory)
            Dim savedRedo As New List(Of String)(redoHistory)
            restoringHistory = True
            Try
                If UserControlDocument IsNot Nothing Then
                    Dim uc As UserControlData = UserControlDocument
                    uc.Name = restored.Name
                    uc.Properties = restored.Properties
                    uc.Controls = restored.Controls
                    LoadUserControl(uc)
                Else
                    Document.Name = restored.Name
                    Document.Properties = restored.Properties
                    Document.Controls = restored.Controls
                    LoadDocument(Document)
                End If
                undoHistory.Clear() : undoHistory.AddRange(savedUndo)
                redoHistory.Clear() : redoHistory.AddRange(savedRedo)
                lastHistoryState = value
            Finally
                restoringHistory = False
            End Try
        End Sub

        Public Function AlignSelection(mode As String) As Boolean
            Dim items As List(Of Control) = ActiveSelection()
            If items.Count < 2 Then Return False
            Dim anchor As Control = items(0)
            Select Case mode.ToLowerInvariant()
                Case "left"
                    For Each c In items.Skip(1) : c.Left = anchor.Left : Next
                Case "right"
                    For Each c In items.Skip(1) : c.Left = anchor.Right - c.Width : Next
                Case "top"
                    For Each c In items.Skip(1) : c.Top = anchor.Top : Next
                Case "bottom"
                    For Each c In items.Skip(1) : c.Top = anchor.Bottom - c.Height : Next
                Case "hcenter"
                    For Each c In items.Skip(1) : c.Left = anchor.Left + (anchor.Width - c.Width) \ 2 : Next
                Case "vcenter"
                    For Each c In items.Skip(1) : c.Top = anchor.Top + (anchor.Height - c.Height) \ 2 : Next
                Case Else
                    Return False
            End Select
            Snapshot() : Invalidate() : Return True
        End Function
        Public Function MakeSameSize(widthOnly As Boolean, heightOnly As Boolean) As Boolean
            Dim items As List(Of Control) = ActiveSelection()
            If items.Count < 2 Then Return False
            Dim anchor As Control = items(0)
            For Each c In items.Skip(1)
                Dim w As Integer = If(heightOnly, c.Width, anchor.Width)
                Dim h As Integer = If(widthOnly, c.Height, anchor.Height)
                c.Size = New Size(w, h)
            Next
            Snapshot() : Invalidate() : Return True
        End Function
        Public Function DistributeSelection(horizontal As Boolean) As Boolean
            Dim items As List(Of Control) = ActiveSelection()
            If items.Count < 3 Then Return False
            If horizontal Then
                items = items.OrderBy(Function(c) c.Left).ToList()
                Dim span As Integer = items.Last().Left - items.First().Left
                Dim stepValue As Double = span / CDbl(items.Count - 1)
                For i As Integer = 1 To items.Count - 2 : items(i).Left = CInt(Math.Round(items.First().Left + stepValue * i)) : Next
            Else
                items = items.OrderBy(Function(c) c.Top).ToList()
                Dim span As Integer = items.Last().Top - items.First().Top
                Dim stepValue As Double = span / CDbl(items.Count - 1)
                For i As Integer = 1 To items.Count - 2 : items(i).Top = CInt(Math.Round(items.First().Top + stepValue * i)) : Next
            End If
            Snapshot() : Invalidate() : Return True
        End Function
        Private Function ActiveSelection() As List(Of Control)
            If selectedControls.Count > 0 Then Return selectedControls.Where(Function(c) c IsNot Nothing AndAlso c.Parent IsNot Nothing AndAlso Not TypeOf c Is ComponentTile).ToList()
            Dim result As New List(Of Control)()
            If _selected IsNot Nothing AndAlso Not TypeOf _selected Is ComponentTile Then result.Add(_selected)
            Return result
        End Function

        Private Function SmartSnap(c As Control, proposed As Point) As Point
            Dim result As Point = Snap(proposed)
            snapGuideX = Integer.MinValue : snapGuideY = Integer.MinValue
            Dim canvas As Control = FindCanvas()
            If canvas Is Nothing Then Return result
            Const tolerance As Integer = 5
            For Each other As Control In canvas.Controls
                If other Is c OrElse selectedControls.Contains(other) OrElse resizeHandles.Contains(TryCast(other, Panel)) Then Continue For
                Dim xCandidates As Integer() = {other.Left, other.Right, other.Left + other.Width \ 2}
                Dim myX As Integer() = {result.X, result.X + c.Width, result.X + c.Width \ 2}
                For i As Integer = 0 To myX.Length - 1
                    For Each targetX As Integer In xCandidates
                        If Math.Abs(myX(i) - targetX) <= tolerance Then
                            result.X += targetX - myX(i)
                            snapGuideX = canvas.Left + targetX
                            Exit For
                        End If
                    Next
                    If snapGuideX <> Integer.MinValue Then Exit For
                Next
                Dim yCandidates As Integer() = {other.Top, other.Bottom, other.Top + other.Height \ 2}
                Dim myY As Integer() = {result.Y, result.Y + c.Height, result.Y + c.Height \ 2}
                For i As Integer = 0 To myY.Length - 1
                    For Each targetY As Integer In yCandidates
                        If Math.Abs(myY(i) - targetY) <= tolerance Then
                            result.Y += targetY - myY(i)
                            snapGuideY = canvas.Top + targetY
                            Exit For
                        End If
                    Next
                    If snapGuideY <> Integer.MinValue Then Exit For
                Next
            Next
            Return result
        End Function
        Private Sub CanvasMouseDown(sender As Object, e As MouseEventArgs)
            If e.Button <> MouseButtons.Left Then Return
            If (ModifierKeys And (Keys.Control Or Keys.Shift)) = Keys.None Then selectedControls.Clear()
            selectingRectangle = True
            selectionStart = PointToClient(DirectCast(sender, Control).PointToScreen(e.Location))
            selectionRectangle = New Rectangle(selectionStart, Size.Empty)
            Invalidate()
        End Sub
        Private Sub CanvasMouseMove(sender As Object, e As MouseEventArgs)
            If Not selectingRectangle Then Return
            Dim current As Point = PointToClient(DirectCast(sender, Control).PointToScreen(e.Location))
            selectionRectangle = Rectangle.FromLTRB(Math.Min(selectionStart.X, current.X), Math.Min(selectionStart.Y, current.Y), Math.Max(selectionStart.X, current.X), Math.Max(selectionStart.Y, current.Y))
            Invalidate()
        End Sub
        Private Sub CanvasMouseUp(sender As Object, e As MouseEventArgs)
            If Not selectingRectangle Then Return
            selectingRectangle = False
            Dim canvas As Control = DirectCast(sender, Control)
            If selectionRectangle.Width < 4 AndAlso selectionRectangle.Height < 4 Then
                SelectForm()
            Else
                For Each c As Control In canvas.Controls
                    If TypeOf c Is ComponentTile OrElse resizeHandles.Contains(TryCast(c, Panel)) Then Continue For
                    Dim rect As New Rectangle(PointToClient(canvas.PointToScreen(c.Location)), c.Size)
                    If selectionRectangle.IntersectsWith(rect) AndAlso Not selectedControls.Contains(c) Then selectedControls.Add(c)
                Next
                If selectedControls.Count > 0 Then
                    _selected = selectedControls(selectedControls.Count - 1)
                    _selectedComponent = InspectableObject(_selected)
                    CreateHandles()
                    RaiseEvent SelectionChanged(Me, _selectedComponent)
                Else
                    SelectForm()
                End If
            End If
            selectionRectangle = Rectangle.Empty
            Invalidate()
        End Sub

        Private Function FindCanvas() As Control
            Return Controls.Cast(Of Control)().FirstOrDefault(Function(c) c.Name = "__FORM__")
        End Function
        Private Function NextName(typeName As String) As String
            Dim canvas As Control = FindCanvas(), i As Integer = 1
            Dim baseName As String = typeName.Split("."c).Last()
            baseName = Regex.Replace(baseName, "[^A-Za-z0-9_]", "")
            If String.IsNullOrWhiteSpace(baseName) Then baseName = "Component"
            While canvas.Controls.Cast(Of Control)().Any(Function(c) c.Name.Equals(baseName & i, StringComparison.OrdinalIgnoreCase)) : i += 1 : End While
            Return baseName & i
        End Function
        Private Function Snap(p As Point) As Point
            If Not SnapToGrid Then Return p
            Return New Point(Math.Max(0, CInt(Math.Round(p.X / GridSize)) * GridSize), Math.Max(0, CInt(Math.Round(p.Y / GridSize)) * GridSize))
        End Function
        Private Shadows Shared Function DefaultSize(typeName As String) As Size
            If {"DataGridView", "Panel", "GradientPanel", "WebBrowser", "RichTextBox", "TreeView", "ListView", "CheckedListBox", "TabControl", "GroupBox", "FlowLayoutPanel", "TableLayoutPanel", "SplitContainer", "PropertyGrid"}.Contains(typeName) Then Return New Size(240, 140)
            If typeName = "MenuStrip" OrElse typeName = "ToolStrip" OrElse typeName = "StatusStrip" OrElse typeName = "BindingNavigator" Then Return New Size(500, 28)
            If typeName = "Splitter" Then Return New Size(6, 180)
            If {"TextBox", "MaskedTextBox", "ComboBox", "ColorComboBox", "NumericUpDown", "DomainUpDown"}.Contains(typeName) Then Return New Size(180, 25)
            If typeName = "MonthCalendar" Then Return New Size(230, 165)
            If typeName = "DigitalDisplay" Then Return New Size(260, 55)
            If typeName = "LedIndicator" Then Return New Size(36, 36)
            If typeName = "ToggleSwitch" Then Return New Size(58, 28)
            If typeName = "RoundedButton" Then Return New Size(140, 42)
            If typeName = "CircularProgress" Then Return New Size(100, 100)
            If typeName = "LevelMeter" Then Return New Size(38, 160)
            If typeName = "BadgeLabel" Then Return New Size(100, 30)
            If typeName = "SeparatorLine" Then Return New Size(180, 4)
            If typeName = "StarRating" Then Return New Size(160, 36)
            If typeName = "NumericKnob" Then Return New Size(90, 90)
            If typeName = "CardPanel" Then Return New Size(220, 140)
            If typeName = "BatteryIndicator" Then Return New Size(110, 45)
            If typeName = "SignalStrength" Then Return New Size(80, 50)
            If typeName = "ThermometerGauge" Then Return New Size(70, 190)
            If typeName = "AnalogGauge" Then Return New Size(180, 115)
            If typeName = "LoadingSpinner" Then Return New Size(48, 48)
            If typeName = "NotificationBanner" Then Return New Size(300, 48)
            If typeName = "ToggleButton" Then Return New Size(120, 38)
            If typeName = "ColorSwatch" Then Return New Size(70, 45)
            If typeName = "NavigationButton" Then Return New Size(48, 48)
            If typeName = "MarqueeLabel" Then Return New Size(240, 32)
            If typeName = "RichTextEditor" Then Return New Size(480, 300)
            If typeName = "SyntaxCodeEditor" Then Return New Size(520, 320)
            If typeName = "Sparkline" Then Return New Size(220, 90)
            If typeName = "TagInput" Then Return New Size(240, 34)
            If typeName = "TabStripCustom" Then Return New Size(320, 36)
            If typeName = "JsonTreeViewer" Then Return New Size(260, 220)
            If typeName = "ImageButton" Then Return New Size(150, 48)
            If typeName = "SearchBox" Then Return New Size(260, 30)
            If typeName = "PasswordBox" Then Return New Size(180, 25)
            If typeName = "IPAddressBox" Then Return New Size(140, 25)
            If typeName = "ModernDatePicker" Then Return New Size(160, 28)
            If typeName = "SimpleChart" Then Return New Size(300, 180)
            If typeName = "VirtualJoystick" Then Return New Size(130, 130)
            If typeName = "LcdDisplay" Then Return New Size(260, 70)
            If typeName = "LedMatrix" Then Return New Size(180, 180)
            If typeName = "TrafficLight" Then Return New Size(70, 180)
            If typeName = "SevenSegmentDigit" Then Return New Size(65, 105)
            If typeName = "ArduinoPin" Then Return New Size(120, 52)
            If typeName = "IoTSensor" Then Return New Size(190, 90)
            Return New Size(120, 35)
        End Function
        Public Shadows Shared Function CreateControl(typeName As String) As Control
            Select Case typeName
                Case "Label" : Return New Label()
                Case "TextBox" : Return New TextBox()
                Case "RichTextBox" : Return New RichTextBox()
                Case "MaskedTextBox" : Return New MaskedTextBox()
                Case "CheckBox" : Return New CheckBox()
                Case "RadioButton" : Return New RadioButton()
                Case "ComboBox" : Return New ComboBox()
                Case "ColorComboBox" : Return New ColorComboBox()
                Case "RoundedButton" : Return New RoundedButton()
                Case "GradientPanel" : Return New GradientPanel()
                Case "LedIndicator" : Return New LedIndicator()
                Case "ToggleSwitch" : Return New ToggleSwitch()
                Case "DigitalDisplay" : Return New DigitalDisplay()
                Case "CircularProgress" : Return New CircularProgress()
                Case "LevelMeter" : Return New LevelMeter()
                Case "BadgeLabel" : Return New BadgeLabel()
                Case "SeparatorLine" : Return New SeparatorLine()
                Case "StarRating" : Return New StarRating()
                Case "NumericKnob" : Return New NumericKnob()
                Case "CardPanel" : Return New CardPanel()
                Case "BatteryIndicator" : Return New BatteryIndicator()
                Case "SignalStrength" : Return New SignalStrength()
                Case "ThermometerGauge" : Return New ThermometerGauge()
                Case "AnalogGauge" : Return New AnalogGauge()
                Case "LoadingSpinner" : Return New LoadingSpinner()
                Case "NotificationBanner" : Return New NotificationBanner()
                Case "ToggleButton" : Return New ToggleButton()
                Case "ColorSwatch" : Return New ColorSwatch()
                Case "NavigationButton" : Return New NavigationButton()
                Case "MarqueeLabel" : Return New MarqueeLabel()
                Case "RichTextEditor" : Return New RichTextEditor()
                Case "SyntaxCodeEditor" : Return New SyntaxCodeEditor()
                Case "Sparkline" : Return New Sparkline()
                Case "TagInput" : Return New TagInput()
                Case "TabStripCustom" : Return New TabStripCustom()
                Case "JsonTreeViewer" : Return New JsonTreeViewer()
                Case "ImageButton" : Return New ImageButton()
                Case "SearchBox" : Return New SearchBox()
                Case "PasswordBox" : Return New PasswordBox()
                Case "IPAddressBox" : Return New IPAddressBox()
                Case "ModernDatePicker" : Return New ModernDatePicker()
                Case "SimpleChart" : Return New SimpleChart()
                Case "VirtualJoystick" : Return New VirtualJoystick()
                Case "LcdDisplay" : Return New LcdDisplay()
                Case "LedMatrix" : Return New LedMatrix()
                Case "TrafficLight" : Return New TrafficLight()
                Case "SevenSegmentDigit" : Return New SevenSegmentDigit()
                Case "ArduinoPin" : Return New ArduinoPin()
                Case "IoTSensor" : Return New IoTSensor()
                Case "ListBox" : Return New ListBox()
                Case "CheckedListBox" : Return New CheckedListBox()
                Case "TreeView" : Return New TreeView()
                Case "ListView" : Return New ListView() With {.View = View.Details}
                Case "PictureBox" : Return New PictureBox() With {.BorderStyle = BorderStyle.FixedSingle}
                Case "TrackBar" : Return New TrackBar()
                Case "HScrollBar" : Return New HScrollBar()
                Case "VScrollBar" : Return New VScrollBar()
                Case "ProgressBar" : Return New ProgressBar()
                Case "NumericUpDown" : Return New NumericUpDown()
                Case "DomainUpDown" : Return New DomainUpDown()
                Case "DateTimePicker" : Return New DateTimePicker()
                Case "MonthCalendar" : Return New MonthCalendar()
                Case "DataGridView" : Return New DataGridView()
                Case "Panel" : Return New Panel() With {.BorderStyle = BorderStyle.FixedSingle}
                Case "GroupBox" : Return New GroupBox()
                Case "TabControl" : Return New TabControl()
                Case "FlowLayoutPanel" : Return New FlowLayoutPanel() With {.BorderStyle = BorderStyle.FixedSingle}
                Case "TableLayoutPanel" : Return New TableLayoutPanel() With {.BorderStyle = BorderStyle.FixedSingle}
                Case "SplitContainer" : Return New SplitContainer()
                Case "LinkLabel" : Return New LinkLabel()
                Case "WebBrowser" : Return New WebBrowser()
                Case "MenuStrip" : Return New MenuStrip()
                Case "ToolStrip" : Return New ToolStrip()
                Case "StatusStrip" : Return New StatusStrip()
                Case "PropertyGrid" : Return New PropertyGrid()
                Case "BindingNavigator" : Return New BindingNavigator()
                Case "Splitter" : Return New Splitter()
                Case Else : Return New Button()
            End Select
        End Function

        Private Shared Function CreateToolStripItem(typeName As String) As ToolStripItem
            Select Case typeName
                Case "ToolStripMenuItem" : Return New ToolStripMenuItem()
                Case "ToolStripSeparator" : Return New ToolStripSeparator()
                Case "ToolStripLabel" : Return New ToolStripLabel()
                Case "ToolStripTextBox" : Return New ToolStripTextBox()
                Case "ToolStripComboBox" : Return New ToolStripComboBox()
                Case "ToolStripDropDownButton" : Return New ToolStripDropDownButton()
                Case "ToolStripSplitButton" : Return New ToolStripSplitButton()
                Case "ToolStripStatusLabel" : Return New ToolStripStatusLabel()
                Case "ToolStripProgressBar" : Return New ToolStripProgressBar()
                Case Else : Return New ToolStripButton()
            End Select
        End Function

        Private Function NextToolStripItemName(typeName As String) As String
            Dim used As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Dim canvas As Control = FindCanvas()
            For Each strip As ToolStrip In canvas.Controls.OfType(Of ToolStrip)()
                CollectToolStripNames(strip.Items, used)
            Next
            Dim index As Integer = 1
            While used.Contains(typeName & index) : index += 1 : End While
            Return typeName & index
        End Function

        Private Shared Sub CollectToolStripNames(items As ToolStripItemCollection, names As HashSet(Of String))
            For Each item As ToolStripItem In items
                names.Add(item.Name)
                Dim parent As ToolStripDropDownItem = TryCast(item, ToolStripDropDownItem)
                If parent IsNot Nothing Then CollectToolStripNames(parent.DropDownItems, names)
            Next
        End Sub

        Private Shared Function CaptureToolStripItems(items As ToolStripItemCollection) As List(Of ToolStripItemData)
            Dim result As New List(Of ToolStripItemData)()
            For Each item As ToolStripItem In items
                Dim data As New ToolStripItemData With {.TypeName = item.GetType().Name, .Name = item.Name, .Text = item.Text, .Properties = CaptureProperties(item)}
                Dim parent As ToolStripDropDownItem = TryCast(item, ToolStripDropDownItem)
                If parent IsNot Nothing Then data.DropDownItems = CaptureToolStripItems(parent.DropDownItems)
                result.Add(data)
            Next
            Return result
        End Function

        Private Shared Sub RestoreToolStripItems(target As ToolStripItemCollection, items As List(Of ToolStripItemData))
            If items Is Nothing Then Return
            For Each data As ToolStripItemData In items
                Dim item As ToolStripItem = CreateToolStripItem(data.TypeName)
                ApplyProperties(item, data.Properties) : item.Name = data.Name : item.Text = data.Text
                target.Add(item)
                Dim parent As ToolStripDropDownItem = TryCast(item, ToolStripDropDownItem)
                If parent IsNot Nothing Then RestoreToolStripItems(parent.DropDownItems, data.DropDownItems)
            Next
        End Sub

        Private Shared Function CreateDesignItem(typeName As String, assemblyPath As String, nonVisual As Boolean) As Control
            If nonVisual Then
                Return New ComponentTile(typeName, assemblyPath, CreateComponent(typeName, assemblyPath))
            End If
            If String.IsNullOrWhiteSpace(assemblyPath) Then Return CreateControl(typeName)
            If assemblyPath.StartsWith("project://", StringComparison.OrdinalIgnoreCase) Then
                Dim placeholder As New ProjectUserControlPlaceholder(typeName)
                placeholder.Tag = assemblyPath
                Return placeholder
            End If
            Dim assembly As Assembly = Assembly.LoadFrom(assemblyPath)
            Dim type As Type = assembly.GetType(typeName, True, True)
            Dim result As Control = TryCast(Activator.CreateInstance(type), Control)
            If result Is Nothing Then Throw New InvalidOperationException(typeName & " não é um controle Windows Forms.")
            result.Tag = assemblyPath
            Return result
        End Function

        Private Shared Function CreateComponent(typeName As String, assemblyPath As String) As Component
            Dim type As Type
            If String.IsNullOrWhiteSpace(assemblyPath) Then
                If typeName.Equals("SerialConnection", StringComparison.OrdinalIgnoreCase) Then Return New SerialConnection()
                type = GetType(Form).Assembly.GetType("System.Windows.Forms." & typeName, True, True)
            Else
                type = Assembly.LoadFrom(assemblyPath).GetType(typeName, True, True)
            End If
            Return DirectCast(Activator.CreateInstance(type), Component)
        End Function

        Private Shared Function InspectableObject(control As Control) As Object
            Dim tile As ComponentTile = TryCast(control, ComponentTile)
            Return If(tile Is Nothing, DirectCast(control, Object), DirectCast(tile.ComponentInstance, Object))
        End Function

        Private Shared Sub ApplyItemProperties(control As Control, values As Dictionary(Of String, String))
            Dim tile As ComponentTile = TryCast(control, ComponentTile)
            If tile Is Nothing Then
                ApplyProperties(control, values)
            Else
                ApplyProperties(tile.ComponentInstance, values)
                Dim storedLocation As Size = ParseSize(GetValue(values, "__TrayLocation", "20, 20"), New Size(20, 20))
                tile.Location = New Point(storedLocation.Width, storedLocation.Height)
            End If
        End Sub
        Public Shared Function FriendlyType(c As Control) As String
            Dim projectControl As ProjectUserControlPlaceholder = TryCast(c, ProjectUserControlPlaceholder)
            If projectControl IsNot Nothing Then Return projectControl.ProjectTypeName
            Return c.GetType().Name
        End Function
        Public Shared Function CaptureProperties(component As Object) As Dictionary(Of String, String)
            Dim values As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            For Each p As PropertyDescriptor In TypeDescriptor.GetProperties(component)
                If Not p.IsBrowsable OrElse p.IsReadOnly Then Continue For
                If IsTransientDesignProperty(component, p.Name) Then Continue For
                Try
                    If p.Converter IsNot Nothing AndAlso p.Converter.CanConvertTo(GetType(String)) AndAlso p.Converter.CanConvertFrom(GetType(String)) Then values(p.Name) = p.Converter.ConvertToInvariantString(p.GetValue(component))
                Catch
                End Try
            Next

            ' Algumas coleções do WinForms não podem ser convertidas para String pelo
            ' TypeConverter padrão. O FlowForge usa chaves internas para preservá-las.
            If TypeOf component Is ComboBox Then
                values("__FF_ITEMS") =
                    EncodeStringList(DirectCast(component, ComboBox).Items.Cast(Of Object)().
                                     Select(Function(v) Convert.ToString(v)))
            ElseIf TypeOf component Is CheckedListBox Then
                values("__FF_ITEMS") =
                    EncodeStringList(DirectCast(component, CheckedListBox).Items.Cast(Of Object)().
                                     Select(Function(v) Convert.ToString(v)))
            ElseIf TypeOf component Is ListBox Then
                values("__FF_ITEMS") =
                    EncodeStringList(DirectCast(component, ListBox).Items.Cast(Of Object)().
                                     Select(Function(v) Convert.ToString(v)))
            ElseIf TypeOf component Is DataGridView Then
                values("__FF_COLUMNS") = SerializeGridColumns(DirectCast(component, DataGridView))
            ElseIf TypeOf component Is TabControl Then
                values("__FF_TABPAGES") = SerializeTabPages(DirectCast(component, TabControl))
            ElseIf TypeOf component Is TreeView Then
                values("__FF_TREENODES") = SerializeTreeNodes(DirectCast(component, TreeView))
            End If

            Return values
        End Function
        Public Shared Sub ApplyProperties(component As Object, values As Dictionary(Of String, String))
            If values Is Nothing Then Return

            For Each pair In values
                If IsTransientDesignProperty(component, pair.Key) Then Continue For

                If pair.Key.Equals("__FF_ITEMS", StringComparison.OrdinalIgnoreCase) Then
                    ApplyStringItems(component, pair.Value)
                    Continue For
                End If

                If pair.Key.Equals("__FF_COLUMNS", StringComparison.OrdinalIgnoreCase) AndAlso
                   TypeOf component Is DataGridView Then
                    ApplyGridColumns(DirectCast(component, DataGridView), pair.Value)
                    Continue For
                End If

                If pair.Key.Equals("__FF_TABPAGES", StringComparison.OrdinalIgnoreCase) AndAlso
                   TypeOf component Is TabControl Then
                    ApplyTabPages(DirectCast(component, TabControl), pair.Value)
                    Continue For
                End If

                If pair.Key.Equals("__FF_TREENODES", StringComparison.OrdinalIgnoreCase) AndAlso
                   TypeOf component Is TreeView Then
                    ApplyTreeNodes(DirectCast(component, TreeView), pair.Value)
                    Continue For
                End If

                Try
                    Dim p As PropertyDescriptor = TypeDescriptor.GetProperties(component)(pair.Key)
                    If p IsNot Nothing AndAlso
                       Not p.IsReadOnly AndAlso
                       p.Converter.CanConvertFrom(GetType(String)) Then
                        p.SetValue(component, p.Converter.ConvertFromInvariantString(pair.Value))
                    End If
                Catch
                End Try
            Next
        End Sub


        Private Shared Function EncodeToken(value As String) As String
            If value Is Nothing Then value = String.Empty
            Return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
        End Function

        Private Shared Function DecodeToken(value As String) As String
            Try
                Return Encoding.UTF8.GetString(Convert.FromBase64String(value))
            Catch
                Return String.Empty
            End Try
        End Function

        Private Shared Function EncodeStringList(values As IEnumerable(Of String)) As String
            If values Is Nothing Then Return String.Empty
            Return String.Join(";", values.Select(Function(v) EncodeToken(v)).ToArray())
        End Function

        Private Shared Function DecodeStringList(value As String) As String()
            If String.IsNullOrEmpty(value) Then Return New String() {}
            Return value.Split(";"c).Select(Function(v) DecodeToken(v)).ToArray()
        End Function

        Private Shared Sub ApplyStringItems(component As Object, encoded As String)
            Dim values As String() = DecodeStringList(encoded)

            If TypeOf component Is ComboBox Then
                Dim c As ComboBox = DirectCast(component, ComboBox)
                c.Items.Clear()
                c.Items.AddRange(values.Cast(Of Object)().ToArray())
            ElseIf TypeOf component Is CheckedListBox Then
                Dim c As CheckedListBox = DirectCast(component, CheckedListBox)
                c.Items.Clear()
                c.Items.AddRange(values.Cast(Of Object)().ToArray())
            ElseIf TypeOf component Is ListBox Then
                Dim c As ListBox = DirectCast(component, ListBox)
                c.Items.Clear()
                c.Items.AddRange(values.Cast(Of Object)().ToArray())
            End If
        End Sub

        Private Shared Function SerializeGridColumns(grid As DataGridView) As String
            Dim rows As New List(Of String)()

            For Each col As DataGridViewColumn In grid.Columns
                rows.Add(String.Join(",", New String() {
                    EncodeToken(col.GetType().Name),
                    EncodeToken(col.Name),
                    EncodeToken(col.HeaderText),
                    col.Width.ToString(CultureInfo.InvariantCulture),
                    col.Visible.ToString(CultureInfo.InvariantCulture)
                }))
            Next

            Return String.Join(";", rows.ToArray())
        End Function

        Private Shared Sub ApplyGridColumns(grid As DataGridView, encoded As String)
            grid.Columns.Clear()
            If String.IsNullOrWhiteSpace(encoded) Then Return

            For Each row As String In encoded.Split(";"c)
                Dim parts As String() = row.Split(","c)
                If parts.Length < 5 Then Continue For

                Dim typeName As String = DecodeToken(parts(0))
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

                col.Name = DecodeToken(parts(1))
                col.HeaderText = DecodeToken(parts(2))

                Dim width As Integer = 100
                Integer.TryParse(parts(3),
                                 NumberStyles.Integer,
                                 CultureInfo.InvariantCulture,
                                 width)
                col.Width = Math.Max(20, width)

                Dim visible As Boolean = True
                Boolean.TryParse(parts(4), visible)
                col.Visible = visible

                grid.Columns.Add(col)
            Next
        End Sub

        Private Shared Function SerializeTabPages(tabs As TabControl) As String
            Dim rows As New List(Of String)()

            For Each page As TabPage In tabs.TabPages
                rows.Add(EncodeToken(page.Name) & "," & EncodeToken(page.Text))
            Next

            Return String.Join(";", rows.ToArray())
        End Function

        Private Shared Sub ApplyTabPages(tabs As TabControl, encoded As String)
            tabs.TabPages.Clear()
            If String.IsNullOrWhiteSpace(encoded) Then Return

            For Each row As String In encoded.Split(";"c)
                Dim parts As String() = row.Split(","c)
                If parts.Length < 2 Then Continue For

                Dim page As New TabPage(DecodeToken(parts(1)))
                page.Name = DecodeToken(parts(0))
                tabs.TabPages.Add(page)
            Next
        End Sub

        Private Shared Function SerializeTreeNodes(tree As TreeView) As String
            Dim rows As New List(Of String)()
            SerializeTreeNodeCollection(tree.Nodes, 0, rows)
            Return String.Join(";", rows.ToArray())
        End Function

        Private Shared Sub SerializeTreeNodeCollection(nodes As TreeNodeCollection,
                                                        depth As Integer,
                                                        rows As List(Of String))
            For Each node As TreeNode In nodes
                rows.Add(depth.ToString(CultureInfo.InvariantCulture) & "," &
                         EncodeToken(node.Name) & "," &
                         EncodeToken(node.Text))
                SerializeTreeNodeCollection(node.Nodes, depth + 1, rows)
            Next
        End Sub

        Private Shared Sub ApplyTreeNodes(tree As TreeView, encoded As String)
            tree.Nodes.Clear()
            If String.IsNullOrWhiteSpace(encoded) Then Return

            Dim parents As New Dictionary(Of Integer, TreeNode)()

            For Each row As String In encoded.Split(";"c)
                Dim parts As String() = row.Split(","c)
                If parts.Length < 3 Then Continue For

                Dim depth As Integer = 0
                Integer.TryParse(parts(0),
                                 NumberStyles.Integer,
                                 CultureInfo.InvariantCulture,
                                 depth)

                Dim node As New TreeNode(DecodeToken(parts(2)))
                node.Name = DecodeToken(parts(1))

                If depth <= 0 OrElse Not parents.ContainsKey(depth - 1) Then
                    tree.Nodes.Add(node)
                Else
                    parents(depth - 1).Nodes.Add(node)
                End If

                parents(depth) = node
            Next
        End Sub

        Private Shared Function IsTransientDesignProperty(component As Object, propertyName As String) As Boolean
            If propertyName.Equals("Visible", StringComparison.OrdinalIgnoreCase) AndAlso (TypeOf component Is Control OrElse TypeOf component Is ToolStripItem) Then Return True
            If propertyName.Equals("Available", StringComparison.OrdinalIgnoreCase) AndAlso TypeOf component Is ToolStripItem Then Return True
            If propertyName.Equals("Selected", StringComparison.OrdinalIgnoreCase) AndAlso TypeOf component Is ToolStripItem Then Return True
            If propertyName.Equals("Pressed", StringComparison.OrdinalIgnoreCase) AndAlso TypeOf component Is ToolStripItem Then Return True
            Return False
        End Function
        Private Shared Function GetValue(values As Dictionary(Of String, String), key As String, fallback As String) As String
            Dim value As String = Nothing : If values IsNot Nothing AndAlso values.TryGetValue(key, value) Then Return value Else Return fallback
        End Function
        Private Shared Function ParseSize(value As String, fallback As Size) As Size
            Try
                Return DirectCast(TypeDescriptor.GetConverter(GetType(Size)).ConvertFromInvariantString(value), Size)
            Catch
                Return fallback
            End Try
        End Function
        Private Shared Function ParseColor(value As String, fallback As Color) As Color
            Try
                Return DirectCast(TypeDescriptor.GetConverter(GetType(Color)).ConvertFromInvariantString(value), Color)
            Catch
                Return fallback
            End Try
        End Function
        Private Shared Function ResizeCursor(edge As String) As Cursor
            If edge = "N" OrElse edge = "S" Then Return Cursors.SizeNS
            If edge = "E" OrElse edge = "W" Then Return Cursors.SizeWE
            If edge = "NW" OrElse edge = "SE" Then Return Cursors.SizeNWSE
            Return Cursors.SizeNESW
        End Function
    End Class

    Friend Class ComponentTile
        Inherits Label
        Public ReadOnly Property ComponentInstance As Component
        Public ReadOnly Property ComponentTypeName As String
        Public ReadOnly Property AssemblyPath As String
        Public Sub New(typeName As String, assemblyFile As String, instance As Component)
            ComponentTypeName = typeName : AssemblyPath = assemblyFile : ComponentInstance = instance
            AutoSize = False : Size = New Size(150, 42) : BorderStyle = BorderStyle.FixedSingle
            BackColor = Color.FromArgb(55, 62, 74) : ForeColor = Color.White
            TextAlign = ContentAlignment.MiddleCenter : Text = "⚙ " & typeName.Split("."c).Last()
        End Sub
        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing AndAlso ComponentInstance IsNot Nothing Then ComponentInstance.Dispose()
            MyBase.Dispose(disposing)
        End Sub
    End Class
End Namespace
