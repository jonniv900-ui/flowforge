Imports System
Imports System.Collections.Generic
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Json
Imports System.IO
Imports Microsoft.VisualBasic

Namespace FlowForgeStudio
    <DataContract>
    Public Class FlowProject
        ' Versiona a estrutura interna do arquivo .flowapp.
        ' Arquivos antigos não possuem este campo e são tratados como formato 1.
        <DataMember> Public Property FormatVersion As Integer = 2
        <DataMember> Public Property Name As String = "NovoProjeto"
        <DataMember> Public Property Version As String = "1.0.0"
        <DataMember> Public Property Forms As New List(Of FormData)()
        <DataMember> Public Property Classes As New List(Of ClassData)()
        <DataMember> Public Property Modules As New List(Of ModuleData)()
        <DataMember> Public Property UserControls As New List(Of UserControlData)()
        <DataMember> Public Property Folders As New List(Of ProjectFolderData)()
        <DataMember> Public Property ApplicationIcon As String = ""
        <DataMember> Public Property AssemblyTitle As String = ""
        <DataMember> Public Property AssemblyDescription As String = ""
        <DataMember> Public Property AssemblyCompany As String = ""
        <DataMember> Public Property AssemblyProduct As String = ""
        <DataMember> Public Property AssemblyCopyright As String = ""
        <DataMember> Public Property AssemblyTrademark As String = ""
        <DataMember> Public Property AssemblyVersion As String = "1.0.0.0"
        <DataMember> Public Property FileVersion As String = "1.0.0.0"
        <DataMember> Public Property Libraries As New List(Of ProjectLibrary)()
    End Class

    <DataContract>
    Public Class ProjectLibrary
        <DataMember> Public Property FileName As String = ""
        <DataMember> Public Property RelativePath As String = ""
        <DataMember> Public Property DataBase64 As String = ""
        <DataMember> Public Property IsReference As Boolean
        <DataMember> Public Property EmbedInExecutable As Boolean = True
        <DataMember> Public Property FileSize As Long
    End Class

    <DataContract>
    Public Class FormData
        <DataMember> Public Property Name As String = "Form1"
        <DataMember> Public Property IsStartup As Boolean
        <DataMember> Public Property Properties As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        <DataMember> Public Property Controls As New List(Of ControlData)()
        <DataMember> Public Property Code As String = ""
        <DataMember> Public Property FolderPath As String = ""

        Public Shared Function CreateDefault(name As String, startup As Boolean) As FormData
            Dim item As New FormData With {.Name = name, .IsStartup = startup}
            item.Properties("Text") = name
            item.Properties("ClientSize") = "800, 500"
            item.Properties("StartPosition") = "CenterScreen"
            item.Properties("FormBorderStyle") = "Sizable"
            item.Properties("BackColor") = "White"
            item.Properties("Font") = "Segoe UI, 9pt"
            item.Properties("MaximizeBox") = "True"
            item.Properties("MinimizeBox") = "True"
            item.Properties("KeyPreview") = "False"
            item.Code = "Imports System" & vbCrLf & "Imports System.Windows.Forms" & vbCrLf & "Imports System.Drawing" & vbCrLf & "Imports System.IO" & vbCrLf & "Imports System.Net.Http" & vbCrLf & vbCrLf & "Partial Public Class " & name & vbCrLf & "    Private Sub " & name & "_Load() Handles MyBase.Load" & vbCrLf & "        ' Olá! Este código executa quando a janela abre." & vbCrLf & "        ' Arraste um componente e clique duas vezes nele para criar uma ação." & vbCrLf & "    End Sub" & vbCrLf & vbCrLf & "End Class"
            Return item
        End Function
    End Class

    <DataContract>
    Public Class ClassData
        <DataMember> Public Property Name As String = "Class1"
        <DataMember> Public Property Code As String = ""
        <DataMember> Public Property FolderPath As String = ""

        Public Shared Function CreateDefault(name As String) As ClassData
            Dim item As New ClassData With {.Name = name}
            item.Code = "Imports System" & vbCrLf & vbCrLf & "Public Class " & name & vbCrLf & vbCrLf & "    Public Sub New()" & vbCrLf & "        ' Inicialização da classe." & vbCrLf & "    End Sub" & vbCrLf & vbCrLf & "End Class"
            Return item
        End Function
    End Class


    <DataContract>
    Public Class ModuleData
        <DataMember> Public Property Name As String = "Module1"
        <DataMember> Public Property Code As String = ""
        <DataMember> Public Property FolderPath As String = ""

        Public Shared Function CreateDefault(name As String) As ModuleData
            Dim item As New ModuleData With {.Name = name}
            item.Code = "Imports System" & vbCrLf & vbCrLf & "Public Module " & name & vbCrLf & vbCrLf & "    ' Métodos e funções compartilhados do módulo." & vbCrLf & vbCrLf & "End Module"
            Return item
        End Function
    End Class

    <DataContract>
    Public Class UserControlData
        <DataMember> Public Property Name As String = "UserControl1"
        <DataMember> Public Property Code As String = ""
        <DataMember> Public Property FolderPath As String = ""
        <DataMember> Public Property Properties As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        <DataMember> Public Property Controls As New List(Of ControlData)()

        Public Shared Function CreateDefault(name As String) As UserControlData
            Dim item As New UserControlData With {.Name = name}
            item.Properties("ClientSize") = "240, 150"
            item.Properties("BackColor") = "White"
            item.Properties("Font") = "Segoe UI, 9pt"
            item.Code = "Imports System" & vbCrLf & "Imports System.Drawing" & vbCrLf & "Imports System.Windows.Forms" & vbCrLf & vbCrLf & "Partial Public Class " & name & vbCrLf & "    Inherits UserControl" & vbCrLf & vbCrLf & "    ' Adicione aqui a lógica do seu UserControl." & vbCrLf & vbCrLf & "End Class"
            Return item
        End Function
    End Class

    <DataContract>
    Public Class ProjectFolderData
        <DataMember> Public Property Path As String = ""
    End Class

    <DataContract>
    Public Class ControlData
        <DataMember> Public Property TypeName As String = "Button"
        <DataMember> Public Property Name As String = "Button1"
        <DataMember> Public Property AssemblyPath As String = ""
        <DataMember> Public Property IsNonVisual As Boolean
        <DataMember> Public Property Properties As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        <DataMember> Public Property Items As New List(Of ToolStripItemData)()
    End Class

    <DataContract>
    Public Class ToolStripItemData
        <DataMember> Public Property TypeName As String = "ToolStripButton"
        <DataMember> Public Property Name As String = "ToolStripButton1"
        <DataMember> Public Property Text As String = "Item"
        <DataMember> Public Property Properties As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        <DataMember> Public Property DropDownItems As New List(Of ToolStripItemData)()
    End Class

    Public NotInheritable Class ProjectStorage
        ' Formato 1 = projetos históricos, sem o campo FormatVersion.
        ' Formato 2 = formato atual, com versionamento explícito.
        Public Const CurrentFormatVersion As Integer = 2
        Public Const LegacyFormatVersion As Integer = 1

        Private Sub New()
        End Sub

        Public Shared Sub Save(path As String, project As FlowProject)
            If project Is Nothing Then Throw New ArgumentNullException(NameOf(project))

            ' Qualquer projeto salvo pela versão atual passa a usar o formato atual.
            project.FormatVersion = CurrentFormatVersion
            NormalizeProject(project)

            Dim serializer As New DataContractJsonSerializer(GetType(FlowProject))
            Using stream As FileStream = File.Create(path)
                serializer.WriteObject(stream, project)
            End Using
        End Sub

        Public Shared Function Load(path As String) As FlowProject
            Dim serializer As New DataContractJsonSerializer(GetType(FlowProject))

            Using stream As FileStream = File.OpenRead(path)
                Dim project = DirectCast(serializer.ReadObject(stream), FlowProject)

                If project Is Nothing Then
                    Throw New InvalidDataException("O arquivo .flowapp está vazio ou é inválido.")
                End If

                ' DataContractJsonSerializer deixa Integer ausente como 0.
                ' Portanto um .flowapp antigo, sem FormatVersion, é formato 1.
                Dim sourceFormatVersion As Integer = project.FormatVersion
                If sourceFormatVersion <= 0 Then
                    sourceFormatVersion = LegacyFormatVersion
                End If

                ' Nunca tenta abrir silenciosamente um formato mais novo.
                If sourceFormatVersion > CurrentFormatVersion Then
                    Throw New InvalidDataException(
                        "Este projeto usa o formato .flowapp " & sourceFormatVersion.ToString() &
                        ", mas esta versão do FlowForge suporta até o formato " & CurrentFormatVersion.ToString() & "." &
                        Environment.NewLine &
                        "Atualize o FlowForge para abrir este projeto.")
                End If

                If project.Forms Is Nothing OrElse project.Forms.Count = 0 Then
                    Throw New InvalidDataException(
                        "Este arquivo pertence à versão WPF antiga. Crie um projeto novo na edição Windows Forms 4.8.")
                End If

                ' Migra somente em memória. O arquivo original não é regravado
                ' até o usuário escolher Salvar.
                MigrateProject(project, sourceFormatVersion)
                NormalizeProject(project)

                project.FormatVersion = CurrentFormatVersion
                Return project
            End Using
        End Function

        Private Shared Sub MigrateProject(project As FlowProject, sourceFormatVersion As Integer)
            Dim version As Integer = Math.Max(LegacyFormatVersion, sourceFormatVersion)

            ' v1 -> v2
            ' O formato 2 introduz o campo FormatVersion.
            ' Coleções criadas em versões mais novas são inicializadas em
            ' NormalizeProject, mantendo compatibilidade com .flowapp antigos.
            If version < 2 Then
                version = 2
            End If

            project.FormatVersion = version
        End Sub

        Private Shared Sub NormalizeProject(project As FlowProject)
            If project.Libraries Is Nothing Then
                project.Libraries = New List(Of ProjectLibrary)()
            End If
            If project.Classes Is Nothing Then
                project.Classes = New List(Of ClassData)()
            End If
            If project.Modules Is Nothing Then
                project.Modules = New List(Of ModuleData)()
            End If
            If project.UserControls Is Nothing Then
                project.UserControls = New List(Of UserControlData)()
            End If

            For Each uc As UserControlData In project.UserControls
                If uc.Properties Is Nothing Then
                    uc.Properties = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
                End If
                If uc.Controls Is Nothing Then
                    uc.Controls = New List(Of ControlData)()
                End If
                If Not uc.Properties.ContainsKey("ClientSize") Then
                    uc.Properties("ClientSize") = "240, 150"
                End If
                If Not uc.Properties.ContainsKey("BackColor") Then
                    uc.Properties("BackColor") = "White"
                End If
            Next

            If project.Folders Is Nothing Then
                project.Folders = New List(Of ProjectFolderData)()
            End If
        End Sub
    End Class

    Public Class ToolboxEntry
        Public Property Category As String
        Public Property TypeName As String
        Public Property DisplayName As String
        Public Property Glyph As String
        Public Property DefaultEvent As String
        Public Property AssemblyPath As String = ""
        Public Property IsNonVisual As Boolean
        Public Overrides Function ToString() As String
            Return DisplayName
        End Function
    End Class
End Namespace
