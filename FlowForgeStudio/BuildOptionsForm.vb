Imports System
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Windows.Forms

Namespace FlowForgeStudio
    Friend Class BuildOptionsForm
        Inherits Form
        Private ReadOnly iconBox As New TextBox()
        Private ReadOnly outputBox As New TextBox()
        Private ReadOnly titleBox As New TextBox()
        Private ReadOnly descriptionBox As New TextBox()
        Private ReadOnly companyBox As New TextBox()
        Private ReadOnly productBox As New TextBox()
        Private ReadOnly copyrightBox As New TextBox()
        Private ReadOnly trademarkBox As New TextBox()
        Private ReadOnly assemblyVersionBox As New TextBox()
        Private ReadOnly fileVersionBox As New TextBox()
        Private ReadOnly iconPreview As New PictureBox()

        Public ReadOnly Property OutputPath As String
            Get
                Return outputBox.Text.Trim()
            End Get
        End Property
        Public ReadOnly Property IconPath As String
            Get
                Return iconBox.Text.Trim()
            End Get
        End Property
        Public ReadOnly Property AssemblyTitleValue As String
            Get
                Return titleBox.Text.Trim()
            End Get
        End Property
        Public ReadOnly Property DescriptionValue As String
            Get
                Return descriptionBox.Text.Trim()
            End Get
        End Property
        Public ReadOnly Property CompanyValue As String
            Get
                Return companyBox.Text.Trim()
            End Get
        End Property
        Public ReadOnly Property ProductValue As String
            Get
                Return productBox.Text.Trim()
            End Get
        End Property
        Public ReadOnly Property CopyrightValue As String
            Get
                Return copyrightBox.Text.Trim()
            End Get
        End Property
        Public ReadOnly Property TrademarkValue As String
            Get
                Return trademarkBox.Text.Trim()
            End Get
        End Property
        Public ReadOnly Property AssemblyVersionValue As String
            Get
                Return assemblyVersionBox.Text.Trim()
            End Get
        End Property
        Public ReadOnly Property FileVersionValue As String
            Get
                Return fileVersionBox.Text.Trim()
            End Get
        End Property

        Public Sub New(project As FlowProject)
            Text = "Gerar executável" : Width = 700 : Height = 570 : MinimumSize = New Size(620, 520)
            StartPosition = FormStartPosition.CenterParent : FormBorderStyle = FormBorderStyle.Sizable
            titleBox.Text = ValueOr(project.AssemblyTitle, project.Name)
            descriptionBox.Text = ValueOr(project.AssemblyDescription, "Aplicativo criado com FlowForge Studio")
            companyBox.Text = ValueOr(project.AssemblyCompany, Environment.UserName)
            productBox.Text = ValueOr(project.AssemblyProduct, project.Name)
            copyrightBox.Text = ValueOr(project.AssemblyCopyright, "Copyright © " & DateTime.Now.Year)
            trademarkBox.Text = ValueOr(project.AssemblyTrademark, "")
            assemblyVersionBox.Text = ValueOr(project.AssemblyVersion, "1.0.0.0")
            fileVersionBox.Text = ValueOr(project.FileVersion, "1.0.0.0")
            iconBox.Text = ValueOr(project.ApplicationIcon, "")
            outputBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), SafeFileName(project.Name) & ".exe")
            BuildInterface() : UpdateIconPreview()
        End Sub

        Private Sub BuildInterface()
            Dim table As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 3, .RowCount = 11, .Padding = New Padding(12), .AutoScroll = True}
            table.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 155))
            table.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            table.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 95))
            AddRow(table, 0, "Arquivo executável:", outputBox, BrowseButton("Procurar...", AddressOf BrowseOutput))
            Dim iconPanel As New Panel With {.Dock = DockStyle.Fill}
            iconPreview.Size = New Size(32, 32) : iconPreview.SizeMode = PictureBoxSizeMode.Zoom : iconPreview.Dock = DockStyle.Left
            iconBox.Dock = DockStyle.Fill : iconPanel.Controls.Add(iconBox) : iconPanel.Controls.Add(iconPreview) : iconBox.BringToFront()
            AddRow(table, 1, "Ícone do aplicativo:", iconPanel, BrowseButton("Ícone...", AddressOf BrowseIcon))
            AddRow(table, 2, "Título:", titleBox, Nothing)
            AddRow(table, 3, "Descrição:", descriptionBox, Nothing)
            AddRow(table, 4, "Empresa:", companyBox, Nothing)
            AddRow(table, 5, "Produto:", productBox, Nothing)
            AddRow(table, 6, "Copyright:", copyrightBox, Nothing)
            AddRow(table, 7, "Marca registrada:", trademarkBox, Nothing)
            AddRow(table, 8, "Versão do assembly:", assemblyVersionBox, Nothing)
            AddRow(table, 9, "Versão do arquivo:", fileVersionBox, Nothing)
            Dim hint As New Label With {.Text = "Use versões com até quatro números, por exemplo 1.0.0.0. O ícone deve estar no formato ICO.", .Dock = DockStyle.Fill, .ForeColor = Color.DimGray, .AutoSize = True}
            table.Controls.Add(hint, 1, 10) : table.SetColumnSpan(hint, 2)
            For Each row As Integer In Enumerable.Range(0, 10) : table.RowStyles.Add(New RowStyle(SizeType.Absolute, 40)) : Next
            table.RowStyles.Add(New RowStyle(SizeType.AutoSize))

            Dim footer As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .Height = 52, .FlowDirection = FlowDirection.RightToLeft, .Padding = New Padding(8)}
            Dim generate As New Button With {.Text = "Gerar EXE", .Width = 110, .Height = 30}
            Dim cancel As New Button With {.Text = "Cancelar", .Width = 90, .Height = 30, .DialogResult = DialogResult.Cancel}
            AddHandler generate.Click, AddressOf ValidateAndClose
            footer.Controls.Add(generate) : footer.Controls.Add(cancel)
            Controls.Add(table) : Controls.Add(footer) : AcceptButton = generate : CancelButton = cancel
        End Sub

        Private Shared Sub AddRow(table As TableLayoutPanel, row As Integer, caption As String, editor As Control, browse As Control)
            Dim label As New Label With {.Text = caption, .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleRight}
            editor.Dock = DockStyle.Fill : table.Controls.Add(label, 0, row) : table.Controls.Add(editor, 1, row)
            If browse IsNot Nothing Then browse.Dock = DockStyle.Fill : table.Controls.Add(browse, 2, row)
        End Sub
        Private Shared Function BrowseButton(text As String, action As EventHandler) As Button
            Dim button As New Button With {.Text = text}
            AddHandler button.Click, action
            Return button
        End Function
        Private Sub BrowseOutput(sender As Object, e As EventArgs)
            Using dialog As New SaveFileDialog With {.Filter = "Aplicativo Windows (*.exe)|*.exe", .FileName = Path.GetFileName(outputBox.Text), .InitialDirectory = Path.GetDirectoryName(outputBox.Text)}
                If dialog.ShowDialog(Me) = DialogResult.OK Then outputBox.Text = dialog.FileName
            End Using
        End Sub
        Private Sub BrowseIcon(sender As Object, e As EventArgs)
            Using dialog As New OpenFileDialog With {.Filter = "Ícones Windows (*.ico)|*.ico"}
                If dialog.ShowDialog(Me) = DialogResult.OK Then iconBox.Text = dialog.FileName : UpdateIconPreview()
            End Using
        End Sub
        Private Sub UpdateIconPreview()
            If iconPreview.Image IsNot Nothing Then iconPreview.Image.Dispose() : iconPreview.Image = Nothing
            If Not File.Exists(iconBox.Text) Then Return
            Try
                Using icon As New Icon(iconBox.Text, 32, 32) : iconPreview.Image = icon.ToBitmap() : End Using
            Catch
            End Try
        End Sub
        Private Sub ValidateAndClose(sender As Object, e As EventArgs)
            If String.IsNullOrWhiteSpace(OutputPath) Then MessageBox.Show("Informe o destino do executável.") : Return
            If IconPath <> "" AndAlso (Not File.Exists(IconPath) OrElse Not IconPath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)) Then MessageBox.Show("Selecione um arquivo de ícone .ico válido.") : Return
            Dim parsed As Version = Nothing
            If Not Version.TryParse(AssemblyVersionValue, parsed) Then MessageBox.Show("A versão do assembly é inválida.") : Return
            If Not Version.TryParse(FileVersionValue, parsed) Then MessageBox.Show("A versão do arquivo é inválida.") : Return
            DialogResult = DialogResult.OK : Close()
        End Sub
        Private Shared Function ValueOr(value As String, fallback As String) As String
            Return If(String.IsNullOrWhiteSpace(value), fallback, value)
        End Function
        Private Shared Function SafeFileName(value As String) As String
            For Each invalid As Char In Path.GetInvalidFileNameChars() : value = value.Replace(invalid, "_"c) : Next
            Return If(String.IsNullOrWhiteSpace(value), "Aplicativo", value)
        End Function
    End Class
End Namespace
