Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions

Namespace FlowForgeStudio
    Friend NotInheritable Class LearningNotebook
        Private Sub New()
        End Sub

        Private Shared ReadOnly Property NotebookFile As String
            Get
                Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlowForgeEducation", "MeuCaderno.txt")
            End Get
        End Property

        Public Shared Function LoadText() As String
            Try
                If File.Exists(NotebookFile) Then Return File.ReadAllText(NotebookFile)
            Catch
            End Try
            Return "Escreva aqui o que você aprendeu, ideias para aplicativos e comandos que deseja lembrar."
        End Function

        Public Shared Function SaveText(value As String) As Boolean
            Try
                Directory.CreateDirectory(Path.GetDirectoryName(NotebookFile))
                File.WriteAllText(NotebookFile, If(value, ""), Encoding.UTF8)
                Return True
            Catch
                Return False
            End Try
        End Function
    End Class

    Friend NotInheritable Class LearningProgress
        Private Shared ReadOnly Completed As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Private Shared loaded As Boolean

        Private Sub New()
        End Sub

        Private Shared ReadOnly Property ProgressFile As String
            Get
                Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlowForgeEducation", "learning-progress.txt")
            End Get
        End Property

        Private Shared Sub EnsureLoaded()
            If loaded Then Return
            loaded = True
            Try
                If File.Exists(ProgressFile) Then
                    For Each line As String In File.ReadAllLines(ProgressFile)
                        If Not String.IsNullOrWhiteSpace(line) Then Completed.Add(line.Trim())
                    Next
                End If
            Catch
            End Try
        End Sub

        Public Shared Function IsCompleted(key As String) As Boolean
            EnsureLoaded()
            Return Completed.Contains(key)
        End Function

        Public Shared Function MarkCompleted(key As String) As Boolean
            EnsureLoaded()
            Dim added As Boolean = Completed.Add(key)
            If added Then Save()
            Return added
        End Function

        Public Shared ReadOnly Property Count As Integer
            Get
                EnsureLoaded()
                Return Completed.Count
            End Get
        End Property

        Private Shared Sub Save()
            Try
                Dim folder As String = Path.GetDirectoryName(ProgressFile)
                Directory.CreateDirectory(folder)
                File.WriteAllLines(ProgressFile, Completed.OrderBy(Function(x) x).ToArray())
            Catch
            End Try
        End Sub
    End Class

    Friend NotInheritable Class LearningCodeChecker
        Private Sub New()
        End Sub

        Public Shared Function Check(source As String) As String
            If String.IsNullOrWhiteSpace(source) Then Return "Ainda não há código para verificar."
            Dim problems As New List(Of String)()
            CheckPair(source, "If", "End If", "^\s*If\b.*\bThen\b", "\bEnd\s+If\b", problems)
            CheckPair(source, "Sub", "End Sub", "\b(Sub)\s+[A-Za-z_]", "\bEnd\s+Sub\b", problems)
            CheckPair(source, "Function", "End Function", "\bFunction\s+[A-Za-z_]", "\bEnd\s+Function\b", problems)
            CheckPair(source, "Try", "End Try", "^\s*Try\s*$", "\bEnd\s+Try\b", problems)
            CheckPair(source, "Select Case", "End Select", "\bSelect\s+Case\b", "\bEnd\s+Select\b", problems)
            CheckPair(source, "For", "Next", "^\s*For\b", "^\s*Next\b", problems)
            CheckPair(source, "While", "End While", "^\s*While\b", "\bEnd\s+While\b", problems)

            Dim lines As String() = source.Replace(Environment.NewLine, Convert.ToChar(10).ToString()).Split(Convert.ToChar(10))
            For index As Integer = 0 To lines.Length - 1
                Dim withoutComment As String = Regex.Replace(lines(index), "'.*$", "")
                Dim quotes As Integer = withoutComment.Where(Function(character) character = Convert.ToChar(34)).Count()
                If quotes Mod 2 <> 0 Then problems.Add("Linha " & (index + 1).ToString() & ": parece faltar uma aspa dupla.")
            Next

            Dim result As New StringBuilder()
            result.AppendLine("VERIFICAÇÃO DO CÓDIGO").AppendLine()
            If problems.Count = 0 Then
                result.AppendLine("✓ A estrutura básica parece correta.")
                result.AppendLine("✓ Os principais blocos estão fechados.")
                result.AppendLine("✓ Não encontrei aspas abertas.")
                result.AppendLine().AppendLine("Agora execute com F5 para testar o comportamento do aplicativo.")
            Else
                result.AppendLine("Encontrei " & problems.Count.ToString() & " ponto(s) para revisar:").AppendLine()
                For Each problem As String In problems.Distinct()
                    result.AppendLine("• " & problem)
                Next
                result.AppendLine().AppendLine("Dica: clique no número da linha para ir até ela.")
            End If
            Return result.ToString()
        End Function

        Private Shared Sub CheckPair(source As String, openingName As String, closingName As String, openingPattern As String, closingPattern As String, problems As List(Of String))
            Dim openingCount As Integer = Regex.Matches(source, openingPattern, RegexOptions.IgnoreCase Or RegexOptions.Multiline).Count
            Dim closingCount As Integer = Regex.Matches(source, closingPattern, RegexOptions.IgnoreCase Or RegexOptions.Multiline).Count
            If openingCount > closingCount Then problems.Add("Há " & openingName & " sem " & closingName & ".")
            If closingCount > openingCount Then problems.Add("Há " & closingName & " sem o início correspondente.")
        End Sub
    End Class

    Friend NotInheritable Class CodeMapBuilder
        Private Sub New()
        End Sub

        Public Shared Function Build(source As String) As String
            If String.IsNullOrWhiteSpace(source) Then Return "Ainda não há código para mapear."
            Dim importsFound As New List(Of String)()
            Dim routines As New List(Of String)()
            Dim variables As New List(Of String)()
            Dim eventsFound As New List(Of String)()
            For Each rawLine As String In source.Replace(Environment.NewLine, Convert.ToChar(10).ToString()).Split(Convert.ToChar(10))
                Dim line As String = rawLine.Trim()
                Dim match As Match = Regex.Match(line, "^Imports\s+(.+)$", RegexOptions.IgnoreCase)
                If match.Success Then importsFound.Add(match.Groups(1).Value.Trim())
                match = Regex.Match(line, "^(?:Public|Private|Protected|Friend)?\s*(Sub|Function)\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.IgnoreCase)
                If match.Success Then
                    routines.Add(match.Groups(2).Value & " (" & match.Groups(1).Value & ")")
                    Dim handlesMatch As Match = Regex.Match(line, "\bHandles\s+(.+)$", RegexOptions.IgnoreCase)
                    If handlesMatch.Success Then eventsFound.Add(handlesMatch.Groups(1).Value.Trim())
                End If
                match = Regex.Match(line, "^(?:Public|Private|Protected|Friend|Dim)\s+([A-Za-z_][A-Za-z0-9_]*)\s+As\s+([^=]+)", RegexOptions.IgnoreCase)
                If match.Success AndAlso line.IndexOf(" Sub ", StringComparison.OrdinalIgnoreCase) < 0 AndAlso line.IndexOf(" Function ", StringComparison.OrdinalIgnoreCase) < 0 Then variables.Add(match.Groups(1).Value & " : " & match.Groups(2).Value.Trim())
            Next
            Dim result As New StringBuilder("MAPA DO CÓDIGO")
            AppendSection(result, "Bibliotecas usadas", importsFound)
            AppendSection(result, "Rotinas", routines)
            AppendSection(result, "Eventos ligados", eventsFound)
            AppendSection(result, "Variáveis encontradas", variables)
            result.AppendLine().AppendLine("Resumo: " & routines.Count.ToString() & " rotina(s), " & eventsFound.Count.ToString() & " evento(s) e " & variables.Count.ToString() & " variável(is).")
            Return result.ToString()
        End Function

        Private Shared Sub AppendSection(result As StringBuilder, title As String, values As IEnumerable(Of String))
            result.AppendLine().AppendLine(title.ToUpperInvariant())
            Dim distinctValues As String() = values.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            If distinctValues.Length = 0 Then
                result.AppendLine("• Nenhum item encontrado")
            Else
                For Each value As String In distinctValues
                    result.AppendLine("• " & value)
                Next
            End If
        End Sub
    End Class

    Friend NotInheritable Class ProjectHealthChecker
        Private Sub New()
        End Sub

        Public Shared Function Check(project As FlowProject) As String
            If project Is Nothing Then Return "Nenhum projeto está aberto."
            Dim warnings As New List(Of String)()
            If project.Forms Is Nothing OrElse project.Forms.Count = 0 Then
                warnings.Add("O projeto não possui nenhum Form.")
            Else
                If Not project.Forms.Any(Function(form) form.IsStartup) Then warnings.Add("Nenhum Form foi definido como inicial.")
                If project.Forms.Where(Function(form) form.IsStartup).Count() > 1 Then warnings.Add("Mais de um Form está marcado como inicial.")
                Dim formNames As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                For Each form As FormData In project.Forms
                    If Not IsValidName(form.Name) Then warnings.Add("O nome do Form '" & form.Name & "' não é um identificador VB válido.")
                    If Not formNames.Add(form.Name) Then warnings.Add("Há Forms repetidos com o nome '" & form.Name & "'.")
                    If String.IsNullOrWhiteSpace(form.Code) Then warnings.Add(form.Name & " não possui código.")
                    Dim componentNames As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                    If form.Controls IsNot Nothing Then
                        For Each component As ControlData In form.Controls
                            If Not IsValidName(component.Name) Then warnings.Add(form.Name & ": o componente '" & component.Name & "' possui nome inválido.")
                            If Not componentNames.Add(component.Name) Then warnings.Add(form.Name & ": o nome '" & component.Name & "' está repetido.")
                        Next
                    End If
                Next
            End If

            Dim result As New StringBuilder("SAÚDE DO PROJETO")
            result.AppendLine().AppendLine("Projeto: " & project.Name)
            result.AppendLine("Forms: " & If(project.Forms Is Nothing, 0, project.Forms.Count).ToString())
            If warnings.Count = 0 Then
                result.AppendLine().AppendLine("✓ Estrutura do projeto aprovada.")
                result.AppendLine("✓ Existe um Form inicial.")
                result.AppendLine("✓ Os nomes de Forms e componentes são únicos e válidos.")
            Else
                result.AppendLine().AppendLine("Revise " & warnings.Count.ToString() & " ponto(s):")
                For Each warning As String In warnings.Distinct()
                    result.AppendLine("• " & warning)
                Next
            End If
            Return result.ToString()
        End Function

        Private Shared Function IsValidName(value As String) As Boolean
            Return Not String.IsNullOrWhiteSpace(value) AndAlso Regex.IsMatch(value, "^[A-Za-z_][A-Za-z0-9_]*$")
        End Function
    End Class
End Namespace
