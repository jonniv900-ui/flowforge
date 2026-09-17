Imports System
Imports System.Windows.Forms
Namespace Demo
 Friend Module Program
  <STAThread> Public Sub Main()
   Application.EnableVisualStyles()
   Application.SetCompatibleTextRenderingDefault(False)
   Application.Run(New DemoForm())
  End Sub
 End Module
End Namespace
