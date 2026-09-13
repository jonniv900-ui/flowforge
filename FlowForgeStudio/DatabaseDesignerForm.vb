Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Text
Imports System.Windows.Forms

Namespace FlowForgeStudio
    Public Class DatabaseDesignerForm
        Inherits Form
        Private ReadOnly connectionText As New TextBox()
        Private ReadOnly tables As New ListBox()
        Private ReadOnly preview As New TextBox()
        Public ReadOnly Property GeneratedModule As String
            Get
                Return BuildModule()
            End Get
        End Property
        Public Sub New()
            Text = "Banco de Dados Visual — SQL Server"
            Width = 900 : Height = 560 : StartPosition = FormStartPosition.CenterParent
            connectionText.Dock = DockStyle.Top : connectionText.Height = 28 : connectionText.Text = "Server=.\SQLEXPRESS;Database=MinhaBase;Integrated Security=True;"
            Dim toolbar As New FlowLayoutPanel With {.Dock = DockStyle.Top, .Height = 42, .Padding = New Padding(6)}
            Dim testButton As New Button With {.Text = "Testar conexão", .AutoSize = True}
            Dim schemaButton As New Button With {.Text = "Carregar tabelas", .AutoSize = True}
            Dim copyButton As New Button With {.Text = "Copiar módulo", .AutoSize = True}
            AddHandler testButton.Click, AddressOf TestConnection
            AddHandler schemaButton.Click, AddressOf LoadTables
            AddHandler copyButton.Click, Sub() Clipboard.SetText(BuildModule())
            toolbar.Controls.AddRange(New Control() {testButton, schemaButton, copyButton})
            tables.Dock = DockStyle.Left : tables.Width = 260
            preview.Dock = DockStyle.Fill : preview.Multiline = True : preview.ScrollBars = ScrollBars.Both : preview.Font = New Drawing.Font("Consolas", 10.0F)
            Dim bottom As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .Height = 44, .FlowDirection = FlowDirection.RightToLeft, .Padding = New Padding(6)}
            Dim ok As New Button With {.Text = "Adicionar módulo", .DialogResult = DialogResult.OK, .Width = 120}
            Dim cancel As New Button With {.Text = "Cancelar", .DialogResult = DialogResult.Cancel, .Width = 90}
            bottom.Controls.Add(ok) : bottom.Controls.Add(cancel)
            AddHandler tables.SelectedIndexChanged, Sub() preview.Text = BuildModule()
            Controls.Add(preview) : Controls.Add(tables) : Controls.Add(toolbar) : Controls.Add(connectionText) : Controls.Add(bottom)
            AcceptButton = ok : CancelButton = cancel
            preview.Text = BuildModule()
        End Sub
        Private Sub TestConnection(sender As Object, e As EventArgs)
            Try
                Using cn As New SqlConnection(connectionText.Text)
                    cn.Open()
                    MessageBox.Show("Conexão realizada com sucesso.", "Banco de dados", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End Using
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Banco de dados", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub
        Private Sub LoadTables(sender As Object, e As EventArgs)
            Try
                tables.Items.Clear()
                Using cn As New SqlConnection(connectionText.Text)
                    cn.Open()
                    Dim schema As DataTable = cn.GetSchema("Tables")
                    For Each row As DataRow In schema.Rows
                        If CStr(row("TABLE_TYPE")).Equals("BASE TABLE", StringComparison.OrdinalIgnoreCase) Then tables.Items.Add(CStr(row("TABLE_NAME")))
                    Next
                End Using
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Banco de dados", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
            preview.Text = BuildModule()
        End Sub
        Private Function BuildModule() As String
            Dim tableName As String = If(tables.SelectedItem Is Nothing, "MinhaTabela", CStr(tables.SelectedItem))
            Dim quoteChar As String = System.Convert.ToChar(34).ToString()
            Dim cs As String = connectionText.Text.Replace(quoteChar, quoteChar & quoteChar)
            Dim b As New StringBuilder()
            b.AppendLine("Imports System.Data").AppendLine("Imports System.Data.SqlClient").AppendLine()
            b.AppendLine("Public Module BancoDados")
            b.AppendLine("    Private ReadOnly ConnectionString As String = " & quoteChar & cs & quoteChar)
            b.AppendLine("    Public Function Consultar(sql As String) As DataTable")
            b.AppendLine("        Dim tabela As New DataTable()")
            b.AppendLine("        Using cn As New SqlConnection(ConnectionString), cmd As New SqlCommand(sql, cn), da As New SqlDataAdapter(cmd)")
            b.AppendLine("            da.Fill(tabela)")
            b.AppendLine("        End Using")
            b.AppendLine("        Return tabela")
            b.AppendLine("    End Function")
            b.AppendLine("    Public Function Listar" & SafeName(tableName) & "() As DataTable")
            Dim selectSql As String = "SELECT * FROM [" & tableName.Replace("]", "]]" ) & "]"
            b.AppendLine("        Return Consultar(" & quoteChar & selectSql & quoteChar & ")")
            b.AppendLine("    End Function")
            b.AppendLine("End Module")
            Return b.ToString()
        End Function
        Private Shared Function SafeName(value As String) As String
            Dim b As New StringBuilder()
            For Each c As Char In value
                If Char.IsLetterOrDigit(c) OrElse c = "_"c Then b.Append(c)
            Next
            If b.Length = 0 OrElse Char.IsDigit(b(0)) Then b.Insert(0, "Tabela")
            Return b.ToString()
        End Function
    End Class
End Namespace
