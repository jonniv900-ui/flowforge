Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports Microsoft.VisualBasic

Namespace FlowForgeStudio
    Public Enum DigitalDisplayStyle
        SevenSegment
        DotMatrix
        Text
    End Enum

    Public Class RoundedButton
        Inherits Button
        Private _cornerRadius As Integer = 14
        Private _borderColor As Color = Color.RoyalBlue
        Private _hoverColor As Color = Color.CornflowerBlue
        Private _mouseOver As Boolean
        Public Sub New()
            FlatStyle = FlatStyle.Flat : FlatAppearance.BorderSize = 0 : BackColor = Color.RoyalBlue : ForeColor = Color.White
        End Sub
        <Category("Aparência")> Public Property CornerRadius As Integer
            Get
                Return _cornerRadius
            End Get
            Set(value As Integer)
                _cornerRadius = Math.Max(0, Math.Min(60, value)) : Invalidate()
            End Set
        End Property
        <Category("Aparência")> Public Property BorderColor As Color
            Get
                Return _borderColor
            End Get
            Set(value As Color)
                _borderColor = value : Invalidate()
            End Set
        End Property
        <Category("Aparência")> Public Property HoverColor As Color
            Get
                Return _hoverColor
            End Get
            Set(value As Color)
                _hoverColor = value
                Invalidate()
            End Set
        End Property
        Protected Overrides Sub OnMouseEnter(e As EventArgs)
            _mouseOver = True
            Invalidate()
            MyBase.OnMouseEnter(e)
        End Sub
        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            _mouseOver = False
            Invalidate()
            MyBase.OnMouseLeave(e)
        End Sub
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            Using path As GraphicsPath = RoundedPath(ClientRectangle, _cornerRadius), fill As New SolidBrush(If(_mouseOver, _hoverColor, BackColor)), border As New Pen(_borderColor)
                e.Graphics.FillPath(fill, path) : e.Graphics.DrawPath(border, path)
            End Using
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
        End Sub
        Private Shared Function RoundedPath(rectangle As Rectangle, radius As Integer) As GraphicsPath
            Dim path As New GraphicsPath()
            Dim diameter As Integer = Math.Max(1, radius * 2)
            Dim area As New Rectangle(rectangle.X, rectangle.Y, diameter, diameter)
            path.AddArc(area, 180, 90) : area.X = rectangle.Right - diameter - 1 : path.AddArc(area, 270, 90)
            area.Y = rectangle.Bottom - diameter - 1 : path.AddArc(area, 0, 90) : area.X = rectangle.X : path.AddArc(area, 90, 90) : path.CloseFigure()
            Return path
        End Function
    End Class

    Public Class GradientPanel
        Inherits Panel
        Private _startColor As Color = Color.RoyalBlue
        Private _endColor As Color = Color.MediumPurple
        Private _angle As Single
        <Category("Aparência")> Public Property StartColor As Color
            Get
                Return _startColor
            End Get
            Set(value As Color)
                _startColor = value : Invalidate()
            End Set
        End Property
        Public Sub SwapColors()
            Dim temporary As Color = _startColor
            _startColor = _endColor
            _endColor = temporary
            Invalidate()
        End Sub
        <Category("Aparência")> Public Property EndColor As Color
            Get
                Return _endColor
            End Get
            Set(value As Color)
                _endColor = value : Invalidate()
            End Set
        End Property
        <Category("Aparência")> Public Property GradientAngle As Single
            Get
                Return _angle
            End Get
            Set(value As Single)
                _angle = value : Invalidate()
            End Set
        End Property
        Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
            If ClientRectangle.Width <= 0 OrElse ClientRectangle.Height <= 0 Then Return
            Using brush As New LinearGradientBrush(ClientRectangle, _startColor, _endColor, _angle) : e.Graphics.FillRectangle(brush, ClientRectangle) : End Using
        End Sub
    End Class

    <DefaultEvent("StateChanged")> Public Class LedIndicator
        Inherits Control
        Private _isOn As Boolean
        Private _onColor As Color = Color.LimeGreen
        Private _offColor As Color = Color.FromArgb(70, 70, 70)
        Public Event StateChanged As EventHandler
        Public Sub New()
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True) : Size = New Size(36, 36)
        End Sub
        <Category("Comportamento")> Public Property IsOn As Boolean
            Get
                Return _isOn
            End Get
            Set(value As Boolean)
                If _isOn = value Then Return
                _isOn = value : Invalidate() : RaiseEvent StateChanged(Me, EventArgs.Empty)
            End Set
        End Property
        Public Sub Toggle()
            IsOn = Not IsOn
        End Sub
        <Category("Aparência")> Public Property OnColor As Color
            Get
                Return _onColor
            End Get
            Set(value As Color)
                _onColor = value : Invalidate()
            End Set
        End Property
        <Category("Aparência")> Public Property OffColor As Color
            Get
                Return _offColor
            End Get
            Set(value As Color)
                _offColor = value : Invalidate()
            End Set
        End Property
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias : Dim bounds As Rectangle = Rectangle.Inflate(ClientRectangle, -3, -3)
            Using brush As New SolidBrush(If(_isOn, _onColor, _offColor)) : e.Graphics.FillEllipse(brush, bounds) : End Using
            e.Graphics.DrawEllipse(Pens.DimGray, bounds)
        End Sub
    End Class

    <DefaultEvent("CheckedChanged")> Public Class ToggleSwitch
        Inherits CheckBox
        Public Sub New()
            Appearance = Appearance.Button : AutoSize = False : Size = New Size(58, 28) : Text = "" : SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        End Sub
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias : e.Graphics.Clear(If(Parent Is Nothing, SystemColors.Control, Parent.BackColor))
            Dim track As New Rectangle(1, 3, Width - 3, Height - 7)
            Using brush As New SolidBrush(If(Checked, Color.SeaGreen, Color.Gray))
                e.Graphics.FillEllipse(brush, track.X, track.Y, track.Height, track.Height) : e.Graphics.FillEllipse(brush, track.Right - track.Height, track.Y, track.Height, track.Height) : e.Graphics.FillRectangle(brush, track.X + track.Height \ 2, track.Y, track.Width - track.Height, track.Height)
            End Using
            Dim diameter As Integer = track.Height - 6, knobX As Integer = If(Checked, track.Right - diameter - 3, track.X + 3)
            Using knob As New SolidBrush(Color.White) : e.Graphics.FillEllipse(knob, knobX, track.Y + 3, diameter, diameter) : End Using
        End Sub
    End Class

    <DefaultEvent("ValueChanged")> Public Class DigitalDisplay
        Inherits Control
        Private _value As String = "0"
        Private _digits As Integer = 8
        Private _displayStyle As DigitalDisplayStyle = DigitalDisplayStyle.SevenSegment
        Private _inactiveColor As Color = Color.FromArgb(25, 65, 25)
        Public Event ValueChanged As EventHandler
        Public Sub New()
            BackColor = Color.Black : ForeColor = Color.Lime : Font = New Font("Consolas", 24.0F, FontStyle.Bold) : Size = New Size(260, 55) : SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        End Sub
        <Category("Dados")> Public Property Value As String
            Get
                Return _value
            End Get
            Set(value As String)
                Dim newValue As String = If(value, "")
                If _value = newValue Then Return
                _value = newValue
                Invalidate()
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End Set
        End Property
        Public Sub ClearDisplay()
            Value = ""
        End Sub
        Public Sub AppendValue(text As String)
            Value &= If(text, "")
        End Sub
        <Category("Dados")> Public Property Digits As Integer
            Get
                Return _digits
            End Get
            Set(value As Integer)
                _digits = Math.Max(1, Math.Min(32, value)) : Invalidate()
            End Set
        End Property
        <Category("Aparência")> Public Property DisplayStyle As DigitalDisplayStyle
            Get
                Return _displayStyle
            End Get
            Set(value As DigitalDisplayStyle)
                _displayStyle = value
                Invalidate()
            End Set
        End Property
        <Category("Aparência")> Public Property InactiveColor As Color
            Get
                Return _inactiveColor
            End Get
            Set(value As Color)
                _inactiveColor = value
                Invalidate()
            End Set
        End Property
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.Clear(BackColor)
            Dim shown As String = If(_value.Length > _digits, _value.Substring(_value.Length - _digits), _value.PadLeft(_digits))
            If _displayStyle = DigitalDisplayStyle.SevenSegment Then
                PaintSevenSegment(e.Graphics, shown)
            ElseIf _displayStyle = DigitalDisplayStyle.DotMatrix Then
                PaintDotMatrix(e.Graphics, shown)
            Else
                TextRenderer.DrawText(e.Graphics, shown, Font, ClientRectangle, ForeColor, TextFormatFlags.Right Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)
            End If
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, Color.DimGray, ButtonBorderStyle.Solid)
        End Sub
        Private Sub PaintSevenSegment(graphics As Graphics, shown As String)
            graphics.SmoothingMode = SmoothingMode.AntiAlias
            Dim cellWidth As Single = CSng(Math.Max(12, (ClientSize.Width - 8) / Math.Max(1, _digits)))
            Dim startX As Single = ClientSize.Width - 4 - shown.Length * cellWidth
            For index As Integer = 0 To shown.Length - 1
                DrawDigit(graphics, shown(index), New RectangleF(startX + index * cellWidth, 5, cellWidth - 2, ClientSize.Height - 10))
            Next
        End Sub
        Private Sub DrawDigit(graphics As Graphics, character As Char, bounds As RectangleF)
            Dim mask As Integer = SegmentMask(character)
            Dim thickness As Single = Math.Max(2.0F, Math.Min(bounds.Width, bounds.Height) / 8.0F)
            Dim half As Single = bounds.Height / 2.0F
            Dim segments As RectangleF() = {New RectangleF(bounds.X + thickness, bounds.Y, bounds.Width - thickness * 2, thickness), New RectangleF(bounds.Right - thickness, bounds.Y + thickness, thickness, half - thickness * 1.5F), New RectangleF(bounds.Right - thickness, bounds.Y + half + thickness / 2, thickness, half - thickness * 1.5F), New RectangleF(bounds.X + thickness, bounds.Bottom - thickness, bounds.Width - thickness * 2, thickness), New RectangleF(bounds.X, bounds.Y + half + thickness / 2, thickness, half - thickness * 1.5F), New RectangleF(bounds.X, bounds.Y + thickness, thickness, half - thickness * 1.5F), New RectangleF(bounds.X + thickness, bounds.Y + half - thickness / 2, bounds.Width - thickness * 2, thickness)}
            For segment As Integer = 0 To 6
                Using brush As New SolidBrush(If((mask And (1 << segment)) <> 0, ForeColor, _inactiveColor))
                    graphics.FillRectangle(brush, segments(segment))
                End Using
            Next
            If character = "."c OrElse character = ","c Then
                Using brush As New SolidBrush(ForeColor)
                    graphics.FillEllipse(brush, bounds.Right - thickness, bounds.Bottom - thickness, thickness, thickness)
                End Using
            End If
        End Sub
        Private Shared Function SegmentMask(character As Char) As Integer
            Select Case Char.ToUpperInvariant(character)
                Case "0"c : Return 63
                Case "1"c : Return 6
                Case "2"c : Return 91
                Case "3"c : Return 79
                Case "4"c : Return 102
                Case "5"c, "S"c : Return 109
                Case "6"c : Return 125
                Case "7"c : Return 7
                Case "8"c : Return 127
                Case "9"c : Return 111
                Case "A"c : Return 119
                Case "B"c : Return 124
                Case "C"c : Return 57
                Case "D"c : Return 94
                Case "E"c : Return 121
                Case "F"c : Return 113
                Case "H"c : Return 118
                Case "L"c : Return 56
                Case "P"c : Return 115
                Case "U"c : Return 62
                Case "-"c : Return 64
                Case "_"c : Return 8
                Case Else : Return 0
            End Select
        End Function
        Private Sub PaintDotMatrix(graphics As Graphics, shown As String)
            Dim cellWidth As Single = CSng(Math.Max(12, (ClientSize.Width - 8) / Math.Max(1, _digits)))
            Dim dotSize As Single = Math.Max(1.0F, Math.Min(cellWidth / 6.2F, (ClientSize.Height - 10) / 8.0F))
            Dim startX As Single = ClientSize.Width - 4 - shown.Length * cellWidth
            Using bitmap As New Bitmap(7, 9)
                Using tinyGraphics As Graphics = Graphics.FromImage(bitmap), activeBrush As New SolidBrush(ForeColor), inactiveBrush As New SolidBrush(_inactiveColor)
                    For characterIndex As Integer = 0 To shown.Length - 1
                        tinyGraphics.Clear(Color.Black)
                        Using tinyFont As New Font(FontFamily.GenericMonospace, 8.0F, FontStyle.Bold, GraphicsUnit.Pixel)
                            TextRenderer.DrawText(tinyGraphics, shown(characterIndex).ToString(), tinyFont, New Rectangle(-1, -1, 9, 11), Color.White, Color.Black, TextFormatFlags.NoPadding)
                        End Using
                        For row As Integer = 0 To 6
                            For column As Integer = 0 To 4
                                Dim active As Boolean = bitmap.GetPixel(column + 1, row + 1).R > 70
                                graphics.FillEllipse(If(active, activeBrush, inactiveBrush), startX + characterIndex * cellWidth + column * dotSize * 1.2F, 5 + row * dotSize * 1.15F, dotSize, dotSize)
                            Next
                        Next
                    Next
                End Using
            End Using
        End Sub
    End Class

    <DefaultEvent("ValueChanged")> Public Class CircularProgress
        Inherits Control
        Private _value As Integer
        Private _maximum As Integer = 100
        Private _progressColor As Color = Color.DodgerBlue
        Public Event ValueChanged As EventHandler
        Public Sub New()
            Size = New Size(100, 100)
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        End Sub
        <Category("Dados")> Public Property Value As Integer
            Get
                Return _value
            End Get
            Set(newValue As Integer)
                Dim limited As Integer = Math.Max(0, Math.Min(_maximum, newValue))
                If limited = _value Then Return
                _value = limited
                Invalidate()
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End Set
        End Property
        <Category("Dados")> Public Property Maximum As Integer
            Get
                Return _maximum
            End Get
            Set(newValue As Integer)
                _maximum = Math.Max(1, newValue)
                If _value > _maximum Then _value = _maximum
                Invalidate()
            End Set
        End Property
        <Category("Aparência")> Public Property ProgressColor As Color
            Get
                Return _progressColor
            End Get
            Set(newValue As Color)
                _progressColor = newValue
                Invalidate()
            End Set
        End Property
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            Dim bounds As Rectangle = Rectangle.Inflate(ClientRectangle, -8, -8)
            Using basePen As New Pen(Color.Gainsboro, 8), valuePen As New Pen(_progressColor, 8)
                basePen.StartCap = LineCap.Round
                basePen.EndCap = LineCap.Round
                valuePen.StartCap = LineCap.Round
                valuePen.EndCap = LineCap.Round
                e.Graphics.DrawArc(basePen, bounds, -90, 360)
                e.Graphics.DrawArc(valuePen, bounds, -90, CSng(360.0 * _value / _maximum))
            End Using
            TextRenderer.DrawText(e.Graphics, CInt(100.0 * _value / _maximum).ToString() & "%", Font, ClientRectangle, ForeColor, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
        End Sub
    End Class

    <DefaultEvent("ValueChanged")> Public Class LevelMeter
        Inherits Control
        Private _value As Integer
        Private _maximum As Integer = 100
        Public Event ValueChanged As EventHandler
        Public Sub New()
            Size = New Size(38, 160)
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        End Sub
        <Category("Dados")> Public Property Value As Integer
            Get
                Return _value
            End Get
            Set(newValue As Integer)
                Dim limited As Integer = Math.Max(0, Math.Min(_maximum, newValue))
                If limited = _value Then Return
                _value = limited
                Invalidate()
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End Set
        End Property
        <Category("Dados")> Public Property Maximum As Integer
            Get
                Return _maximum
            End Get
            Set(newValue As Integer)
                _maximum = Math.Max(1, newValue)
                If _value > _maximum Then _value = _maximum
                Invalidate()
            End Set
        End Property
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.Clear(BackColor)
            Dim bars As Integer = 10
            Dim activeBars As Integer = CInt(Math.Ceiling(bars * _value / CDbl(_maximum)))
            For index As Integer = 0 To bars - 1
                Dim y As Integer = Height - (index + 1) * Height \ bars + 2
                Dim barColor As Color = If(index < activeBars, If(index >= 8, Color.Red, If(index >= 6, Color.Gold, Color.LimeGreen)), Color.FromArgb(55, 55, 55))
                Using brush As New SolidBrush(barColor)
                    e.Graphics.FillRectangle(brush, 3, y, Width - 6, Math.Max(2, Height \ bars - 3))
                End Using
            Next
        End Sub
    End Class

    Public Class BadgeLabel
        Inherits Label
        Private _badgeColor As Color = Color.Crimson
        Public Sub New()
            AutoSize = False
            Size = New Size(100, 30)
            ForeColor = Color.White
            TextAlign = ContentAlignment.MiddleCenter
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        End Sub
        <Category("Aparência")> Public Property BadgeColor As Color
            Get
                Return _badgeColor
            End Get
            Set(value As Color)
                _badgeColor = value
                Invalidate()
            End Set
        End Property
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            Using brush As New SolidBrush(_badgeColor)
                e.Graphics.FillEllipse(brush, 0, 0, Height, Height)
                e.Graphics.FillEllipse(brush, Width - Height, 0, Height, Height)
                e.Graphics.FillRectangle(brush, Height \ 2, 0, Math.Max(0, Width - Height), Height)
            End Using
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
        End Sub
    End Class

    Public Class SeparatorLine
        Inherits Control
        Private _lineColor As Color = Color.Silver
        Private _thickness As Integer = 1
        Public Sub New()
            Size = New Size(180, 4)
        End Sub
        <Category("Aparência")> Public Property LineColor As Color
            Get
                Return _lineColor
            End Get
            Set(value As Color)
                _lineColor = value
                Invalidate()
            End Set
        End Property
        <Category("Aparência")> Public Property Thickness As Integer
            Get
                Return _thickness
            End Get
            Set(value As Integer)
                _thickness = Math.Max(1, Math.Min(10, value))
                Invalidate()
            End Set
        End Property
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Using pen As New Pen(_lineColor, _thickness)
                e.Graphics.DrawLine(pen, 0, Height \ 2, Width, Height \ 2)
            End Using
        End Sub
    End Class

    <DefaultEvent("RatingChanged")> Public Class StarRating
        Inherits Control
        Private _rating As Integer = 3
        Private _maximum As Integer = 5
        Public Event RatingChanged As EventHandler
        Public Sub New()
            Font = New Font("Segoe UI Symbol", 16.0F)
            Size = New Size(160, 36)
            Cursor = Cursors.Hand
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        End Sub
        <Category("Dados")> Public Property Rating As Integer
            Get
                Return _rating
            End Get
            Set(value As Integer)
                Dim limited As Integer = Math.Max(0, Math.Min(_maximum, value))
                If limited = _rating Then Return
                _rating = limited
                Invalidate()
                RaiseEvent RatingChanged(Me, EventArgs.Empty)
            End Set
        End Property
        <Category("Dados")> Public Property Maximum As Integer
            Get
                Return _maximum
            End Get
            Set(value As Integer)
                _maximum = Math.Max(1, Math.Min(10, value))
                If _rating > _maximum Then _rating = _maximum
                Invalidate()
            End Set
        End Property
        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            Rating = Math.Min(_maximum, Math.Max(1, CInt(Math.Ceiling(e.X / CDbl(Math.Max(1, Width)) * _maximum))))
            MyBase.OnMouseDown(e)
        End Sub
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim cellWidth As Integer = Math.Max(1, Width \ _maximum)
            For index As Integer = 0 To _maximum - 1
                TextRenderer.DrawText(e.Graphics, If(index < _rating, "★", "☆"), Font, New Rectangle(index * cellWidth, 0, cellWidth, Height), If(index < _rating, Color.Goldenrod, Color.Gray), TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
            Next
        End Sub
    End Class

    <DefaultEvent("ValueChanged")> Public Class NumericKnob
        Inherits Control
        Private _value As Integer
        Private _maximum As Integer = 100
        Public Event ValueChanged As EventHandler
        Public Sub New()
            Size = New Size(90, 90)
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        End Sub
        <Category("Dados")> Public Property Value As Integer
            Get
                Return _value
            End Get
            Set(newValue As Integer)
                Dim limited As Integer = Math.Max(0, Math.Min(_maximum, newValue))
                If limited = _value Then Return
                _value = limited
                Invalidate()
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End Set
        End Property
        <Category("Dados")> Public Property Maximum As Integer
            Get
                Return _maximum
            End Get
            Set(newValue As Integer)
                _maximum = Math.Max(1, newValue)
                If _value > _maximum Then _value = _maximum
                Invalidate()
            End Set
        End Property
        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            UpdateFromMouse(e.Y)
            MyBase.OnMouseDown(e)
        End Sub
        Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
            If e.Button = MouseButtons.Left Then UpdateFromMouse(e.Y)
            MyBase.OnMouseMove(e)
        End Sub
        Private Sub UpdateFromMouse(mouseY As Integer)
            Value = CInt((_maximum * (Height - Math.Max(0, Math.Min(Height, mouseY)))) / CDbl(Math.Max(1, Height)))
        End Sub
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            Dim bounds As Rectangle = Rectangle.Inflate(ClientRectangle, -8, -8)
            Using brush As New LinearGradientBrush(bounds, Color.WhiteSmoke, Color.DimGray, 45.0F)
                e.Graphics.FillEllipse(brush, bounds)
            End Using
            e.Graphics.DrawEllipse(Pens.Gray, bounds)
            Dim angle As Double = (-135.0 + 270.0 * _value / _maximum) * Math.PI / 180.0
            Dim center As New PointF(Width / 2.0F, Height / 2.0F)
            Dim radius As Single = Math.Min(bounds.Width, bounds.Height) * 0.34F
            Using pen As New Pen(Color.RoyalBlue, 4)
                pen.StartCap = LineCap.Round
                pen.EndCap = LineCap.Round
                e.Graphics.DrawLine(pen, center, New PointF(center.X + CSng(Math.Cos(angle)) * radius, center.Y + CSng(Math.Sin(angle)) * radius))
            End Using
        End Sub
    End Class

    Public Class CardPanel
        Inherits Panel
        Private _borderColor As Color = Color.Gainsboro
        Public Sub New()
            BackColor = Color.White
            Padding = New Padding(12)
            Size = New Size(220, 140)
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        End Sub
        <Category("Aparência")> Public Property BorderColor As Color
            Get
                Return _borderColor
            End Get
            Set(value As Color)
                _borderColor = value
                Invalidate()
            End Set
        End Property
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, _borderColor, ButtonBorderStyle.Solid)
        End Sub
    End Class

    <DefaultEvent("ValueChanged")> Public Class BatteryIndicator
        Inherits Control
        Private _value As Integer = 75
        Private _charging As Boolean
        Public Event ValueChanged As EventHandler
        Public Sub New()
            Size = New Size(110, 45)
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        End Sub
        <Category("Dados")> Public Property Value As Integer
            Get
                Return _value
            End Get
            Set(newValue As Integer)
                Dim limited As Integer = Math.Max(0, Math.Min(100, newValue))
                If limited = _value Then Return
                _value = limited
                Invalidate()
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End Set
        End Property
        <Category("Dados")> Public Property Charging As Boolean
            Get
                Return _charging
            End Get
            Set(value As Boolean)
                _charging = value
                Invalidate()
            End Set
        End Property
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            Dim body As New Rectangle(2, 4, Width - 10, Height - 8)
            Using outline As New Pen(ForeColor, 2), terminalBrush As New SolidBrush(ForeColor)
                e.Graphics.DrawRectangle(outline, body)
                e.Graphics.FillRectangle(terminalBrush, body.Right + 2, Height \ 3, 6, Height \ 3)
            End Using
            Dim fillWidth As Integer = CInt((body.Width - 6) * _value / 100.0)
            Dim fillColor As Color = If(_value <= 20, Color.OrangeRed, If(_value <= 50, Color.Gold, Color.LimeGreen))
            Using brush As New SolidBrush(fillColor)
                e.Graphics.FillRectangle(brush, body.X + 3, body.Y + 3, fillWidth, body.Height - 5)
            End Using
            Dim caption As String = _value.ToString() & "%" & If(_charging, " ⚡", "")
            TextRenderer.DrawText(e.Graphics, caption, Font, body, ForeColor, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
        End Sub
    End Class

    <DefaultEvent("ValueChanged")> Public Class SignalStrength
        Inherits Control
        Private _value As Integer = 60
        Public Event ValueChanged As EventHandler
        Public Sub New()
            Size = New Size(80, 50)
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        End Sub
        <Category("Dados")> Public Property Value As Integer
            Get
                Return _value
            End Get
            Set(newValue As Integer)
                Dim limited As Integer = Math.Max(0, Math.Min(100, newValue))
                If limited = _value Then Return
                _value = limited
                Invalidate()
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End Set
        End Property
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim active As Integer = CInt(Math.Ceiling(_value / 20.0))
            For index As Integer = 0 To 4
                Dim barWidth As Integer = Math.Max(3, Width \ 7)
                Dim barHeight As Integer = CInt((index + 1) * (Height - 6) / 5.0)
                Dim bounds As New Rectangle(3 + index * (barWidth + 3), Height - barHeight - 2, barWidth, barHeight)
                Using brush As New SolidBrush(If(index < active, ForeColor, Color.Gainsboro))
                    e.Graphics.FillRectangle(brush, bounds)
                End Using
            Next
        End Sub
    End Class

    <DefaultEvent("ValueChanged")> Public Class ThermometerGauge
        Inherits Control
        Private _value As Double = 25
        Private _minimum As Double = -20
        Private _maximum As Double = 100
        Public Event ValueChanged As EventHandler
        Public Sub New()
            Size = New Size(70, 190)
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        End Sub
        <Category("Dados")> Public Property Value As Double
            Get
                Return _value
            End Get
            Set(newValue As Double)
                Dim limited As Double = Math.Max(_minimum, Math.Min(_maximum, newValue))
                If limited = _value Then Return
                _value = limited
                Invalidate()
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End Set
        End Property
        <Category("Dados")> Public Property Minimum As Double
            Get
                Return _minimum
            End Get
            Set(value As Double)
                _minimum = Math.Min(value, _maximum - 1)
                Invalidate()
            End Set
        End Property
        <Category("Dados")> Public Property Maximum As Double
            Get
                Return _maximum
            End Get
            Set(value As Double)
                _maximum = Math.Max(value, _minimum + 1)
                Invalidate()
            End Set
        End Property
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            Dim tube As New Rectangle(Width \ 2 - 7, 8, 14, Height - 45)
            Dim bulb As New Rectangle(Width \ 2 - 18, Height - 42, 36, 36)
            e.Graphics.DrawRectangle(Pens.Gray, tube)
            e.Graphics.DrawEllipse(Pens.Gray, bulb)
            Dim ratio As Double = (_value - _minimum) / (_maximum - _minimum)
            Dim mercuryHeight As Integer = CInt((tube.Height - 5) * ratio)
            Using brush As New SolidBrush(If(ratio > 0.75, Color.Red, Color.DeepSkyBlue))
                e.Graphics.FillRectangle(brush, tube.X + 3, tube.Bottom - mercuryHeight, tube.Width - 5, mercuryHeight)
                e.Graphics.FillEllipse(brush, Rectangle.Inflate(bulb, -4, -4))
            End Using
            TextRenderer.DrawText(e.Graphics, _value.ToString("0") & "°", Font, New Rectangle(0, 0, Width, 25), ForeColor, TextFormatFlags.HorizontalCenter)
        End Sub
    End Class

    <DefaultEvent("ValueChanged")> Public Class AnalogGauge
        Inherits Control
        Private _value As Double
        Private _maximum As Double = 100
        Private _caption As String = "VALOR"
        Public Event ValueChanged As EventHandler
        Public Sub New()
            Size = New Size(180, 115)
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        End Sub
        <Category("Dados")> Public Property Value As Double
            Get
                Return _value
            End Get
            Set(newValue As Double)
                Dim limited As Double = Math.Max(0, Math.Min(_maximum, newValue))
                If limited = _value Then Return
                _value = limited
                Invalidate()
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End Set
        End Property
        <Category("Dados")> Public Property Maximum As Double
            Get
                Return _maximum
            End Get
            Set(value As Double)
                _maximum = Math.Max(1, value)
                Invalidate()
            End Set
        End Property
        <Category("Aparência")> Public Property Caption As String
            Get
                Return _caption
            End Get
            Set(value As String)
                _caption = If(value, "")
                Invalidate()
            End Set
        End Property
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            Dim center As New PointF(Width / 2.0F, Height - 15.0F)
            Dim radius As Single = Math.Min(Width / 2.0F - 8, Height - 28)
            Using arcPen As New Pen(Color.Silver, 7)
                e.Graphics.DrawArc(arcPen, center.X - radius, center.Y - radius, radius * 2, radius * 2, 180, 180)
            End Using
            Dim angle As Double = (180 + 180 * _value / _maximum) * Math.PI / 180
            Using needle As New Pen(Color.Crimson, 3)
                e.Graphics.DrawLine(needle, center, New PointF(center.X + CSng(Math.Cos(angle)) * radius * 0.8F, center.Y + CSng(Math.Sin(angle)) * radius * 0.8F))
            End Using
            e.Graphics.FillEllipse(Brushes.DimGray, center.X - 5, center.Y - 5, 10, 10)
            TextRenderer.DrawText(e.Graphics, _caption & " " & _value.ToString("0"), Font, New Rectangle(0, Height - 25, Width, 22), ForeColor, TextFormatFlags.HorizontalCenter)
        End Sub
    End Class

    <DefaultEvent("ActiveChanged")> Public Class LoadingSpinner
        Inherits Control
        Private ReadOnly animationTimer As New Timer()
        Private _active As Boolean = True
        Private frame As Integer
        Public Event ActiveChanged As EventHandler
        Public Sub New()
            Size = New Size(48, 48)
            animationTimer.Interval = 80
            AddHandler animationTimer.Tick, AddressOf Animate
            animationTimer.Start()
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        End Sub
        <Category("Comportamento")> Public Property Active As Boolean
            Get
                Return _active
            End Get
            Set(value As Boolean)
                If value = _active Then Return
                _active = value
                animationTimer.Enabled = value
                Invalidate()
                RaiseEvent ActiveChanged(Me, EventArgs.Empty)
            End Set
        End Property
        Private Sub Animate(sender As Object, e As EventArgs)
            frame = (frame + 1) Mod 12
            Invalidate()
        End Sub
        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then animationTimer.Dispose()
            MyBase.Dispose(disposing)
        End Sub
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            Dim center As New PointF(Width / 2.0F, Height / 2.0F)
            Dim radius As Single = Math.Min(Width, Height) * 0.34F
            For index As Integer = 0 To 11
                Dim alpha As Integer = 35 + ((index - frame + 12) Mod 12) * 18
                Dim angle As Double = index * Math.PI * 2 / 12
                Using brush As New SolidBrush(Color.FromArgb(Math.Min(255, alpha), ForeColor))
                    e.Graphics.FillEllipse(brush, center.X + CSng(Math.Cos(angle)) * radius - 3, center.Y + CSng(Math.Sin(angle)) * radius - 3, 6, 6)
                End Using
            Next
        End Sub
    End Class

    Public Enum NotificationBannerStyle
        Information
        Success
        Warning
        [Error]
    End Enum

    Public Class NotificationBanner
        Inherits Control
        Private _bannerStyle As NotificationBannerStyle
        Public Sub New()
            Size = New Size(300, 48)
            Text = "Mensagem importante"
            Padding = New Padding(12)
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        End Sub
        <Category("Aparência")> Public Property BannerStyle As NotificationBannerStyle
            Get
                Return _bannerStyle
            End Get
            Set(value As NotificationBannerStyle)
                _bannerStyle = value
                Invalidate()
            End Set
        End Property
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim color As Color
            Select Case _bannerStyle
                Case NotificationBannerStyle.Success : color = Color.SeaGreen
                Case NotificationBannerStyle.Warning : color = Color.DarkOrange
                Case NotificationBannerStyle.[Error] : color = Color.Firebrick
                Case Else : color = Color.RoyalBlue
            End Select
            e.Graphics.Clear(Color.FromArgb(25, color))
            Using brush As New SolidBrush(color)
                e.Graphics.FillRectangle(brush, 0, 0, 6, Height)
            End Using
            TextRenderer.DrawText(e.Graphics, Text, Font, New Rectangle(16, 0, Width - 20, Height), color, TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, color, ButtonBorderStyle.Solid)
        End Sub
    End Class

    <DefaultEvent("CheckedChanged")> Public Class ToggleButton
        Inherits CheckBox
        Public Sub New()
            Appearance = Appearance.Button
            AutoSize = False
            Size = New Size(120, 38)
            Text = "Alternar"
            TextAlign = ContentAlignment.MiddleCenter
            FlatStyle = FlatStyle.Flat
        End Sub
        Protected Overrides Sub OnCheckedChanged(e As EventArgs)
            BackColor = If(Checked, Color.SeaGreen, SystemColors.Control)
            ForeColor = If(Checked, Color.White, SystemColors.ControlText)
            MyBase.OnCheckedChanged(e)
        End Sub
    End Class

    <DefaultEvent("SelectedColorChanged")> Public Class ColorSwatch
        Inherits Control
        Private _selectedColor As Color = Color.RoyalBlue
        Public Event SelectedColorChanged As EventHandler
        Public Sub New()
            Size = New Size(70, 45)
            Cursor = Cursors.Hand
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        End Sub
        <Category("Dados")> Public Property SelectedColor As Color
            Get
                Return _selectedColor
            End Get
            Set(value As Color)
                If value = _selectedColor Then Return
                _selectedColor = value
                Invalidate()
                RaiseEvent SelectedColorChanged(Me, EventArgs.Empty)
            End Set
        End Property
        Public Sub ChooseColor()
            Using dialog As New ColorDialog()
                dialog.Color = _selectedColor
                If dialog.ShowDialog() = DialogResult.OK Then SelectedColor = dialog.Color
            End Using
        End Sub
        Protected Overrides Sub OnDoubleClick(e As EventArgs)
            ChooseColor()
            MyBase.OnDoubleClick(e)
        End Sub
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.Clear(_selectedColor)
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, Color.DimGray, ButtonBorderStyle.Solid)
        End Sub
    End Class

    Public Enum ArrowDirection
        Left
        Right
        Up
        Down
    End Enum

    Public Class NavigationButton
        Inherits Button
        Private _direction As ArrowDirection = ArrowDirection.Right
        Public Sub New()
            Size = New Size(48, 48)
            FlatStyle = FlatStyle.Flat
            Text = ""
        End Sub
        <Category("Aparência")> Public Property Direction As ArrowDirection
            Get
                Return _direction
            End Get
            Set(value As ArrowDirection)
                _direction = value
                Invalidate()
            End Set
        End Property
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            Dim points As Point()
            If _direction = ArrowDirection.Left Then
                points = {New Point(Width * 2 \ 3, Height \ 4), New Point(Width \ 3, Height \ 2), New Point(Width * 2 \ 3, Height * 3 \ 4)}
            ElseIf _direction = ArrowDirection.Up Then
                points = {New Point(Width \ 4, Height * 2 \ 3), New Point(Width \ 2, Height \ 3), New Point(Width * 3 \ 4, Height * 2 \ 3)}
            ElseIf _direction = ArrowDirection.Down Then
                points = {New Point(Width \ 4, Height \ 3), New Point(Width \ 2, Height * 2 \ 3), New Point(Width * 3 \ 4, Height \ 3)}
            Else
                points = {New Point(Width \ 3, Height \ 4), New Point(Width * 2 \ 3, Height \ 2), New Point(Width \ 3, Height * 3 \ 4)}
            End If
            Using pen As New Pen(ForeColor, 3)
                pen.StartCap = LineCap.Round
                pen.EndCap = LineCap.Round
                e.Graphics.DrawLines(pen, points)
            End Using
        End Sub
    End Class

    <DefaultEvent("TextChanged")> Public Class MarqueeLabel
        Inherits Control
        Private ReadOnly scrollTimer As New Timer()
        Private offset As Integer
        Private _speed As Integer = 50
        Public Sub New()
            Size = New Size(240, 32)
            Text = "Texto em movimento"
            scrollTimer.Interval = _speed
            AddHandler scrollTimer.Tick, AddressOf ScrollText
            scrollTimer.Start()
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        End Sub
        <Category("Comportamento")> Public Property Speed As Integer
            Get
                Return _speed
            End Get
            Set(value As Integer)
                _speed = Math.Max(15, Math.Min(1000, value))
                scrollTimer.Interval = _speed
            End Set
        End Property
        Private Sub ScrollText(sender As Object, e As EventArgs)
            offset -= 2
            If offset < -TextRenderer.MeasureText(Text, Font).Width Then offset = Width
            Invalidate()
        End Sub
        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then scrollTimer.Dispose()
            MyBase.Dispose(disposing)
        End Sub
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.Clear(BackColor)
            TextRenderer.DrawText(e.Graphics, Text, Font, New Point(offset, (Height - Font.Height) \ 2), ForeColor)
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, Color.Gainsboro, ButtonBorderStyle.Solid)
        End Sub
    End Class


    <DefaultEvent("SelectedIndexChanged")>
    Public Class FontPreviewComboBox
        Inherits ComboBox
        Private _previewSize As Single = 11.0F

        Public Sub New()
            DrawMode = DrawMode.OwnerDrawFixed
            DropDownStyle = ComboBoxStyle.DropDown
            ItemHeight = 24
            IntegralHeight = False
            DropDownHeight = 360
            DropDownWidth = 260
            Width = 190
            LoadInstalledFonts()
        End Sub

        <Category("Aparência"), DefaultValue(11.0F)>
        Public Property PreviewFontSize As Single
            Get
                Return _previewSize
            End Get
            Set(value As Single)
                _previewSize = Math.Max(6.0F, Math.Min(24.0F, value))
                ItemHeight = Math.Max(22, CInt(Math.Ceiling(_previewSize + 11.0F)))
                Invalidate()
            End Set
        End Property

        <Browsable(False)>
        Public ReadOnly Property SelectedFontName As String
            Get
                Return Text
            End Get
        End Property

        Public Sub ReloadFonts()
            LoadInstalledFonts()
        End Sub

        Private Sub LoadInstalledFonts()
            Dim previous As String = Text
            BeginUpdate()
            Try
                Items.Clear()
                For Each family As FontFamily In FontFamily.Families
                    Items.Add(family.Name)
                Next
            Finally
                EndUpdate()
            End Try
            If Not String.IsNullOrWhiteSpace(previous) Then
                Text = previous
            ElseIf Items.Contains("Segoe UI") Then
                Text = "Segoe UI"
            ElseIf Items.Count > 0 Then
                SelectedIndex = 0
            End If
        End Sub

        Protected Overrides Sub OnDrawItem(e As DrawItemEventArgs)
            e.DrawBackground()
            If e.Index < 0 OrElse e.Index >= Items.Count Then
                e.DrawFocusRectangle()
                Return
            End If
            Dim name As String = Items(e.Index).ToString()
            Dim sample As Font = Nothing
            Try
                sample = New Font(name, _previewSize, FontStyle.Regular)
            Catch
                sample = New Font(Font.FontFamily, _previewSize, FontStyle.Regular)
            End Try
            Try
                Dim color As Color = If((e.State And DrawItemState.Selected) = DrawItemState.Selected,
                                        SystemColors.HighlightText, ForeColor)
                Using brush As New SolidBrush(color)
                    e.Graphics.DrawString(name, sample, brush,
                                          CSng(e.Bounds.Left + 4),
                                          CSng(e.Bounds.Top + 2))
                End Using
            Finally
                sample.Dispose()
            End Try
            e.DrawFocusRectangle()
        End Sub
    End Class

    <DefaultEvent("ContentChanged")> Public Class RichTextEditor
        Inherits UserControl
        Private ReadOnly formatBar As New ToolStrip()
        Private ReadOnly document As New RichTextBox()
        Private ReadOnly fontNames As New FontPreviewComboBox()
        Private ReadOnly fontHost As ToolStripControlHost
        Private ReadOnly fontSizes As New ToolStripComboBox()
        Public Event ContentChanged As EventHandler
        Public Sub New()
            Size = New Size(480, 300)
            formatBar.GripStyle = ToolStripGripStyle.Hidden
            formatBar.Dock = DockStyle.Top
            document.Dock = DockStyle.Fill
            document.BorderStyle = BorderStyle.FixedSingle
            document.AcceptsTab = True
            fontNames.Width = 190
            fontNames.PreviewFontSize = 11.0F
            fontNames.Text = "Segoe UI"
            fontHost = New ToolStripControlHost(fontNames)
            fontHost.AutoSize = False
            fontHost.Width = 190
            fontSizes.Width = 48
            fontSizes.Items.AddRange(New Object() {"8", "9", "10", "11", "12", "14", "16", "18", "24", "32", "48"})
            fontSizes.Text = "10"
            formatBar.Items.Add(fontHost)
            formatBar.Items.Add(fontSizes)
            AddCommand("N", FontStyle.Bold, "Negrito")
            AddCommand("I", FontStyle.Italic, "Itálico")
            AddCommand("S", FontStyle.Underline, "Sublinhado")
            formatBar.Items.Add(New ToolStripSeparator())
            AddButton("Cor", AddressOf ChooseTextColor)
            AddButton("Fundo", AddressOf ChooseBackgroundColor)
            formatBar.Items.Add(New ToolStripSeparator())
            AddButton("←", Sub() document.SelectionAlignment = HorizontalAlignment.Left)
            AddButton("↔", Sub() document.SelectionAlignment = HorizontalAlignment.Center)
            AddButton("→", Sub() document.SelectionAlignment = HorizontalAlignment.Right)
            formatBar.Items.Add(New ToolStripSeparator())
            AddButton("Desfazer", AddressOf UndoDocument)
            AddButton("Refazer", AddressOf RedoDocument)
            AddButton("Abrir", AddressOf OpenDocument)
            AddButton("Salvar", AddressOf SaveDocument)
            Controls.Add(document)
            Controls.Add(formatBar)
            AddHandler fontNames.SelectedIndexChanged, AddressOf ApplySelectedFont
            AddHandler fontNames.KeyDown, AddressOf FontNameKeyDown
            AddHandler fontSizes.SelectedIndexChanged, AddressOf ApplySelectedFont
            AddHandler fontSizes.KeyDown, AddressOf FontSizeKeyDown
            AddHandler document.TextChanged, Sub() RaiseEvent ContentChanged(Me, EventArgs.Empty)
        End Sub
        <Browsable(True), Category("Dados")> Public Shadows Property Text As String
            Get
                Return document.Text
            End Get
            Set(value As String)
                document.Text = If(value, "")
            End Set
        End Property
        <Category("Dados")> Public Property Rtf As String
            Get
                Return document.Rtf
            End Get
            Set(value As String)
                If String.IsNullOrWhiteSpace(value) Then
                    document.Clear()
                Else
                    document.Rtf = value
                End If
            End Set
        End Property
        <Category("Comportamento")> Public Property [ReadOnly] As Boolean
            Get
                Return document.ReadOnly
            End Get
            Set(value As Boolean)
                document.ReadOnly = value
            End Set
        End Property
        <Category("Aparência")> Public Property ToolbarVisible As Boolean
            Get
                Return formatBar.Visible
            End Get
            Set(value As Boolean)
                formatBar.Visible = value
            End Set
        End Property
        <Browsable(False)> Public ReadOnly Property Editor As RichTextBox
            Get
                Return document
            End Get
        End Property
        Public Sub ClearDocument()
            document.Clear()
        End Sub
        Public Sub SelectAllText()
            document.SelectAll()
        End Sub
        Public Sub OpenDocument()
            Using dialog As New OpenFileDialog()
                dialog.Filter = "Documento RTF (*.rtf)|*.rtf|Texto (*.txt)|*.txt|Todos os arquivos (*.*)|*.*"
                If dialog.ShowDialog() <> DialogResult.OK Then Return
                If IO.Path.GetExtension(dialog.FileName).Equals(".rtf", StringComparison.OrdinalIgnoreCase) Then
                    document.LoadFile(dialog.FileName, RichTextBoxStreamType.RichText)
                Else
                    document.LoadFile(dialog.FileName, RichTextBoxStreamType.PlainText)
                End If
            End Using
        End Sub
        Public Sub SaveDocument()
            Using dialog As New SaveFileDialog()
                dialog.Filter = "Documento RTF (*.rtf)|*.rtf|Texto (*.txt)|*.txt"
                dialog.DefaultExt = "rtf"
                If dialog.ShowDialog() <> DialogResult.OK Then Return
                If IO.Path.GetExtension(dialog.FileName).Equals(".txt", StringComparison.OrdinalIgnoreCase) Then
                    document.SaveFile(dialog.FileName, RichTextBoxStreamType.PlainText)
                Else
                    document.SaveFile(dialog.FileName, RichTextBoxStreamType.RichText)
                End If
            End Using
        End Sub
        Private Sub AddCommand(caption As String, style As FontStyle, tooltip As String)
            Dim button As New ToolStripButton(caption) With {.CheckOnClick = True, .ToolTipText = tooltip, .Tag = style}
            AddHandler button.Click, AddressOf ToggleFontStyle
            formatBar.Items.Add(button)
        End Sub
        Private Sub AddButton(caption As String, action As Action)
            Dim button As New ToolStripButton(caption)
            AddHandler button.Click, Sub() action()
            formatBar.Items.Add(button)
        End Sub
        Private Sub ToggleFontStyle(sender As Object, e As EventArgs)
            Dim button As ToolStripButton = DirectCast(sender, ToolStripButton)
            Dim current As Font = If(document.SelectionFont, document.Font)
            Dim style As FontStyle = CType(button.Tag, FontStyle)
            document.SelectionFont = New Font(current, current.Style Xor style)
        End Sub
        Private Sub ApplySelectedFont(sender As Object, e As EventArgs)
            Dim current As Font = If(document.SelectionFont, document.Font)
            Dim size As Single = current.Size
            Single.TryParse(fontSizes.Text, Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, size)
            Try
                document.SelectionFont = New Font(If(String.IsNullOrWhiteSpace(fontNames.Text), current.FontFamily.Name, fontNames.Text), Math.Max(6, size), current.Style)
            Catch
            End Try
        End Sub
        Private Sub FontNameKeyDown(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Enter Then ApplySelectedFont(sender, EventArgs.Empty)
        End Sub
        Private Sub FontSizeKeyDown(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Enter Then ApplySelectedFont(sender, EventArgs.Empty)
        End Sub
        Private Sub ChooseTextColor()
            Using dialog As New ColorDialog()
                If dialog.ShowDialog() = DialogResult.OK Then document.SelectionColor = dialog.Color
            End Using
        End Sub
        Private Sub UndoDocument()
            If document.CanUndo Then document.Undo()
        End Sub
        Private Sub RedoDocument()
            If document.CanRedo Then document.Redo()
        End Sub
        Private Sub ChooseBackgroundColor()
            Using dialog As New ColorDialog()
                If dialog.ShowDialog() = DialogResult.OK Then document.SelectionBackColor = dialog.Color
            End Using
        End Sub
    End Class

    Public Enum CodeLanguage
        VisualBasic
        Html
        Json
        PlainText
    End Enum

    <DefaultEvent("CodeChanged")> Public Class SyntaxCodeEditor
        Inherits UserControl
        Private ReadOnly codeBox As New RichTextBox()
        Private ReadOnly lineMargin As LineNumberMargin
        Private ReadOnly highlightTimer As New Timer()
        Private _language As CodeLanguage = CodeLanguage.VisualBasic
        Private coloring As Boolean
        Public Event CodeChanged As EventHandler
        Public Sub New()
            Size = New Size(520, 320)
            BackColor = Color.FromArgb(30, 30, 30)
            codeBox.Dock = DockStyle.Fill
            codeBox.BorderStyle = BorderStyle.None
            codeBox.BackColor = Color.FromArgb(30, 30, 30)
            codeBox.ForeColor = Color.Gainsboro
            codeBox.Font = New Font("Consolas", 10.5F)
            codeBox.AcceptsTab = True
            codeBox.WordWrap = False
            codeBox.DetectUrls = False
            lineMargin = New LineNumberMargin(codeBox)
            lineMargin.Dock = DockStyle.Left
            lineMargin.Width = 48
            Controls.Add(codeBox)
            Controls.Add(lineMargin)
            highlightTimer.Interval = 180
            AddHandler highlightTimer.Tick, AddressOf HighlightTimerTick
            AddHandler codeBox.TextChanged, AddressOf EditorTextChanged
            AddHandler codeBox.VScroll, Sub() lineMargin.Invalidate()
            AddHandler codeBox.Resize, Sub() lineMargin.Invalidate()
        End Sub
        <Browsable(True), Category("Dados")> Public Shadows Property Text As String
            Get
                Return codeBox.Text
            End Get
            Set(value As String)
                codeBox.Text = If(value, "")
                ScheduleHighlight()
            End Set
        End Property
        <Category("Comportamento")> Public Property Language As CodeLanguage
            Get
                Return _language
            End Get
            Set(value As CodeLanguage)
                _language = value
                ScheduleHighlight()
            End Set
        End Property
        <Category("Comportamento")> Public Property [ReadOnly] As Boolean
            Get
                Return codeBox.ReadOnly
            End Get
            Set(value As Boolean)
                codeBox.ReadOnly = value
            End Set
        End Property
        <Category("Aparência")> Public Property ShowLineNumbers As Boolean
            Get
                Return lineMargin.Visible
            End Get
            Set(value As Boolean)
                lineMargin.Visible = value
            End Set
        End Property
        <Browsable(False)> Public ReadOnly Property Editor As RichTextBox
            Get
                Return codeBox
            End Get
        End Property
        <Browsable(False)> Public ReadOnly Property LineCount As Integer
            Get
                Return Math.Max(1, codeBox.Lines.Length)
            End Get
        End Property
        Public Sub GoToLine(lineNumber As Integer)
            Dim line As Integer = Math.Max(0, Math.Min(codeBox.Lines.Length - 1, lineNumber - 1))
            If line >= 0 Then codeBox.Select(codeBox.GetFirstCharIndexFromLine(line), 0)
            codeBox.Focus()
            codeBox.ScrollToCaret()
        End Sub
        Public Sub HighlightNow()
            ApplyHighlighting()
        End Sub
        Private Sub EditorTextChanged(sender As Object, e As EventArgs)
            If coloring Then Return
            lineMargin.Invalidate()
            ScheduleHighlight()
            RaiseEvent CodeChanged(Me, EventArgs.Empty)
        End Sub
        Private Sub ScheduleHighlight()
            highlightTimer.Stop()
            highlightTimer.Start()
        End Sub
        Private Sub HighlightTimerTick(sender As Object, e As EventArgs)
            highlightTimer.Stop()
            ApplyHighlighting()
        End Sub
        Private Sub ApplyHighlighting()
            If coloring OrElse _language = CodeLanguage.PlainText OrElse codeBox.TextLength > 200000 Then Return
            coloring = True
            Dim selectionStart As Integer = codeBox.SelectionStart
            Dim selectionLength As Integer = codeBox.SelectionLength
            NativeMethods.SendMessage(codeBox.Handle, NativeMethods.WmSetRedraw, IntPtr.Zero, IntPtr.Zero)
            Try
                codeBox.SelectAll()
                codeBox.SelectionColor = Color.Gainsboro
                If _language = CodeLanguage.VisualBasic Then
                    ColorMatches("\b(AddHandler|AddressOf|And|AndAlso|As|Boolean|ByRef|ByVal|Case|Catch|Class|Const|Continue|Date|Decimal|Dim|Do|Double|Each|Else|ElseIf|End|Enum|Event|Exit|False|Finally|For|Friend|Function|Get|Handles|If|Implements|Imports|In|Inherits|Integer|Interface|Is|IsNot|Long|Loop|Me|Mod|Module|MyBase|MyClass|Namespace|New|Next|Not|Nothing|Object|Of|On|Option|Or|OrElse|Overrides|Private|Property|Protected|Public|RaiseEvent|ReadOnly|Return|Select|Set|Shared|Short|Single|Static|Step|Stop|String|Structure|Sub|Then|Throw|To|True|Try|Using|When|While|With|WriteOnly|Xor)\b", Color.DeepSkyBlue, RegexOptions.IgnoreCase)
                    ColorMatches(QuotedTextPattern(), Color.LightSalmon, RegexOptions.None)
                    ColorMatches("'.*$", Color.ForestGreen, RegexOptions.Multiline)
                ElseIf _language = CodeLanguage.Html Then
                    ColorMatches("</?[A-Za-z][^>]*>", Color.DeepSkyBlue, RegexOptions.None)
                    ColorMatches(QuotedTextPattern(), Color.LightSalmon, RegexOptions.None)
                    ColorMatches("<!--[\s\S]*?-->", Color.ForestGreen, RegexOptions.None)
                ElseIf _language = CodeLanguage.Json Then
                    ColorMatches(QuotedTextPattern(), Color.LightSalmon, RegexOptions.None)
                    ColorMatches(QuotedTextPattern() & "(?=\s*:)", Color.DeepSkyBlue, RegexOptions.None)
                    ColorMatches("\b(true|false|null)\b", Color.MediumPurple, RegexOptions.IgnoreCase)
                    ColorMatches("-?\b\d+(\.\d+)?([eE][+-]?\d+)?\b", Color.LightGreen, RegexOptions.None)
                End If
                codeBox.Select(Math.Min(selectionStart, codeBox.TextLength), Math.Min(selectionLength, Math.Max(0, codeBox.TextLength - selectionStart)))
            Finally
                NativeMethods.SendMessage(codeBox.Handle, NativeMethods.WmSetRedraw, New IntPtr(1), IntPtr.Zero)
                codeBox.Invalidate()
                coloring = False
            End Try
        End Sub
        Private Sub ColorMatches(pattern As String, color As Color, options As RegexOptions)
            For Each match As Match In Regex.Matches(codeBox.Text, pattern, options)
                codeBox.Select(match.Index, match.Length)
                codeBox.SelectionColor = color
            Next
        End Sub
        Private Shared Function QuotedTextPattern() As String
            Dim quote As String = Convert.ToChar(34).ToString()
            Return quote & "(?:\\.|[^" & quote & "])*" & quote
        End Function
        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then highlightTimer.Dispose()
            MyBase.Dispose(disposing)
        End Sub

        Private NotInheritable Class NativeMethods
            Public Const WmSetRedraw As Integer = &HB
            <DllImport("user32.dll")> Public Shared Function SendMessage(windowHandle As IntPtr, message As Integer, wordParameter As IntPtr, longParameter As IntPtr) As IntPtr
            End Function
        End Class

        Private Class LineNumberMargin
            Inherits Control
            Private ReadOnly owner As RichTextBox
            Public Sub New(editor As RichTextBox)
                owner = editor
                BackColor = Color.FromArgb(38, 38, 38)
                ForeColor = Color.Gray
                Font = New Font("Consolas", 9.0F)
                SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
            End Sub
            Protected Overrides Sub OnPaint(e As PaintEventArgs)
                e.Graphics.Clear(BackColor)
                Dim firstCharacter As Integer = owner.GetCharIndexFromPosition(New Point(1, 1))
                Dim firstLine As Integer = Math.Max(0, owner.GetLineFromCharIndex(firstCharacter))
                Dim lineHeight As Integer = Math.Max(1, TextRenderer.MeasureText("0", owner.Font).Height)
                Dim visibleLines As Integer = Height \ lineHeight + 2
                For index As Integer = 0 To visibleLines
                    Dim lineNumber As Integer = firstLine + index + 1
                    If lineNumber > Math.Max(1, owner.Lines.Length) Then Exit For
                    TextRenderer.DrawText(e.Graphics, lineNumber.ToString(), Font, New Rectangle(0, index * lineHeight, Width - 6, lineHeight), ForeColor, TextFormatFlags.Right Or TextFormatFlags.VerticalCenter)
                Next
                e.Graphics.DrawLine(Pens.DimGray, Width - 1, 0, Width - 1, Height)
            End Sub
        End Class
    End Class

    Public Enum SparklineStyle
        Line
        Bars
        Area
    End Enum

    Public Enum SparklineLegendPosition
        TopLeft
        TopRight
        BottomLeft
        BottomRight
    End Enum

    <DefaultEvent("DataChanged")> Public Class Sparkline
        Inherits Control

        Private _data As String = "3, 6, 2, 8, 5, 9, 4, 7, 6, 10"
        Private _lineColor As Color = Color.DodgerBlue
        Private _fillColor As Color = Color.DodgerBlue
        Private _gridColor As Color = Color.Gainsboro
        Private _borderColor As Color = Color.Silver
        Private _textColor As Color = Color.DimGray
        Private _fillLine As Boolean = True
        Private _showDots As Boolean = True
        Private _showGrid As Boolean = False
        Private _showBorder As Boolean = False
        Private _showLegend As Boolean = False
        Private _showMinMax As Boolean = False
        Private _showLastValue As Boolean = False
        Private _showValueLabels As Boolean = False
        Private _autoScale As Boolean = True
        Private _lineWidth As Integer = 2
        Private _dotSize As Integer = 5
        Private _fillOpacity As Integer = 40
        Private _gridLines As Integer = 3
        Private _minimumValue As Double = 0
        Private _maximumValue As Double = 100
        Private _style As SparklineStyle = SparklineStyle.Line
        Private _legendPosition As SparklineLegendPosition = SparklineLegendPosition.TopRight
        Private _title As String = ""
        Private _legendText As String = "Série"
        Private _valueFormat As String = "0.##"

        Public Event DataChanged As EventHandler

        Public Sub New()
            Size = New Size(220, 90)
            BackColor = Color.White
            ForeColor = Color.DimGray
            Padding = New Padding(6)
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint Or ControlStyles.ResizeRedraw Or ControlStyles.AllPaintingInWmPaint, True)
        End Sub

        <Category("Dados"), Description("Valores numéricos separados por vírgula, ex.: 3, 6, 2, 8")>
        Public Property DataPoints As String
            Get
                Return _data
            End Get
            Set(newValue As String)
                _data = If(newValue, "")
                Invalidate()
                RaiseEvent DataChanged(Me, EventArgs.Empty)
            End Set
        End Property

        <Category("Dados"), Description("Quando True, mínimo e máximo são calculados automaticamente a partir dos dados.")>
        Public Property AutoScale As Boolean
            Get
                Return _autoScale
            End Get
            Set(value As Boolean)
                _autoScale = value
                Invalidate()
            End Set
        End Property

        <Category("Dados"), Description("Valor mínimo da escala quando AutoScale=False.")>
        Public Property MinimumValue As Double
            Get
                Return _minimumValue
            End Get
            Set(value As Double)
                _minimumValue = value
                Invalidate()
            End Set
        End Property

        <Category("Dados"), Description("Valor máximo da escala quando AutoScale=False.")>
        Public Property MaximumValue As Double
            Get
                Return _maximumValue
            End Get
            Set(value As Double)
                _maximumValue = value
                Invalidate()
            End Set
        End Property

        <Category("Dados"), Description("Formato numérico usado nos rótulos, por exemplo 0.0 ou 0.##.")>
        Public Property ValueFormat As String
            Get
                Return _valueFormat
            End Get
            Set(value As String)
                _valueFormat = If(String.IsNullOrWhiteSpace(value), "0.##", value)
                Invalidate()
            End Set
        End Property

        <Category("Aparência")>
        Public Property ChartStyle As SparklineStyle
            Get
                Return _style
            End Get
            Set(newValue As SparklineStyle)
                _style = newValue
                Invalidate()
            End Set
        End Property

        <Category("Aparência")>
        Public Property LineColor As Color
            Get
                Return _lineColor
            End Get
            Set(newValue As Color)
                _lineColor = newValue
                Invalidate()
            End Set
        End Property

        <Category("Aparência")>
        Public Property FillColor As Color
            Get
                Return _fillColor
            End Get
            Set(value As Color)
                _fillColor = value
                Invalidate()
            End Set
        End Property

        <Category("Aparência"), Description("Opacidade do preenchimento, de 0 a 255.")>
        Public Property FillOpacity As Integer
            Get
                Return _fillOpacity
            End Get
            Set(value As Integer)
                _fillOpacity = Math.Max(0, Math.Min(255, value))
                Invalidate()
            End Set
        End Property

        <Category("Aparência")>
        Public Property FillArea As Boolean
            Get
                Return _fillLine
            End Get
            Set(newValue As Boolean)
                _fillLine = newValue
                Invalidate()
            End Set
        End Property

        <Category("Aparência")>
        Public Property ShowDots As Boolean
            Get
                Return _showDots
            End Get
            Set(newValue As Boolean)
                _showDots = newValue
                Invalidate()
            End Set
        End Property

        <Category("Aparência"), Description("Diâmetro dos marcadores dos pontos.")>
        Public Property DotSize As Integer
            Get
                Return _dotSize
            End Get
            Set(value As Integer)
                _dotSize = Math.Max(2, Math.Min(20, value))
                Invalidate()
            End Set
        End Property

        <Category("Aparência")>
        Public Property LineWidth As Integer
            Get
                Return _lineWidth
            End Get
            Set(newValue As Integer)
                _lineWidth = Math.Max(1, Math.Min(10, newValue))
                Invalidate()
            End Set
        End Property

        <Category("Grade")>
        Public Property ShowGrid As Boolean
            Get
                Return _showGrid
            End Get
            Set(value As Boolean)
                _showGrid = value
                Invalidate()
            End Set
        End Property

        <Category("Grade")>
        Public Property GridColor As Color
            Get
                Return _gridColor
            End Get
            Set(value As Color)
                _gridColor = value
                Invalidate()
            End Set
        End Property

        <Category("Grade"), Description("Quantidade de divisões horizontais da grade.")>
        Public Property GridLines As Integer
            Get
                Return _gridLines
            End Get
            Set(value As Integer)
                _gridLines = Math.Max(1, Math.Min(10, value))
                Invalidate()
            End Set
        End Property

        <Category("Legenda")>
        Public Property Title As String
            Get
                Return _title
            End Get
            Set(value As String)
                _title = If(value, "")
                Invalidate()
            End Set
        End Property

        <Category("Legenda")>
        Public Property ShowLegend As Boolean
            Get
                Return _showLegend
            End Get
            Set(value As Boolean)
                _showLegend = value
                Invalidate()
            End Set
        End Property

        <Category("Legenda")>
        Public Property LegendText As String
            Get
                Return _legendText
            End Get
            Set(value As String)
                _legendText = If(value, "")
                Invalidate()
            End Set
        End Property

        <Category("Legenda")>
        Public Property LegendPosition As SparklineLegendPosition
            Get
                Return _legendPosition
            End Get
            Set(value As SparklineLegendPosition)
                _legendPosition = value
                Invalidate()
            End Set
        End Property

        <Category("Legenda")>
        Public Property TextColor As Color
            Get
                Return _textColor
            End Get
            Set(value As Color)
                _textColor = value
                Invalidate()
            End Set
        End Property

        <Category("Rótulos")>
        Public Property ShowMinMax As Boolean
            Get
                Return _showMinMax
            End Get
            Set(value As Boolean)
                _showMinMax = value
                Invalidate()
            End Set
        End Property

        <Category("Rótulos")>
        Public Property ShowLastValue As Boolean
            Get
                Return _showLastValue
            End Get
            Set(value As Boolean)
                _showLastValue = value
                Invalidate()
            End Set
        End Property

        <Category("Rótulos"), Description("Exibe o valor sobre cada ponto/barra. Recomendado para poucos pontos.")>
        Public Property ShowValueLabels As Boolean
            Get
                Return _showValueLabels
            End Get
            Set(value As Boolean)
                _showValueLabels = value
                Invalidate()
            End Set
        End Property

        <Category("Borda")>
        Public Property ShowBorder As Boolean
            Get
                Return _showBorder
            End Get
            Set(value As Boolean)
                _showBorder = value
                Invalidate()
            End Set
        End Property

        <Category("Borda")>
        Public Property BorderColor As Color
            Get
                Return _borderColor
            End Get
            Set(value As Color)
                _borderColor = value
                Invalidate()
            End Set
        End Property

        Public Sub SetValues(values As IEnumerable(Of Double))
            If values Is Nothing Then
                DataPoints = ""
                Return
            End If
            DataPoints = String.Join(", ", values.Select(Function(v) v.ToString(Globalization.CultureInfo.InvariantCulture)))
        End Sub

        Public Sub AddValue(value As Double)
            Dim values As New List(Of Double)(ParsedValues())
            values.Add(value)
            SetValues(values)
        End Sub

        Public Sub ClearValues()
            DataPoints = ""
        End Sub

        Private Function ParsedValues() As Double()
            If String.IsNullOrWhiteSpace(_data) Then Return New Double() {}
            Dim result As New List(Of Double)()
            For Each token As String In _data.Split(","c)
                Dim number As Double
                If Double.TryParse(token.Trim(), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, number) OrElse
                   Double.TryParse(token.Trim(), Globalization.NumberStyles.Any, Globalization.CultureInfo.CurrentCulture, number) Then
                    result.Add(number)
                End If
            Next
            Return result.ToArray()
        End Function

        Private Function FormatValue(value As Double) As String
            Try
                Return value.ToString(_valueFormat, Globalization.CultureInfo.CurrentCulture)
            Catch
                Return value.ToString(Globalization.CultureInfo.CurrentCulture)
            End Try
        End Function

        Private Sub DrawLegend(g As Graphics, bounds As RectangleF)
            If Not _showLegend OrElse String.IsNullOrWhiteSpace(_legendText) Then Return
            Dim textSize As Size = TextRenderer.MeasureText(_legendText, Font)
            Dim legendWidth As Single = textSize.Width + 24.0F
            Dim legendHeight As Single = Math.Max(16.0F, textSize.Height)
            Dim x As Single
            Dim y As Single
            Select Case _legendPosition
                Case SparklineLegendPosition.TopLeft
                    x = bounds.Left
                    y = bounds.Top
                Case SparklineLegendPosition.BottomLeft
                    x = bounds.Left
                    y = bounds.Bottom - legendHeight
                Case SparklineLegendPosition.BottomRight
                    x = bounds.Right - legendWidth
                    y = bounds.Bottom - legendHeight
                Case Else
                    x = bounds.Right - legendWidth
                    y = bounds.Top
            End Select
            Using pen As New Pen(_lineColor, Math.Max(2, _lineWidth))
                g.DrawLine(pen, x + 2, y + legendHeight / 2.0F, x + 16, y + legendHeight / 2.0F)
            End Using
            Using brush As New SolidBrush(_lineColor)
                g.FillEllipse(brush, x + 7, y + legendHeight / 2.0F - 3, 6, 6)
            End Using
            TextRenderer.DrawText(g, _legendText, Font, New Point(CInt(x + 21), CInt(y)), _textColor)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            e.Graphics.Clear(BackColor)

            Dim client As New RectangleF(Padding.Left, Padding.Top, Math.Max(1, ClientSize.Width - Padding.Horizontal), Math.Max(1, ClientSize.Height - Padding.Vertical))
            If _showBorder Then
                Using borderPen As New Pen(_borderColor)
                    e.Graphics.DrawRectangle(borderPen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1))
                End Using
            End If

            Dim titleHeight As Integer = 0
            If Not String.IsNullOrWhiteSpace(_title) Then
                titleHeight = Math.Max(Font.Height + 4, 18)
                Using titleFont As New Font(Font, FontStyle.Bold)
                    TextRenderer.DrawText(e.Graphics, _title, titleFont, New Rectangle(CInt(client.Left), CInt(client.Top), CInt(client.Width), titleHeight), _textColor, TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)
                End Using
                client.Y += titleHeight
                client.Height = Math.Max(1, client.Height - titleHeight)
            End If

            DrawLegend(e.Graphics, client)

            Dim legendReserveTop As Integer = 0
            Dim legendReserveBottom As Integer = 0
            If _showLegend AndAlso Not String.IsNullOrWhiteSpace(_legendText) Then
                Select Case _legendPosition
                    Case SparklineLegendPosition.TopLeft, SparklineLegendPosition.TopRight
                        legendReserveTop = Font.Height + 4
                    Case Else
                        legendReserveBottom = Font.Height + 4
                End Select
            End If

            client.Y += legendReserveTop
            client.Height = Math.Max(1, client.Height - legendReserveTop - legendReserveBottom)

            Dim values As Double() = ParsedValues()
            If values.Length = 0 Then
                TextRenderer.DrawText(e.Graphics, "Sem dados", Font, Rectangle.Round(client), _textColor, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
                Return
            End If

            Dim minimum As Double
            Dim maximum As Double
            If _autoScale Then
                minimum = values.Min()
                maximum = values.Max()
            Else
                minimum = _minimumValue
                maximum = _maximumValue
            End If
            If maximum <= minimum Then maximum = minimum + 1

            Dim labelReserveLeft As Integer = If(_showMinMax, 34, 0)
            Dim labelReserveRight As Integer = If(_showLastValue, 40, 0)
            Dim valueLabelReserve As Integer = If(_showValueLabels, Font.Height + 2, 0)
            Dim plot As New RectangleF(client.Left + labelReserveLeft, client.Top + valueLabelReserve, Math.Max(1, client.Width - labelReserveLeft - labelReserveRight), Math.Max(1, client.Height - valueLabelReserve))

            If _showGrid Then
                Using gridPen As New Pen(_gridColor, 1.0F)
                    gridPen.DashStyle = DashStyle.Dot
                    For i As Integer = 0 To _gridLines
                        Dim y As Single = plot.Top + (plot.Height * i / _gridLines)
                        e.Graphics.DrawLine(gridPen, plot.Left, y, plot.Right, y)
                    Next
                End Using
            End If

            If _showMinMax Then
                TextRenderer.DrawText(e.Graphics, FormatValue(maximum), Font, New Rectangle(CInt(client.Left), CInt(plot.Top - 2), labelReserveLeft - 2, Font.Height + 2), _textColor, TextFormatFlags.Right)
                TextRenderer.DrawText(e.Graphics, FormatValue(minimum), Font, New Rectangle(CInt(client.Left), CInt(plot.Bottom - Font.Height), labelReserveLeft - 2, Font.Height + 2), _textColor, TextFormatFlags.Right)
            End If

            Dim points As New List(Of PointF)()
            For index As Integer = 0 To values.Length - 1
                Dim x As Single = plot.Left + If(values.Length = 1, plot.Width / 2.0F, index * plot.Width / (values.Length - 1))
                Dim ratio As Double = (values(index) - minimum) / (maximum - minimum)
                ratio = Math.Max(0.0, Math.Min(1.0, ratio))
                Dim y As Single = plot.Bottom - CSng(ratio * plot.Height)
                points.Add(New PointF(x, y))
            Next

            If _style = SparklineStyle.Bars Then
                Dim barWidth As Single = Math.Max(2.0F, plot.Width / Math.Max(1, values.Length))
                Using brush As New SolidBrush(_lineColor)
                    For index As Integer = 0 To points.Count - 1
                        Dim left As Single = points(index).X - barWidth / 2.0F + 1
                        Dim height As Single = Math.Max(1.0F, plot.Bottom - points(index).Y)
                        e.Graphics.FillRectangle(brush, left, points(index).Y, Math.Max(1.0F, barWidth - 2), height)
                    Next
                End Using
            Else
                If (_fillLine OrElse _style = SparklineStyle.Area) AndAlso points.Count > 1 Then
                    Dim fillPoints As New List(Of PointF)(points)
                    fillPoints.Add(New PointF(points(points.Count - 1).X, plot.Bottom))
                    fillPoints.Add(New PointF(points(0).X, plot.Bottom))
                    Using fillBrush As New SolidBrush(Color.FromArgb(_fillOpacity, _fillColor))
                        e.Graphics.FillPolygon(fillBrush, fillPoints.ToArray())
                    End Using
                End If
                If points.Count > 1 Then
                    Using linePen As New Pen(_lineColor, _lineWidth) With {.LineJoin = LineJoin.Round, .StartCap = LineCap.Round, .EndCap = LineCap.Round}
                        e.Graphics.DrawLines(linePen, points.ToArray())
                    End Using
                End If
                If _showDots Then
                    Using dotBrush As New SolidBrush(_lineColor)
                        Dim radius As Single = _dotSize / 2.0F
                        For Each point As PointF In points
                            e.Graphics.FillEllipse(dotBrush, point.X - radius, point.Y - radius, _dotSize, _dotSize)
                        Next
                    End Using
                End If
            End If

            If _showValueLabels Then
                For index As Integer = 0 To points.Count - 1
                    Dim label As String = FormatValue(values(index))
                    Dim textSize As Size = TextRenderer.MeasureText(label, Font)
                    Dim labelX As Integer = CInt(points(index).X - textSize.Width / 2.0F)
                    Dim labelY As Integer = Math.Max(CInt(client.Top), CInt(points(index).Y - textSize.Height - 2))
                    TextRenderer.DrawText(e.Graphics, label, Font, New Point(labelX, labelY), _textColor)
                Next
            End If

            If _showLastValue AndAlso points.Count > 0 Then
                Dim lastText As String = FormatValue(values(values.Length - 1))
                Dim lastPoint As PointF = points(points.Count - 1)
                TextRenderer.DrawText(e.Graphics, lastText, Font, New Rectangle(CInt(plot.Right + 4), CInt(lastPoint.Y - Font.Height / 2.0F), Math.Max(1, labelReserveRight - 4), Font.Height + 4), _lineColor, TextFormatFlags.Left Or TextFormatFlags.VerticalCenter)
            End If
        End Sub
    End Class

    <DefaultEvent("Click")>
    Public Class ImageButton
        Inherits Button

        Private _imageFile As String = ""

        Public Sub New()
            Size = New Size(150, 48)
            Text = "Imagem"
            TextImageRelation = TextImageRelation.ImageBeforeText
        End Sub

        <Category("Aparência")>
        Public Property ImageFile As String
            Get
                Return _imageFile
            End Get
            Set(value As String)
                _imageFile = If(value, "")
                Try
                    If Image IsNot Nothing Then
                        Image.Dispose()
                    End If
                    If IO.File.Exists(_imageFile) Then
                        Image = Drawing.Image.FromFile(_imageFile)
                    Else
                        Image = Nothing
                    End If
                Catch
                    Image = Nothing
                End Try
                Invalidate()
            End Set
        End Property
    End Class

    <DefaultEvent("Search")>
    Public Class SearchBox
        Inherits UserControl

        Private ReadOnly box As New TextBox()
        Private ReadOnly button As New Button()

        Public Event Search As EventHandler

        Public Sub New()
            Size = New Size(260, 30)
            box.Dock = DockStyle.Fill
            button.Dock = DockStyle.Right
            button.Width = 42
            button.Text = "🔎"
            Controls.Add(box)
            Controls.Add(button)
            AddHandler button.Click, AddressOf SearchButtonClick
            AddHandler box.KeyDown, AddressOf SearchBoxKeyDown
        End Sub

        <Category("Dados")>
        Public Shadows Property Text As String
            Get
                Return box.Text
            End Get
            Set(value As String)
                box.Text = If(value, "")
            End Set
        End Property

        <Category("Aparência")>
        Public Property Placeholder As String
            Get
                If box.Tag Is Nothing Then
                    Return ""
                End If
                Return box.Tag.ToString()
            End Get
            Set(value As String)
                box.Tag = If(value, "")
            End Set
        End Property

        Public Sub ClearSearch()
            box.Clear()
        End Sub

        Private Sub SearchButtonClick(sender As Object, e As EventArgs)
            RaiseEvent Search(Me, EventArgs.Empty)
        End Sub

        Private Sub SearchBoxKeyDown(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Enter Then
                RaiseEvent Search(Me, EventArgs.Empty)
            End If
        End Sub
    End Class

    <DefaultEvent("TextChanged")>
    Public Class PasswordBox
        Inherits TextBox

        Private _showPassword As Boolean

        Public Sub New()
            UseSystemPasswordChar = True
            Size = New Size(180, 25)
        End Sub

        <Category("Comportamento")>
        Public Property ShowPassword As Boolean
            Get
                Return _showPassword
            End Get
            Set(value As Boolean)
                _showPassword = value
                UseSystemPasswordChar = Not value
            End Set
        End Property
    End Class

    <DefaultEvent("TextChanged")>
    Public Class IPAddressBox
        Inherits MaskedTextBox

        Public Sub New()
            Mask = "000\.000\.000\.000"
            PromptChar = " "c
            Size = New Size(140, 25)
        End Sub

        <Browsable(False)>
        Public ReadOnly Property IsValidAddress As Boolean
            Get
                Dim ip As Net.IPAddress = Nothing
                Return Net.IPAddress.TryParse(Text.Replace(" ", ""), ip)
            End Get
        End Property
    End Class

    <DefaultEvent("ValueChanged")>
    Public Class ModernDatePicker
        Inherits UserControl

        Private ReadOnly picker As New DateTimePicker()
        Private _accentColor As Color = Color.RoyalBlue

        Public Event ValueChanged As EventHandler

        Public Sub New()
            DoubleBuffered = True
            BackColor = Color.White
            Padding = New Padding(2)
            Size = New Size(170, 32)
            picker.Dock = DockStyle.Fill
            picker.Format = DateTimePickerFormat.Short
            picker.CalendarTitleBackColor = _accentColor
            Controls.Add(picker)
            AddHandler picker.ValueChanged, AddressOf PickerValueChanged
        End Sub

        <Category("Dados")>
        Public Property Value As Date
            Get
                Return picker.Value
            End Get
            Set(value As Date)
                picker.Value = value
            End Set
        End Property

        <Category("Dados")>
        Public Property Format As DateTimePickerFormat
            Get
                Return picker.Format
            End Get
            Set(value As DateTimePickerFormat)
                picker.Format = value
            End Set
        End Property

        <Category("Dados")>
        Public Property CustomFormat As String
            Get
                Return picker.CustomFormat
            End Get
            Set(value As String)
                picker.CustomFormat = If(value, "")
            End Set
        End Property

        <Category("Aparência")>
        Public Property AccentColor As Color
            Get
                Return _accentColor
            End Get
            Set(value As Color)
                _accentColor = value
                picker.CalendarTitleBackColor = value
                Invalidate()
            End Set
        End Property

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            Using p As New Pen(_accentColor, 2)
                e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1)
            End Using
        End Sub

        Private Sub PickerValueChanged(sender As Object, e As EventArgs)
            RaiseEvent ValueChanged(Me, EventArgs.Empty)
        End Sub
    End Class

    Public Enum SimpleChartStyle
        Line
        Bars
    End Enum

    <DefaultEvent("DataChanged")>
    Public Class SimpleChart
        Inherits Control

        Private _data As String = "10,25,18,42,35,60"
        Private _chartStyle As SimpleChartStyle = SimpleChartStyle.Line
        Private _chartColor As Color = Color.DodgerBlue

        Public Event DataChanged As EventHandler

        Public Sub New()
            DoubleBuffered = True
            BackColor = Color.White
            Size = New Size(300, 180)
        End Sub

        <Category("Dados")>
        Public Property DataPoints As String
            Get
                Return _data
            End Get
            Set(value As String)
                _data = If(value, "")
                Invalidate()
                RaiseEvent DataChanged(Me, EventArgs.Empty)
            End Set
        End Property

        <Category("Aparência")>
        Public Property ChartStyle As SimpleChartStyle
            Get
                Return _chartStyle
            End Get
            Set(value As SimpleChartStyle)
                _chartStyle = value
                Invalidate()
            End Set
        End Property

        <Category("Aparência")>
        Public Property ChartColor As Color
            Get
                Return _chartColor
            End Get
            Set(value As Color)
                _chartColor = value
                Invalidate()
            End Set
        End Property

        Private Function Values() As Double()
            Return _data.Split(","c).Select(Function(t)
                                                 Dim v As Double = 0
                                                 Double.TryParse(t.Trim(), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, v)
                                                 Return v
                                             End Function).ToArray()
        End Function

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            e.Graphics.Clear(BackColor)

            Dim a As Double() = Values()
            If a.Length = 0 Then
                Return
            End If

            Dim minValue As Double = a.Min()
            Dim maxValue As Double = a.Max()
            If maxValue = minValue Then
                maxValue = minValue + 1
            End If

            Dim r As New RectangleF(28, 10, Math.Max(1, Width - 38), Math.Max(1, Height - 32))
            e.Graphics.DrawLine(Pens.Gray, r.Left, r.Bottom, r.Right, r.Bottom)
            e.Graphics.DrawLine(Pens.Gray, r.Left, r.Top, r.Left, r.Bottom)

            If _chartStyle = SimpleChartStyle.Bars Then
                Dim bw As Single = r.Width / a.Length
                Using b As New SolidBrush(_chartColor)
                    For i As Integer = 0 To a.Length - 1
                        Dim h As Single = CSng((a(i) - minValue) / (maxValue - minValue) * r.Height)
                        e.Graphics.FillRectangle(b, r.Left + i * bw + 2, r.Bottom - h, Math.Max(1, bw - 4), h)
                    Next
                End Using
            ElseIf a.Length > 1 Then
                Dim pts As New List(Of PointF)()
                For i As Integer = 0 To a.Length - 1
                    pts.Add(New PointF(r.Left + CSng(i * r.Width / (a.Length - 1)), r.Bottom - CSng((a(i) - minValue) / (maxValue - minValue) * r.Height)))
                Next
                Using p As New Pen(_chartColor, 2)
                    e.Graphics.DrawLines(p, pts.ToArray())
                End Using
            End If
        End Sub
    End Class

    <DefaultEvent("PositionChanged")>
    Public Class VirtualJoystick
        Inherits Control

        Private _x As Integer
        Private _y As Integer

        Public Event PositionChanged As EventHandler

        Public Sub New()
            DoubleBuffered = True
            Size = New Size(130, 130)
        End Sub

        <Category("Dados")>
        Public ReadOnly Property XValue As Integer
            Get
                Return _x
            End Get
        End Property

        <Category("Dados")>
        Public ReadOnly Property YValue As Integer
            Get
                Return _y
            End Get
        End Property

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)
            UpdatePosition(e.Location)
        End Sub

        Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
            MyBase.OnMouseMove(e)
            If e.Button = MouseButtons.Left Then
                UpdatePosition(e.Location)
            End If
        End Sub

        Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
            MyBase.OnMouseUp(e)
            _x = 0
            _y = 0
            Invalidate()
            RaiseEvent PositionChanged(Me, EventArgs.Empty)
        End Sub

        Private Sub UpdatePosition(p As Point)
            _x = Math.Max(-100, Math.Min(100, CInt((p.X - Width / 2.0) / (Width / 2.0) * 100)))
            _y = Math.Max(-100, Math.Min(100, CInt((Height / 2.0 - p.Y) / (Height / 2.0) * 100)))
            Invalidate()
            RaiseEvent PositionChanged(Me, EventArgs.Empty)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            e.Graphics.Clear(BackColor)
            Dim d As Integer = Math.Min(Width, Height) - 8
            Dim baseR As New Rectangle((Width - d) \ 2, (Height - d) \ 2, d, d)
            e.Graphics.FillEllipse(Brushes.Gainsboro, baseR)
            e.Graphics.DrawEllipse(Pens.Gray, baseR)
            Dim cx As Integer = Width \ 2 + CInt(_x / 100.0 * d * 0.28)
            Dim cy As Integer = Height \ 2 - CInt(_y / 100.0 * d * 0.28)
            e.Graphics.FillEllipse(Brushes.DimGray, cx - 18, cy - 18, 36, 36)
        End Sub
    End Class

    <DefaultEvent("ValueChanged")>
    Public Class LcdDisplay
        Inherits Control

        Private _value As String = "HELLO"

        Public Event ValueChanged As EventHandler

        Public Sub New()
            DoubleBuffered = True
            BackColor = Color.FromArgb(150, 190, 70)
            ForeColor = Color.FromArgb(35, 55, 20)
            Font = New Font("Consolas", 18, FontStyle.Bold)
            Size = New Size(260, 70)
        End Sub

        <Category("Dados")>
        Public Property Value As String
            Get
                Return _value
            End Get
            Set(value As String)
                _value = If(value, "")
                Invalidate()
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End Set
        End Property

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            e.Graphics.Clear(BackColor)
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, Color.DarkOliveGreen, ButtonBorderStyle.Solid)
            TextRenderer.DrawText(e.Graphics, _value, Font, Rectangle.Inflate(ClientRectangle, -8, -6), ForeColor, TextFormatFlags.Left Or TextFormatFlags.VerticalCenter)
        End Sub
    End Class

    <DefaultEvent("CellChanged")>
    Public Class LedMatrix
        Inherits Control

        Private _rows As Integer = 8
        Private _columns As Integer = 8
        Private _pattern As String = ""

        Public Event CellChanged As EventHandler

        Public Sub New()
            DoubleBuffered = True
            BackColor = Color.Black
            Size = New Size(180, 180)
        End Sub

        <Category("Dados")>
        Public Property Rows As Integer
            Get
                Return _rows
            End Get
            Set(value As Integer)
                _rows = Math.Max(1, Math.Min(32, value))
                Invalidate()
            End Set
        End Property

        <Category("Dados")>
        Public Property Columns As Integer
            Get
                Return _columns
            End Get
            Set(value As Integer)
                _columns = Math.Max(1, Math.Min(32, value))
                Invalidate()
            End Set
        End Property

        <Category("Dados")>
        Public Property Pattern As String
            Get
                Return _pattern
            End Get
            Set(value As String)
                _pattern = If(value, "")
                Invalidate()
            End Set
        End Property

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)
            Dim c As Integer = Math.Max(0, Math.Min(_columns - 1, CInt(Math.Floor(e.X / Math.Max(1.0, Width / CDbl(_columns))))))
            Dim r As Integer = Math.Max(0, Math.Min(_rows - 1, CInt(Math.Floor(e.Y / Math.Max(1.0, Height / CDbl(_rows))))))
            Dim bits As Char() = (_pattern & New String("0"c, _rows * _columns)).Substring(0, _rows * _columns).ToCharArray()
            Dim i As Integer = r * _columns + c
            bits(i) = If(bits(i) = "1"c, "0"c, "1"c)
            _pattern = New String(bits)
            Invalidate()
            RaiseEvent CellChanged(Me, EventArgs.Empty)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            e.Graphics.Clear(BackColor)
            Dim cw As Double = Width / CDbl(_columns)
            Dim ch As Double = Height / CDbl(_rows)
            Dim bits As String = _pattern & New String("0"c, _rows * _columns)

            For r As Integer = 0 To _rows - 1
                For c As Integer = 0 To _columns - 1
                    Dim onState As Boolean = bits(r * _columns + c) = "1"c
                    Using b As New SolidBrush(If(onState, Color.Lime, Color.FromArgb(25, 70, 25)))
                        e.Graphics.FillEllipse(b, CSng(c * cw + 2), CSng(r * ch + 2), CSng(Math.Max(1, cw - 4)), CSng(Math.Max(1, ch - 4)))
                    End Using
                Next
            Next
        End Sub
    End Class

    Public Enum TrafficLightState
        Off
        Red
        Yellow
        Green
    End Enum

    <DefaultEvent("StateChanged")>
    Public Class TrafficLight
        Inherits Control

        Private _state As TrafficLightState = TrafficLightState.Red

        Public Event StateChanged As EventHandler

        Public Sub New()
            DoubleBuffered = True
            Size = New Size(70, 180)
            BackColor = Color.FromArgb(45, 45, 45)
        End Sub

        <Category("Dados")>
        Public Property State As TrafficLightState
            Get
                Return _state
            End Get
            Set(value As TrafficLightState)
                _state = value
                Invalidate()
                RaiseEvent StateChanged(Me, EventArgs.Empty)
            End Set
        End Property

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            e.Graphics.Clear(BackColor)
            Dim colors As Color() = {
                If(_state = TrafficLightState.Red, Color.Red, Color.FromArgb(70, 20, 20)),
                If(_state = TrafficLightState.Yellow, Color.Gold, Color.FromArgb(70, 60, 15)),
                If(_state = TrafficLightState.Green, Color.LimeGreen, Color.FromArgb(15, 65, 20))
            }
            For i As Integer = 0 To 2
                Using b As New SolidBrush(colors(i))
                    e.Graphics.FillEllipse(b, 12, 10 + i * 55, 46, 46)
                End Using
            Next
        End Sub
    End Class

    <DefaultEvent("ValueChanged")>
    Public Class SevenSegmentDigit
        Inherits Control

        Private _value As Integer = 0
        Private _inactiveColor As Color = Color.FromArgb(55, 15, 15)

        Public Event ValueChanged As EventHandler

        Public Sub New()
            DoubleBuffered = True
            BackColor = Color.Black
            ForeColor = Color.Red
            Size = New Size(65, 105)
        End Sub

        <Category("Dados")>
        Public Property Value As Integer
            Get
                Return _value
            End Get
            Set(value As Integer)
                _value = Math.Max(0, Math.Min(9, value))
                Invalidate()
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End Set
        End Property

        <Category("Aparência")>
        Public Property InactiveColor As Color
            Get
                Return _inactiveColor
            End Get
            Set(value As Color)
                _inactiveColor = value
                Invalidate()
            End Set
        End Property

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            e.Graphics.Clear(BackColor)
            Dim mask As Integer() = {63, 6, 91, 79, 102, 109, 125, 7, 127, 111}
            Dim m As Integer = mask(_value)
            Dim t As Single = Math.Max(4.0F, Math.Min(Width, Height) / 9.0F)
            Dim left As Single = 8
            Dim top As Single = 6
            Dim right As Single = Width - 8
            Dim bottom As Single = Height - 6
            Dim middle As Single = Height / 2.0F
            Dim segs As RectangleF() = {
                New RectangleF(left + t, top, right - left - 2 * t, t),
                New RectangleF(right - t, top + t, t, middle - top - t * 1.5F),
                New RectangleF(right - t, middle + t / 2, t, bottom - middle - t * 1.5F),
                New RectangleF(left + t, bottom - t, right - left - 2 * t, t),
                New RectangleF(left, middle + t / 2, t, bottom - middle - t * 1.5F),
                New RectangleF(left, top + t, t, middle - top - t * 1.5F),
                New RectangleF(left + t, middle - t / 2, right - left - 2 * t, t)
            }

            For i As Integer = 0 To 6
                Using b As New SolidBrush(If((m And (1 << i)) <> 0, ForeColor, _inactiveColor))
                    e.Graphics.FillRectangle(b, segs(i))
                End Using
            Next
        End Sub
    End Class

    <DefaultEvent("ValueChanged")>
    Public Class ArduinoPin
        Inherits Control

        Private _pinNumber As Integer = 13
        Private _value As Integer
        Private _analog As Boolean

        Public Event ValueChanged As EventHandler

        Public Sub New()
            DoubleBuffered = True
            Size = New Size(120, 52)
        End Sub

        <Category("Arduino")>
        Public Property PinNumber As Integer
            Get
                Return _pinNumber
            End Get
            Set(value As Integer)
                _pinNumber = Math.Max(0, value)
                Invalidate()
            End Set
        End Property

        <Category("Arduino")>
        Public Property Analog As Boolean
            Get
                Return _analog
            End Get
            Set(value As Boolean)
                _analog = value
                If _analog AndAlso _value > 1023 Then
                    _value = 1023
                ElseIf Not _analog AndAlso _value > 1 Then
                    _value = 1
                End If
                Invalidate()
            End Set
        End Property

        <Category("Arduino")>
        Public Property Value As Integer
            Get
                Return _value
            End Get
            Set(value As Integer)
                _value = Math.Max(0, Math.Min(If(_analog, 1023, 1), value))
                Invalidate()
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End Set
        End Property

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            e.Graphics.Clear(BackColor)
            Dim title As String = If(_analog, "A", "D") & _pinNumber.ToString()
            TextRenderer.DrawText(e.Graphics, title, Font, New Rectangle(4, 4, 42, 20), ForeColor)
            Dim r As New Rectangle(50, 8, Math.Max(1, Width - 56), 22)
            e.Graphics.DrawRectangle(Pens.Gray, r)
            Dim maxValue As Integer = If(_analog, 1023, 1)
            Dim w As Integer = CInt((r.Width - 2) * _value / CDbl(maxValue))
            e.Graphics.FillRectangle(Brushes.LimeGreen, r.X + 1, r.Y + 1, Math.Max(0, w), r.Height - 1)
            TextRenderer.DrawText(e.Graphics, _value.ToString(), Font, New Rectangle(4, 28, Width - 8, 20), ForeColor)
        End Sub
    End Class

    <DefaultEvent("ValueChanged")>
    Public Class IoTSensor
        Inherits Control

        Private _sensorName As String = "Sensor"
        Private _unit As String = "°C"
        Private _value As Double

        Public Event ValueChanged As EventHandler

        Public Sub New()
            DoubleBuffered = True
            Size = New Size(190, 90)
            BackColor = Color.FromArgb(245, 245, 245)
        End Sub

        <Category("IoT")>
        Public Property SensorName As String
            Get
                Return _sensorName
            End Get
            Set(value As String)
                _sensorName = If(value, "")
                Invalidate()
            End Set
        End Property

        <Category("IoT")>
        Public Property Unit As String
            Get
                Return _unit
            End Get
            Set(value As String)
                _unit = If(value, "")
                Invalidate()
            End Set
        End Property

        <Category("IoT")>
        Public Property Value As Double
            Get
                Return _value
            End Get
            Set(value As Double)
                _value = value
                Invalidate()
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End Set
        End Property

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            e.Graphics.Clear(BackColor)
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, Color.Silver, ButtonBorderStyle.Solid)
            TextRenderer.DrawText(e.Graphics, _sensorName, Font, New Rectangle(8, 8, Width - 16, 20), ForeColor)
            Using f As New Font(Font.FontFamily, 22, FontStyle.Bold)
                TextRenderer.DrawText(e.Graphics, _value.ToString("0.##") & " " & _unit, f, New Rectangle(8, 30, Width - 16, 48), ForeColor, TextFormatFlags.Left Or TextFormatFlags.VerticalCenter)
            End Using
        End Sub
    End Class
    <DefaultEvent("TagsChanged")> Public Class TagInput
        Inherits UserControl
        Private ReadOnly flow As New FlowLayoutPanel()
        Private ReadOnly inputBox As New TextBox()
        Private ReadOnly hint As New ToolTip()
        Private ReadOnly tagList As New List(Of String)
        Private _tagColor As Color = Color.FromArgb(226, 236, 255)
        Private _placeholder As String = "Digite e pressione Enter..."
        Public Event TagsChanged As EventHandler
        Public Sub New()
            Size = New Size(240, 34)
            BackColor = Color.White
            BorderStyle = BorderStyle.FixedSingle
            flow.Dock = DockStyle.Fill
            flow.AutoScroll = True
            flow.WrapContents = True
            flow.Padding = New Padding(3)
            inputBox.BorderStyle = BorderStyle.None
            inputBox.Width = 110
            inputBox.Margin = New Padding(3, 5, 3, 3)
            AddHandler inputBox.KeyDown, AddressOf InputKeyDown
            flow.Controls.Add(inputBox)
            Controls.Add(flow)
            hint.SetToolTip(inputBox, _placeholder)
        End Sub
        <Category("Dados"), Description("Tags separadas por vírgula")> Public Property Tags As String
            Get
                Return String.Join(", ", tagList)
            End Get
            Set(newValue As String)
                ClearTags()
                If Not String.IsNullOrWhiteSpace(newValue) Then
                    For Each part As String In newValue.Split(","c)
                        Dim trimmed As String = part.Trim()
                        If trimmed <> "" Then AddTagChip(trimmed)
                    Next
                End If
            End Set
        End Property
        <Category("Aparência")> Public Property Placeholder As String
            Get
                Return _placeholder
            End Get
            Set(newValue As String)
                _placeholder = If(newValue, "")
                hint.SetToolTip(inputBox, _placeholder)
            End Set
        End Property
        <Category("Aparência")> Public Property TagColor As Color
            Get
                Return _tagColor
            End Get
            Set(newValue As Color)
                _tagColor = newValue
                For Each chip As Control In flow.Controls.Cast(Of Control)().Where(Function(c) Not ReferenceEquals(c, inputBox))
                    chip.BackColor = _tagColor
                Next
            End Set
        End Property
        <Browsable(False)> Public ReadOnly Property TagCount As Integer
            Get
                Return tagList.Count
            End Get
        End Property
        Public Function GetTags() As String()
            Return tagList.ToArray()
        End Function
        Public Sub ClearTags()
            For Each chip As Control In flow.Controls.Cast(Of Control)().Where(Function(c) Not ReferenceEquals(c, inputBox)).ToList()
                flow.Controls.Remove(chip)
                chip.Dispose()
            Next
            tagList.Clear()
        End Sub
        Private Sub InputKeyDown(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Enter AndAlso inputBox.Text.Trim() <> "" Then
                AddTagChip(inputBox.Text.Trim())
                inputBox.Clear()
                e.SuppressKeyPress = True
            ElseIf e.KeyCode = Keys.Back AndAlso inputBox.Text = "" AndAlso tagList.Count > 0 Then
                Dim lastIndex As Integer = flow.Controls.Count - 2
                If lastIndex >= 0 Then
                    Dim lastChip As Panel = TryCast(flow.Controls(lastIndex), Panel)
                    If lastChip IsNot Nothing Then RemoveTagChip(lastChip, tagList(tagList.Count - 1))
                End If
            End If
        End Sub
        Private Sub AddTagChip(text As String)
            Dim chip As New Panel With {.BackColor = _tagColor, .Height = 22, .Margin = New Padding(3), .Padding = New Padding(6, 2, 4, 2), .AutoSize = True, .AutoSizeMode = AutoSizeMode.GrowAndShrink}
            Dim tagLabel As New Label With {.Text = text, .AutoSize = True, .TextAlign = ContentAlignment.MiddleLeft, .Margin = New Padding(0), .Font = Font}
            Dim closeLabel As New Label With {.Text = "×", .AutoSize = True, .Cursor = Cursors.Hand, .Margin = New Padding(6, 0, 0, 0), .ForeColor = Color.DimGray, .Font = New Font(Font, FontStyle.Bold)}
            AddHandler closeLabel.Click, Sub() RemoveTagChip(chip, text)
            Dim inner As New FlowLayoutPanel With {.FlowDirection = FlowDirection.LeftToRight, .AutoSize = True, .AutoSizeMode = AutoSizeMode.GrowAndShrink, .WrapContents = False}
            inner.Controls.Add(tagLabel) : inner.Controls.Add(closeLabel)
            chip.Controls.Add(inner)
            flow.Controls.Add(chip)
            flow.Controls.SetChildIndex(inputBox, flow.Controls.Count - 1)
            tagList.Add(text)
            RaiseEvent TagsChanged(Me, EventArgs.Empty)
        End Sub
        Private Sub RemoveTagChip(chip As Panel, text As String)
            flow.Controls.Remove(chip)
            chip.Dispose()
            tagList.Remove(text)
            RaiseEvent TagsChanged(Me, EventArgs.Empty)
        End Sub
    End Class

    <DefaultEvent("SelectedIndexChanged")> Public Class TabStripCustom
        Inherits Control
        Private _tabsText As String = "Aba 1, Aba 2, Aba 3"
        Private _selectedIndex As Integer = 0
        Private _selectedColor As Color = Color.DodgerBlue
        Private _tabBackColor As Color = Color.WhiteSmoke
        Private _hoverIndex As Integer = -1
        Public Event SelectedIndexChanged As EventHandler
        Public Sub New()
            Size = New Size(320, 36)
            BackColor = Color.Gainsboro
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint Or ControlStyles.ResizeRedraw Or ControlStyles.StandardClick, True)
            Cursor = Cursors.Hand
        End Sub
        <Category("Dados"), Description("Nomes das abas separados por vírgula")> Public Property Tabs As String
            Get
                Return _tabsText
            End Get
            Set(newValue As String)
                _tabsText = If(newValue, "")
                Dim count As Integer = TabNames().Length
                If _selectedIndex >= count Then _selectedIndex = Math.Max(0, count - 1)
                Invalidate()
            End Set
        End Property
        <Category("Comportamento")> Public Property SelectedIndex As Integer
            Get
                Return _selectedIndex
            End Get
            Set(newValue As Integer)
                Dim count As Integer = TabNames().Length
                Dim limited As Integer = Math.Max(0, Math.Min(count - 1, newValue))
                If limited = _selectedIndex Then Return
                _selectedIndex = limited
                Invalidate()
                RaiseEvent SelectedIndexChanged(Me, EventArgs.Empty)
            End Set
        End Property
        <Category("Aparência")> Public Property SelectedColor As Color
            Get
                Return _selectedColor
            End Get
            Set(newValue As Color)
                _selectedColor = newValue
                Invalidate()
            End Set
        End Property
        <Category("Aparência")> Public Property TabBackColor As Color
            Get
                Return _tabBackColor
            End Get
            Set(newValue As Color)
                _tabBackColor = newValue
                Invalidate()
            End Set
        End Property
        <Browsable(False)> Public ReadOnly Property SelectedTabText As String
            Get
                Dim names As String() = TabNames()
                Return If(_selectedIndex >= 0 AndAlso _selectedIndex < names.Length, names(_selectedIndex), "")
            End Get
        End Property
        Private Function TabNames() As String()
            Return _tabsText.Split(","c).Select(Function(s) s.Trim()).Where(Function(s) s <> "").ToArray()
        End Function
        Protected Overrides Sub OnMouseClick(e As MouseEventArgs)
            MyBase.OnMouseClick(e)
            Dim index As Integer = TabIndexAt(e.X)
            If index >= 0 Then SelectedIndex = index
        End Sub
        Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
            MyBase.OnMouseMove(e)
            Dim index As Integer = TabIndexAt(e.X)
            If index <> _hoverIndex Then
                _hoverIndex = index
                Invalidate()
            End If
        End Sub
        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            MyBase.OnMouseLeave(e)
            _hoverIndex = -1
            Invalidate()
        End Sub
        Private Function TabIndexAt(x As Integer) As Integer
            Dim names As String() = TabNames()
            If names.Length = 0 Then Return -1
            Dim tabWidth As Single = Width / CSng(names.Length)
            Dim index As Integer = CInt(Math.Floor(x / tabWidth))
            If index < 0 OrElse index >= names.Length Then Return -1
            Return index
        End Function
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.Clear(BackColor)
            Dim names As String() = TabNames()
            If names.Length = 0 Then Return
            Dim tabWidth As Single = Width / CSng(names.Length)
            For index As Integer = 0 To names.Length - 1
                Dim bounds As New RectangleF(index * tabWidth, 0, tabWidth, Height)
                Dim isSelected As Boolean = index = _selectedIndex
                Dim backColor As Color = If(isSelected, Color.White, If(index = _hoverIndex, ControlPaint.Light(_tabBackColor), _tabBackColor))
                Using brush As New SolidBrush(backColor)
                    e.Graphics.FillRectangle(brush, bounds)
                End Using
                Dim textColor As Color = If(isSelected, _selectedColor, ForeColor)
                Dim tabFont As Font = If(isSelected, New Font(Font, FontStyle.Bold), Font)
                Try
                    TextRenderer.DrawText(e.Graphics, names(index), tabFont, Rectangle.Round(bounds), textColor, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
                Finally
                    If isSelected Then tabFont.Dispose()
                End Try
                If isSelected Then
                    Using indicatorPen As New Pen(_selectedColor, 3)
                        e.Graphics.DrawLine(indicatorPen, bounds.Left + 4, Height - 2, bounds.Right - 4, Height - 2)
                    End Using
                End If
            Next
            Using borderPen As New Pen(Color.Gainsboro)
                e.Graphics.DrawLine(borderPen, 0, Height - 1, Width, Height - 1)
            End Using
        End Sub
    End Class

    <DefaultEvent("JsonParsed")> Public Class JsonTreeViewer
        Inherits UserControl
        Private ReadOnly tree As New TreeView()
        Private _jsonText As String = "{""nome"": ""FlowForge"", ""versao"": 1, ""ativo"": true, ""tags"": [""educacao"", ""ide""]}"
        Private _lastError As String = ""
        Public Event JsonParsed As EventHandler
        Public Event JsonError As EventHandler
        Public Sub New()
            Size = New Size(260, 220)
            tree.Dock = DockStyle.Fill
            tree.BorderStyle = BorderStyle.None
            tree.HideSelection = False
            Controls.Add(tree)
            RefreshTree()
        End Sub
        <Category("Dados"), Description("Texto no formato JSON a ser exibido em árvore")> Public Property JsonText As String
            Get
                Return _jsonText
            End Get
            Set(newValue As String)
                _jsonText = If(newValue, "")
                RefreshTree()
            End Set
        End Property
        <Category("Comportamento")> Public Property ShowRootLines As Boolean
            Get
                Return tree.ShowRootLines
            End Get
            Set(newValue As Boolean)
                tree.ShowRootLines = newValue
            End Set
        End Property
        <Browsable(False)> Public ReadOnly Property LastError As String
            Get
                Return _lastError
            End Get
        End Property
        <Browsable(False)> Public ReadOnly Property IsValid As Boolean
            Get
                Return _lastError = ""
            End Get
        End Property
        Public Sub RefreshTree()
            tree.Nodes.Clear()
            _lastError = ""
            Try
                Dim parser As New MiniJsonParser(_jsonText)
                Dim root As Object = parser.Parse()
                Dim rootNode As New TreeNode("json")
                BuildNode(rootNode, root)
                tree.Nodes.Add(rootNode)
                rootNode.Expand()
                RaiseEvent JsonParsed(Me, EventArgs.Empty)
            Catch ex As Exception
                _lastError = ex.Message
                tree.Nodes.Add(New TreeNode("Erro: " & ex.Message))
                RaiseEvent JsonError(Me, EventArgs.Empty)
            End Try
        End Sub
        Private Sub BuildNode(parent As TreeNode, value As Object)
            Dim dictionaryValue As Dictionary(Of String, Object) = TryCast(value, Dictionary(Of String, Object))
            If dictionaryValue IsNot Nothing Then
                For Each pair As KeyValuePair(Of String, Object) In dictionaryValue
                    Dim child As New TreeNode(pair.Key)
                    BuildNode(child, pair.Value)
                    parent.Nodes.Add(child)
                Next
                Return
            End If
            Dim listValue As List(Of Object) = TryCast(value, List(Of Object))
            If listValue IsNot Nothing Then
                For index As Integer = 0 To listValue.Count - 1
                    Dim child As New TreeNode("[" & index.ToString() & "]")
                    BuildNode(child, listValue(index))
                    parent.Nodes.Add(child)
                Next
                Return
            End If
            parent.Text &= ": " & FormatScalar(value)
        End Sub
        Private Function FormatScalar(value As Object) As String
            If value Is Nothing Then Return "null"
            If TypeOf value Is Boolean Then Return CBool(value).ToString().ToLowerInvariant()
            If TypeOf value Is Double Then Return CDbl(value).ToString(Globalization.CultureInfo.InvariantCulture)
            Return ChrW(34) & value.ToString() & ChrW(34)
        End Function

        Private Class MiniJsonParser
            Private ReadOnly text As String
            Private position As Integer
            Public Sub New(source As String)
                text = If(source, "")
                position = 0
            End Sub
            Public Function Parse() As Object
                SkipWhitespace()
                Dim result As Object = ParseValue()
                SkipWhitespace()
                Return result
            End Function
            Private Sub SkipWhitespace()
                While position < text.Length AndAlso Char.IsWhiteSpace(text(position))
                    position += 1
                End While
            End Sub
            Private Function ParseValue() As Object
                SkipWhitespace()
                If position >= text.Length Then Throw New FormatException("JSON incompleto.")
                Select Case text(position)
                    Case "{"c : Return ParseObject()
                    Case "["c : Return ParseArray()
                    Case ChrW(34) : Return ParseString()
                    Case "t"c, "f"c : Return ParseBoolean()
                    Case "n"c : Return ParseNull()
                    Case Else : Return ParseNumber()
                End Select
            End Function
            Private Function ParseObject() As Dictionary(Of String, Object)
                Dim result As New Dictionary(Of String, Object)
                position += 1
                SkipWhitespace()
                If position < text.Length AndAlso text(position) = "}"c Then
                    position += 1
                    Return result
                End If
                Do
                    SkipWhitespace()
                    Dim key As String = ParseString()
                    SkipWhitespace()
                    If position >= text.Length OrElse text(position) <> ":"c Then Throw New FormatException("Esperado ':' em um objeto JSON.")
                    position += 1
                    Dim value As Object = ParseValue()
                    result(key) = value
                    SkipWhitespace()
                    If position >= text.Length Then Throw New FormatException("Objeto JSON não foi fechado.")
                    If text(position) = ","c Then
                        position += 1
                    ElseIf text(position) = "}"c Then
                        position += 1
                        Exit Do
                    Else
                        Throw New FormatException("Caractere inesperado em objeto JSON.")
                    End If
                Loop
                Return result
            End Function
            Private Function ParseArray() As List(Of Object)
                Dim result As New List(Of Object)
                position += 1
                SkipWhitespace()
                If position < text.Length AndAlso text(position) = "]"c Then
                    position += 1
                    Return result
                End If
                Do
                    Dim value As Object = ParseValue()
                    result.Add(value)
                    SkipWhitespace()
                    If position >= text.Length Then Throw New FormatException("Lista JSON não foi fechada.")
                    If text(position) = ","c Then
                        position += 1
                    ElseIf text(position) = "]"c Then
                        position += 1
                        Exit Do
                    Else
                        Throw New FormatException("Caractere inesperado em lista JSON.")
                    End If
                Loop
                Return result
            End Function
            Private Function ParseString() As String
                If text(position) <> ChrW(34) Then Throw New FormatException("Esperado texto entre aspas.")
                position += 1
                Dim builder As New System.Text.StringBuilder()
                While position < text.Length AndAlso text(position) <> ChrW(34)
                    Dim currentChar As Char = text(position)
                    If currentChar = "\"c AndAlso position + 1 < text.Length Then
                        position += 1
                        Select Case text(position)
                            Case ChrW(34) : builder.Append(ChrW(34))
                            Case "\"c : builder.Append("\"c)
                            Case "/"c : builder.Append("/"c)
                            Case "n"c : builder.Append(ChrW(10))
                            Case "r"c : builder.Append(ChrW(13))
                            Case "t"c : builder.Append(ChrW(9))
                            Case "b"c : builder.Append(ChrW(8))
                            Case "f"c : builder.Append(ChrW(12))
                            Case "u"c
                                Dim hex As String = text.Substring(position + 1, 4)
                                builder.Append(ChrW(Convert.ToInt32(hex, 16)))
                                position += 4
                            Case Else : builder.Append(text(position))
                        End Select
                    Else
                        builder.Append(currentChar)
                    End If
                    position += 1
                End While
                If position >= text.Length Then Throw New FormatException("Texto JSON não foi fechado.")
                position += 1
                Return builder.ToString()
            End Function
            Private Function ParseNumber() As Double
                Dim start As Integer = position
                While position < text.Length AndAlso ("0123456789+-.eE".IndexOf(text(position)) >= 0)
                    position += 1
                End While
                Dim token As String = text.Substring(start, position - start)
                Dim result As Double = 0
                If Not Double.TryParse(token, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, result) Then Throw New FormatException("Número inválido: " & token)
                Return result
            End Function
            Private Function ParseBoolean() As Boolean
                If text.Substring(position).StartsWith("true", StringComparison.Ordinal) Then
                    position += 4
                    Return True
                End If
                If text.Substring(position).StartsWith("false", StringComparison.Ordinal) Then
                    position += 5
                    Return False
                End If
                Throw New FormatException("Valor booleano inválido.")
            End Function
            Private Function ParseNull() As Object
                If text.Substring(position).StartsWith("null", StringComparison.Ordinal) Then
                    position += 4
                    Return Nothing
                End If
                Throw New FormatException("Valor nulo inválido.")
            End Function
        End Class
    End Class

    ' =========================================================
    ' ToastNotification — aviso temporário que aparece e some
    ' sozinho depois de alguns segundos.
    ' =========================================================
    Public Enum ToastKind
        Info
        Success
        Warning
        [Error]
    End Enum

    <DefaultEvent("Dismissed")> Public Class ToastNotification
        Inherits Panel
        Private ReadOnly hideTimer As New Timer()
        Private ReadOnly messageLabel As New Label()
        Private _kind As ToastKind = ToastKind.Info

        Public Event Dismissed As EventHandler

        Public Sub New()
            Size = New Size(280, 48)
            hideTimer.Interval = 2500
            AddHandler hideTimer.Tick, AddressOf HideTimerTick
            messageLabel.Dock = DockStyle.Fill
            messageLabel.TextAlign = ContentAlignment.MiddleLeft
            messageLabel.Padding = New Padding(12, 0, 12, 0)
            messageLabel.ForeColor = Color.White
            messageLabel.Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
            messageLabel.Text = "Notificação"
            Controls.Add(messageLabel)
            AtualizarCorPeloTipo()
        End Sub

        <Category("Aparência")> Public Property Kind As ToastKind
            Get
                Return _kind
            End Get
            Set(newValue As ToastKind)
                _kind = newValue
                AtualizarCorPeloTipo()
            End Set
        End Property

        <Category("Comportamento"), DefaultValue(2500), Description("Tempo em milissegundos até o aviso desaparecer sozinho")>
        Public Property DurationMs As Integer
            Get
                Return hideTimer.Interval
            End Get
            Set(newValue As Integer)
                hideTimer.Interval = Math.Max(500, newValue)
            End Set
        End Property

        <Category("Dados")> Public Property Message As String
            Get
                Return messageLabel.Text
            End Get
            Set(newValue As String)
                messageLabel.Text = newValue
            End Set
        End Property

        ' Mostra o aviso e agenda o desaparecimento automático. Chame isso a
        ' partir do código do formulário, por exemplo: Toast1.Show("Salvo com sucesso!", ToastKind.Success)
        Public Overloads Sub Show(texto As String, Optional tipo As ToastKind = ToastKind.Info)
            Kind = tipo
            Message = texto
            Visible = True
            BringToFront()
            hideTimer.Stop()
            hideTimer.Start()
        End Sub

        Public Sub Dismiss()
            hideTimer.Stop()
            Visible = False
            RaiseEvent Dismissed(Me, EventArgs.Empty)
        End Sub

        Private Sub HideTimerTick(sender As Object, e As EventArgs)
            Dismiss()
        End Sub

        Private Sub AtualizarCorPeloTipo()
            Select Case _kind
                Case ToastKind.Success : BackColor = Color.FromArgb(60, 170, 90)
                Case ToastKind.Warning : BackColor = Color.FromArgb(230, 160, 30)
                Case ToastKind.[Error] : BackColor = Color.FromArgb(210, 60, 60)
                Case Else : BackColor = Color.FromArgb(50, 110, 200)
            End Select
        End Sub

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then hideTimer.Dispose()
            MyBase.Dispose(disposing)
        End Sub
    End Class

    ' =========================================================
    ' Accordion — painéis expansíveis empilhados. Cada seção é
    ' "Título::Conteúdo", uma por linha, na propriedade Sections.
    ' =========================================================
    <DefaultEvent("SectionToggled")> Public Class Accordion
        Inherits Panel
        Private _sectionsText As String = "Pergunta 1::Resposta ou conteúdo da primeira seção.;;Pergunta 2::Resposta ou conteúdo da segunda seção."
        Private _headerColor As Color = Color.FromArgb(230, 236, 245)
        Private _headerHoverColor As Color = Color.FromArgb(214, 224, 240)
        Private _accentColor As Color = Color.FromArgb(50, 110, 200)
        Private ReadOnly headers As New List(Of Panel)
        Private ReadOnly setas As New List(Of Label)
        Private ReadOnly bodies As New List(Of Label)
        Private _expandedIndex As Integer = -1

        Public Event SectionToggled As EventHandler

        Public Sub New()
            AutoScroll = True
            BackColor = Color.White
            BorderStyle = BorderStyle.FixedSingle
            Size = New Size(260, 220)
            RebuildSections()
        End Sub

        <Category("Dados"), Description("Seções separadas por ;; — cada uma no formato Título::Conteúdo")>
        Public Property Sections As String
            Get
                Return _sectionsText
            End Get
            Set(newValue As String)
                _sectionsText = If(newValue, "")
                _expandedIndex = -1
                RebuildSections()
            End Set
        End Property

        <Category("Aparência")> Public Property HeaderColor As Color
            Get
                Return _headerColor
            End Get
            Set(newValue As Color)
                _headerColor = newValue
                RebuildSections()
            End Set
        End Property

        <Category("Aparência")> Public Property AccentColor As Color
            Get
                Return _accentColor
            End Get
            Set(newValue As Color)
                _accentColor = newValue
                RebuildSections()
            End Set
        End Property

        <Browsable(False)> Public ReadOnly Property ExpandedIndex As Integer
            Get
                Return _expandedIndex
            End Get
        End Property

        Public Sub ExpandSection(indice As Integer)
            If indice < 0 OrElse indice >= headers.Count Then Return
            If _expandedIndex <> indice Then ToggleSection(indice)
        End Sub

        Public Sub CollapseAll()
            _expandedIndex = -1
            ReposicionarSecoes()
        End Sub

        Private Sub RebuildSections()
            SuspendLayout()
            Controls.Clear()
            headers.Clear()
            setas.Clear()
            bodies.Clear()
            Dim linhas() As String = _sectionsText.Split(New String() {";;"}, StringSplitOptions.None)
            For Each linhaBruta As String In linhas
                Dim linha As String = linhaBruta.Trim()
                If linha = "" Then Continue For
                Dim partes() As String = linha.Split(New String() {"::"}, 2, StringSplitOptions.None)
                Dim titulo As String = partes(0)
                Dim conteudo As String = If(partes.Length > 1, partes(1), "")

                Dim header As New Panel With {.Size = New Size(Math.Max(1, Width), 32), .BackColor = _headerColor, .Cursor = Cursors.Hand, .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right}
                Dim seta As New Label With {.Text = "+", .Dock = DockStyle.Right, .Width = 30, .TextAlign = ContentAlignment.MiddleCenter, .Font = New Font(Font, FontStyle.Bold), .ForeColor = _accentColor}
                Dim rotulo As New Label With {.Text = titulo, .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft, .Padding = New Padding(10, 0, 0, 0), .Font = New Font(Font, FontStyle.Bold)}
                header.Controls.Add(seta)
                header.Controls.Add(rotulo)

                Dim corpo As New Label With {.Size = New Size(Math.Max(1, Width), 0), .AutoSize = False, .Padding = New Padding(12, 8, 12, 8), .BackColor = Color.White, .Visible = False, .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right, .Text = conteudo}

                Dim indiceCapturado As Integer = headers.Count
                AddHandler header.Click, Sub() ToggleSection(indiceCapturado)
                AddHandler rotulo.Click, Sub() ToggleSection(indiceCapturado)
                AddHandler seta.Click, Sub() ToggleSection(indiceCapturado)
                AddHandler header.MouseEnter, Sub() header.BackColor = _headerHoverColor
                AddHandler header.MouseLeave, Sub() header.BackColor = _headerColor

                Controls.Add(header)
                Controls.Add(corpo)
                headers.Add(header)
                setas.Add(seta)
                bodies.Add(corpo)
            Next
            ReposicionarSecoes()
            ResumeLayout()
        End Sub

        Private Sub ToggleSection(indice As Integer)
            If indice < 0 OrElse indice >= bodies.Count Then Return
            _expandedIndex = If(_expandedIndex = indice, -1, indice)
            ReposicionarSecoes()
            RaiseEvent SectionToggled(Me, EventArgs.Empty)
        End Sub

        Private Sub ReposicionarSecoes()
            Dim y As Integer = 0
            For indice As Integer = 0 To headers.Count - 1
                headers(indice).Top = y
                setas(indice).Text = If(indice = _expandedIndex, "−", "+")
                y += headers(indice).Height
                Dim corpo As Label = bodies(indice)
                If indice = _expandedIndex Then
                    Using g As Graphics = CreateGraphics()
                        Dim largura As Integer = Math.Max(40, Width - 24)
                        Dim tamanho As SizeF = g.MeasureString(corpo.Text, corpo.Font, largura)
                        corpo.Height = CInt(tamanho.Height) + 20
                    End Using
                    corpo.Visible = True
                Else
                    corpo.Height = 0
                    corpo.Visible = False
                End If
                corpo.Top = y
                y += corpo.Height
            Next
        End Sub
    End Class

    ' =========================================================
    ' ProgressStepper — indicador de progresso em etapas (1→2→3...),
    ' útil pra wizards e formulários em múltiplas telas.
    ' =========================================================
    <DefaultEvent("StepChanged")> Public Class ProgressStepper
        Inherits Control
        Private _stepsText As String = "Início, Detalhes, Revisão, Concluído"
        Private _currentStep As Integer = 0
        Private _accentColor As Color = Color.FromArgb(50, 150, 90)
        Private ReadOnly _pendingColor As Color = Color.Gainsboro

        Public Event StepChanged As EventHandler

        Public Sub New()
            Size = New Size(360, 60)
            SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint Or ControlStyles.ResizeRedraw, True)
            BackColor = Color.White
        End Sub

        <Category("Dados"), Description("Nomes das etapas separados por vírgula")> Public Property Steps As String
            Get
                Return _stepsText
            End Get
            Set(newValue As String)
                _stepsText = If(newValue, "")
                Dim total As Integer = StepNames().Length
                If _currentStep >= total Then _currentStep = Math.Max(0, total - 1)
                Invalidate()
            End Set
        End Property

        <Category("Comportamento")> Public Property CurrentStep As Integer
            Get
                Return _currentStep
            End Get
            Set(newValue As Integer)
                Dim total As Integer = StepNames().Length
                Dim limitado As Integer = Math.Max(0, Math.Min(total - 1, newValue))
                If limitado = _currentStep Then Return
                _currentStep = limitado
                Invalidate()
                RaiseEvent StepChanged(Me, EventArgs.Empty)
            End Set
        End Property

        <Category("Aparência")> Public Property AccentColor As Color
            Get
                Return _accentColor
            End Get
            Set(newValue As Color)
                _accentColor = newValue
                Invalidate()
            End Set
        End Property

        Public Sub NextStep()
            CurrentStep += 1
        End Sub

        Public Sub PreviousStep()
            CurrentStep -= 1
        End Sub

        Private Function StepNames() As String()
            Return _stepsText.Split(","c).Select(Function(s) s.Trim()).Where(Function(s) s <> "").ToArray()
        End Function

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            e.Graphics.Clear(BackColor)
            Dim nomes As String() = StepNames()
            If nomes.Length = 0 Then Return
            Dim raio As Integer = 12
            Dim margem As Integer = 30
            Dim centroY As Integer = 20
            Dim larguraUtil As Single = Math.Max(1, Width - margem * 2)
            For indice As Integer = 0 To nomes.Length - 1
                Dim centroX As Single = margem + If(nomes.Length = 1, 0, indice * larguraUtil / (nomes.Length - 1))
                If indice < nomes.Length - 1 Then
                    Dim proximoX As Single = margem + If(nomes.Length = 1, 0, (indice + 1) * larguraUtil / (nomes.Length - 1))
                    Dim corLinha As Color = If(indice < _currentStep, _accentColor, _pendingColor)
                    Using linha As New Pen(corLinha, 3)
                        e.Graphics.DrawLine(linha, centroX + raio, centroY, proximoX - raio, centroY)
                    End Using
                End If
                Dim concluido As Boolean = indice < _currentStep
                Dim atual As Boolean = indice = _currentStep
                Dim corCirculo As Color = If(concluido OrElse atual, _accentColor, _pendingColor)
                Using preenchimento As New SolidBrush(corCirculo)
                    e.Graphics.FillEllipse(preenchimento, centroX - raio, centroY - raio, raio * 2, raio * 2)
                End Using
                Dim texto As String = If(concluido, "✓", (indice + 1).ToString())
                Using fonteNumero As New Font(Font, FontStyle.Bold)
                    Dim tamanho As SizeF = e.Graphics.MeasureString(texto, fonteNumero)
                    Using corTexto As New SolidBrush(Color.White)
                        e.Graphics.DrawString(texto, fonteNumero, corTexto, centroX - tamanho.Width / 2, centroY - tamanho.Height / 2)
                    End Using
                End Using
                Using fonteRotulo As New Font(Font.FontFamily, 8)
                    Dim corRotulo As Color = If(atual, _accentColor, Color.Gray)
                    Dim tamanhoRotulo As SizeF = e.Graphics.MeasureString(nomes(indice), fonteRotulo)
                    Using corTextoRotulo As New SolidBrush(corRotulo)
                        e.Graphics.DrawString(nomes(indice), fonteRotulo, corTextoRotulo, centroX - tamanhoRotulo.Width / 2, centroY + raio + 6)
                    End Using
                End Using
            Next
        End Sub
    End Class

    ' =========================================================
    ' TerminalView — console/terminal com histórico e linha de
    ' comando opcional. Combina com o tema Arduino/IoT do projeto
    ' (monitor serial, logs, etc.)
    ' =========================================================
    <DefaultEvent("CommandEntered")> Public Class TerminalView
        Inherits UserControl
        Private ReadOnly output As New RichTextBox()
        Private ReadOnly inputBox As New TextBox()
        Private ReadOnly promptLabel As New Label()
        Private ReadOnly inputLine As New Panel()
        Private _prompt As String = ">"

        Public Event CommandEntered As EventHandler(Of TerminalCommandEventArgs)

        Public Sub New()
            Size = New Size(400, 220)
            BackColor = Color.Black

            output.Dock = DockStyle.Fill
            output.BackColor = Color.Black
            output.ForeColor = Color.FromArgb(80, 220, 100)
            output.Font = New Font("Consolas", 10)
            output.BorderStyle = BorderStyle.None
            output.ReadOnly = True
            output.WordWrap = True
            output.ScrollBars = RichTextBoxScrollBars.Vertical

            inputLine.Dock = DockStyle.Bottom
            inputLine.Height = 26
            inputLine.BackColor = Color.FromArgb(20, 20, 20)
            promptLabel.Text = _prompt
            promptLabel.Dock = DockStyle.Left
            promptLabel.Width = 18
            promptLabel.ForeColor = Color.FromArgb(80, 220, 100)
            promptLabel.Font = New Font("Consolas", 10, FontStyle.Bold)
            promptLabel.TextAlign = ContentAlignment.MiddleCenter
            inputBox.Dock = DockStyle.Fill
            inputBox.BackColor = Color.FromArgb(20, 20, 20)
            inputBox.ForeColor = Color.White
            inputBox.Font = New Font("Consolas", 10)
            inputBox.BorderStyle = BorderStyle.None
            AddHandler inputBox.KeyDown, AddressOf InputKeyDown
            inputLine.Controls.Add(inputBox)
            inputLine.Controls.Add(promptLabel)

            Controls.Add(output)
            Controls.Add(inputLine)
        End Sub

        <Category("Comportamento"), DefaultValue(True)> Public Property ShowInputLine As Boolean
            Get
                Return inputLine.Visible
            End Get
            Set(newValue As Boolean)
                inputLine.Visible = newValue
            End Set
        End Property

        <Category("Aparência")> Public Property Prompt As String
            Get
                Return _prompt
            End Get
            Set(newValue As String)
                _prompt = If(newValue, "")
                promptLabel.Text = _prompt
            End Set
        End Property

        <Category("Aparência")> Public Property TextColor As Color
            Get
                Return output.ForeColor
            End Get
            Set(newValue As Color)
                output.ForeColor = newValue
            End Set
        End Property

        <Category("Aparência")> Public Property TerminalFont As Font
            Get
                Return output.Font
            End Get
            Set(newValue As Font)
                If newValue Is Nothing Then Return
                output.Font = newValue
                inputBox.Font = newValue
                promptLabel.Font = newValue
            End Set
        End Property

        Public Sub WriteLine(Optional texto As String = "")
            WriteLine(texto, output.ForeColor)
        End Sub

        Public Sub WriteLine(texto As String, cor As Color)
            output.SelectionStart = output.TextLength
            output.SelectionColor = cor
            output.AppendText(texto & Environment.NewLine)
            output.SelectionStart = output.TextLength
            output.ScrollToCaret()
        End Sub

        Public Sub ClearScreen()
            output.Clear()
        End Sub

        Private Sub InputKeyDown(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Enter Then
                Dim comando As String = inputBox.Text
                inputBox.Clear()
                WriteLine(_prompt & " " & comando, Color.White)
                RaiseEvent CommandEntered(Me, New TerminalCommandEventArgs(comando))
                e.SuppressKeyPress = True
            End If
        End Sub
    End Class

    Public NotInheritable Class TerminalCommandEventArgs
        Inherits System.EventArgs

        Private ReadOnly _command As String

        Public Sub New(commandText As String)
            _command = If(commandText, String.Empty)
        End Sub

        Public ReadOnly Property Command As String
            Get
                Return _command
            End Get
        End Property
    End Class

    ' GlyphImageList: 480 glyphs em 16 categorias, renderizados por GDI+.
    Public Enum GlyphTheme
        Classic
        Office2003
        Windows9x
        Modern
        Dark
        Monochrome
    End Enum
    Public Enum GlyphType
        File_New
        File_Open
        File_OpenFolder
        File_Save
        File_SaveAs
        File_SaveAll
        File_Close
        File_CloseAll
        File_Import
        File_Export
        File_Print
        File_PrintPreview
        File_PageSetup
        File_Properties
        File_Info
        File_Archive
        File_Extract
        File_Upload
        File_Download
        File_Cloud
        File_CloudUpload
        File_CloudDownload
        File_Recent
        File_Favorite
        File_Lock
        File_Unlock
        File_Refresh
        File_Sync
        File_Search
        File_Replace
        Edit_Cut
        Edit_Copy
        Edit_Paste
        Edit_PasteSpecial
        Edit_Delete
        Edit_Clear
        Edit_Undo
        Edit_Redo
        Edit_Repeat
        Edit_Select
        Edit_SelectAll
        Edit_Find
        Edit_FindNext
        Edit_FindPrevious
        Edit_Replace
        Edit_Duplicate
        Edit_Move
        Edit_Resize
        Edit_Crop
        Edit_RotateLeft
        Edit_RotateRight
        Edit_FlipHorizontal
        Edit_FlipVertical
        Edit_Group
        Edit_Ungroup
        Edit_BringFront
        Edit_SendBack
        Edit_AlignLeft
        Edit_AlignCenter
        Edit_AlignRight
        Text_Bold
        Text_Italic
        Text_Underline
        Text_Strikeout
        Text_Font
        Text_FontSize
        Text_TextColor
        Text_Highlight
        Text_AlignLeft
        Text_AlignCenter
        Text_AlignRight
        Text_Justify
        Text_Indent
        Text_Outdent
        Text_Bullets
        Text_Numbering
        Text_Superscript
        Text_Subscript
        Text_Uppercase
        Text_Lowercase
        Text_TitleCase
        Text_Paragraph
        Text_LineSpacing
        Text_SortAZ
        Text_SortZA
        Text_SpellCheck
        Text_Comment
        Text_Quote
        Text_Code
        Text_Link
        Navigation_Home
        Navigation_Back
        Navigation_Forward
        Navigation_Up
        Navigation_Down
        Navigation_Left
        Navigation_Right
        Navigation_First
        Navigation_Last
        Navigation_Previous
        Navigation_Next
        Navigation_Expand
        Navigation_Collapse
        Navigation_ChevronUp
        Navigation_ChevronDown
        Navigation_ChevronLeft
        Navigation_ChevronRight
        Navigation_ArrowUp
        Navigation_ArrowDown
        Navigation_ArrowLeft
        Navigation_ArrowRight
        Navigation_Refresh
        Navigation_Reload
        Navigation_History
        Navigation_Map
        Navigation_Location
        Navigation_Compass
        Navigation_Route
        Navigation_Bookmark
        Navigation_Pin
        UI_Add
        UI_Remove
        UI_Plus
        UI_Minus
        UI_Check
        UI_Cancel
        UI_Close
        UI_Menu
        UI_More
        UI_Settings
        UI_Tools
        UI_Filter
        UI_Sort
        UI_Grid
        UI_List
        UI_Details
        UI_Tiles
        UI_Window
        UI_Windows
        UI_Fullscreen
        UI_Restore
        UI_Minimize
        UI_Maximize
        UI_Dock
        UI_Undock
        UI_Pin
        UI_Unpin
        UI_Eye
        UI_EyeOff
        UI_Help
        Image_Image
        Image_Photo
        Image_Camera
        Image_Scanner
        Image_Gallery
        Image_Crop
        Image_Resize
        Image_Rotate
        Image_Flip
        Image_Brightness
        Image_Contrast
        Image_Saturation
        Image_Hue
        Image_Color
        Image_Palette
        Image_Brush
        Image_Pencil
        Image_Eraser
        Image_Fill
        Image_Picker
        Image_Line
        Image_Rectangle
        Image_Ellipse
        Image_Polygon
        Image_Curve
        Image_Text
        Image_Layers
        Image_LayerAdd
        Image_LayerRemove
        Image_Transparency
        Media_Play
        Media_Pause
        Media_Stop
        Media_Record
        Media_Rewind
        Media_FastForward
        Media_Previous
        Media_Next
        Media_Volume
        Media_VolumeUp
        Media_VolumeDown
        Media_Mute
        Media_Music
        Media_Microphone
        Media_Headphones
        Media_Video
        Media_Film
        Media_Playlist
        Media_Shuffle
        Media_Repeat
        Media_Eject
        Media_Radio
        Media_Podcast
        Media_Speaker
        Media_Equalizer
        Media_Subtitles
        Media_Stream
        Media_Cast
        Media_Webcam
        Media_Snapshot
        Network_Wifi
        Network_WifiOff
        Network_Ethernet
        Network_Internet
        Network_Globe
        Network_Router
        Network_Modem
        Network_Server
        Network_Client
        Network_Computer
        Network_Laptop
        Network_Phone
        Network_Tablet
        Network_Cloud
        Network_CloudSync
        Network_Upload
        Network_Download
        Network_Link
        Network_Unlink
        Network_Connect
        Network_Disconnect
        Network_Signal
        Network_Antenna
        Network_Bluetooth
        Network_Hotspot
        Network_VPN
        Network_Firewall
        Network_Ping
        Network_Port
        Network_NetworkMap
        Hardware_CPU
        Hardware_RAM
        Hardware_GPU
        Hardware_HDD
        Hardware_SSD
        Hardware_USB
        Hardware_Keyboard
        Hardware_Mouse
        Hardware_Monitor
        Hardware_Printer
        Hardware_Scanner
        Hardware_Camera
        Hardware_Battery
        Hardware_BatteryCharging
        Hardware_Power
        Hardware_Fan
        Hardware_Temperature
        Hardware_Chip
        Hardware_Board
        Hardware_Cable
        Hardware_Plug
        Hardware_Socket
        Hardware_MemoryCard
        Hardware_OpticalDrive
        Hardware_Gamepad
        Hardware_Joystick
        Hardware_Touch
        Hardware_Sensor
        Hardware_Clock
        Hardware_Device
        Database_Database
        Database_DatabaseAdd
        Database_DatabaseRemove
        Database_Table
        Database_TableAdd
        Database_TableRemove
        Database_Column
        Database_Row
        Database_Key
        Database_PrimaryKey
        Database_ForeignKey
        Database_Relation
        Database_Query
        Database_SQL
        Database_Filter
        Database_Sort
        Database_Import
        Database_Export
        Database_Backup
        Database_Restore
        Database_Connect
        Database_Disconnect
        Database_Server
        Database_Transaction
        Database_Commit
        Database_Rollback
        Database_Index
        Database_View
        Database_Procedure
        Database_Data
        Development_Code
        Development_Brackets
        Development_Terminal
        Development_Console
        Development_Run
        Development_Debug
        Development_Build
        Development_Compile
        Development_Stop
        Development_Breakpoint
        Development_Bug
        Development_Package
        Development_Library
        Development_Reference
        Development_Class
        Development_Module
        Development_Function
        Development_Variable
        Development_Constant
        Development_Event
        Development_Property
        Development_Method
        Development_Form
        Development_Control
        Development_Component
        Development_Designer
        Development_Project
        Development_Solution
        Development_Git
        Development_Branch
        IoT_Arduino
        IoT_RaspberryPi
        IoT_GPIO
        IoT_Pin
        IoT_Digital
        IoT_Analog
        IoT_PWM
        IoT_Serial
        IoT_UART
        IoT_I2C
        IoT_SPI
        IoT_CAN
        IoT_Sensor
        IoT_Actuator
        IoT_Relay
        IoT_Motor
        IoT_Servo
        IoT_Stepper
        IoT_LED
        IoT_RGB
        IoT_Display
        IoT_LCD
        IoT_Matrix
        IoT_Temperature
        IoT_Humidity
        IoT_Pressure
        IoT_Light
        IoT_Distance
        IoT_RFID
        IoT_Bluetooth
        Security_Lock
        Security_Unlock
        Security_Key
        Security_Shield
        Security_ShieldCheck
        Security_ShieldAlert
        Security_Password
        Security_User
        Security_UserAdd
        Security_UserRemove
        Security_Login
        Security_Logout
        Security_Fingerprint
        Security_Certificate
        Security_Encrypt
        Security_Decrypt
        Security_Firewall
        Security_VPN
        Security_Private
        Security_Public
        Security_Permission
        Security_Admin
        Security_Warning
        Security_Blocked
        Security_Allowed
        Security_Eye
        Security_EyeOff
        Security_Token
        Security_Secure
        Security_Audit
        System_Windows
        System_Desktop
        System_Folder
        System_Process
        System_Service
        System_Task
        System_Clock
        System_Calendar
        System_Date
        System_Time
        System_Settings
        System_ControlPanel
        System_Registry
        System_Command
        System_Power
        System_Restart
        System_Shutdown
        System_Sleep
        System_Hibernate
        System_Update
        System_Install
        System_Uninstall
        System_Download
        System_Upload
        System_Language
        System_Accessibility
        System_Notification
        System_Clipboard
        System_Trash
        System_Recycle
        Status_Info
        Status_Success
        Status_Warning
        Status_Error
        Status_Question
        Status_Help
        Status_Online
        Status_Offline
        Status_Busy
        Status_Away
        Status_New
        Status_Updated
        Status_Pending
        Status_Running
        Status_Stopped
        Status_Paused
        Status_Completed
        Status_Failed
        Status_Favorite
        Status_Star
        Status_Heart
        Status_Flag
        Status_Bell
        Status_BellOff
        Status_Message
        Status_Mail
        Status_MailOpen
        Status_Calendar
        Status_Clock
        Status_Alert
        Office_Document
        Office_Spreadsheet
        Office_Presentation
        Office_PDF
        Office_Text
        Office_RTF
        Office_CSV
        Office_XML
        Office_JSON
        Office_HTML
        Office_Word
        Office_Excel
        Office_PowerPoint
        Office_Chart
        Office_PieChart
        Office_BarChart
        Office_LineChart
        Office_Calculator
        Office_Calendar
        Office_Contacts
        Office_Mail
        Office_Envelope
        Office_Attachment
        Office_Paperclip
        Office_Signature
        Office_Stamp
        Office_Folder
        Office_Briefcase
        Office_Clipboard
        Office_Notes
    End Enum

    <ToolboxItem(True), DefaultProperty("Theme")>
    Public Class GlyphImageList
        Inherits System.ComponentModel.Component
        Private _theme As GlyphTheme = GlyphTheme.Modern
        Private _glyphSize As Integer = 16
        Private ReadOnly _imageList As New System.Windows.Forms.ImageList()

        Public Sub New()
            _imageList.ColorDepth = ColorDepth.Depth32Bit
            _imageList.ImageSize = New Size(16, 16)
            Rebuild()
        End Sub

        <Browsable(False)>
        Public ReadOnly Property ImageList As System.Windows.Forms.ImageList
            Get
                Return _imageList
            End Get
        End Property

        <Browsable(False)>
        Public ReadOnly Property Images As ImageList.ImageCollection
            Get
                Return _imageList.Images
            End Get
        End Property
        <Category("Glyphs")> Public Property Theme As GlyphTheme
            Get
                Return _theme
            End Get
            Set(value As GlyphTheme)
                If _theme <> value Then _theme = value : Rebuild()
            End Set
        End Property
        <Category("Glyphs"), DefaultValue(16)> Public Property GlyphSize As Integer
            Get
                Return _glyphSize
            End Get
            Set(value As Integer)
                Dim n As Integer = Math.Max(8, Math.Min(128, value))
                If n <> _glyphSize Then _glyphSize = n : _imageList.ImageSize = New Size(n, n) : Rebuild()
            End Set
        End Property
        <Browsable(False)> Public ReadOnly Property GlyphCount As Integer
            Get
                Return [Enum].GetValues(GetType(GlyphType)).Length
            End Get
        End Property
        Public Sub Rebuild()
            Images.Clear()
            For Each v As GlyphType In [Enum].GetValues(GetType(GlyphType))
                Images.Add(v.ToString(), GlyphRenderer.Render(v, _glyphSize, _theme))
            Next
        End Sub
        Public Function GetGlyph(v As GlyphType) As Image
            Return Images(v.ToString())
        End Function
        Public Function GetGlyph(key As String) As Image
            If String.IsNullOrWhiteSpace(key) Then Return Nothing
            Dim k As String = key.Replace(".", "_")
            Return If(Images.ContainsKey(k), Images(k), Nothing)
        End Function
        Public Function GetGlyph(v As GlyphType, size As Integer) As Image
            Return GlyphRenderer.Render(v, size, _theme)
        End Function
        Public Shared Function CategoryOf(v As GlyphType) As String
            Dim n As String = v.ToString()
            Return n.Substring(0, n.IndexOf("_"c))
        End Function
        Public Shared Function Search(text As String) As GlyphType()
            Dim q As String = If(text, String.Empty)
            Return [Enum].GetValues(GetType(GlyphType)).Cast(Of GlyphType)().Where(Function(v) v.ToString().IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToArray()
        End Function
    End Class

    Public NotInheritable Class GlyphRenderer
        Private Sub New()
        End Sub
        Public Shared Function Render(v As GlyphType, size As Integer, theme As GlyphTheme) As Bitmap
            Dim s As Integer = Math.Max(8, Math.Min(256, size))
            Dim bmp As New Bitmap(s, s, System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.Clear(Color.Transparent)
                g.SmoothingMode = If(theme = GlyphTheme.Windows9x OrElse theme = GlyphTheme.Classic, SmoothingMode.None, SmoothingMode.AntiAlias)
                DrawGlyph(g, New RectangleF(1, 1, s - 2, s - 2), v, theme)
            End Using
            Return bmp
        End Function
        Private Shared Sub DrawGlyph(g As Graphics, bounds As RectangleF, v As GlyphType, theme As GlyphTheme)
            Dim full As String = v.ToString()
            Dim name As String = full.Substring(full.IndexOf("_"c) + 1)
            Dim cat As String = GlyphImageList.CategoryOf(v)
            Dim fg As Color = If(theme = GlyphTheme.Dark, Color.Gainsboro, Color.FromArgb(45, 55, 65))
            Dim ac As Color = If(theme = GlyphTheme.Office2003, Color.FromArgb(49, 106, 197), If(theme = GlyphTheme.Dark, Color.DeepSkyBlue, Color.FromArgb(0, 120, 215)))
            If theme = GlyphTheme.Monochrome Then ac = Color.DimGray
            Using p As New Pen(fg, Math.Max(1.0F, bounds.Width / 12.0F)), pa As New Pen(ac, Math.Max(1.0F, bounds.Width / 12.0F)), b As New SolidBrush(fg), ba As New SolidBrush(ac)
                p.StartCap = LineCap.Round : p.EndCap = LineCap.Round
                pa.StartCap = LineCap.Round : pa.EndCap = LineCap.Round
                Select Case name
                    Case "Save", "SaveAs", "SaveAll"
                        g.FillRectangle(ba, GlyphRect(bounds,.12F,.08F,.76F,.84F)) : g.FillRectangle(Brushes.White,GlyphRect(bounds,.27F,.12F,.42F,.25F)) : g.DrawRectangle(p,Rectangle.Round(GlyphRect(bounds,.26F,.56F,.48F,.27F)))
                    Case "Open", "OpenFolder", "Folder"
                        g.DrawRectangle(p,Rectangle.Round(GlyphRect(bounds,.08F,.28F,.84F,.58F))) : g.FillRectangle(ba,GlyphRect(bounds,.12F,.16F,.38F,.22F))
                    Case "Search", "Find", "FindNext", "FindPrevious"
                        g.DrawEllipse(p,GlyphRect(bounds,.10F,.10F,.54F,.54F)) : g.DrawLine(p,GlyphPoint(bounds,.56F,.56F),GlyphPoint(bounds,.88F,.88F))
                    Case "Add", "Plus"
                        g.DrawLine(p,GlyphPoint(bounds,.18F,.5F),GlyphPoint(bounds,.82F,.5F)) : g.DrawLine(p,GlyphPoint(bounds,.5F,.18F),GlyphPoint(bounds,.5F,.82F))
                    Case "Check", "Success", "Completed", "Commit", "Allowed"
                        g.DrawLines(pa,New PointF(){GlyphPoint(bounds,.12F,.52F),GlyphPoint(bounds,.40F,.80F),GlyphPoint(bounds,.88F,.18F)})
                    Case "Error", "Cancel", "Close", "Failed", "Blocked"
                        g.DrawLine(pa,GlyphPoint(bounds,.20F,.20F),GlyphPoint(bounds,.80F,.80F)) : g.DrawLine(pa,GlyphPoint(bounds,.80F,.20F),GlyphPoint(bounds,.20F,.80F))
                    Case "Play", "Run"
                        g.FillPolygon(ba,New PointF(){GlyphPoint(bounds,.28F,.15F),GlyphPoint(bounds,.82F,.50F),GlyphPoint(bounds,.28F,.85F)})
                    Case "Pause", "Paused"
                        g.FillRectangle(ba,GlyphRect(bounds,.24F,.18F,.17F,.64F)) : g.FillRectangle(ba,GlyphRect(bounds,.59F,.18F,.17F,.64F))
                    Case "Stop", "Stopped"
                        g.FillRectangle(ba,GlyphRect(bounds,.22F,.22F,.56F,.56F))
                    Case "Wifi", "Signal", "Antenna"
                        g.DrawArc(p,GlyphRect(bounds,.10F,.18F,.80F,.70F),210,120) : g.DrawArc(p,GlyphRect(bounds,.26F,.36F,.48F,.42F),210,120) : g.FillEllipse(ba,GlyphRect(bounds,.44F,.72F,.12F,.12F))
                    Case "Terminal", "Console", "Command"
                        g.DrawRectangle(p,Rectangle.Round(GlyphRect(bounds,.08F,.12F,.84F,.76F))) : g.DrawLines(pa,New PointF(){GlyphPoint(bounds,.20F,.34F),GlyphPoint(bounds,.36F,.50F),GlyphPoint(bounds,.20F,.66F)})
                    Case Else
                        DrawFallback(g,bounds,cat,name,p,b,ba)
                End Select
            End Using
        End Sub
        Private Shared Sub DrawFallback(g As Graphics, bounds As RectangleF,cat As String,name As String,p As Pen,b As Brush,ba As Brush)
            If cat = "Status" OrElse cat = "Network" Then
                g.DrawEllipse(p,GlyphRect(bounds,.08F,.08F,.84F,.84F))
            Else
                g.DrawRectangle(p,Rectangle.Round(GlyphRect(bounds,.08F,.08F,.84F,.84F)))
            End If
            Dim letters As String = Initials(name)
            Using f As New Font("Segoe UI",Math.Max(5.0F,bounds.Width*.22F),FontStyle.Bold,GraphicsUnit.Pixel), sf As New StringFormat()
                sf.Alignment=StringAlignment.Center : sf.LineAlignment=StringAlignment.Center
                g.DrawString(letters,f,b,bounds,sf)
            End Using
        End Sub
        Private Shared Function Initials(name As String) As String
            Dim s As String=""
            For Each c As Char In name
                If Char.IsUpper(c) OrElse Char.IsDigit(c) Then s &= c
                If s.Length=2 Then Exit For
            Next
            If s.Length=0 Then s=name.Substring(0,Math.Min(2,name.Length)).ToUpperInvariant()
            Return s
        End Function
        Private Shared Function GlyphRect(bounds As RectangleF, x As Single, y As Single, w As Single, h As Single) As RectangleF
            Return New RectangleF(bounds.X + bounds.Width * x, bounds.Y + bounds.Height * y, bounds.Width * w, bounds.Height * h)
        End Function
        Private Shared Function GlyphPoint(bounds As RectangleF, x As Single, y As Single) As PointF
            Return New PointF(bounds.X + bounds.Width * x, bounds.Y + bounds.Height * y)
        End Function
    End Class

End Namespace
