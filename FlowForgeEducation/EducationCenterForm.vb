Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Windows.Forms

Namespace FlowForgeStudio
    Friend Class EducationCenterForm
        Inherits Form

        Private ReadOnly tabs As New TabControl()
        Private ReadOnly lessons As New ListBox()
        Private ReadOnly lessonText As New RichTextBox()
        Private ReadOnly glossaryTerms As New ListBox()
        Private ReadOnly glossaryText As New RichTextBox()
        Private ReadOnly challenges As New ListBox()
        Private ReadOnly challengeText As New RichTextBox()
        Private ReadOnly errorTerms As New ListBox()
        Private ReadOnly errorText As New RichTextBox()
        Private ReadOnly progressText As New RichTextBox()
        Private ReadOnly sourceCode As New RichTextBox()
        Private ReadOnly explanation As New RichTextBox()
        Private ReadOnly progressLabel As New Label()
        Private ReadOnly lessonCompleteButton As New Button()
        Private ReadOnly challengeCompleteButton As New Button()
        Private ReadOnly notebook As New RichTextBox()
        Private ReadOnly quizQuestion As New Label()
        Private ReadOnly quizFeedback As New Label()
        Private ReadOnly quizOptions As RadioButton() = {New RadioButton(), New RadioButton(), New RadioButton()}
        Private quizIndex As Integer
        Private quizScore As Integer

        Public Sub New(Optional selectedCode As String = "", Optional selectedTab As Integer = 0)
            Text = "Central de Aprendizado — FlowForge Education"
            Width = 900
            Height = 650
            MinimumSize = New Size(720, 500)
            StartPosition = FormStartPosition.CenterParent
            Font = New Font("Segoe UI", 10.0F)
            BackColor = Color.White
            BuildInterface()
            LoadContent()
            sourceCode.Text = selectedCode
            tabs.SelectedIndex = Math.Max(0, Math.Min(selectedTab, tabs.TabPages.Count - 1))
            If sourceCode.Text.Trim() <> "" Then explanation.Text = CodeTeacher.Explain(sourceCode.Text)
        End Sub

        Private Sub BuildInterface()
            tabs.Dock = DockStyle.Fill
            tabs.TabPages.Add(BuildWelcomeTab())
            Dim tutorialPage As TabPage = BuildSplitTab("Tutorial", lessons, lessonText)
            AddCompletionFooter(tutorialPage, lessonCompleteButton, AddressOf CompleteLesson)
            tabs.TabPages.Add(tutorialPage)
            tabs.TabPages.Add(BuildSplitTab("Glossário", glossaryTerms, glossaryText))
            Dim challengePage As TabPage = BuildSplitTab("Desafios", challenges, challengeText)
            AddCompletionFooter(challengePage, challengeCompleteButton, AddressOf CompleteChallenge)
            tabs.TabPages.Add(challengePage)
            tabs.TabPages.Add(BuildSplitTab("Erros comuns", errorTerms, errorText))
            tabs.TabPages.Add(BuildProgressTab())
            tabs.TabPages.Add(BuildExplainTab())
            tabs.TabPages.Add(BuildQuizTab())
            tabs.TabPages.Add(BuildNotebookTab())
            tabs.TabPages.Add(BuildShortcutsTab())
            Controls.Add(tabs)
        End Sub

        Private Function BuildQuizTab() As TabPage
            Dim page As New TabPage("Quiz VB") With {.BackColor = Color.White, .Padding = New Padding(35)}
            quizQuestion.Dock = DockStyle.Top
            quizQuestion.Height = 90
            quizQuestion.Font = New Font("Segoe UI", 16.0F, FontStyle.Bold)
            Dim choices As New FlowLayoutPanel With {.Dock = DockStyle.Top, .Height = 155, .FlowDirection = FlowDirection.TopDown, .WrapContents = False}
            For Each optionButton As RadioButton In quizOptions
                optionButton.AutoSize = True
                optionButton.Font = New Font("Segoe UI", 12.0F)
                optionButton.Margin = New Padding(8)
                choices.Controls.Add(optionButton)
            Next
            Dim answerButton As New Button With {.Text = "Responder", .Dock = DockStyle.Top, .Height = 42, .BackColor = Color.FromArgb(52, 120, 200), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            quizFeedback.Dock = DockStyle.Top
            quizFeedback.Height = 100
            quizFeedback.Padding = New Padding(8, 18, 8, 8)
            quizFeedback.Font = New Font("Segoe UI", 11.0F, FontStyle.Bold)
            AddHandler answerButton.Click, AddressOf AnswerQuiz
            page.Controls.Add(quizFeedback)
            page.Controls.Add(answerButton)
            page.Controls.Add(choices)
            page.Controls.Add(quizQuestion)
            ShowQuizQuestion()
            Return page
        End Function

        Private Function BuildNotebookTab() As TabPage
            Dim page As New TabPage("Meu caderno") With {.BackColor = Color.White, .Padding = New Padding(18)}
            notebook.Dock = DockStyle.Fill
            notebook.Font = New Font("Segoe UI", 12.0F)
            notebook.AcceptsTab = True
            notebook.Text = LearningNotebook.LoadText()
            Dim saveButton As New Button With {.Text = "Salvar meu caderno", .Dock = DockStyle.Bottom, .Height = 42, .BackColor = Color.FromArgb(52, 120, 200), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            AddHandler saveButton.Click, AddressOf SaveNotebook
            page.Controls.Add(notebook)
            page.Controls.Add(saveButton)
            Return page
        End Function

        Private Function BuildShortcutsTab() As TabPage
            Dim page As New TabPage("Atalhos") With {.BackColor = Color.White, .Padding = New Padding(24)}
            Dim text As New RichTextBox With {.Dock = DockStyle.Fill, .ReadOnly = True, .BorderStyle = BorderStyle.None, .BackColor = Color.White, .Font = New Font("Segoe UI", 11.0F)}
            text.Text = "ATALHOS IMPORTANTES" & Environment.NewLine & Environment.NewLine & "F5 — executar o aplicativo" & Environment.NewLine & "F7 — abrir o código" & Environment.NewLine & "Shift+F7 — abrir o designer" & Environment.NewLine & "F4 — propriedades" & Environment.NewLine & "Ctrl+S — salvar" & Environment.NewLine & "Ctrl+Espaço — abrir o autocompletar" & Environment.NewLine & "Ctrl+F7 — verificar meu código" & Environment.NewLine & "Ctrl+F — localizar" & Environment.NewLine & "Ctrl+G — ir para linha" & Environment.NewLine & "Ctrl+Z / Ctrl+Y — desfazer / refazer" & Environment.NewLine & "Delete — excluir componente" & Environment.NewLine & "Setas — mover componente" & Environment.NewLine & "Shift+setas — redimensionar componente" & Environment.NewLine & "Ctrl+setas — mover pela grade"
            page.Controls.Add(text)
            Return page
        End Function

        Private Sub SaveNotebook(sender As Object, e As EventArgs)
            If LearningNotebook.SaveText(notebook.Text) Then
                MessageBox.Show("Seu caderno foi salvo.", "Meu caderno", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                MessageBox.Show("Não foi possível salvar o caderno.", "Meu caderno", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        End Sub

        Private Sub ShowQuizQuestion()
            Dim questions As String() = {"Qual palavra cria uma variável?", "Qual evento acontece ao clicar em um Button?", "Como terminamos um bloco If?", "Qual propriedade altera o texto visível?", "Qual comando repete de 1 até 5?", "Qual valor Boolean significa verdadeiro?"}
            Dim answers As String(,) = {{"Dim", "Show", "End"}, {"Load", "Click", "Tick"}, {"Next", "End If", "End Sub"}, {"Name", "Text", "Size"}, {"For numero As Integer = 1 To 5", "If numero = 5 Then", "Dim numero = 5"}, {"True", "Nothing", "False"}}
            quizQuestion.Text = "Pergunta " & (quizIndex + 1).ToString() & " de " & questions.Length.ToString() & Environment.NewLine & questions(quizIndex)
            For optionIndex As Integer = 0 To quizOptions.Length - 1
                quizOptions(optionIndex).Text = answers(quizIndex, optionIndex)
                quizOptions(optionIndex).Checked = False
            Next
            quizFeedback.Text = "Escolha uma resposta."
            quizFeedback.ForeColor = Color.DimGray
        End Sub

        Private Sub AnswerQuiz(sender As Object, e As EventArgs)
            Dim selected As Integer = -1
            For index As Integer = 0 To quizOptions.Length - 1
                If quizOptions(index).Checked Then selected = index
            Next
            If selected < 0 Then
                quizFeedback.Text = "Escolha uma das três respostas."
                Return
            End If
            Dim correctAnswers As Integer() = {0, 1, 1, 1, 0, 0}
            If selected = correctAnswers(quizIndex) Then
                quizScore += 1
                quizFeedback.Text = "✓ Resposta correta!"
                quizFeedback.ForeColor = Color.DarkGreen
            Else
                quizFeedback.Text = "Ainda não. A resposta correta está destacada."
                quizFeedback.ForeColor = Color.Firebrick
                quizOptions(correctAnswers(quizIndex)).Checked = True
            End If
            quizIndex += 1
            If quizIndex >= correctAnswers.Length Then
                MessageBox.Show("Você acertou " & quizScore.ToString() & " de " & correctAnswers.Length.ToString() & " perguntas!", "Resultado do Quiz VB", MessageBoxButtons.OK, MessageBoxIcon.Information)
                If quizScore >= 4 Then LearningProgress.MarkCompleted("quiz:vb-basico")
                quizIndex = 0
                quizScore = 0
                UpdateProgressDisplay()
            End If
            BeginInvoke(New MethodInvoker(AddressOf ShowQuizQuestion))
        End Sub

        Private Function BuildProgressTab() As TabPage
            Dim page As New TabPage("Meu progresso") With {.BackColor = Color.White, .Padding = New Padding(24)}
            progressText.Dock = DockStyle.Fill
            progressText.ReadOnly = True
            progressText.BorderStyle = BorderStyle.None
            progressText.BackColor = Color.White
            progressText.Font = New Font("Segoe UI", 12.0F)
            Dim buttons As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .Height = 50, .FlowDirection = FlowDirection.RightToLeft, .Padding = New Padding(6)}
            Dim refreshButton As New Button With {.Text = "Atualizar progresso", .Width = 170, .Height = 34, .BackColor = Color.FromArgb(52, 120, 200), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            Dim exportButton As New Button With {.Text = "Exportar relatório...", .Width = 170, .Height = 34}
            AddHandler refreshButton.Click, Sub() UpdateProgressDisplay()
            AddHandler exportButton.Click, AddressOf ExportProgress
            buttons.Controls.Add(refreshButton)
            buttons.Controls.Add(exportButton)
            page.Controls.Add(progressText)
            page.Controls.Add(buttons)
            Return page
        End Function

        Private Sub ExportProgress(sender As Object, e As EventArgs)
            UpdateProgressDisplay()
            Using dialog As New SaveFileDialog With {.Filter = "Relatório de texto (*.txt)|*.txt", .FileName = "MeuProgressoFlowForge.txt", .DefaultExt = "txt"}
                If dialog.ShowDialog(Me) <> DialogResult.OK Then Return
                Try
                    System.IO.File.WriteAllText(dialog.FileName, progressText.Text, Encoding.UTF8)
                    MessageBox.Show("Relatório salvo com sucesso.", "Meu progresso", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    MessageBox.Show("Não foi possível salvar o relatório: " & ex.Message, "Meu progresso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End Try
            End Using
        End Sub

        Private Sub AddCompletionFooter(page As TabPage, button As Button, action As EventHandler)
            Dim footer As New Panel With {.Dock = DockStyle.Bottom, .Height = 52, .Padding = New Padding(12, 8, 12, 8), .BackColor = Color.FromArgb(239, 245, 252)}
            button.Text = "Marcar como concluído"
            button.Dock = DockStyle.Right
            button.Width = 190
            button.BackColor = Color.FromArgb(52, 120, 200)
            button.ForeColor = Color.White
            button.FlatStyle = FlatStyle.Flat
            AddHandler button.Click, action
            progressLabel.AutoSize = True
            progressLabel.Location = New Point(12, 16)
            progressLabel.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
            footer.Controls.Add(button)
            If page.Text = "Tutorial" Then footer.Controls.Add(progressLabel)
            page.Controls.Add(footer)
            footer.BringToFront()
        End Sub

        Private Function BuildWelcomeTab() As TabPage
            Dim page As New TabPage("Começar")
            page.BackColor = Color.White
            Dim title As New Label With {.Text = "Aprenda programação criando aplicativos", .Font = New Font("Segoe UI", 22.0F, FontStyle.Bold), .ForeColor = Color.FromArgb(38, 91, 166), .Location = New Point(35, 35), .AutoSize = True}
            Dim body As New Label With {.Text = "1. Arraste um componente para o Form." & Environment.NewLine & "2. Altere suas propriedades." & Environment.NewLine & "3. Clique duas vezes para abrir o evento." & Environment.NewLine & "4. Escreva uma ação em Visual Basic." & Environment.NewLine & "5. Pressione F5 para ver o resultado.", .Font = New Font("Segoe UI", 14.0F), .Location = New Point(40, 105), .Size = New Size(760, 190)}
            Dim example As New RichTextBox With {.ReadOnly = True, .BackColor = Color.FromArgb(245, 248, 252), .BorderStyle = BorderStyle.FixedSingle, .Font = New Font("Consolas", 12.0F), .Text = "Private Sub Button1_Click() Handles Button1.Click" & Environment.NewLine & "    MessageBox.Show(""Olá! Eu criei meu primeiro programa!"")" & Environment.NewLine & "End Sub", .Location = New Point(40, 315), .Size = New Size(760, 105)}
            Dim hint As New Label With {.Text = "Dica: você não precisa decorar tudo. Experimente, execute e observe o que muda.", .ForeColor = Color.DarkGreen, .Font = New Font("Segoe UI", 11.0F, FontStyle.Bold), .Location = New Point(40, 450), .Size = New Size(760, 45)}
            page.Controls.Add(title)
            page.Controls.Add(body)
            page.Controls.Add(example)
            page.Controls.Add(hint)
            Return page
        End Function

        Private Function BuildSplitTab(title As String, list As ListBox, text As RichTextBox) As TabPage
            Dim page As New TabPage(title)
            Dim split As New SplitContainer With {.Dock = DockStyle.Fill, .SplitterDistance = 245}
            list.Dock = DockStyle.Fill
            list.Font = New Font("Segoe UI", 10.0F)
            text.Dock = DockStyle.Fill
            text.ReadOnly = True
            text.BackColor = Color.White
            text.BorderStyle = BorderStyle.None
            text.Font = New Font("Segoe UI", 11.0F)
            split.Panel1.Controls.Add(list)
            split.Panel2.Padding = New Padding(16)
            split.Panel2.Controls.Add(text)
            page.Controls.Add(split)
            Return page
        End Function

        Private Function BuildExplainTab() As TabPage
            Dim page As New TabPage("Entenda o código")
            Dim split As New SplitContainer With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal, .SplitterDistance = 245}
            sourceCode.Dock = DockStyle.Fill
            sourceCode.Font = New Font("Consolas", 11.0F)
            sourceCode.AcceptsTab = True
            Dim explainButton As New Button With {.Text = "Explicar este código", .Dock = DockStyle.Bottom, .Height = 38, .BackColor = Color.FromArgb(52, 120, 200), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            explanation.Dock = DockStyle.Fill
            explanation.ReadOnly = True
            explanation.BackColor = Color.White
            explanation.Font = New Font("Segoe UI", 11.0F)
            AddHandler explainButton.Click, Sub() explanation.Text = CodeTeacher.Explain(sourceCode.Text)
            split.Panel1.Controls.Add(sourceCode)
            split.Panel1.Controls.Add(explainButton)
            split.Panel2.Controls.Add(explanation)
            page.Controls.Add(split)
            Return page
        End Function

        Private Sub LoadContent()
            lessons.Items.AddRange(New Object() {"1 — Meu primeiro botão", "2 — Textos e propriedades", "3 — Variáveis", "4 — If, Then e Else", "5 — Repetições", "6 — Listas", "7 — Vários Forms", "8 — Arquivos e imagens"})
            glossaryTerms.Items.AddRange(New Object() {"Algoritmo", "Aplicativo", "Boolean", "Classe", "Componente", "Evento", "Form", "Function", "If / Then / Else", "Integer", "Loop", "Método", "Propriedade", "String", "Sub", "Variável"})
            challenges.Items.AddRange(New Object() {"⭐ Olá, mundo!", "⭐ Botão contador", "⭐ Troca de cores", "⭐⭐ Quiz de três perguntas", "⭐⭐ Conversor de temperatura", "⭐⭐ Lista de tarefas", "⭐⭐⭐ Jogo de adivinhação", "⭐⭐⭐ Editor de texto"})
            errorTerms.Items.AddRange(New Object() {"BC30451 — nome não declarado", "BC30035 — erro de sintaxe", "BC30205 — fim de instrução", "BC30040 — código na mesma linha", "BC30648 — aspas não terminadas", "BC30002 — tipo não definido", "BC30512 — Option Strict", "NullReferenceException", "Arquivo não encontrado"})
            AddHandler lessons.SelectedIndexChanged, AddressOf LessonChanged
            AddHandler glossaryTerms.SelectedIndexChanged, AddressOf GlossaryChanged
            AddHandler challenges.SelectedIndexChanged, AddressOf ChallengeChanged
            AddHandler errorTerms.SelectedIndexChanged, AddressOf ErrorChanged
            lessons.SelectedIndex = 0
            glossaryTerms.SelectedIndex = 0
            challenges.SelectedIndex = 0
            errorTerms.SelectedIndex = 0
            UpdateProgressDisplay()
        End Sub

        Private Sub ErrorChanged(sender As Object, e As EventArgs)
            Dim descriptions As String() = {
                "O código tentou usar um nome que não existe nesse ponto. Confira a escrita, o Name do componente e se a variável foi criada com Dim.",
                "O compilador encontrou uma frase VB incompleta. Verifique parênteses, vírgulas, Then e a posição das palavras.",
                "Há texto extra depois de uma instrução. Divida comandos longos em linhas separadas e evite colocar blocos inteiros após dois-pontos.",
                "A declaração de Sub ou Function deve terminar a linha. Coloque o primeiro comando na linha seguinte e finalize com End Sub ou End Function.",
                "Uma String começou com aspas, mas não terminou. Para escrever uma aspa dentro do texto, use duas aspas duplas.",
                "O tipo do componente ou classe não foi encontrado. Verifique Imports, referências, DLLs incorporadas e o nome do tipo.",
                "O código tentou converter tipos automaticamente. Declare as variáveis com tipos claros e use CInt, CStr, CDbl ou TryParse.",
                "Um objeto está Nothing. Crie-o com New ou confira se o componente foi encontrado antes de acessar suas propriedades.",
                "Confira o caminho e use File.Exists antes de abrir. OpenFileDialog ajuda a escolher um arquivo existente."
            }
            If errorTerms.SelectedIndex >= 0 Then errorText.Text = errorTerms.SelectedItem.ToString() & Environment.NewLine & Environment.NewLine & descriptions(errorTerms.SelectedIndex) & Environment.NewLine & Environment.NewLine & "Use o número da linha informado na compilação para encontrar o ponto exato."
        End Sub

        Private Sub LessonChanged(sender As Object, e As EventArgs)
            Dim texts As String() = {
                "OBJETIVO: fazer um botão mostrar uma mensagem." & Environment.NewLine & Environment.NewLine & "Arraste um Button, clique duas vezes nele e escreva:" & Environment.NewLine & Environment.NewLine & "MessageBox.Show(""Olá, mundo!"")" & Environment.NewLine & Environment.NewLine & "Execute com F5.",
                "Todo componente possui propriedades. Text muda o texto visível; Name é o nome usado no código; BackColor muda a cor do fundo; Size controla o tamanho.",
                "Uma variável guarda um valor para usar depois." & Environment.NewLine & Environment.NewLine & "Dim pontos As Integer = 10" & Environment.NewLine & "Dim nome As String = ""Ana""",
                "If toma uma decisão." & Environment.NewLine & Environment.NewLine & "If idade >= 10 Then" & Environment.NewLine & "    MessageBox.Show(""Pode jogar"")" & Environment.NewLine & "Else" & Environment.NewLine & "    MessageBox.Show(""Espere mais um pouco"")" & Environment.NewLine & "End If",
                "For e While repetem ações. Comece com:" & Environment.NewLine & Environment.NewLine & "For numero As Integer = 1 To 5" & Environment.NewLine & "    ListBox1.Items.Add(numero)" & Environment.NewLine & "Next",
                "ListBox, ComboBox e arrays guardam vários itens. Use Items.Add para acrescentar algo visível a uma lista.",
                "Cada Form é uma janela. Adicione Form2 e abra com:" & Environment.NewLine & Environment.NewLine & "Form2.Show()" & Environment.NewLine & Environment.NewLine & "ShowDialog abre a janela e espera ela ser fechada.",
                "OpenFileDialog escolhe um arquivo. PictureBox mostra imagens. File.ReadAllText lê textos. Sempre use Try/Catch quando o arquivo puder falhar."
            }
            If lessons.SelectedIndex >= 0 Then lessonText.Text = texts(lessons.SelectedIndex)
            UpdateCompletionButtons()
        End Sub

        Private Sub GlossaryChanged(sender As Object, e As EventArgs)
            Dim descriptions As String() = {
                "Sequência de passos usada para resolver um problema.", "Programa criado para realizar uma tarefa.", "Tipo que guarda somente True ou False.", "Modelo que reúne dados e comportamentos.", "Objeto visual ou não visual colocado no projeto.", "Algo que aconteceu, como Click ou TextChanged.", "Janela do aplicativo.", "Bloco de código que calcula e devolve um valor.", "Estrutura que escolhe qual código será executado.", "Tipo usado para números inteiros.", "Estrutura que repete um trecho de código.", "Ação que um objeto consegue realizar.", "Característica configurável, como Text, Name ou Color.", "Tipo usado para guardar textos.", "Bloco de código que executa uma tarefa sem devolver valor.", "Espaço com nome usado para guardar um valor."
            }
            If glossaryTerms.SelectedIndex >= 0 Then glossaryText.Text = glossaryTerms.SelectedItem.ToString() & Environment.NewLine & Environment.NewLine & descriptions(glossaryTerms.SelectedIndex)
        End Sub

        Private Sub ChallengeChanged(sender As Object, e As EventArgs)
            Dim objectives As String() = {
                "Crie um Form com um botão que mostre seu nome em uma MessageBox.", "Crie um botão e um Label. Cada clique deve aumentar um número.", "Crie três botões que mudem a cor do Form.", "Faça três perguntas, conte os acertos e mostre o resultado.", "Converta Celsius para Fahrenheit usando TextBox e Button.", "Adicione tarefas a uma ListBox e permita removê-las.", "Escolha um número secreto e dê dicas de maior ou menor.", "Crie abrir, salvar, copiar, colar e localizar usando RichTextBox."
            }
            If challenges.SelectedIndex >= 0 Then challengeText.Text = "DESAFIO" & Environment.NewLine & Environment.NewLine & objectives(challenges.SelectedIndex) & Environment.NewLine & Environment.NewLine & "Execute várias vezes e teste também valores inesperados."
            UpdateCompletionButtons()
        End Sub

        Private Sub CompleteLesson(sender As Object, e As EventArgs)
            If lessons.SelectedIndex < 0 Then Return
            MarkProgress("lesson:" & lessons.SelectedIndex.ToString(), lessons.SelectedItem.ToString())
        End Sub

        Private Sub CompleteChallenge(sender As Object, e As EventArgs)
            If challenges.SelectedIndex < 0 Then Return
            MarkProgress("challenge:" & challenges.SelectedIndex.ToString(), challenges.SelectedItem.ToString())
        End Sub

        Private Sub MarkProgress(key As String, title As String)
            If LearningProgress.MarkCompleted(key) Then
                Dim message As String = "Muito bem! Você concluiu: " & title
                If LearningProgress.Count = 1 Then message &= Environment.NewLine & Environment.NewLine & "🏆 Conquista: Primeiro passo"
                If LearningProgress.Count = 5 Then message &= Environment.NewLine & Environment.NewLine & "🏆 Conquista: Explorador de código"
                MessageBox.Show(message, "Progresso salvo", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
            UpdateProgressDisplay()
            UpdateCompletionButtons()
        End Sub

        Private Sub UpdateProgressDisplay()
            progressLabel.Text = "Progresso: " & LearningProgress.Count.ToString() & " atividade(s) concluída(s)"
            Dim completed As Integer = LearningProgress.Count
            Dim medal As String = "Ainda sem medalha — conclua sua primeira atividade."
            If completed >= 1 Then medal = "🏆 Primeiro passo"
            If completed >= 5 Then medal &= Environment.NewLine & "🏆 Explorador de código"
            If completed >= 10 Then medal &= Environment.NewLine & "🏆 Construtor de aplicativos"
            If completed >= 17 Then medal &= Environment.NewLine & "🏆 Mestre do FlowForge"
            progressText.Text = "MEU PROGRESSO" & Environment.NewLine & Environment.NewLine & completed.ToString() & " de 17 atividades concluídas" & Environment.NewLine & Environment.NewLine & "MEDALHAS" & Environment.NewLine & medal & Environment.NewLine & Environment.NewLine & "Continue praticando: crie algo, altere uma parte do código e observe o resultado."
        End Sub

        Private Sub UpdateCompletionButtons()
            If lessons.SelectedIndex >= 0 Then
                lessonCompleteButton.Text = If(LearningProgress.IsCompleted("lesson:" & lessons.SelectedIndex.ToString()), "✓ Concluído", "Marcar como concluído")
            End If
            If challenges.SelectedIndex >= 0 Then
                challengeCompleteButton.Text = If(LearningProgress.IsCompleted("challenge:" & challenges.SelectedIndex.ToString()), "✓ Concluído", "Marcar como concluído")
            End If
        End Sub
    End Class

    Friend NotInheritable Class CodeTeacher
        Private Sub New()
        End Sub

        Public Shared Function Explain(code As String) As String
            If String.IsNullOrWhiteSpace(code) Then Return "Selecione um trecho no editor ou cole um código acima."
            Dim points As New List(Of String)()
            If Regex.IsMatch(code, "\bPrivate\s+Sub\b", RegexOptions.IgnoreCase) Then points.Add("• Private Sub inicia uma rotina que pertence a este Form.")
            If Regex.IsMatch(code, "\bHandles\b", RegexOptions.IgnoreCase) Then points.Add("• Handles liga a rotina a um evento, como o clique de um botão.")
            If Regex.IsMatch(code, "\bDim\b", RegexOptions.IgnoreCase) Then points.Add("• Dim cria uma variável para guardar uma informação.")
            If Regex.IsMatch(code, "\bIf\b", RegexOptions.IgnoreCase) Then points.Add("• If verifica uma condição. O bloco Then roda quando ela é verdadeira.")
            If Regex.IsMatch(code, "\bElse\b", RegexOptions.IgnoreCase) Then points.Add("• Else define o que acontece quando a condição do If é falsa.")
            If Regex.IsMatch(code, "\bFor\b|\bWhile\b", RegexOptions.IgnoreCase) Then points.Add("• Há uma repetição: parte do código pode executar várias vezes.")
            If Regex.IsMatch(code, "\bSelect\s+Case\b", RegexOptions.IgnoreCase) Then points.Add("• Select Case escolhe uma ação entre várias possibilidades.")
            If Regex.IsMatch(code, "\bFunction\b", RegexOptions.IgnoreCase) Then points.Add("• Function é uma rotina que devolve um resultado usando Return.")
            If Regex.IsMatch(code, "\bReturn\b", RegexOptions.IgnoreCase) Then points.Add("• Return devolve um valor e encerra a Function naquele ponto.")
            If Regex.IsMatch(code, "\b(New|CreateInstance)\b", RegexOptions.IgnoreCase) Then points.Add("• New cria um novo objeto para que ele possa ser usado.")
            If Regex.IsMatch(code, "\b(List|Array|Items)\b", RegexOptions.IgnoreCase) Then points.Add("• O trecho trabalha com uma coleção, isto é, vários valores ou itens.")
            If code.IndexOf("MessageBox.Show", StringComparison.OrdinalIgnoreCase) >= 0 Then points.Add("• MessageBox.Show abre uma pequena janela com uma mensagem.")
            If Regex.IsMatch(code, "\bFile\.(Read|Write|Open|Create)", RegexOptions.IgnoreCase) Then points.Add("• File acessa um arquivo do computador. É importante verificar o caminho e tratar erros.")
            If Regex.IsMatch(code, "\bInteger\.TryParse\b|\bDouble\.TryParse\b", RegexOptions.IgnoreCase) Then points.Add("• TryParse tenta converter um texto em número sem derrubar o aplicativo quando o valor é inválido.")
            If Regex.IsMatch(code, "\.Text\b", RegexOptions.IgnoreCase) Then points.Add("• A propriedade Text lê ou altera o texto de um componente.")
            If Regex.IsMatch(code, "\bTry\b", RegexOptions.IgnoreCase) Then points.Add("• Try tenta executar algo; Catch evita que uma falha feche o programa.")
            If Regex.IsMatch(code, "\bEnd\s+(Sub|If|Try|Select|Function)\b", RegexOptions.IgnoreCase) Then points.Add("• Uma instrução End encerra o bloco iniciado anteriormente.")
            If points.Count = 0 Then points.Add("• Este trecho usa comandos ou chamadas de objetos. Leia da esquerda para a direita e identifique primeiro o nome antes do ponto.")
            Dim result As New StringBuilder()
            result.AppendLine("O QUE ESTE CÓDIGO FAZ").AppendLine()
            For Each point As String In points.Distinct()
                result.AppendLine(point).AppendLine()
            Next
            result.AppendLine("Experimente alterar um valor de cada vez e executar novamente.")
            Return result.ToString()
        End Function
    End Class
End Namespace
