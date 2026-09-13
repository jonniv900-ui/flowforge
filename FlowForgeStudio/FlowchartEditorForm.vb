Imports System
Imports System.Collections.Generic
Imports System.Text
Imports System.Windows.Forms

Namespace FlowForgeStudio
    Public Class FlowchartEditorForm
        Inherits Form
        Private ReadOnly steps As New ListBox()
        Private ReadOnly kind As New ComboBox()
        Private ReadOnly expression As New TextBox()
        Private ReadOnly preview As New TextBox()
        Public ReadOnly Property GeneratedCode As String
            Get
                Return BuildCode()
            End Get
        End Property
        Public Sub New()
            Text = "Fluxograma → VB.NET"
            Width = 820 : Height = 530 : StartPosition = FormStartPosition.CenterParent
            steps.Dock = DockStyle.Left : steps.Width = 360
            kind.Items.AddRange(New Object() {"Atribuição", "Mensagem", "If", "Else", "End If", "For", "Next", "While", "End While", "Comentário"}) : kind.SelectedIndex = 0 : kind.Width = 120
            expression.Width = 300
            Dim top As New FlowLayoutPanel With {.Dock = DockStyle.Top, .Height = 45, .Padding = New Padding(6)}
            Dim addButton As New Button With {.Text = "Adicionar", .AutoSize = True}
            Dim removeButton As New Button With {.Text = "Remover", .AutoSize = True}
            Dim upButton As New Button With {.Text = "↑", .Width = 35}
            Dim downButton As New Button With {.Text = "↓", .Width = 35}
            AddHandler addButton.Click, AddressOf AddStep
            AddHandler removeButton.Click, Sub() If steps.SelectedIndex >= 0 Then steps.Items.RemoveAt(steps.SelectedIndex) : preview.Text = BuildCode()
            AddHandler upButton.Click, Sub() MoveSelected(-1)
            AddHandler downButton.Click, Sub() MoveSelected(1)
            top.Controls.AddRange(New Control() {kind, expression, addButton, removeButton, upButton, downButton})
            preview.Dock = DockStyle.Fill : preview.Multiline = True : preview.ScrollBars = ScrollBars.Both : preview.ReadOnly = True : preview.Font = New Drawing.Font("Consolas", 10.0F)
            Dim buttons As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .Height = 44, .FlowDirection = FlowDirection.RightToLeft, .Padding = New Padding(6)}
            Dim ok As New Button With {.Text = "Criar módulo", .DialogResult = DialogResult.OK, .Width = 110}
            Dim cancel As New Button With {.Text = "Cancelar", .DialogResult = DialogResult.Cancel, .Width = 90}
            buttons.Controls.Add(ok) : buttons.Controls.Add(cancel)
            AddHandler steps.SelectedIndexChanged, Sub() preview.Text = BuildCode()
            AddHandler steps.SelectedValueChanged, Sub() preview.Text = BuildCode()
            AddHandler steps.ControlAdded, Sub() preview.Text = BuildCode()
            Controls.Add(preview) : Controls.Add(steps) : Controls.Add(top) : Controls.Add(buttons)
            AcceptButton = ok : CancelButton = cancel
            AddHandler Activated, Sub() preview.Text = BuildCode()
        End Sub
        Private Sub AddStep(sender As Object, e As EventArgs)
            steps.Items.Add(New FlowStep(CStr(kind.SelectedItem), expression.Text))
            preview.Text = BuildCode()
            expression.SelectAll() : expression.Focus()
            Refresh()
        End Sub
        Private Sub MoveSelected(delta As Integer)
            Dim i As Integer = steps.SelectedIndex : If i < 0 Then Return
            Dim target As Integer = i + delta : If target < 0 OrElse target >= steps.Items.Count Then Return
            Dim value As Object = steps.Items(i) : steps.Items.RemoveAt(i) : steps.Items.Insert(target, value) : steps.SelectedIndex = target : preview.Text = BuildCode()
        End Sub
        Private Function BuildCode() As String
            Dim b As New StringBuilder()
            b.AppendLine("Imports System").AppendLine("Imports System.Windows.Forms").AppendLine().AppendLine("Public Module Fluxograma1").AppendLine("    Public Sub Executar()")
            Dim indent As Integer = 2
            For Each item As FlowStep In steps.Items
                Select Case item.Kind
                    Case "Else", "End If", "Next", "End While" : indent = Math.Max(2, indent - 1)
                End Select
                Dim pad As String = New String(" "c, indent * 4)
                Select Case item.Kind
                    Case "Atribuição" : b.AppendLine(pad & item.Text)
                    Case "Mensagem" : b.AppendLine(pad & "MessageBox.Show(" & item.Text & ")")
                    Case "If" : b.AppendLine(pad & "If " & item.Text & " Then") : indent += 1
                    Case "Else" : b.AppendLine(pad & "Else") : indent += 1
                    Case "End If" : b.AppendLine(pad & "End If")
                    Case "For" : b.AppendLine(pad & "For " & item.Text) : indent += 1
                    Case "Next" : b.AppendLine(pad & "Next")
                    Case "While" : b.AppendLine(pad & "While " & item.Text) : indent += 1
                    Case "End While" : b.AppendLine(pad & "End While")
                    Case "Comentário" : b.AppendLine(pad & "' " & item.Text)
                End Select
            Next
            b.AppendLine("    End Sub").AppendLine("End Module")
            Return b.ToString()
        End Function
        Private Class FlowStep
            Public ReadOnly Kind As String
            Public ReadOnly Text As String
            Public Sub New(stepKind As String, stepText As String)
                Kind = stepKind : Text = stepText
            End Sub
            Public Overrides Function ToString() As String
                Return Kind & If(String.IsNullOrWhiteSpace(Text), "", ": " & Text)
            End Function
        End Class
    End Class
End Namespace
