Imports System
Imports System.ComponentModel
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

Namespace FlowForgeStudio
    Public Class ColorComboBox
        Inherits ComboBox
        Public Event SelectedColorChanged As EventHandler

        Public Sub New()
            DrawMode = DrawMode.OwnerDrawFixed
            DropDownStyle = ComboBoxStyle.DropDownList
            ItemHeight = 20
            Items.AddRange(New Object() {"Transparent", "Black", "White", "Gray", "Silver", "Red", "DarkRed", "Orange", "Gold", "Yellow", "Green", "Lime", "Teal", "Cyan", "Blue", "Navy", "Purple", "Magenta", "Brown", "Pink"})
            SelectedItem = "Black"
        End Sub

        <Category("Aparência"), Description("Cor atualmente selecionada.")>
        Public Property SelectedColor As Color
            Get
                If SelectedItem Is Nothing Then Return Color.Empty
                Return Color.FromName(CStr(SelectedItem))
            End Get
            Set(value As Color)
                Dim name As String = If(value.IsNamedColor, value.Name, ColorTranslator.ToHtml(value))
                If Not Items.Cast(Of Object)().Any(Function(item) String.Equals(CStr(item), name, StringComparison.OrdinalIgnoreCase)) Then Items.Add(name)
                SelectedItem = Items.Cast(Of Object)().FirstOrDefault(Function(item) String.Equals(CStr(item), name, StringComparison.OrdinalIgnoreCase))
            End Set
        End Property

        Protected Overrides Sub OnSelectedIndexChanged(e As EventArgs)
            MyBase.OnSelectedIndexChanged(e)
            RaiseEvent SelectedColorChanged(Me, e)
        End Sub

        Protected Overrides Sub OnDrawItem(e As DrawItemEventArgs)
            e.DrawBackground()
            If e.Index >= 0 Then
                Dim name As String = CStr(Items(e.Index))
                Dim colorValue As Color
                Try : colorValue = ColorTranslator.FromHtml(name) : Catch : colorValue = Color.FromName(name) : End Try
                Dim box As New Rectangle(e.Bounds.X + 3, e.Bounds.Y + 3, 28, Math.Max(8, e.Bounds.Height - 6))
                Using brush As New SolidBrush(colorValue) : e.Graphics.FillRectangle(brush, box) : End Using
                e.Graphics.DrawRectangle(Pens.DimGray, box)
                TextRenderer.DrawText(e.Graphics, name, e.Font, New Rectangle(e.Bounds.X + 38, e.Bounds.Y, e.Bounds.Width - 40, e.Bounds.Height), e.ForeColor, TextFormatFlags.VerticalCenter Or TextFormatFlags.Left)
            End If
            e.DrawFocusRectangle()
        End Sub
    End Class
End Namespace
