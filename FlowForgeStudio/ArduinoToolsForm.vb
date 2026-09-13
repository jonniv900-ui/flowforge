Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.IO.Ports
Imports System.Linq
Imports System.Windows.Forms

Namespace FlowForgeStudio
    Public Class ArduinoToolsForm
        Inherits Form
        Private ReadOnly ports As New ComboBox()
        Private ReadOnly baud As New ComboBox()
        Private ReadOnly boards As New ComboBox()
        Private ReadOnly monitor As New TextBox()
        Private ReadOnly sendBox As New TextBox()
        Private ReadOnly serial As New SerialPort()
        Private ReadOnly connectButton As New Button()
        Public Sub New()
            Text = "Arduino / ESP — Monitor e Upload"
            Width = 850 : Height = 560 : StartPosition = FormStartPosition.CenterParent
            Dim top As New FlowLayoutPanel With {.Dock = DockStyle.Top, .Height = 76, .Padding = New Padding(6), .AutoSize = False}
            ports.Width = 110 : baud.Width = 90 : boards.Width = 210
            baud.Items.AddRange(New Object() {"9600", "19200", "38400", "57600", "115200"}) : baud.Text = "115200"
            boards.Items.AddRange(New Object() {"Arduino UNO|arduino:avr:uno", "Arduino Nano|arduino:avr:nano", "ESP32|esp32:esp32:esp32", "ESP8266|esp8266:esp8266:nodemcuv2"}) : boards.SelectedIndex = 0
            Dim refreshButton As New Button With {.Text = "Atualizar portas", .AutoSize = True}
            connectButton.Text = "Conectar" : connectButton.AutoSize = True
            Dim uploadButton As New Button With {.Text = "Compilar/Upload .ino...", .AutoSize = True}
            AddHandler refreshButton.Click, Sub() RefreshPorts()
            AddHandler connectButton.Click, AddressOf ToggleConnection
            AddHandler uploadButton.Click, AddressOf UploadSketch
            top.Controls.AddRange(New Control() {New Label With {.Text = "Porta:", .AutoSize = True, .Padding = New Padding(0, 7, 0, 0)}, ports, New Label With {.Text = "Baud:", .AutoSize = True, .Padding = New Padding(0, 7, 0, 0)}, baud, connectButton, refreshButton, New Label With {.Text = "Placa:", .AutoSize = True, .Padding = New Padding(0, 7, 0, 0)}, boards, uploadButton})
            monitor.Dock = DockStyle.Fill : monitor.Multiline = True : monitor.ScrollBars = ScrollBars.Both : monitor.ReadOnly = True : monitor.Font = New Drawing.Font("Consolas", 10.0F)
            Dim bottom As New Panel With {.Dock = DockStyle.Bottom, .Height = 38}
            sendBox.Dock = DockStyle.Fill
            Dim sendButton As New Button With {.Text = "Enviar", .Dock = DockStyle.Right, .Width = 90}
            AddHandler sendButton.Click, AddressOf SendSerial
            AddHandler sendBox.KeyDown, Sub(sender, e)
                                            If e.KeyCode = Keys.Enter Then SendSerial(sender, EventArgs.Empty) : e.SuppressKeyPress = True
                                        End Sub
            bottom.Controls.Add(sendBox) : bottom.Controls.Add(sendButton)
            Controls.Add(monitor) : Controls.Add(bottom) : Controls.Add(top)
            AddHandler serial.DataReceived, AddressOf SerialDataReceived
            AddHandler FormClosed, Sub() If serial.IsOpen Then serial.Close()
            RefreshPorts()
        End Sub
        Private Sub RefreshPorts()
            Dim current As String = ports.Text
            ports.Items.Clear() : ports.Items.AddRange(SerialPort.GetPortNames().OrderBy(Function(x) x).Cast(Of Object).ToArray())
            If ports.Items.Contains(current) Then
                ports.SelectedItem = current
            ElseIf ports.Items.Count > 0 Then
                ports.SelectedIndex = 0
            End If
        End Sub
        Private Sub ToggleConnection(sender As Object, e As EventArgs)
            Try
                If serial.IsOpen Then
                    serial.Close() : connectButton.Text = "Conectar"
                Else
                    If String.IsNullOrWhiteSpace(ports.Text) Then Throw New InvalidOperationException("Selecione uma porta serial.")
                    serial.PortName = ports.Text : serial.BaudRate = Integer.Parse(baud.Text) : serial.NewLine = System.Convert.ToChar(10).ToString() : serial.Open() : connectButton.Text = "Desconectar"
                End If
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Serial", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub
        Private Sub SerialDataReceived(sender As Object, e As SerialDataReceivedEventArgs)
            Try
                Dim value As String = serial.ReadExisting()
                BeginInvoke(New Action(Sub() monitor.AppendText(value)))
            Catch
            End Try
        End Sub
        Private Sub SendSerial(sender As Object, e As EventArgs)
            If Not serial.IsOpen Then MessageBox.Show("Conecte à porta serial primeiro.", "Serial") : Return
            serial.WriteLine(sendBox.Text) : sendBox.SelectAll() : sendBox.Focus()
        End Sub
        Private Sub UploadSketch(sender As Object, e As EventArgs)
            Using dialog As New OpenFileDialog With {.Filter = "Arduino sketch (*.ino)|*.ino", .Title = "Selecionar sketch"}
                If dialog.ShowDialog(Me) <> DialogResult.OK Then Return
                Dim cli As String = FindArduinoCli()
                If cli = "" Then
                    MessageBox.Show("arduino-cli não foi encontrado no PATH. Instale o Arduino CLI ou coloque arduino-cli.exe ao lado do FlowForge.", "Arduino CLI", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                Dim fqbn As String = boards.Text.Split("|"c).Last()
                Dim sketchFolder As String = Path.GetDirectoryName(dialog.FileName)
                Dim args As String = "compile --fqbn " & Quote(fqbn) & " " & Quote(sketchFolder)
                If Not String.IsNullOrWhiteSpace(ports.Text) Then args &= " && " & Quote(cli) & " upload -p " & Quote(ports.Text) & " --fqbn " & Quote(fqbn) & " " & Quote(sketchFolder)
                Dim info As New ProcessStartInfo With {.FileName = "cmd.exe", .Arguments = "/c " & Quote(Quote(cli) & " " & args), .UseShellExecute = False, .RedirectStandardOutput = True, .RedirectStandardError = True, .CreateNoWindow = True}
                Try
                    Using process As Process = Process.Start(info)
                        Dim output As String = process.StandardOutput.ReadToEnd() & process.StandardError.ReadToEnd()
                        process.WaitForExit()
                        monitor.AppendText(Environment.NewLine & output & Environment.NewLine)
                    End Using
                Catch ex As Exception
                    MessageBox.Show(ex.Message, "Arduino CLI", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End Using
        End Sub
        Private Shared Function FindArduinoCli() As String
            Dim local As String = Path.Combine(Application.StartupPath, "arduino-cli.exe")
            If File.Exists(local) Then Return local
            For Each folder As String In If(Environment.GetEnvironmentVariable("PATH"), "").Split(";"c)
                Try
                    Dim candidate As String = Path.Combine(folder.Trim(), "arduino-cli.exe")
                    If File.Exists(candidate) Then Return candidate
                Catch
                End Try
            Next
            Return ""
        End Function
        Private Shared Function Quote(value As String) As String
            Dim quoteChar As String = System.Convert.ToChar(34).ToString()
            Return quoteChar & value.Replace(quoteChar, quoteChar & quoteChar) & quoteChar
        End Function
    End Class
End Namespace
