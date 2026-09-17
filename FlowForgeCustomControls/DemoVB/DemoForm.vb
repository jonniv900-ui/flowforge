Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports FlowForgeStudio
Namespace Demo
 Public Class DemoForm
  Inherits Form
  Private ReadOnly glyphs As New GlyphImageList()
  Private ReadOnly toast As New ToastNotification()
  Private ReadOnly accordion As New Accordion()
  Private ReadOnly stepper As New ProgressStepper()
  Private ReadOnly terminal As New TerminalView()
  Public Sub New()
   Text="FlowForge Custom Controls - Visual Studio Demo" : ClientSize=New Size(900,560) : StartPosition=FormStartPosition.CenterScreen
   stepper.Location=New Point(20,20) : stepper.Size=New Size(850,75) : stepper.Steps="Abrir,Editar,Compilar,Concluir"
   accordion.Location=New Point(20,115) : accordion.Size=New Size(360,300)
   accordion.Sections="GlyphImageList::480 glyphs dinâmicos.;;ToastNotification::Notificações temporárias.;;TerminalView::Console interativo."
   terminal.Location=New Point(400,115) : terminal.Size=New Size(470,300) : terminal.WriteLine("Digite NEXT, BACK, TOAST ou CLEAR.")
   Dim nextButton As New Button With {.Text="Próxima",.Location=New Point(20,440),.Size=New Size(120,36),.Image=glyphs.GetGlyph(GlyphType.Navigation_Next),.TextImageRelation=TextImageRelation.ImageBeforeText}
   AddHandler nextButton.Click, Sub() stepper.NextStep()
   Dim toastButton As New Button With {.Text="Toast",.Location=New Point(150,440),.Size=New Size(120,36),.Image=glyphs.GetGlyph(GlyphType.Status_Info),.TextImageRelation=TextImageRelation.ImageBeforeText}
   AddHandler toastButton.Click, Sub() toast.Show("Funcionando no Visual Studio!",ToastKind.Success)
   toast.Location=New Point(590,485) : toast.Visible=False
   AddHandler terminal.CommandEntered, AddressOf TerminalCommand
   Controls.AddRange(New Control(){stepper,accordion,terminal,nextButton,toastButton,toast})
  End Sub
  Private Sub TerminalCommand(sender As Object,e As TerminalCommandEventArgs)
   Select Case e.Command.Trim().ToUpperInvariant()
    Case "NEXT" : stepper.NextStep()
    Case "BACK" : stepper.PreviousStep()
    Case "TOAST" : toast.Show("Comando recebido.",ToastKind.Info)
    Case "CLEAR" : terminal.ClearScreen()
    Case Else : terminal.WriteLine("Comando: " & e.Command)
   End Select
  End Sub
 End Class
End Namespace
