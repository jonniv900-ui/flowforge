Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports Microsoft.VisualBasic

Namespace FlowForgeStudio
    Public Class VbEditor
        Inherits RichTextBox
        Public Event CaretPositionChanged(sender As Object, line As Integer, column As Integer)
        Public Event ViewportChanged(sender As Object, e As EventArgs)
        Private coloring As Boolean
        Private ReadOnly delay As New Timer With {.Interval = 250}
        Private ReadOnly completionList As New ListBox()
        Private ReadOnly lineNumbers As New Panel()
        Private ReadOnly projectSuggestions As New List(Of String)()
        Private ReadOnly projectSymbols As New Dictionary(Of String, Type)(StringComparer.OrdinalIgnoreCase)
        Private regularCodeFont As Font
        Private boldCodeFont As Font
        Private italicCodeFont As Font
        Private Const WM_SETREDRAW As Integer = &HB
        Private Const WM_VSCROLL As Integer = &H115
        Private Const WM_MOUSEWHEEL As Integer = &H20A
        Private Const EM_GETFIRSTVISIBLELINE As Integer = &HCE
        Private Const EM_SETMARGINS As Integer = &HD3
        Private Const EM_LINESCROLL As Integer = &HB6
        <DllImport("user32.dll", CharSet:=CharSet.Auto)>
        Private Shared Function SendMessage(hWnd As IntPtr, message As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
        End Function
        Private Shared ReadOnly completionWords As String() = {
            "AddHandler", "AddressOf", "And", "AndAlso", "As", "Boolean", "Button", "ByRef", "ByVal", "Call", "Case", "Catch", "CheckedListBox", "Class", "Close", "Color", "ColorComboBox", "ColorDialog", "ComboBox", "Const", "Continue", "Date", "DateTime", "DateTimePicker", "Decimal", "Dim", "DirectCast", "Do", "DomainUpDown", "Double", "Each", "Else", "ElseIf", "End", "End Class", "End Function", "End If", "End Select", "End Sub", "End Try", "End Using", "End While", "Enum", "Event", "EventArgs", "Exit", "False", "File", "File.Exists", "File.ReadAllText", "File.WriteAllText", "Finally", "FlowLayoutPanel", "FolderBrowserDialog", "Font", "FontDialog", "For", "Form", "Friend", "Function", "GroupBox", "Handles", "HScrollBar", "HttpClient", "If", "In", "Inherits", "Integer", "Interface", "Is", "IsNot", "Label", "LinkLabel", "List", "ListBox", "ListView", "Long", "Loop", "MaskedTextBox", "Me", "MessageBox", "MessageBox.Show", "Module", "MonthCalendar", "MouseEventArgs", "MyBase", "Namespace", "New", "Next", "Not", "Nothing", "NumericUpDown", "Object", "OpenFileDialog", "Or", "OrElse", "Overrides", "Panel", "Partial", "PictureBox", "PrintDialog", "PrintPreviewDialog", "Private", "ProgressBar", "Protected", "Public", "RadioButton", "RaiseEvent", "ReadOnly", "Return", "RichTextBox", "SaveFileDialog", "Select", "Select Case", "SerialConnection", "SerialPort", "Shared", "Short", "Show", "ShowDialog", "Single", "SplitContainer", "String", "Sub", "SyncLock", "TabControl", "TableLayoutPanel", "Then", "Throw", "To", "ToolTip", "NotifyIcon", "ErrorProvider", "ImageList", "BindingSource", "HelpProvider", "ContextMenuStrip", "PropertyGrid", "BindingNavigator", "Splitter", "TrackBar", "TreeView", "True", "Try", "TryCast", "Using", "VScrollBar", "WebBrowser", "While", "With", "ImageButton", "SearchBox", "PasswordBox", "IPAddressBox", "ModernDatePicker", "SimpleChart", "VirtualJoystick", "LcdDisplay", "LedMatrix", "TrafficLight", "SevenSegmentDigit", "ArduinoPin", "IoTSensor", "WithEvents"}
        Private Shared ReadOnly keywords As New Regex("\b(Imports|Namespace|Class|Module|Public|Private|Protected|Friend|Shared|Sub|Function|End|If|Then|Else|ElseIf|Select|Case|For|Each|Next|While|Do|Loop|Try|Catch|Finally|Throw|Return|Exit|Continue|Dim|As|New|Nothing|True|False|And|AndAlso|Or|OrElse|Not|Handles|WithEvents|ByVal|ByRef|Async|Await|Using|Inherits|Me)\b", RegexOptions.IgnoreCase Or RegexOptions.Compiled)
        Private Shared ReadOnly types As New Regex("\b(String|Integer|Long|Double|Decimal|Boolean|Date|Object|EventArgs|MouseEventArgs|KeyEventArgs|MessageBox|Form|Button|TextBox|HttpClient|File)\b", RegexOptions.IgnoreCase Or RegexOptions.Compiled)
        Private Shared ReadOnly methods As New Regex("\b[A-Za-z_]\w*(?=\s*\()", RegexOptions.Compiled)
        Public Sub New()
            Font = New Font("Consolas", 11.0F) : BackColor = Color.FromArgb(17, 19, 24) : ForeColor = Color.Gainsboro : BorderStyle = BorderStyle.None : AcceptsTab = True : WordWrap = False : DetectUrls = False
            RecreateHighlightFonts()
            ' A régua é hospedada externamente pelo MainForm. Um painel filho do
            ' RichEdit pode ser coberto pelo redesenho nativo do Windows.
            lineNumbers.Visible = False
            completionList.Visible = False : completionList.Width = 420 : completionList.Height = 190 : completionList.IntegralHeight = False : completionList.Font = New Font("Consolas", 10.0F)
            completionList.BackColor = Color.FromArgb(37, 40, 48) : completionList.ForeColor = Color.White : completionList.BorderStyle = BorderStyle.FixedSingle
            Controls.Add(completionList) : completionList.BringToFront()
            AddHandler delay.Tick, Sub(sender, e)
                                       delay.Stop()
                                       Colorize()
                                   End Sub
            AddHandler TextChanged, Sub(sender, e)
                                        RaiseEvent ViewportChanged(Me, EventArgs.Empty)
                                        If Not coloring Then delay.Stop() : delay.Start() : UpdateCompletion(False)
                                    End Sub
            AddHandler KeyDown, AddressOf EditorKeyDown
            AddHandler LostFocus, Sub() If Not completionList.Focused Then completionList.Visible = False
            AddHandler completionList.DoubleClick, Sub() AcceptCompletion()
        End Sub

        Protected Overrides Sub OnHandleCreated(e As EventArgs)
            MyBase.OnHandleCreated(e)
            ApplyTextMargin()
        End Sub

        Protected Overrides Sub OnFontChanged(e As EventArgs)
            MyBase.OnFontChanged(e)
            If lineNumbers Is Nothing Then Return
            RecreateHighlightFonts()
            ApplyTextMargin()
            RaiseEvent ViewportChanged(Me, EventArgs.Empty)
        End Sub

        Protected Overrides Sub OnResize(e As EventArgs)
            MyBase.OnResize(e)
            ApplyTextMargin()
            lineNumbers.Invalidate()
        End Sub

        Protected Overrides Sub OnSelectionChanged(e As EventArgs)
            MyBase.OnSelectionChanged(e)
            If coloring OrElse lineNumbers Is Nothing Then Return
            Dim lineIndex As Integer = GetLineFromCharIndex(SelectionStart)
            Dim firstCharacter As Integer = GetFirstCharIndexFromLine(lineIndex)
            RaiseEvent CaretPositionChanged(Me, lineIndex + 1, Math.Max(1, SelectionStart - firstCharacter + 1))
            RaiseEvent ViewportChanged(Me, EventArgs.Empty)
        End Sub

        Protected Overrides Sub WndProc(ByRef message As Message)
            MyBase.WndProc(message)
            If message.Msg = WM_VSCROLL OrElse message.Msg = WM_MOUSEWHEEL Then RaiseEvent ViewportChanged(Me, EventArgs.Empty)
        End Sub

        Private Sub ApplyTextMargin()
            If IsHandleCreated Then SendMessage(Handle, EM_SETMARGINS, New IntPtr(1), New IntPtr(6))
        End Sub

        Public ReadOnly Property FirstVisibleLine As Integer
            Get
                If Not IsHandleCreated Then Return 0
                Return Math.Max(0, SendMessage(Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero).ToInt32())
            End Get
        End Property

        Public Function GetLineTop(lineIndex As Integer) As Integer
            If lineIndex < 0 OrElse lineIndex >= Lines.Length Then Return Integer.MinValue
            Dim characterIndex As Integer = GetFirstCharIndexFromLine(lineIndex)
            If characterIndex < 0 Then Return Integer.MinValue
            Return GetPositionFromCharIndex(characterIndex).Y
        End Function

        Public Sub GoToEditorLine(lineIndex As Integer)
            If lineIndex < 0 OrElse lineIndex >= Lines.Length Then Return
            Dim characterIndex As Integer = GetFirstCharIndexFromLine(lineIndex)
            If characterIndex >= 0 Then
                Me.Select(characterIndex, 0)
                Me.ScrollToCaret()
                Me.Focus()
            End If
        End Sub

        Private Sub PaintLineNumbers(sender As Object, e As PaintEventArgs)
            e.Graphics.Clear(lineNumbers.BackColor)
            Using separator As New Pen(Color.FromArgb(55, 60, 70))
                e.Graphics.DrawLine(separator, lineNumbers.Width - 1, 0, lineNumbers.Width - 1, lineNumbers.Height)
            End Using
            If Not IsHandleCreated OrElse Lines.Length = 0 Then Return
            Dim firstLine As Integer = SendMessage(Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero).ToInt32()
            For line As Integer = Math.Max(0, firstLine) To Lines.Length - 1
                Dim characterIndex As Integer = GetFirstCharIndexFromLine(line)
                If characterIndex < 0 Then Exit For
                Dim position As Point = GetPositionFromCharIndex(characterIndex)
                If position.Y > ClientSize.Height Then Exit For
                TextRenderer.DrawText(e.Graphics, (line + 1).ToString(), Font, New Rectangle(0, position.Y, lineNumbers.Width - 7, Font.Height + 2), Color.FromArgb(125, 135, 150), TextFormatFlags.Right Or TextFormatFlags.NoPadding)
            Next
        End Sub

        Private Sub RecreateHighlightFonts()
            If regularCodeFont IsNot Nothing Then regularCodeFont.Dispose()
            If boldCodeFont IsNot Nothing Then boldCodeFont.Dispose()
            If italicCodeFont IsNot Nothing Then italicCodeFont.Dispose()
            regularCodeFont = New Font(Font, FontStyle.Regular)
            boldCodeFont = New Font(Font, FontStyle.Bold)
            italicCodeFont = New Font(Font, FontStyle.Italic)
        End Sub

        Public Sub SetProjectSuggestions(values As IEnumerable(Of String))
            projectSuggestions.Clear()
            If values IsNot Nothing Then projectSuggestions.AddRange(values.Where(Function(v) Not String.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase))
        End Sub

        Public Sub SetProjectSymbols(values As IDictionary(Of String, Type))
            projectSymbols.Clear()
            If values Is Nothing Then Return
            For Each pair In values
                If Not String.IsNullOrWhiteSpace(pair.Key) AndAlso pair.Value IsNot Nothing Then projectSymbols(pair.Key) = pair.Value
            Next
        End Sub

        Private Sub EditorKeyDown(sender As Object, e As KeyEventArgs)
            If e.Control AndAlso e.KeyCode = Keys.Space Then
                UpdateCompletion(True) : e.SuppressKeyPress = True : Return
            End If
            If e.KeyCode = Keys.Enter AndAlso Not completionList.Visible Then
                InsertIndentedNewLine() : e.SuppressKeyPress = True : Return
            End If
            If Not completionList.Visible Then Return
            Select Case e.KeyCode
                Case Keys.Down
                    If completionList.SelectedIndex < completionList.Items.Count - 1 Then completionList.SelectedIndex += 1
                    e.SuppressKeyPress = True
                Case Keys.Up
                    If completionList.SelectedIndex > 0 Then completionList.SelectedIndex -= 1
                    e.SuppressKeyPress = True
                Case Keys.Enter, Keys.Tab
                    AcceptCompletion() : e.SuppressKeyPress = True
                Case Keys.Escape
                    completionList.Visible = False : e.SuppressKeyPress = True
            End Select
        End Sub

        Private Sub InsertIndentedNewLine()
            Dim lineIndex As Integer = GetLineFromCharIndex(SelectionStart)
            Dim lineStart As Integer = GetFirstCharIndexFromLine(lineIndex)
            Dim beforeCaret As String = Text.Substring(Math.Max(0, lineStart), SelectionStart - Math.Max(0, lineStart))
            Dim indentation As String = Regex.Match(beforeCaret, "^\s*").Value.Replace(vbCr, "").Replace(vbLf, "")
            Dim trimmed As String = beforeCaret.TrimEnd()
            If Regex.IsMatch(trimmed, "(?i)(\bThen|\bElse|\bDo|\bTry|\bFor\b.*|\bWhile\b.*|\bUsing\b.*|\bWith\b.*|\bSelect\s+Case\b.*|\b(Sub|Function|Class|Module|Namespace)\b.*)$") Then indentation &= "    "
            SelectedText = vbCrLf & indentation
        End Sub

        Private Sub UpdateCompletion(force As Boolean)
            If coloring OrElse SelectionLength > 0 Then Return
            Dim prefix As String = CurrentWord()
            Dim textBeforeCaret As String = Text.Substring(0, SelectionStart)
            Dim memberMatch As Match = Regex.Match(textBeforeCaret, "(?i)([A-Za-z_]\w*)\.([A-Za-z_]\w*)?$")
            Dim candidates As Object()
            If memberMatch.Success Then
                prefix = memberMatch.Groups(2).Value
                Dim symbolType As Type = Nothing
                If projectSymbols.ContainsKey(memberMatch.Groups(1).Value) Then symbolType = projectSymbols(memberMatch.Groups(1).Value)
                If symbolType Is Nothing Then symbolType = ResolveLocalSymbolType(memberMatch.Groups(1).Value, textBeforeCaret)
                If symbolType IsNot Nothing Then
                    candidates = GetMembers(symbolType, prefix).Cast(Of Object).ToArray()
                Else
                    candidates = New Object() {}
                End If
            Else
                If Not force AndAlso prefix.Length < 2 Then completionList.Visible = False : Return
                candidates = completionWords.Concat(projectSuggestions).Where(Function(v) v.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) AndAlso Not v.Equals(prefix, StringComparison.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(Function(v) v).Take(80).Cast(Of Object).ToArray()
            End If
            If candidates.Length = 0 Then completionList.Visible = False : Return
            completionList.BeginUpdate() : completionList.Items.Clear() : completionList.Items.AddRange(candidates) : completionList.EndUpdate() : completionList.SelectedIndex = 0
            Dim location As Point = GetPositionFromCharIndex(SelectionStart)
            location.Y += Font.Height + 3
            If location.X + completionList.Width > ClientSize.Width Then location.X = Math.Max(0, ClientSize.Width - completionList.Width)
            If location.Y + completionList.Height > ClientSize.Height Then location.Y = Math.Max(0, location.Y - completionList.Height - Font.Height)
            completionList.Location = location : completionList.Visible = True : completionList.BringToFront()
        End Sub

        Private Shared Function ResolveLocalSymbolType(symbolName As String, sourceBeforeCaret As String) As Type
            Dim matches As MatchCollection = Regex.Matches(sourceBeforeCaret, "(?im)\b(?:Dim|Private|Public|Friend|Protected)\s+" & Regex.Escape(symbolName) & "\s+As\s+(?:New\s+)?([A-Za-z_][\w.]*)")
            If matches.Count = 0 Then Return Nothing
            Dim typeName As String = matches(matches.Count - 1).Groups(1).Value
            Dim candidates As String() = {typeName, "System." & typeName, "System.Drawing." & typeName, "System.IO." & typeName, "System.Windows.Forms." & typeName, "System.Net.Http." & typeName, "System.IO.Ports." & typeName, "System.Data." & typeName, "System.Data.SqlClient." & typeName}
            For Each candidate As String In candidates
                Dim resolved As Type = Type.GetType(candidate, False, True)
                If resolved IsNot Nothing Then Return resolved
                For Each assembly As Assembly In AppDomain.CurrentDomain.GetAssemblies()
                    resolved = assembly.GetType(candidate, False, True)
                    If resolved IsNot Nothing Then Return resolved
                Next
            Next
            Return Nothing
        End Function

        Private Shared Function GetMembers(type As Type, prefix As String) As MemberCompletionItem()
            Dim result As New List(Of MemberCompletionItem)()
            For Each propertyInfo As PropertyInfo In type.GetProperties(BindingFlags.Instance Or BindingFlags.Public)
                If propertyInfo.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) Then result.Add(New MemberCompletionItem(propertyInfo.Name, propertyInfo.Name, "Propriedade", propertyInfo.PropertyType.Name))
            Next
            For Each eventInfo As EventInfo In type.GetEvents(BindingFlags.Instance Or BindingFlags.Public)
                If eventInfo.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) Then result.Add(New MemberCompletionItem(eventInfo.Name, eventInfo.Name, "Evento", eventInfo.EventHandlerType.Name))
            Next
            For Each methodInfo As MethodInfo In type.GetMethods(BindingFlags.Instance Or BindingFlags.Public).Where(Function(m) Not m.IsSpecialName).GroupBy(Function(m) m.Name).Select(Function(g) g.OrderBy(Function(m) m.GetParameters().Length).First())
                If methodInfo.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) Then
                    Dim parameters As String = String.Join(", ", methodInfo.GetParameters().Take(4).Select(Function(p) p.Name & " As " & p.ParameterType.Name))
                    If methodInfo.GetParameters().Length > 4 Then parameters &= ", ..."
                    Dim insertion As String = If(methodInfo.GetParameters().Length = 0, methodInfo.Name & "()", methodInfo.Name & "(")
                    result.Add(New MemberCompletionItem(methodInfo.Name, insertion, "Método", parameters & ") As " & methodInfo.ReturnType.Name))
                End If
            Next
            Return result.GroupBy(Function(i) i.Kind & ":" & i.Name, StringComparer.OrdinalIgnoreCase).Select(Function(g) g.First()).OrderBy(Function(i) i.Name).ThenBy(Function(i) i.Kind).Take(150).ToArray()
        End Function

        Private Function CurrentWord() As String
            Dim start As Integer = SelectionStart
            While start > 0 AndAlso (Char.IsLetterOrDigit(Text(start - 1)) OrElse Text(start - 1) = "_"c) : start -= 1 : End While
            Return Text.Substring(start, SelectionStart - start)
        End Function

        Private Sub AcceptCompletion()
            If Not completionList.Visible OrElse completionList.SelectedItem Is Nothing Then Return
            Dim prefix As String = CurrentWord()
            Dim start As Integer = SelectionStart - prefix.Length
            Me.Select(start, prefix.Length)
            Dim member As MemberCompletionItem = TryCast(completionList.SelectedItem, MemberCompletionItem)
            SelectedText = If(member Is Nothing, CStr(completionList.SelectedItem), member.InsertText)
            completionList.Visible = False
            Focus()
        End Sub

        Private NotInheritable Class MemberCompletionItem
            Public ReadOnly Name As String
            Public ReadOnly InsertText As String
            Public ReadOnly Kind As String
            Public ReadOnly Detail As String
            Public Sub New(memberName As String, insertion As String, memberKind As String, memberDetail As String)
                Name = memberName : InsertText = insertion : Kind = memberKind : Detail = memberDetail
            End Sub
            Public Overrides Function ToString() As String
                Dim glyph As String = If(Kind = "Método", "ƒ", If(Kind = "Evento", "⚡", "▣"))
                Return glyph & "  " & Name & "    " & Detail
            End Function
        End Class
        Public Sub Colorize()
            If coloring Then Return
            coloring = True
            Dim selectionPosition As Integer = SelectionStart, selectionSize As Integer = SelectionLength
            Dim firstVisibleLine As Integer = If(IsHandleCreated, SendMessage(Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero).ToInt32(), 0)
            If IsHandleCreated Then SendMessage(Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero)
            Try
                SelectAll() : SelectionColor = Color.Gainsboro : SelectionFont = regularCodeFont
                PaintMatches(keywords, Color.FromArgb(86, 156, 214), boldCodeFont)
                PaintMatches(types, Color.FromArgb(78, 201, 176), regularCodeFont)
                PaintMatches(methods, Color.FromArgb(220, 220, 170), regularCodeFont)
                Dim quote As String = ChrW(34)
                PaintMatches(New Regex(quote & "(?:" & quote & quote & "|[^" & quote & "])*" & quote), Color.FromArgb(206, 145, 120), regularCodeFont)
                PaintMatches(New Regex("'.*$", RegexOptions.Multiline), Color.FromArgb(106, 153, 85), italicCodeFont)
                PaintMatches(New Regex("\b\d+(\.\d+)?\b"), Color.FromArgb(181, 206, 168), regularCodeFont)
            Finally
                SelectionStart = Math.Min(selectionPosition, TextLength)
                SelectionLength = Math.Min(selectionSize, TextLength - SelectionStart)
                Dim currentFirstLine As Integer = If(IsHandleCreated, SendMessage(Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero).ToInt32(), firstVisibleLine)
                If IsHandleCreated AndAlso currentFirstLine <> firstVisibleLine Then SendMessage(Handle, EM_LINESCROLL, IntPtr.Zero, New IntPtr(firstVisibleLine - currentFirstLine))
                If SelectionLength = 0 Then SelectionColor = Color.Gainsboro : SelectionFont = regularCodeFont
                If IsHandleCreated Then SendMessage(Handle, WM_SETREDRAW, New IntPtr(1), IntPtr.Zero)
                coloring = False : Invalidate() : lineNumbers.Invalidate()
            End Try
        End Sub
        Private Sub PaintMatches(regex As Regex, color As Color, highlightFont As Font)
            For Each match As Match In regex.Matches(Text)
                SelectionStart = match.Index : SelectionLength = match.Length : SelectionColor = color : SelectionFont = highlightFont
            Next
        End Sub

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                delay.Dispose()
                If regularCodeFont IsNot Nothing Then regularCodeFont.Dispose()
                If boldCodeFont IsNot Nothing Then boldCodeFont.Dispose()
                If italicCodeFont IsNot Nothing Then italicCodeFont.Dispose()
            End If
            MyBase.Dispose(disposing)
        End Sub
    End Class
End Namespace
