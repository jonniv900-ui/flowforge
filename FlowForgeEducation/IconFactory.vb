Imports System
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace FlowForgeStudio
    Friend NotInheritable Class IconFactory
        Private Sub New()
        End Sub

        Public Shared Function Create(name As String, Optional size As Integer = 16) As Bitmap
            Dim image As New Bitmap(size, size)
            Using g As Graphics = Graphics.FromImage(image)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.Clear(Color.Transparent)
                Dim blue As Color = Color.FromArgb(70, 150, 235)
                Dim green As Color = Color.FromArgb(65, 180, 120)
                Dim yellow As Color = Color.FromArgb(238, 190, 70)
                Dim red As Color = Color.FromArgb(225, 90, 90)
                Dim light As Color = Color.FromArgb(220, 225, 232)
                Using pen As New Pen(light, Math.Max(1.4F, size / 11.0F))
                    pen.LineJoin = LineJoin.Round
                    Select Case name.ToLowerInvariant()
                        Case "new"
                            g.DrawRectangle(pen, 3, 2, 9, 12) : g.DrawLine(pen, 8, 2, 12, 6)
                            Using p As New Pen(green, 2) : g.DrawLine(p, 2, 11, 7, 11) : g.DrawLine(p, 4.5F, 8.5F, 4.5F, 13.5F) : End Using
                        Case "open"
                            Using b As New SolidBrush(yellow) : g.FillPolygon(b, {New Point(1, 6), New Point(6, 6), New Point(8, 4), New Point(15, 4), New Point(12, 13), New Point(2, 13)}) : End Using
                        Case "save"
                            Using b As New SolidBrush(blue) : g.FillRectangle(b, 2, 2, 12, 12) : End Using
                            g.FillRectangle(Brushes.White, 5, 3, 6, 4) : g.FillRectangle(Brushes.White, 5, 10, 6, 4)
                        Case "undo", "redo"
                            Using p As New Pen(blue, 2) : g.DrawArc(p, 3, 4, 10, 8, If(name = "undo", 175, -5), If(name = "undo", 230, -230)) : End Using
                            Dim pts() As Point
                            If name = "undo" Then
                                pts = {New Point(2, 6), New Point(7, 3), New Point(7, 9)}
                            Else
                                pts = {New Point(14, 6), New Point(9, 3), New Point(9, 9)}
                            End If
                            Using b As New SolidBrush(blue) : g.FillPolygon(b, pts) : End Using
                        Case "cut"
                            g.DrawEllipse(pen, 1, 2, 4, 4) : g.DrawEllipse(pen, 1, 10, 4, 4) : g.DrawLine(pen, 5, 5, 14, 12) : g.DrawLine(pen, 5, 11, 14, 4)
                        Case "copy"
                            g.DrawRectangle(pen, 5, 3, 8, 10) : g.DrawRectangle(pen, 2, 6, 8, 8)
                        Case "paste"
                            Using b As New SolidBrush(yellow) : g.FillRectangle(b, 3, 3, 10, 11) : End Using : g.DrawRectangle(pen, 5, 1, 6, 4)
                        Case "delete"
                            Using b As New SolidBrush(red) : g.FillRectangle(b, 4, 5, 8, 9) : g.FillRectangle(b, 2, 3, 12, 2) : End Using
                        Case "form"
                            Using b As New SolidBrush(blue) : g.FillRectangle(b, 1, 2, 14, 12) : End Using : g.FillRectangle(Brushes.White, 3, 5, 10, 7)
                        Case "code"
                            g.DrawLines(pen, {New Point(6, 3), New Point(2, 8), New Point(6, 13)}) : g.DrawLines(pen, {New Point(10, 3), New Point(14, 8), New Point(10, 13)})
                        Case "properties"
                            For y As Integer = 3 To 12 Step 4 : g.DrawEllipse(pen, 2, y, 2, 2) : g.DrawLine(pen, 7, y + 1, 14, y + 1) : Next
                        Case "toolbox"
                            Using b As New SolidBrush(yellow) : g.FillRectangle(b, 1, 5, 14, 9) : End Using : g.DrawRectangle(pen, 5, 2, 6, 4)
                        Case "build"
                            Using p As New Pen(yellow, 2.5F) : g.DrawLine(p, 3, 13, 11, 5) : End Using : Using b As New SolidBrush(light) : g.FillPolygon(b, {New Point(8, 2), New Point(14, 2), New Point(14, 8)}) : End Using
                        Case "run"
                            Using b As New SolidBrush(green) : g.FillPolygon(b, {New Point(4, 2), New Point(14, 8), New Point(4, 14)}) : End Using
                        Case "grid"
                            Using p As New Pen(blue, 1) : For i As Integer = 2 To 14 Step 4 : g.DrawLine(p, i, 1, i, 15) : g.DrawLine(p, 1, i, 15, i) : Next : End Using
                        Case "snap"
                            Using p As New Pen(red, 2) : g.DrawArc(p, 2, 2, 11, 12, 40, 280) : End Using : Using b As New SolidBrush(red) : g.FillPolygon(b, {New Point(11, 1), New Point(15, 2), New Point(13, 6)}) : End Using
                        Case "help"
                            Using b As New SolidBrush(blue) : g.FillEllipse(b, 1, 1, 14, 14) : End Using : Using f As New Font("Segoe UI", 10, FontStyle.Bold) : TextRenderer.DrawText(g, "?", f, New Rectangle(1, 0, 14, 15), Color.White, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter) : End Using
                        Case Else
                            Using b As New SolidBrush(blue) : g.FillEllipse(b, 3, 3, 10, 10) : End Using
                    End Select
                End Using
            End Using
            Return image
        End Function
    End Class
End Namespace
