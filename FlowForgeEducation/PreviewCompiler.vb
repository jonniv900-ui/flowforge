Imports System
Imports System.CodeDom.Compiler
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Text
Imports Microsoft.VisualBasic

Namespace FlowForgeStudio
    Public NotInheritable Class PreviewCompiler
        Private Sub New()
        End Sub
        Public Shared Function CompileAndRun(project As FlowProject) As String
            Dim temp As String = Path.Combine(Path.GetTempPath(), "FlowForgeStudio", "Preview", Guid.NewGuid().ToString("N")) : Directory.CreateDirectory(temp)
            Dim exe As String = Path.Combine(temp, SafeName(project.Name) & ".exe")
            Return Compile(project, exe, True)
        End Function
        Public Shared Function BuildExecutable(project As FlowProject, outputFile As String) As String
            Return Compile(project, outputFile, False)
        End Function
        Private Shared Function Compile(project As FlowProject, exe As String, runAfterBuild As Boolean) As String
            Dim temp As String = Path.Combine(Path.GetTempPath(), "FlowForgeStudio", "Build", Guid.NewGuid().ToString("N")) : Directory.CreateDirectory(temp)
            Dim outputFolder As String = Path.GetDirectoryName(exe)
            If Not Directory.Exists(outputFolder) Then Directory.CreateDirectory(outputFolder)
            Dim sourceFiles As New List(Of String)()
            sourceFiles.Add(WriteSource(temp, "Program.vb", GenerateProgram(project)))
            sourceFiles.Add(WriteSource(temp, "FlowForgeForms.vb", GenerateFormRegistry(project)))
            sourceFiles.Add(WriteSource(temp, "FlowForgeResources.vb", GenerateResourceBootstrap(project)))
            sourceFiles.Add(WriteSource(temp, "PropertyLoader.vb", PropertyLoaderSource()))
            sourceFiles.Add(WriteSource(temp, "ColorComboBox.vb", ColorComboBoxSource()))
            sourceFiles.Add(WriteSource(temp, "CustomControls.vb", CustomControlsSource()))
            sourceFiles.Add(WriteSource(temp, "AssemblyInfo.vb", GenerateAssemblyInfo(project)))
            If project.Classes IsNot Nothing Then
                For Each classFile As ClassData In project.Classes
                    sourceFiles.Add(WriteSource(temp, SafeName(classFile.Name) & ".vb", RewriteDefaultFormReferences(classFile.Code, project)))
                Next
            End If
            If project.Modules IsNot Nothing Then
                For Each moduleFile As ModuleData In project.Modules
                    sourceFiles.Add(WriteSource(temp, SafeName(moduleFile.Name) & ".vb", RewriteDefaultFormReferences(moduleFile.Code, project)))
                Next
            End If
            If project.UserControls IsNot Nothing Then
                For Each controlFile As UserControlData In project.UserControls
                    sourceFiles.Add(WriteSource(temp, SafeName(controlFile.Name) & ".vb", RewriteDefaultFormReferences(PrepareUserControlCode(controlFile), project)))
                    sourceFiles.Add(WriteSource(temp, SafeName(controlFile.Name) & ".Designer.vb", GenerateUserControlDesigner(controlFile)))
                Next
            End If
            For Each form As FormData In project.Forms
                sourceFiles.Add(WriteSource(temp, SafeName(form.Name) & ".vb", RewriteDefaultFormReferences(form.Code, project)))
                sourceFiles.Add(WriteSource(temp, SafeName(form.Name) & ".Designer.vb", GenerateDesigner(form)))
            Next
            Dim compilerOptions As String = "/target:winexe /optimize+ /optioninfer+ /optionexplicit+"
            If Not String.IsNullOrWhiteSpace(project.ApplicationIcon) AndAlso File.Exists(project.ApplicationIcon) Then compilerOptions &= " /win32icon:" & ChrW(34) & project.ApplicationIcon & ChrW(34)
            Dim options As New CompilerParameters With {.GenerateExecutable = True, .GenerateInMemory = False, .OutputAssembly = exe, .CompilerOptions = compilerOptions, .IncludeDebugInformation = False}
            For Each reference As String In {"System.dll", "System.Core.dll", "System.Data.dll", "System.Drawing.dll", "System.Windows.Forms.dll", "System.Net.Http.dll", "System.Web.Extensions.dll", "System.Xml.dll"} : options.ReferencedAssemblies.Add(reference) : Next
            AddExternalReferences(project, options, outputFolder, temp)
            Dim provider As New VBCodeProvider(New Dictionary(Of String, String) From {{"CompilerVersion", "v4.0"}})
            Dim result As CompilerResults = provider.CompileAssemblyFromFile(options, sourceFiles.ToArray())
            If result.Errors.HasErrors Then
                Dim output As New StringBuilder("Falha na compilação:" & vbCrLf & vbCrLf)
                For Each err As CompilerError In result.Errors
                    If Not err.IsWarning Then
                        output.AppendLine(String.Format("{0} — linha {1}, coluna {2}: {3} {4}", Path.GetFileName(err.FileName), err.Line, err.Column, err.ErrorNumber, err.ErrorText))
                        Dim friendly As String = FriendlyCompilerMessage(err)
                        If friendly <> "" Then output.AppendLine("    💡 Dica: " & friendly)
                        output.AppendLine()
                    End If
                Next
                Return output.ToString()
            End If
            WriteRuntimeConfig(exe)
            If runAfterBuild Then Process.Start(exe)
            Return ""
        End Function
        Private Shared Function FriendlyCompilerMessage(errorItem As CompilerError) As String
            Select Case errorItem.ErrorNumber.ToUpperInvariant()
                Case "BC30035", "BC30201", "BC30203"
                    Return "Há uma instrução incompleta. Confira parênteses, vírgulas, nomes e o final da linha indicada."
                Case "BC30081", "BC30082", "BC30083", "BC30085", "BC30086", "BC30087", "BC30088", "BC30095"
                    Return "Um bloco não foi encerrado corretamente. Procure por End If, End Sub, End Select, End Try, Next ou End While."
                Case "BC30198", "BC30205", "BC32017"
                    Return "A expressão está incompleta. Verifique operadores, aspas e parênteses perto desta linha."
                Case "BC30451"
                    Return "O nome usado não foi encontrado. Confira a escrita e veja se o componente ou a variável existe."
                Case "BC30456"
                    Return "Esse objeto não possui a propriedade ou função escrita depois do ponto. Use o autocompletar para ver as opções."
                Case "BC30002"
                    Return "O tipo não foi encontrado. Pode faltar um Imports, uma biblioteca ou o nome pode estar escrito incorretamente."
                Case "BC30311", "BC30512", "BC30519"
                    Return "Os tipos dos valores não combinam com essa operação. Confira se está usando texto, número ou outro objeto."
                Case "BC30648"
                    Return "Um texto começou com aspas, mas não terminou. Acrescente as aspas duplas que faltam."
                Case Else
                    If errorItem.ErrorText.IndexOf("End", StringComparison.OrdinalIgnoreCase) >= 0 Then Return "Confira onde cada bloco começa e termina. A indentação ajuda a enxergar a estrutura."
                    Return "Leia a linha indicada e compare com um exemplo semelhante na Central de Aprendizado."
            End Select
        End Function
        Private Shared Sub WriteRuntimeConfig(exe As String)
            Dim config As String = "<?xml version=""1.0"" encoding=""utf-8""?>" & vbCrLf & "<configuration>" & vbCrLf & "  <startup useLegacyV2RuntimeActivationPolicy=""true"">" & vbCrLf & "    <supportedRuntime version=""v4.0"" sku="".NETFramework,Version=v4.8"" />" & vbCrLf & "  </startup>" & vbCrLf & "</configuration>"
            File.WriteAllText(exe & ".config", config, New UTF8Encoding(False))
        End Sub
        Private Shared Sub AddExternalReferences(project As FlowProject, options As CompilerParameters, outputFolder As String, buildFolder As String)
            Dim files = project.Forms.SelectMany(Function(f) f.Controls).Select(Function(c) c.AssemblyPath).Where(Function(p) Not String.IsNullOrWhiteSpace(p) AndAlso File.Exists(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            Dim embeddedReferences As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            If project.Libraries IsNot Nothing Then
                Dim resourceIndex As Integer = 0
                For Each library As ProjectLibrary In project.Libraries
                    If String.IsNullOrWhiteSpace(library.RelativePath) OrElse String.IsNullOrWhiteSpace(library.DataBase64) Then Continue For
                    Dim relative As String = SafeRelativePath(library.RelativePath)
                    Dim baseFolder As String = If(library.EmbedInExecutable, Path.Combine(buildFolder, "EmbeddedResources"), outputFolder)
                    Dim destination As String = Path.Combine(baseFolder, relative)
                    Dim destinationFolder As String = Path.GetDirectoryName(destination)
                    If Not Directory.Exists(destinationFolder) Then Directory.CreateDirectory(destinationFolder)
                    File.WriteAllBytes(destination, Convert.FromBase64String(library.DataBase64))
                    If library.IsReference Then
                        files.RemoveAll(Function(filePath) IO.Path.GetFileName(filePath).Equals(library.FileName, StringComparison.OrdinalIgnoreCase))
                        files.Add(destination)
                        If library.EmbedInExecutable Then embeddedReferences.Add(destination)
                    End If
                    If library.EmbedInExecutable Then
                        options.CompilerOptions &= " /resource:" & ChrW(34) & destination & ChrW(34) & ",FlowForge.Embedded." & resourceIndex.ToString()
                        resourceIndex += 1
                    End If
                Next
            End If
            For Each assemblyFile As String In files.ToArray()
                Dim dependencyFolder As String = Path.GetDirectoryName(assemblyFile)
                If Directory.Exists(dependencyFolder) Then
                    For Each dependency As String In Directory.GetFiles(dependencyFolder, "*.dll")
                        If Path.GetFileName(dependency).StartsWith("Interop.", StringComparison.OrdinalIgnoreCase) OrElse Path.GetFileName(dependency).StartsWith("AxInterop.", StringComparison.OrdinalIgnoreCase) Then
                            If Not files.Contains(dependency, StringComparer.OrdinalIgnoreCase) Then files.Add(dependency)
                        End If
                    Next
                End If
            Next
            For Each assemblyFile As String In files
                If Not options.ReferencedAssemblies.Contains(assemblyFile) Then options.ReferencedAssemblies.Add(assemblyFile)
                Dim destination As String = Path.Combine(outputFolder, Path.GetFileName(assemblyFile))
                If Not embeddedReferences.Contains(assemblyFile) AndAlso Not Path.GetFullPath(assemblyFile).Equals(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase) AndAlso Not File.Exists(destination) Then File.Copy(assemblyFile, destination, False)
            Next
        End Sub
        Private Shared Function SafeRelativePath(value As String) As String
            Dim normalized As String = value.Replace("/"c, Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar)
            If Path.IsPathRooted(normalized) OrElse normalized.Split(Path.DirectorySeparatorChar).Any(Function(part) part = "..") Then Throw New InvalidDataException("Caminho inseguro de biblioteca incorporada: " & value)
            Return normalized
        End Function
        Private Shared Function WriteSource(folder As String, fileName As String, contents As String) As String
            Dim path As String = IO.Path.Combine(folder, fileName)
            File.WriteAllText(path, contents, New UTF8Encoding(False))
            Return path
        End Function
        Private Shared Function GenerateProgram(project As FlowProject) As String
            Dim startup As FormData = project.Forms.FirstOrDefault(Function(f) f.IsStartup) : If startup Is Nothing Then startup = project.Forms.First()
            Return "Imports System" & vbCrLf & "Imports System.Windows.Forms" & vbCrLf & "Public Module Program" & vbCrLf & " <STAThread> Public Sub Main()" & vbCrLf & "  FlowForgeResources.Initialize()" & vbCrLf & "  Application.EnableVisualStyles()" & vbCrLf & "  Application.SetCompatibleTextRenderingDefault(False)" & vbCrLf & "  Application.Run(New " & startup.Name & "())" & vbCrLf & " End Sub" & vbCrLf & "End Module"
        End Function
        Private Shared Function GenerateResourceBootstrap(project As FlowProject) As String
            Dim libraries As List(Of ProjectLibrary) = If(project.Libraries, New List(Of ProjectLibrary)())
            Dim embedded As List(Of ProjectLibrary) = libraries.Where(Function(x) x.EmbedInExecutable AndAlso Not String.IsNullOrWhiteSpace(x.RelativePath) AndAlso Not String.IsNullOrWhiteSpace(x.DataBase64)).ToList()
            Dim b As New StringBuilder()
            b.AppendLine("Imports System")
            b.AppendLine("Imports System.Collections.Generic")
            b.AppendLine("Imports System.Drawing")
            b.AppendLine("Imports System.IO")
            b.AppendLine("Imports System.Reflection")
            b.AppendLine("Imports System.Runtime.InteropServices")
            b.AppendLine("Imports System.Resources")
            b.AppendLine("Public Module FlowForgeResources")
            b.AppendLine(" Private _root As String")
            b.AppendLine(" Private ReadOnly _files As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)")
            b.AppendLine(" <DllImport(""kernel32.dll"", CharSet:=CharSet.Unicode, SetLastError:=True)>")
            b.AppendLine(" Private Function SetDllDirectory(path As String) As Boolean")
            b.AppendLine(" End Function")
            b.AppendLine(" Public Sub Initialize()")
            b.AppendLine("  If _root IsNot Nothing Then Return")
            b.AppendLine("  Dim identity As String = Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId.ToString(""N"")")
            b.AppendLine("  _root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ""FlowForgeRuntime"", Assembly.GetExecutingAssembly().GetName().Name, identity)")
            b.AppendLine("  Directory.CreateDirectory(_root)")
            For index As Integer = 0 To embedded.Count - 1
                Dim relative As String = SafeRelativePath(embedded(index).RelativePath)
                b.AppendLine("  Extract(""FlowForge.Embedded." & index.ToString() & """, """ & Vb(relative) & """)")
            Next
            b.AppendLine("  AddHandler AppDomain.CurrentDomain.AssemblyResolve, AddressOf ResolveAssembly")
            b.AppendLine("  Dim nativeFolder As String = Path.Combine(_root, If(IntPtr.Size = 8, ""x64"", ""x86""))")
            b.AppendLine("  If Directory.Exists(nativeFolder) Then")
            b.AppendLine("   SetDllDirectory(nativeFolder)")
            b.AppendLine("  Else")
            b.AppendLine("   SetDllDirectory(_root)")
            b.AppendLine("  End If")
            b.AppendLine(" End Sub")
            b.AppendLine(" Private Sub Extract(resourceName As String, relativePath As String)")
            b.AppendLine("  Dim safeRelative As String = relativePath.Replace(""/""c, Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar)")
            b.AppendLine("  If Path.IsPathRooted(safeRelative) OrElse safeRelative.Contains("".."") Then Throw New InvalidDataException(""Caminho de recurso inválido: "" & relativePath)")
            b.AppendLine("  Dim output As String = Path.Combine(_root, safeRelative)")
            b.AppendLine("  Dim folder As String = Path.GetDirectoryName(output)")
            b.AppendLine("  If Not Directory.Exists(folder) Then Directory.CreateDirectory(folder)")
            b.AppendLine("  Using source As Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)")
            b.AppendLine("   If source Is Nothing Then Throw New MissingManifestResourceException(resourceName)")
            b.AppendLine("   Using target As New FileStream(output, FileMode.Create, FileAccess.Write, FileShare.Read)")
            b.AppendLine("    source.CopyTo(target)")
            b.AppendLine("   End Using")
            b.AppendLine("  End Using")
            b.AppendLine("  _files(relativePath) = output")
            b.AppendLine(" End Sub")
            b.AppendLine(" Private Function ResolveAssembly(sender As Object, args As ResolveEventArgs) As Assembly")
            b.AppendLine("  Dim requested As New AssemblyName(args.Name)")
            b.AppendLine("  For Each filePath As String In Directory.GetFiles(_root, ""*.dll"", SearchOption.AllDirectories)")
            b.AppendLine("   Try")
            b.AppendLine("    If AssemblyName.GetAssemblyName(filePath).Name.Equals(requested.Name, StringComparison.OrdinalIgnoreCase) Then Return Assembly.LoadFrom(filePath)")
            b.AppendLine("   Catch")
            b.AppendLine("   End Try")
            b.AppendLine("  Next")
            b.AppendLine("  Return Nothing")
            b.AppendLine(" End Function")
            b.AppendLine(" Public Function GetPath(relativePath As String) As String")
            b.AppendLine("  Initialize()")
            b.AppendLine("  Dim result As String = Nothing")
            b.AppendLine("  If _files.TryGetValue(relativePath, result) Then Return result")
            b.AppendLine("  result = Path.Combine(_root, relativePath)")
            b.AppendLine("  If File.Exists(result) Then Return result")
            b.AppendLine("  Throw New FileNotFoundException(""Recurso incorporado não encontrado."", relativePath)")
            b.AppendLine(" End Function")
            b.AppendLine(" Public Function GetBytes(relativePath As String) As Byte()")
            b.AppendLine("  Return File.ReadAllBytes(GetPath(relativePath))")
            b.AppendLine(" End Function")
            b.AppendLine(" Public Function GetImage(relativePath As String) As Image")
            b.AppendLine("  Using temporary As Image = Image.FromFile(GetPath(relativePath))")
            b.AppendLine("   Return New Bitmap(temporary)")
            b.AppendLine("  End Using")
            b.AppendLine(" End Function")
            b.AppendLine("End Module")
            Return b.ToString()
        End Function
        Private Shared Function GenerateFormRegistry(project As FlowProject) As String
            Dim b As New StringBuilder()
            b.AppendLine("Imports System.Windows.Forms")
            b.AppendLine("Public Module FlowForgeForms")
            For Each form As FormData In project.Forms
                Dim name As String = form.Name
                b.AppendLine(" Private _" & name & " As Global." & name)
                b.AppendLine(" Public ReadOnly Property " & name & " As Global." & name)
                b.AppendLine("  Get")
                b.AppendLine("   If _" & name & " Is Nothing OrElse _" & name & ".IsDisposed Then _" & name & " = New Global." & name & "()")
                b.AppendLine("   Return _" & name)
                b.AppendLine("  End Get")
                b.AppendLine(" End Property")
            Next
            b.AppendLine("End Module")
            Return b.ToString()
        End Function
        Private Shared Function RewriteDefaultFormReferences(source As String, project As FlowProject) As String
            Dim result As String = If(source, "")
            For Each form As FormData In project.Forms.OrderByDescending(Function(f) f.Name.Length)
                result = System.Text.RegularExpressions.Regex.Replace(result, "(?<![\w.])" & System.Text.RegularExpressions.Regex.Escape(form.Name) & "(?=\s*\.)", "FlowForgeForms." & form.Name, System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            Next
            Return result
        End Function
        Private Shared Function GenerateAssemblyInfo(project As FlowProject) As String
            Dim title As String = If(String.IsNullOrWhiteSpace(project.AssemblyTitle), project.Name, project.AssemblyTitle)
            Dim product As String = If(String.IsNullOrWhiteSpace(project.AssemblyProduct), project.Name, project.AssemblyProduct)
            Dim assemblyVersion As String = ValidVersion(project.AssemblyVersion)
            Dim fileVersion As String = ValidVersion(project.FileVersion)
            Dim b As New StringBuilder()
            b.AppendLine("Imports System.Reflection")
            b.AppendLine("<Assembly: AssemblyTitle(""" & Vb(title) & """)>")
            b.AppendLine("<Assembly: AssemblyDescription(""" & Vb(project.AssemblyDescription) & """)>")
            b.AppendLine("<Assembly: AssemblyCompany(""" & Vb(project.AssemblyCompany) & """)>")
            b.AppendLine("<Assembly: AssemblyProduct(""" & Vb(product) & """)>")
            b.AppendLine("<Assembly: AssemblyCopyright(""" & Vb(project.AssemblyCopyright) & """)>")
            b.AppendLine("<Assembly: AssemblyTrademark(""" & Vb(project.AssemblyTrademark) & """)>")
            b.AppendLine("<Assembly: AssemblyVersion(""" & assemblyVersion & """)>")
            b.AppendLine("<Assembly: AssemblyFileVersion(""" & fileVersion & """)>")
            Return b.ToString()
        End Function
        Private Shared Function ValidVersion(value As String) As String
            Dim parsed As Version = Nothing
            If Version.TryParse(value, parsed) Then Return parsed.ToString()
            Return "1.0.0.0"
        End Function
        Private Shared Function PrepareUserControlCode(controlFile As UserControlData) As String
            Dim source As String = If(controlFile.Code, "")
            If Not System.Text.RegularExpressions.Regex.IsMatch(source, "(?im)^\s*Partial\s+Public\s+Class\s+" & System.Text.RegularExpressions.Regex.Escape(controlFile.Name) & "\b") Then
                Dim classRegex As New System.Text.RegularExpressions.Regex("(?im)^(\s*)Public\s+Class\s+" & System.Text.RegularExpressions.Regex.Escape(controlFile.Name) & "\b")
                source = classRegex.Replace(source, "$1Partial Public Class " & controlFile.Name, 1)
            End If
            Dim ctor As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(source, "(?im)^\s*(?:Public|Private|Protected|Friend)?\s*Sub\s+New\s*\([^\r\n]*\)\s*$")
            If ctor.Success Then
                Dim endOfLine As Integer = source.IndexOf(System.Convert.ToChar(10), ctor.Index + ctor.Length)
                If endOfLine < 0 Then endOfLine = ctor.Index + ctor.Length
                Dim ctorEnd As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(source.Substring(ctor.Index), "(?im)^\s*End\s+Sub\s*$")
                Dim ctorText As String = If(ctorEnd.Success, source.Substring(ctor.Index, ctorEnd.Index + ctorEnd.Length), "")
                If ctorText <> "" AndAlso ctorText.IndexOf("InitializeComponent", StringComparison.OrdinalIgnoreCase) < 0 Then
                    source = source.Insert(endOfLine + If(endOfLine < source.Length, 1, 0), "        InitializeComponent()" & System.Environment.NewLine)
                End If
            End If
            Return source
        End Function

        Private Shared Function GenerateUserControlDesigner(controlFile As UserControlData) As String
            Dim b As New StringBuilder()
            b.AppendLine("Imports System").AppendLine("Imports System.Drawing").AppendLine("Imports System.Windows.Forms")
            b.AppendLine("Partial Public Class " & controlFile.Name)
            For Each item In controlFile.Controls
                b.AppendLine(" Friend WithEvents " & item.Name & " As " & GeneratedTypeName(item))
                AppendToolStripItemDeclarations(b, item.Items)
            Next
            If Not System.Text.RegularExpressions.Regex.IsMatch(controlFile.Code, "(?i)\bSub\s+New\s*\(") Then
                b.AppendLine(" Public Sub New()").AppendLine("  MyBase.New() : InitializeComponent()").AppendLine(" End Sub")
            End If
            b.AppendLine(" Private Sub InitializeComponent()").AppendLine("  Me.SuspendLayout()")
            For Each pair In controlFile.Properties
                If Not IsTransientGeneratedProperty(pair.Key, False) Then
                    Dim propertyName As String = If(pair.Key.Equals("ClientSize", StringComparison.OrdinalIgnoreCase), "Size", pair.Key)
                    b.AppendLine("  PropertyLoader.Apply(Me, """ & Vb(propertyName) & """, """ & Vb(pair.Value) & """)")
                End If
            Next
            For Each item In controlFile.Controls
                b.AppendLine("  Me." & item.Name & " = New " & GeneratedTypeName(item) & "()")
                For Each pair In item.Properties
                    If Not IsTransientGeneratedProperty(pair.Key, False) Then b.AppendLine("  PropertyLoader.Apply(Me." & item.Name & ", """ & Vb(pair.Key) & """, """ & Vb(pair.Value) & """)")
                Next
                AppendToolStripItemsInitialization(b, "Me." & item.Name & ".Items", item.Items)
                If Not item.IsNonVisual Then b.AppendLine("  Me.Controls.Add(Me." & item.Name & ")")
            Next
            b.AppendLine("  Me.Name = """ & Vb(controlFile.Name) & """").AppendLine("  Me.ResumeLayout(False)").AppendLine("  Me.PerformLayout()")
            b.AppendLine(" End Sub").AppendLine("End Class")
            Return b.ToString()
        End Function

        Private Shared Function GenerateDesigner(form As FormData) As String
            Dim b As New StringBuilder() : b.AppendLine("Imports System").AppendLine("Imports System.Drawing").AppendLine("Imports System.Windows.Forms").AppendLine("Partial Public Class " & form.Name).AppendLine(" Inherits Form")
            For Each item In form.Controls
                b.AppendLine(" Friend WithEvents " & item.Name & " As " & GeneratedTypeName(item))
                AppendToolStripItemDeclarations(b, item.Items)
            Next
            b.AppendLine(" Public Sub New()").AppendLine("  MyBase.New() : InitializeComponent()").AppendLine(" End Sub").AppendLine(" Private Sub InitializeComponent()").AppendLine("  Me.SuspendLayout()")
            For Each pair In form.Properties
                If Not IsTransientGeneratedProperty(pair.Key, False) Then b.AppendLine("  PropertyLoader.Apply(Me, """ & Vb(pair.Key) & """, """ & Vb(pair.Value) & """)")
            Next
            For Each item In form.Controls
                b.AppendLine("  Me." & item.Name & " = New " & GeneratedTypeName(item) & "()")
                For Each pair In item.Properties
                    If Not IsTransientGeneratedProperty(pair.Key, False) Then b.AppendLine("  PropertyLoader.Apply(Me." & item.Name & ", """ & Vb(pair.Key) & """, """ & Vb(pair.Value) & """)")
                Next
                AppendToolStripItemsInitialization(b, "Me." & item.Name & ".Items", item.Items)
                If Not item.IsNonVisual Then b.AppendLine("  Me.Controls.Add(Me." & item.Name & ")")
            Next
            b.AppendLine("  Me.Name = """ & Vb(form.Name) & """").AppendLine("  Me.ResumeLayout(False)").AppendLine("  Me.PerformLayout()").AppendLine(" End Sub").AppendLine("End Class") : Return b.ToString()
        End Function
        Private Shared Function GeneratedTypeName(item As ControlData) As String
            If item.TypeName.Equals("SerialConnection", StringComparison.OrdinalIgnoreCase) Then Return "System.IO.Ports.SerialPort"
            If IsCustomControl(item.TypeName) Then Return "FlowForgeStudio." & item.TypeName
            Return item.TypeName
        End Function

        Private Shared Function IsCustomControl(typeName As String) As Boolean
            Return {"RoundedButton", "GradientPanel", "LedIndicator", "ToggleSwitch", "DigitalDisplay", "CircularProgress", "LevelMeter", "BadgeLabel", "SeparatorLine", "StarRating", "NumericKnob", "CardPanel", "BatteryIndicator", "SignalStrength", "ThermometerGauge", "AnalogGauge", "LoadingSpinner", "NotificationBanner", "ToggleButton", "ColorSwatch", "NavigationButton", "MarqueeLabel", "RichTextEditor", "SyntaxCodeEditor", "Sparkline", "ImageButton", "SearchBox", "PasswordBox", "IPAddressBox", "ModernDatePicker", "SimpleChart", "VirtualJoystick", "LcdDisplay", "LedMatrix", "TrafficLight", "SevenSegmentDigit", "ArduinoPin", "IoTSensor", "TagInput", "TabStripCustom", "JsonTreeViewer"}.Contains(typeName, StringComparer.OrdinalIgnoreCase)
        End Function

        Private Shared Function CustomControlsSource() As String
            Using stream As Stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("FlowForge.CustomControls.vb")
                If stream Is Nothing Then Throw New InvalidOperationException("O código runtime dos controles personalizados não foi encontrado.")
                Using reader As New StreamReader(stream, Encoding.UTF8)
                    Return reader.ReadToEnd()
                End Using
            End Using
        End Function
        Private Shared Sub AppendToolStripItemDeclarations(builder As StringBuilder, items As List(Of ToolStripItemData))
            If items Is Nothing Then Return
            For Each item As ToolStripItemData In items
                builder.AppendLine(" Friend WithEvents " & item.Name & " As " & item.TypeName)
                AppendToolStripItemDeclarations(builder, item.DropDownItems)
            Next
        End Sub
        Private Shared Sub AppendToolStripItemsInitialization(builder As StringBuilder, collectionExpression As String, items As List(Of ToolStripItemData))
            If items Is Nothing Then Return
            For Each item As ToolStripItemData In items
                builder.AppendLine("  Me." & item.Name & " = New " & item.TypeName & "()")
                For Each pair In item.Properties
                    If Not IsTransientGeneratedProperty(pair.Key, True) Then builder.AppendLine("  PropertyLoader.Apply(Me." & item.Name & ", """ & Vb(pair.Key) & """, """ & Vb(pair.Value) & """)")
                Next
                builder.AppendLine("  Me." & item.Name & ".Name = """ & Vb(item.Name) & """")
                builder.AppendLine("  Me." & item.Name & ".Text = """ & Vb(item.Text) & """")
                builder.AppendLine("  " & collectionExpression & ".Add(Me." & item.Name & ")")
                builder.AppendLine("  Me." & item.Name & ".Available = True")
                Dim visibleValue As String = Nothing
                If item.Properties IsNot Nothing AndAlso item.Properties.TryGetValue("Visible", visibleValue) Then
                    builder.AppendLine("  PropertyLoader.Apply(Me." & item.Name & ", ""Visible"", """ & Vb(visibleValue) & """)")
                End If
                AppendToolStripItemsInitialization(builder, "Me." & item.Name & ".DropDownItems", item.DropDownItems)
            Next
        End Sub
        Private Shared Function IsTransientGeneratedProperty(propertyName As String, toolStripItem As Boolean) As Boolean
            If propertyName.Equals("Visible", StringComparison.OrdinalIgnoreCase) Then Return True
            If toolStripItem AndAlso (propertyName.Equals("Available", StringComparison.OrdinalIgnoreCase) OrElse propertyName.Equals("Selected", StringComparison.OrdinalIgnoreCase) OrElse propertyName.Equals("Pressed", StringComparison.OrdinalIgnoreCase)) Then Return True
            Return False
        End Function
        Private Shared Function PropertyLoaderSource() As String
            Return "Imports System" & vbCrLf & "Imports System.ComponentModel" & vbCrLf & "Public Module PropertyLoader" & vbCrLf & " Public Sub Apply(target As Object, name As String, value As String)" & vbCrLf & "  Try" & vbCrLf & "   Dim p As PropertyDescriptor = TypeDescriptor.GetProperties(target)(name)" & vbCrLf & "   If p IsNot Nothing AndAlso Not p.IsReadOnly AndAlso p.Converter.CanConvertFrom(GetType(String)) Then p.SetValue(target, p.Converter.ConvertFromInvariantString(value))" & vbCrLf & "  Catch" & vbCrLf & "  End Try" & vbCrLf & " End Sub" & vbCrLf & "End Module"
        End Function
        Private Shared Function ColorComboBoxSource() As String
            Dim b As New StringBuilder()
            b.AppendLine("Imports System").AppendLine("Imports System.ComponentModel").AppendLine("Imports System.Drawing").AppendLine("Imports System.Linq").AppendLine("Imports System.Windows.Forms")
            b.AppendLine("Public Class ColorComboBox").AppendLine(" Inherits ComboBox").AppendLine(" Public Event SelectedColorChanged As EventHandler")
            b.AppendLine(" Public Sub New()").AppendLine("  DrawMode = DrawMode.OwnerDrawFixed : DropDownStyle = ComboBoxStyle.DropDownList : ItemHeight = 20")
            b.AppendLine("  Items.AddRange(New Object() {""Transparent"", ""Black"", ""White"", ""Gray"", ""Silver"", ""Red"", ""DarkRed"", ""Orange"", ""Gold"", ""Yellow"", ""Green"", ""Lime"", ""Teal"", ""Cyan"", ""Blue"", ""Navy"", ""Purple"", ""Magenta"", ""Brown"", ""Pink""}) : SelectedItem = ""Black""").AppendLine(" End Sub")
            b.AppendLine(" <Category(""Aparência"")> Public Property SelectedColor As Color").AppendLine("  Get").AppendLine("   If SelectedItem Is Nothing Then Return Color.Empty").AppendLine("   Return Color.FromName(CStr(SelectedItem))").AppendLine("  End Get")
            b.AppendLine("  Set(value As Color)").AppendLine("   Dim n As String = If(value.IsNamedColor, value.Name, ColorTranslator.ToHtml(value))").AppendLine("   If Not Items.Cast(Of Object)().Any(Function(x) String.Equals(CStr(x), n, StringComparison.OrdinalIgnoreCase)) Then Items.Add(n)").AppendLine("   SelectedItem = Items.Cast(Of Object)().FirstOrDefault(Function(x) String.Equals(CStr(x), n, StringComparison.OrdinalIgnoreCase))").AppendLine("  End Set").AppendLine(" End Property")
            b.AppendLine(" Protected Overrides Sub OnSelectedIndexChanged(e As EventArgs)").AppendLine("  MyBase.OnSelectedIndexChanged(e)").AppendLine("  RaiseEvent SelectedColorChanged(Me, e)").AppendLine(" End Sub")
            b.AppendLine(" Protected Overrides Sub OnDrawItem(e As DrawItemEventArgs)").AppendLine("  e.DrawBackground()").AppendLine("  If e.Index >= 0 Then").AppendLine("   Dim n As String = CStr(Items(e.Index))").AppendLine("   Dim c As Color").AppendLine("   Try").AppendLine("    c = ColorTranslator.FromHtml(n)").AppendLine("   Catch").AppendLine("    c = Color.FromName(n)").AppendLine("   End Try").AppendLine("   Dim r As New Rectangle(e.Bounds.X + 3, e.Bounds.Y + 3, 28, Math.Max(8, e.Bounds.Height - 6))").AppendLine("   Using br As New SolidBrush(c)").AppendLine("    e.Graphics.FillRectangle(br, r)").AppendLine("   End Using").AppendLine("   e.Graphics.DrawRectangle(Pens.DimGray, r)").AppendLine("   TextRenderer.DrawText(e.Graphics, n, e.Font, New Rectangle(e.Bounds.X + 38, e.Bounds.Y, e.Bounds.Width - 40, e.Bounds.Height), e.ForeColor, TextFormatFlags.VerticalCenter Or TextFormatFlags.Left)").AppendLine("  End If").AppendLine("  e.DrawFocusRectangle()").AppendLine(" End Sub").AppendLine("End Class")
            Return b.ToString()
        End Function
        Private Shared Function Vb(value As String) As String
            Dim result As String = If(value, "").Replace(ChrW(34), ChrW(34) & ChrW(34))
            Dim quote As String = ChrW(34)
            result = result.Replace(vbCrLf, quote & " & Environment.NewLine & " & quote)
            result = result.Replace(vbCr, quote & " & Environment.NewLine & " & quote)
            result = result.Replace(vbLf, quote & " & Environment.NewLine & " & quote)
            Return result
        End Function
        Private Shared Function SafeName(value As String) As String
            Dim result As String = New String(value.Where(Function(c) Char.IsLetterOrDigit(c) OrElse c = "_"c).ToArray()) : Return If(String.IsNullOrWhiteSpace(result), "Aplicativo", result)
        End Function
    End Class
End Namespace
