Imports System
Imports System.ComponentModel
Imports System.IO.Ports
Imports System.Text
Imports System.Threading

Namespace FlowForgeStudio
    <DefaultEvent("DataReceived"), ToolboxItem(True)>
    Public Class SerialConnection
        Inherits Component

        Private ReadOnly port As New SerialPort()

        ' Captura o contexto de sincronização da UI no momento em que o componente é
        ' criado — normalmente dentro do InitializeComponent do formulário, já na
        ' thread de UI. O SerialPort do .NET dispara DataReceived/ErrorReceived/
        ' PinChanged em uma thread de segundo plano própria; sem repassar para a
        ' thread de UI, qualquer código que tente atualizar um controle dentro
        ' desses handlers lançaria "cross-thread operation not valid".
        Private ReadOnly uiContext As SynchronizationContext = SynchronizationContext.Current

        Public Sub New()
            AddHandler port.DataReceived, AddressOf PortDataReceived
            AddHandler port.ErrorReceived, AddressOf PortErrorReceived
            AddHandler port.PinChanged, AddressOf PortPinChanged
        End Sub

        <Category("Design")>
        Public Property Name As String = "SerialConnection1"

        <Category("Serial"), DefaultValue("COM1")>
        Public Property PortName As String
            Get
                Return port.PortName
            End Get
            Set(value As String)
                EnsureClosed() : port.PortName = value
            End Set
        End Property

        <Category("Serial"), DefaultValue(9600)>
        Public Property BaudRate As Integer
            Get
                Return port.BaudRate
            End Get
            Set(value As Integer)
                EnsureClosed() : port.BaudRate = value
            End Set
        End Property

        <Category("Serial"), DefaultValue(8)>
        Public Property DataBits As Integer
            Get
                Return port.DataBits
            End Get
            Set(value As Integer)
                EnsureClosed() : port.DataBits = value
            End Set
        End Property

        <Category("Serial"), DefaultValue(Parity.None)>
        Public Property Parity As Parity
            Get
                Return port.Parity
            End Get
            Set(value As Parity)
                EnsureClosed() : port.Parity = value
            End Set
        End Property

        <Category("Serial"), DefaultValue(StopBits.One)>
        Public Property StopBits As StopBits
            Get
                Return port.StopBits
            End Get
            Set(value As StopBits)
                EnsureClosed() : port.StopBits = value
            End Set
        End Property

        <Category("Serial"), DefaultValue(Handshake.None)>
        Public Property Handshake As Handshake
            Get
                Return port.Handshake
            End Get
            Set(value As Handshake)
                EnsureClosed() : port.Handshake = value
            End Set
        End Property

        <Category("Serial"), DefaultValue(False)>
        Public Property DtrEnable As Boolean
            Get
                Return port.DtrEnable
            End Get
            Set(value As Boolean)
                port.DtrEnable = value
            End Set
        End Property

        <Category("Serial"), DefaultValue(False)>
        Public Property RtsEnable As Boolean
            Get
                Return port.RtsEnable
            End Get
            Set(value As Boolean)
                port.RtsEnable = value
            End Set
        End Property

        <Category("Serial"), DefaultValue(1000)>
        Public Property ReadTimeout As Integer
            Get
                Return port.ReadTimeout
            End Get
            Set(value As Integer)
                port.ReadTimeout = value
            End Set
        End Property

        <Category("Serial"), DefaultValue(1000)>
        Public Property WriteTimeout As Integer
            Get
                Return port.WriteTimeout
            End Get
            Set(value As Integer)
                port.WriteTimeout = value
            End Set
        End Property

        <Category("Serial"), DefaultValue("\r\n")>
        Public Property NewLine As String
            Get
                Return port.NewLine
            End Get
            Set(value As String)
                port.NewLine = value
            End Set
        End Property

        <Category("Serial"), DefaultValue("UTF-8")>
        Public Property EncodingName As String
            Get
                Return port.Encoding.WebName
            End Get
            Set(value As String)
                port.Encoding = Encoding.GetEncoding(value)
            End Set
        End Property

        <Browsable(False)>
        Public ReadOnly Property IsOpen As Boolean
            Get
                Return port.IsOpen
            End Get
        End Property

        <Browsable(False)>
        Public ReadOnly Property BytesToRead As Integer
            Get
                Return If(port.IsOpen, port.BytesToRead, 0)
            End Get
        End Property

        Public Event DataReceived As EventHandler(Of SerialTextEventArgs)
        Public Event ErrorReceived As EventHandler(Of SerialErrorEventArgs)
        Public Event PinChanged As EventHandler(Of SerialPinEventArgs)
        Public Event ConnectionChanged As EventHandler

        Public Sub Open()
            If port.IsOpen Then Return
            port.Open()
            RaiseEvent ConnectionChanged(Me, EventArgs.Empty)
        End Sub

        Public Sub Close()
            If Not port.IsOpen Then Return
            port.Close()
            RaiseEvent ConnectionChanged(Me, EventArgs.Empty)
        End Sub

        Public Sub Write(text As String)
            RequireOpen() : port.Write(text)
        End Sub

        Public Sub WriteLine(text As String)
            RequireOpen() : port.WriteLine(text)
        End Sub

        Public Function ReadExisting() As String
            RequireOpen() : Return port.ReadExisting()
        End Function

        Public Sub DiscardBuffers()
            RequireOpen() : port.DiscardInBuffer() : port.DiscardOutBuffer()
        End Sub

        Public Shared Function GetPortNames() As String()
            Return SerialPort.GetPortNames()
        End Function

        Private Sub PortDataReceived(sender As Object, e As SerialDataReceivedEventArgs)
            Dim text As String = port.ReadExisting()
            If text.Length = 0 Then Return
            RaiseOnUiThread(Sub() RaiseEvent DataReceived(Me, New SerialTextEventArgs(text)))
        End Sub

        Private Sub PortErrorReceived(sender As Object, e As SerialErrorReceivedEventArgs)
            RaiseOnUiThread(Sub() RaiseEvent ErrorReceived(Me, New SerialErrorEventArgs(e.EventType)))
        End Sub

        Private Sub PortPinChanged(sender As Object, e As SerialPinChangedEventArgs)
            RaiseOnUiThread(Sub() RaiseEvent PinChanged(Me, New SerialPinEventArgs(e.EventType)))
        End Sub

        Private Sub RaiseOnUiThread(action As Action)
            If uiContext IsNot Nothing Then
                uiContext.Post(Sub(state) action(), Nothing)
            Else
                action()
            End If
        End Sub

        Private Sub EnsureClosed()
            If port.IsOpen Then Throw New InvalidOperationException("Feche a porta serial antes de alterar esta propriedade.")
        End Sub

        Private Sub RequireOpen()
            If Not port.IsOpen Then Throw New InvalidOperationException("A porta serial não está aberta.")
        End Sub

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                RemoveHandler port.DataReceived, AddressOf PortDataReceived
                RemoveHandler port.ErrorReceived, AddressOf PortErrorReceived
                RemoveHandler port.PinChanged, AddressOf PortPinChanged
                If port.IsOpen Then port.Close()
                port.Dispose()
            End If
            MyBase.Dispose(disposing)
        End Sub
    End Class

    Public NotInheritable Class SerialTextEventArgs
        Inherits EventArgs
        Public Sub New(value As String)
            Text = value
        End Sub
        Public ReadOnly Property Text As String
    End Class

    Public NotInheritable Class SerialErrorEventArgs
        Inherits EventArgs
        Public Sub New(value As SerialError)
            EventType = value
        End Sub
        Public ReadOnly Property EventType As SerialError
    End Class

    Public NotInheritable Class SerialPinEventArgs
        Inherits EventArgs
        Public Sub New(value As SerialPinChange)
            EventType = value
        End Sub
        Public ReadOnly Property EventType As SerialPinChange
    End Class
End Namespace
