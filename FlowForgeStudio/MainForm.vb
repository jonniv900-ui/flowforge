Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Diagnostics
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports Microsoft.VisualBasic

Namespace FlowForgeStudio
    Public Class MainForm
        Inherits Form

        Private _darkTheme As Boolean = True
        Private _rcMainTools As ToolStrip
        Private _themeLightItem As ToolStripMenuItem
        Private _themeDarkItem As ToolStripMenuItem


        Private Sub ApplyIdeTheme(dark As Boolean)
            _darkTheme = dark

            Dim back As Color
            Dim panelBack As Color
            Dim fore As Color
            Dim stripBack As Color

            If dark Then
                back = Color.FromArgb(30, 33, 40)
                panelBack = Color.FromArgb(36, 39, 47)
                stripBack = Color.FromArgb(32, 35, 43)
                fore = Color.White
            Else
                back = SystemColors.Control
                panelBack = Color.White
                stripBack = SystemColors.Control
                fore = SystemColors.ControlText
            End If

            Me.BackColor = back
            Me.ForeColor = fore

            ' Aplica o tema somente à interface da IDE.
            ' A superfície do Designer contém o Form/UserControl do projeto e deve
            ' preservar integralmente BackColor/ForeColor definidos pelo usuário.
            ApplyThemeRecursive(Me, dark, back, panelBack, fore)

            For Each ts As ToolStrip In New ToolStrip() {fileTools, designerTools, buildTools, projectTools, _rcMainTools}
                If ts IsNot Nothing Then
                    ts.BackColor = stripBack
                    ts.ForeColor = fore
                    For Each it As ToolStripItem In ts.Items
                        it.BackColor = stripBack
                        it.ForeColor = fore
                    Next
                End If
            Next
            If _themeDarkItem IsNot Nothing Then _themeDarkItem.Checked = dark
            If _themeLightItem IsNot Nothing Then _themeLightItem.Checked = Not dark
            Me.Invalidate(True)
        End Sub

        Private Shared Sub ApplyThemeRecursive(parent As Control, dark As Boolean, back As Color, panelBack As Color, fore As Color)
            For Each child As Control In parent.Controls
                ' Nunca tematizar a superfície de design nem o controle raiz que ela hospeda.
                ' O tema é da IDE, não do aplicativo criado pelo usuário.
                If TypeOf child Is DesignerSurface Then
                    Continue For
                End If
                If TypeOf child Is Form Then
                    Continue For
                End If
                If TypeOf child Is UserControl AndAlso child.Parent IsNot Nothing AndAlso TypeOf child.Parent Is DesignerSurface Then
                    Continue For
                End If
                If TypeOf child Is TextBoxBase OrElse TypeOf child Is ListBox OrElse TypeOf child Is ListView OrElse TypeOf child Is TreeView Then
                    child.BackColor = If(dark, Color.FromArgb(28, 31, 38), Color.White)
                    child.ForeColor = fore
                ElseIf TypeOf child Is PropertyGrid Then
                    child.BackColor = If(dark, Color.FromArgb(30, 33, 40), SystemColors.Control)
                    child.ForeColor = fore
                ElseIf TypeOf child Is Panel OrElse TypeOf child Is SplitContainer OrElse TypeOf child Is TabControl Then
                    child.BackColor = If(dark, panelBack, SystemColors.Control)
                    child.ForeColor = fore
                End If
                ApplyThemeRecursive(child, dark, back, panelBack, fore)
            Next
        End Sub

        Private Shared Sub MoveToolStripItems(source As ToolStrip, target As ToolStrip)
            If source Is Nothing OrElse target Is Nothing Then Return
            While source.Items.Count > 0
                Dim item As ToolStripItem = source.Items(0)
                source.Items.RemoveAt(0)
                target.Items.Add(item)
            End While
        End Sub

        Private project As FlowProject
        Private currentFile As String
        Private activeIndex As Integer
        Private activeClassIndex As Integer = -1
        Private activeModuleIndex As Integer = -1
        Private activeUserControlIndex As Integer = -1
        Private changing As Boolean
        Private changingDocumentTabs As Boolean
        Private ReadOnly designer As New DesignerSurface()
        Private ReadOnly properties As New PropertyGrid()
        Private ReadOnly propertyTabs As New TabControl()
        Private ReadOnly eventInspector As New EventInspector()
        Private ReadOnly toolbox As New ListBox()
        Private ReadOnly toolboxSearch As New TextBox() With {.Dock = DockStyle.Top, .Height = 27, .BorderStyle = BorderStyle.FixedSingle}
        Private ReadOnly allTools As New List(Of ToolboxEntry)()
        Private ReadOnly projectTree As New TreeView()
        Private ReadOnly formsBox As New ComboBox()
        ' Barras principais. Mantemos referências para restaurar e salvar a posição entre execuções.
        Private fileTools As ToolStrip
        Private projectTools As ToolStrip
        Private designerTools As ToolStrip
        Private buildTools As ToolStrip
        Private ReadOnly tabs As New TabControl()
        Private ReadOnly documentTabs As New TabControl()
        Private ReadOnly workspaceLayout As New TableLayoutPanel()
        Private ReadOnly workspaceOutputLayout As New TableLayoutPanel()
        Private ReadOnly ideOutput As New IdeOutputPanel()
        Private outputPanelVisible As Boolean = False
        Private outputPanelHeight As Single = 190.0F
        Private ReadOnly workspaceHeader As New Panel()
        Private ReadOnly formViewBar As New Panel()
        Private ReadOnly formViewLabel As New Label()
        Private ReadOnly viewDesignerButton As New Button()
        Private ReadOnly viewCodeButton As New Button()
        Private ReadOnly code As New VbEditor()
        Private ReadOnly codeRuler As New Panel() With {.Dock = DockStyle.Left, .Width = 58, .BackColor = Color.FromArgb(25, 28, 34), .Cursor = Cursors.Hand}
        Private ReadOnly status As New ToolStripStatusLabel("Pronto")
        Private ReadOnly caretStatus As New ToolStripStatusLabel("Ln 1, Col 1")
        Private ReadOnly formStatus As New ToolStripStatusLabel("Form1")
        Private ReadOnly selectionStatus As New ToolStripStatusLabel("Form")
        Private ReadOnly sampleBox As New ToolStripComboBox()
        Private ReadOnly searchBox As New ToolStripTextBox() With {.Width = 150, .ToolTipText = "Localizar"}
        Private ReadOnly replaceBox As New ToolStripTextBox() With {.Width = 130, .ToolTipText = "Substituir por"}
        Private ReadOnly outerSplit As New SplitContainer()
        Private ReadOnly leftSplit As New SplitContainer()
        Private ReadOnly rightSplit As New SplitContainer()
        Private ReadOnly recentProjectsMenu As New ToolStripMenuItem("Projetos recentes")
        Private savedProjectState As String = ""

        Public Sub New(Optional initialProjectPath As String = Nothing)
            Text = "FlowForge" : WindowState = FormWindowState.Maximized : MinimumSize = New Size(1100, 700)
            Try
                Dim appIconPath As String = Path.Combine(Application.StartupPath, "FlowForgeStudio.ico")
                If File.Exists(appIconPath) Then Me.Icon = New Icon(appIconPath)
            Catch
                ' O icone incorporado no EXE continua sendo usado como fallback.
            End Try
            BuildInterface() : LoadToolbox() : NewProject()
            If Not String.IsNullOrWhiteSpace(initialProjectPath) Then LoadProjectFromPath(initialProjectPath)
            AllowDrop = True
            AddHandler DragEnter, AddressOf MainDragEnter
            AddHandler DragDrop, AddressOf MainDragDrop
            AddHandler FormClosing, AddressOf MainFormClosingCheck
            AddHandler Shown, Sub() BeginInvoke(New MethodInvoker(AddressOf NormalizeLayout))
        End Sub

        Private ReadOnly Property CurrentDocument As FormData
            Get
                Return project.Forms(Math.Max(0, Math.Min(activeIndex, project.Forms.Count - 1)))
            End Get
        End Property

        Private Sub BuildInterface()
            Dim menuStrip As New MenuStrip With {.BackColor = Color.FromArgb(45, 48, 56), .ForeColor = Color.White, .RenderMode = ToolStripRenderMode.System}
            Dim fileMenu As ToolStripMenuItem = Menu("Arquivo", _
                Item("Novo projeto", Sub() NewProject(), "new", Keys.Control Or Keys.N), _
                Item("Abrir projeto...", AddressOf OpenProject, "open", Keys.Control Or Keys.O), _
                Item("Salvar", AddressOf SaveProject, "save", Keys.Control Or Keys.S), _
                Item("Salvar tudo", AddressOf SaveAll, "save", Keys.Control Or Keys.Alt Or Keys.S), _
                Item("Salvar como...", AddressOf SaveProjectAs, "save", Keys.Control Or Keys.Shift Or Keys.S), _
                recentProjectsMenu, _
                Item("Abrir pasta do projeto", AddressOf OpenProjectFolder, "open"), _
                Item("Copiar caminho do projeto", AddressOf CopyProjectPath, "copy"), _
                New ToolStripSeparator(), Item("Sair", Sub() Close(), "delete", Keys.Alt Or Keys.F4))
            RefreshRecentProjectsMenu()
            Dim editMenu As ToolStripMenuItem = Menu("Editar", _
                Item("Desfazer", Sub() EditCommand("undo"), "undo", Keys.Control Or Keys.Z), _
                Item("Refazer", Sub() EditCommand("redo"), "redo", Keys.Control Or Keys.Y), _
                New ToolStripSeparator(), _
                Item("Recortar", Sub() EditCommand("cut"), "cut", Keys.Control Or Keys.X), _
                Item("Copiar", Sub() EditCommand("copy"), "copy", Keys.Control Or Keys.C), _
                Item("Colar", Sub() EditCommand("paste"), "paste", Keys.Control Or Keys.V), _
                Item("Selecionar tudo", Sub() EditCommand("selectall"), "code", Keys.Control Or Keys.A), _
                New ToolStripSeparator(), _
                Item("Localizar", AddressOf FocusSearch, "code", Keys.Control Or Keys.F), _
                Item("Localizar no projeto...", AddressOf FindInProject, "code", Keys.Control Or Keys.Shift Or Keys.F), _
                Item("Substituir", AddressOf FocusReplace, "code", Keys.Control Or Keys.H), _
                Item("Ir para linha...", AddressOf GoToLine, "code", Keys.Control Or Keys.G), _
                Item("Comentar seleção", Sub() ToggleComment(True), "code", Keys.Control Or Keys.K), _
                Item("Descomentar seleção", Sub() ToggleComment(False), "code", Keys.Control Or Keys.U), _
                New ToolStripSeparator(), Item("Excluir componente", Sub() designer.DeleteSelected(), "delete", Keys.Delete))
            Dim viewMenu As ToolStripMenuItem = Menu("Exibir", _
                Item("Designer", Sub() ShowWorkspaceTab(0), "form", Keys.Shift Or Keys.F7), _
                Item("Código VB.NET", Sub() ShowWorkspaceTab(1), "code", Keys.F7), _
                New ToolStripSeparator(), Item("Explorador do projeto", Sub() FocusProjectPanel(), "form"), _
                Item("Caixa de ferramentas", Sub() toolbox.Focus(), "toolbox"), _
                Item("Janela de propriedades", Sub() properties.Focus(), "properties", Keys.F4), _
                New ToolStripSeparator(), _
                Item("Mostrar/ocultar Explorador", AddressOf ToggleExplorerPanel, "form"), _
                Item("Mostrar/ocultar Caixa de ferramentas", AddressOf ToggleToolboxPanel, "toolbox"), _
                Item("Mostrar/ocultar Propriedades", AddressOf TogglePropertiesPanel, "properties"), _
                CheckItem("Moldura do Form", designer.ShowFormChrome, Sub() ToggleFormChrome(), "form"), _
                New ToolStripSeparator(), _
                Item("Saída / Lista de erros", AddressOf ToggleOutputPanel, "code", Keys.Control Or Keys.Alt Or Keys.O), _
                Item("Restaurar layout", AddressOf RestoreLayout, "undo"))
            Dim projectItemsMenu As ToolStripMenuItem = Menu("Itens do projeto", _
                Item("Adicionar Form", AddressOf AddForm, "form", Keys.Control Or Keys.Shift Or Keys.A), _
                Item("Adicionar Classe...", AddressOf AddClass, "code", Keys.Control Or Keys.Shift Or Keys.C), _
                Item("Adicionar Módulo...", AddressOf AddModule, "code"), _
                Item("Adicionar UserControl...", AddressOf AddUserControl, "form"), _
                Item("Nova Pasta...", AddressOf AddProjectFolder, "open"), _
                New ToolStripSeparator(), _
                Item("Duplicar Form", AddressOf DuplicateForm, "copy"), _
                Item("Definir como Form inicial", AddressOf SetStartup, "run"), _
                Item("Excluir Form", AddressOf DeleteForm, "delete"))

            Dim projectMenu As ToolStripMenuItem = Menu("Projeto", _
                Item("Renomear projeto...", AddressOf RenameProject, "properties"), _
                Item("Recursos do projeto...", AddressOf ManageProjectResources, "open"), _
                Item("Referências / DLLs...", AddressOf ManageProjectLibraries, "toolbox"), _
                New ToolStripSeparator(), _
                projectItemsMenu)
            Dim buildMenu As ToolStripMenuItem = Menu("Compilar", _
                Item("Gerar executável...", AddressOf BuildProject, "build", Keys.Control Or Keys.Shift Or Keys.B), _
                Item("Compilar e executar", AddressOf RunProject, "run", Keys.F6))
            Dim runMenu As ToolStripMenuItem = Menu("Executar", Item("Iniciar aplicativo", AddressOf RunProject, "run", Keys.F5))
            Dim toolsMenu As ToolStripMenuItem = Menu("Ferramentas", _
                CheckItem("Exibir grade", designer.ShowGrid, AddressOf ToggleGrid, "grid"), _
                CheckItem("Encaixar na grade", designer.SnapToGrid, AddressOf ToggleSnap, "snap"), _
                Menu("Tamanho da grade", Item("5 pixels", Sub() SetGridSize(5)), Item("8 pixels", Sub() SetGridSize(8)), Item("10 pixels", Sub() SetGridSize(10)), Item("16 pixels", Sub() SetGridSize(16))), _
                New ToolStripSeparator(), _
                Item("Adicionar controle de biblioteca .NET...", AddressOf ImportDotNetLibrary, "toolbox"), _
                Item("Importar controle ActiveX/OCX...", AddressOf ImportOcx, "toolbox"), _
                New ToolStripSeparator(), _
                Item("Arduino / ESP — Monitor e Upload...", AddressOf OpenArduinoTools, "run"), _
                Item("Fluxograma → VB.NET...", AddressOf OpenFlowchartEditor, "code"), _
                Item("Banco de Dados Visual...", AddressOf OpenDatabaseDesigner, "properties"), _
                New ToolStripSeparator(), CreateAddItemsMenu(), _
                Item("Editar itens da barra...", AddressOf EditToolStripItems, "properties"), _
                Item("Editar coleção...", AddressOf EditSelectedCollection, "properties"), _
                New ToolStripSeparator(), Item("Selecionar Form", Sub() designer.SelectForm(), "form"))
            Dim basicExamples As ToolStripMenuItem = Menu("Básicos", _
                Item("Calculadora", Sub() OpenExample("Calculadora.flowapp"), "build"), _
                Item("Calculadora científica", Sub() OpenExample("CalculadoraCientifica.flowapp"), "build"), _
                Item("Conversor de temperatura", Sub() OpenExample("ConversorTemperatura.flowapp"), "properties"), _
                Item("Painel de controles customizados", Sub() OpenExample("CustomControlsDemo.flowapp"), "toolbox"), _
                Item("Controles avançados e displays", Sub() OpenExample("AdvancedControls.flowapp"), "toolbox"), _
                Item("Painel de telemetria", Sub() OpenExample("TelemetryDashboard.flowapp"), "run"), _
                Item("Editores de texto e código", Sub() OpenExample("TextEditorsDemo.flowapp"), "code"), _
                Item("Adivinhe o número", Sub() OpenExample("AdivinheNumero.flowapp"), "help"))
            Dim productivityExamples As ToolStripMenuItem = Menu("Produtividade", _
                Item("Lista de tarefas", Sub() OpenExample("ListaTarefas.flowapp"), "code"), _
                Item("Editor de Texto — FlowWriter", Sub() OpenExample("EditorDeTexto.flowapp"), "code"), _
                Item("Paint", Sub() OpenExample("Paint.flowapp"), "form"), _
                Item("FlowPaint — editor de imagens", Sub() OpenExample("FlowPaint.flowapp"), "form"), _
                Item("Mini Editor de Texto", Sub() OpenExample("MiniEditor.flowapp"), "code"), _
                Item("FlowExplorer — explorador de arquivos", Sub() OpenExample("FlowExplorer.flowapp"), "open"), _
                Item("Visualizador de imagens", Sub() OpenExample("ImageViewer.flowapp"), "form"), _
                Item("Leitor de CSV", Sub() OpenExample("CsvViewer.flowapp"), "open"), _
                Item("Cronômetro", Sub() OpenExample("Cronometro.flowapp"), "run"), _
                Item("Informações do sistema", Sub() OpenExample("SystemInfo.flowapp"), "properties"))
            Dim webExamples As ToolStripMenuItem = Menu("Web", _
                Item("Mini HTML Renderer", Sub() OpenExample("MiniHtmlRenderer.flowapp"), "code"), _
                Item("Mini HTML Renderer Web", Sub() OpenExample("MiniHtmlRendererweb.flowapp"), "open"))
            Dim hardwareExamples As ToolStripMenuItem = Menu("Hardware", _
                Item("Serial Console — Arduino", Sub() OpenExample("SerialConsole.flowapp"), "run"), _
                Item("Controle de LED Arduino", Sub() OpenExample("ArduinoLedControl.flowapp"), "run"))
            Dim newComponentsExamples As ToolStripMenuItem = Menu("Novos componentes", _
                Item("Dashboard de dados", Sub() OpenExample("DashboardDados.flowapp"), "run"), _
                Item("Formulário moderno", Sub() OpenExample("FormularioModerno.flowapp"), "properties"), _
                Item("Laboratório Arduino / IoT", Sub() OpenExample("LaboratorioIoT.flowapp"), "run"), _
                Item("JSON Explorer", Sub() OpenExample("JsonExplorer.flowapp"), "code"))
            Dim networkExamples As ToolStripMenuItem = Menu("Rede", _
                Item("Cliente TCP", Sub() OpenExample("TcpConsole.flowapp"), "run"), _
                Item("Network Scanner", Sub() OpenExample("NetworkScanner.flowapp"), "run"), _
                Item("Wi-Fi Scanner", Sub() OpenExample("WifiScanner.flowapp"), "run"))
            Dim gameExamples As ToolStripMenuItem = Menu("Jogos", _
                Item("Clicker Game", Sub() OpenExample("ClickerGame.flowapp"), "run"), _
                Item("Flappy Bird", Sub() OpenExample("FlappyBird.flowapp"), "run"), _
                Item("Aventura do Encanador", Sub() OpenExample("AventuraDoEncanador.flowapp"), "run"), _
                Item("Xadrez — FlowBoardGames", Sub() OpenExample("Xadrez.flowapp"), "run"), _
                Item("Jogo da Forca", Sub() OpenExample("JogoDaForca.flowapp"), "run"), _
                Item("Jogo da Velha — 2 jogadores", Sub() OpenExample("JogoDaVelha2jogadores.flowapp"), "run"), _
                Item("Jogo da Velha — IA", Sub() OpenExample("JogoDaVelhaIA.flowapp"), "run"))
            Dim examplesMenu As ToolStripMenuItem = Menu("Exemplos", basicExamples, productivityExamples, newComponentsExamples, webExamples, hardwareExamples, networkExamples, gameExamples)
            Dim formatMenu As ToolStripMenuItem = Menu("Formatar", _
                Item("Duplicar componente", AddressOf DuplicateSelectedComponent, "copy", Keys.Control Or Keys.D), _
                New ToolStripSeparator(), _
                Menu("Alinhar", _
                    Item("À esquerda", Sub() DesignerCommand("left")), _
                    Item("À direita", Sub() DesignerCommand("right")), _
                    Item("Ao topo", Sub() DesignerCommand("top")), _
                    Item("À base", Sub() DesignerCommand("bottom")), _
                    Item("Centros horizontais", Sub() DesignerCommand("hcenter")), _
                    Item("Centros verticais", Sub() DesignerCommand("vcenter"))), _
                Menu("Mesmo tamanho", _
                    Item("Largura", Sub() DesignerCommand("samewidth")), _
                    Item("Altura", Sub() DesignerCommand("sameheight")), _
                    Item("Largura e altura", Sub() DesignerCommand("samesize"))), _
                Menu("Distribuir", _
                    Item("Horizontalmente", Sub() DesignerCommand("distributeh")), _
                    Item("Verticalmente", Sub() DesignerCommand("distributev"))), _
                New ToolStripSeparator(), _
                Item("Centralizar horizontalmente no Form", Sub() DesignerCommand("centerh"), "properties"), _
                Item("Centralizar verticalmente no Form", Sub() DesignerCommand("centerv"), "properties"), _
                New ToolStripSeparator(), _
                Item("Trazer para frente", Sub() DesignerCommand("front"), "redo"), _
                Item("Enviar para trás", Sub() DesignerCommand("back"), "undo"))
            Dim helpMenu As ToolStripMenuItem = Menu("Ajuda", Item("Sobre o FlowForge Studio", AddressOf ShowAbout, "help", Keys.F1))
            menuStrip.Items.AddRange(New ToolStripItem() {fileMenu, editMenu, viewMenu, projectMenu, formatMenu, buildMenu, runMenu, toolsMenu, examplesMenu, helpMenu})
            MainMenuStrip = menuStrip
            ' As ferramentas principais ficam em barras independentes, como em uma IDE tradicional.
            ' Isso evita uma única barra excessivamente longa e permite reorganizá-las no ToolStripContainer.
            fileTools = New ToolStrip With {.Name = "FileTools", .GripStyle = ToolStripGripStyle.Hidden, .BackColor = Color.FromArgb(37, 40, 48), .ForeColor = Color.White, .ImageScalingSize = New Size(18, 18), .Padding = New Padding(2)}
            fileTools.AllowItemReorder = False
            fileTools.Items.Add(New ToolStripLabel("Arquivo"))
            fileTools.Items.Add(New ToolStripSeparator())
            fileTools.Items.Add(Button("Novo", Sub() NewProject(), "new"))
            fileTools.Items.Add(Button("Abrir", AddressOf OpenProject, "open"))
            fileTools.Items.Add(Button("Salvar", AddressOf SaveProject, "save"))
            fileTools.Items.Add(Button("Salvar tudo", AddressOf SaveAll, "save"))

            projectTools = New ToolStrip With {.Name = "ProjectTools", .GripStyle = ToolStripGripStyle.Hidden, .BackColor = Color.FromArgb(37, 40, 48), .ForeColor = Color.White, .ImageScalingSize = New Size(18, 18), .Padding = New Padding(2)}
            projectTools.AllowItemReorder = False
            projectTools.Items.Add(New ToolStripLabel("Projeto"))
            projectTools.Items.Add(New ToolStripSeparator())
            formsBox.DropDownStyle = ComboBoxStyle.DropDownList
            formsBox.Width = 145
            AddHandler formsBox.SelectedIndexChanged, AddressOf FormChanged
            Dim formsHost As New ToolStripControlHost(formsBox)
            projectTools.Items.Add(New ToolStripLabel("Form:"))
            projectTools.Items.Add(formsHost)
            projectTools.Items.Add(Button("Adicionar Form", AddressOf AddForm, "form"))
            projectTools.Items.Add(Button("Adicionar Classe", AddressOf AddClass, "code"))
            projectTools.Items.Add(Button("Duplicar", AddressOf DuplicateForm, "copy"))
            projectTools.Items.Add(Button("Inicial", AddressOf SetStartup, "run"))
            projectTools.Items.Add(Button("Excluir", AddressOf DeleteForm, "delete"))

            designerTools = New ToolStrip With {.Name = "DesignerTools", .GripStyle = ToolStripGripStyle.Hidden, .BackColor = Color.FromArgb(37, 40, 48), .ForeColor = Color.White, .ImageScalingSize = New Size(18, 18), .Padding = New Padding(2)}
            designerTools.AllowItemReorder = False
            designerTools.Items.Add(New ToolStripLabel("Designer"))
            designerTools.Items.Add(New ToolStripSeparator())
            designerTools.Items.Add(Button("Designer", Sub() ShowWorkspaceTab(0), "form"))
            designerTools.Items.Add(Button("Código", Sub() ShowWorkspaceTab(1), "code"))
            designerTools.Items.Add(CreateAddItemsButton())
            designerTools.Items.Add(Button("Editar itens", AddressOf EditToolStripItems, "properties"))
            designerTools.Items.Add(Button("Coleção", AddressOf EditSelectedCollection, "properties"))
            designerTools.Items.Add(New ToolStripSeparator())
            designerTools.Items.Add(Button("Duplicar", AddressOf DuplicateSelectedComponent, "copy"))
            designerTools.Items.Add(Button("Frente", Sub() DesignerCommand("front"), "redo"))
            designerTools.Items.Add(Button("Trás", Sub() DesignerCommand("back"), "undo"))

            buildTools = New ToolStrip With {.Name = "BuildTools", .GripStyle = ToolStripGripStyle.Hidden, .BackColor = Color.FromArgb(37, 40, 48), .ForeColor = Color.White, .ImageScalingSize = New Size(18, 18), .Padding = New Padding(2)}
            buildTools.AllowItemReorder = False
            buildTools.Items.Add(New ToolStripLabel("Execução"))
            buildTools.Items.Add(New ToolStripSeparator())
            buildTools.Items.Add(Button("Gerar EXE", AddressOf BuildProject, "build"))
            buildTools.Items.Add(Button("Executar", AddressOf RunProject, "run"))

            Dim statusBar As New StatusStrip()
            Dim statusSpacer As New ToolStripStatusLabel() With {.Spring = True}
            statusBar.Items.Add(status) : statusBar.Items.Add(statusSpacer) : statusBar.Items.Add(formStatus) : statusBar.Items.Add(New ToolStripStatusLabel("|")) : statusBar.Items.Add(selectionStatus) : statusBar.Items.Add(New ToolStripStatusLabel("|")) : statusBar.Items.Add(caretStatus)
            outerSplit.Dock = DockStyle.Fill : outerSplit.BackColor = Color.FromArgb(32, 35, 42)
            leftSplit.Dock = DockStyle.Fill : leftSplit.Orientation = Orientation.Horizontal
            projectTree.Dock = DockStyle.Fill : projectTree.BackColor = Color.FromArgb(32, 35, 42) : projectTree.ForeColor = Color.White : projectTree.BorderStyle = BorderStyle.None : AddHandler projectTree.NodeMouseDoubleClick, AddressOf ProjectNodeDoubleClick : AddHandler projectTree.NodeMouseClick, AddressOf ProjectNodeMouseClick : AddHandler projectTree.KeyDown, AddressOf ProjectTreeKeyDown
            Dim projectMenuTree As New ContextMenuStrip()
            projectMenuTree.Items.Add(Item("Adicionar Form", AddressOf AddForm, "form"))
            projectMenuTree.Items.Add(Item("Adicionar Classe...", AddressOf AddClass, "code"))
            projectTree.ContextMenuStrip = projectMenuTree
            toolbox.Dock = DockStyle.Fill : toolbox.DrawMode = DrawMode.OwnerDrawFixed : toolbox.ItemHeight = 38 : toolbox.BackColor = Color.FromArgb(32, 35, 42) : toolbox.ForeColor = Color.White : toolbox.BorderStyle = BorderStyle.None
            toolboxSearch.BackColor = Color.FromArgb(48, 52, 62) : toolboxSearch.ForeColor = Color.White : toolboxSearch.Font = New Font("Segoe UI", 9.0F) : toolboxSearch.Text = ""
            AddHandler toolboxSearch.TextChanged, AddressOf FilterToolbox
            AddHandler toolboxSearch.KeyDown, AddressOf ToolboxSearchKeyDown
            AddHandler toolbox.DrawItem, AddressOf DrawToolbox : AddHandler toolbox.DoubleClick, Sub() AddSelectedTool()
            AddHandler toolbox.MouseDown, Sub(sender, e) If e.Button = MouseButtons.Left AndAlso toolbox.SelectedItem IsNot Nothing Then toolbox.DoDragDrop(toolbox.SelectedItem, DragDropEffects.Copy)
            Dim toolboxMenu As New ContextMenuStrip()
            toolboxMenu.Items.Add(Item("Adicionar biblioteca .NET...", AddressOf ImportDotNetLibrary, "toolbox"))
            toolboxMenu.Items.Add(Item("Importar ActiveX/OCX...", AddressOf ImportOcx, "toolbox"))
            toolbox.ContextMenuStrip = toolboxMenu
            leftSplit.Panel1.Controls.Add(Group("EXPLORADOR DO PROJETO", projectTree)) : leftSplit.Panel2.Controls.Add(Group("CAIXA DE FERRAMENTAS — pesquise pelo nome", ToolboxHost())) : outerSplit.Panel1.Controls.Add(leftSplit)

            rightSplit.Dock = DockStyle.Fill : rightSplit.FixedPanel = FixedPanel.Panel2
            tabs.Dock = DockStyle.Fill
            ' O TabControl interno serve apenas para trocar o conteúdo. A navegação visual
            ' é feita pela faixa Designer/Código, evitando uma segunda barra de abas.
            tabs.Appearance = TabAppearance.FlatButtons : tabs.SizeMode = TabSizeMode.Fixed : tabs.ItemSize = New Size(0, 1)
            Dim designTab As New TabPage("DESIGNER") With {.BackColor = Color.FromArgb(20, 22, 27)} : designer.Dock = DockStyle.Fill : designTab.Controls.Add(designer)
            Dim codeTab As New TabPage("CÓDIGO") With {.BackColor = Color.FromArgb(17, 19, 24)}
            Dim codeTools As New ToolStrip With {.GripStyle = ToolStripGripStyle.Hidden, .Dock = DockStyle.Fill, .CanOverflow = True}
            sampleBox.Items.AddRange({"If / Else", "MessageBox", "Try / Catch", "For Each", "While", "Select Case", "Abrir outro Form", "Ler arquivo", "HTTP GET"}) : sampleBox.SelectedIndex = 0
            codeTools.Items.Add(New ToolStripLabel("Exemplo:")) : codeTools.Items.Add(sampleBox) : codeTools.Items.Add(Button("Inserir", AddressOf InsertSample))
            codeTools.Items.Add(New ToolStripSeparator()) : codeTools.Items.Add(New ToolStripLabel("Localizar:")) : codeTools.Items.Add(searchBox)
            codeTools.Items.Add(Button("Anterior", Sub() FindCode(True), "undo")) : codeTools.Items.Add(Button("Próximo", Sub() FindCode(False), "redo"))
            codeTools.Items.Add(New ToolStripLabel("Substituir:")) : codeTools.Items.Add(replaceBox) : codeTools.Items.Add(Button("Trocar", AddressOf ReplaceCode, "code")) : codeTools.Items.Add(Button("Todas", AddressOf ReplaceAllCode, "code"))
            AddHandler searchBox.KeyDown, Sub(sender, e) If e.KeyCode = Keys.Enter Then FindCode(e.Shift) : e.SuppressKeyPress = True
            Dim editorHost As New Panel With {.Dock = DockStyle.Fill, .BackColor = Color.FromArgb(17, 19, 24), .Margin = New Padding(0)}
            code.Dock = DockStyle.Fill : editorHost.Controls.Add(code) : editorHost.Controls.Add(codeRuler)
            AddHandler codeRuler.Paint, AddressOf PaintCodeRuler
            AddHandler codeRuler.MouseDown, AddressOf CodeRulerMouseDown
            AddHandler code.ViewportChanged, AddressOf CodeViewportChanged
            ' Usa linhas separadas para impedir que a barra de busca/substituição sobreponha o editor.
            Dim codeLayout As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2, .Margin = New Padding(0), .Padding = New Padding(0)}
            codeLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
            codeLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 27.0F))
            codeLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
            codeLayout.Controls.Add(codeTools, 0, 0)
            codeLayout.Controls.Add(editorHost, 0, 1)
            codeTab.Controls.Add(codeLayout) : tabs.TabPages.Add(designTab) : tabs.TabPages.Add(codeTab)

            documentTabs.Dock = DockStyle.Fill : documentTabs.Multiline = False : documentTabs.DrawMode = TabDrawMode.OwnerDrawFixed : documentTabs.Padding = New Point(18, 4)
            AddHandler documentTabs.SelectedIndexChanged, AddressOf DocumentTabChanged
            AddHandler documentTabs.MouseDown, AddressOf DocumentTabsMouseDown
            AddHandler documentTabs.DrawItem, AddressOf DocumentTabsDrawItem

            ' Faixa de alternância exibida somente quando o documento ativo é um Form.
            ' O layout usa linhas reais (abas / alternância / conteúdo), portanto nenhum
            ' controle fica por cima do Designer ou da primeira linha do editor.
            formViewBar.Dock = DockStyle.Fill : formViewBar.BackColor = Color.FromArgb(31, 34, 40) : formViewBar.Visible = False : formViewBar.Margin = New Padding(0)
            formViewLabel.AutoSize = False : formViewLabel.Dock = DockStyle.Left : formViewLabel.Width = 150 : formViewLabel.TextAlign = ContentAlignment.MiddleLeft : formViewLabel.Padding = New Padding(8, 0, 0, 0) : formViewLabel.ForeColor = Color.Gainsboro
            viewDesignerButton.Text = "Designer" : viewDesignerButton.Size = New Size(82, 24) : viewDesignerButton.Location = New Point(154, 2) : viewDesignerButton.FlatStyle = FlatStyle.Flat : viewDesignerButton.FlatAppearance.BorderSize = 0
            viewCodeButton.Text = "Código" : viewCodeButton.Size = New Size(72, 24) : viewCodeButton.Location = New Point(238, 2) : viewCodeButton.FlatStyle = FlatStyle.Flat : viewCodeButton.FlatAppearance.BorderSize = 0
            AddHandler viewDesignerButton.Click, Sub() ShowWorkspaceTab(0)
            AddHandler viewCodeButton.Click, Sub() ShowWorkspaceTab(1)
            formViewBar.Controls.Add(viewCodeButton) : formViewBar.Controls.Add(viewDesignerButton) : formViewBar.Controls.Add(formViewLabel)

            workspaceLayout.Dock = DockStyle.Fill : workspaceLayout.Margin = New Padding(0) : workspaceLayout.Padding = New Padding(0)
            workspaceLayout.ColumnCount = 1 : workspaceLayout.RowCount = 3
            workspaceLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
            workspaceLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 31.0F))
            workspaceLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 0.0F))
            workspaceLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
            workspaceLayout.Controls.Add(documentTabs, 0, 0)
            workspaceLayout.Controls.Add(formViewBar, 0, 1)
            workspaceLayout.Controls.Add(tabs, 0, 2)

            ' Área inferior de Saída/Erros. Usa uma linha própria para não sobrepor o Designer/editor.
            workspaceOutputLayout.Dock = DockStyle.Fill : workspaceOutputLayout.Margin = New Padding(0) : workspaceOutputLayout.Padding = New Padding(0)
            workspaceOutputLayout.ColumnCount = 1 : workspaceOutputLayout.RowCount = 2
            workspaceOutputLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
            workspaceOutputLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
            workspaceOutputLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 0.0F))
            workspaceOutputLayout.Controls.Add(workspaceLayout, 0, 0)
            ideOutput.Dock = DockStyle.Fill : ideOutput.Visible = False
            AddHandler ideOutput.IssueActivated, AddressOf CompilerIssueActivated
            AddHandler ideOutput.CloseRequested, AddressOf OutputPanelCloseRequested
            workspaceOutputLayout.Controls.Add(ideOutput, 0, 1)
            rightSplit.Panel1.Controls.Add(workspaceOutputLayout)
            properties.Dock = DockStyle.Fill : properties.HelpVisible = True : properties.ToolbarVisible = True : properties.PropertySort = PropertySort.CategorizedAlphabetical : AddHandler properties.PropertyValueChanged, AddressOf PropertyGridChanged
            propertyTabs.Dock = DockStyle.Fill
            Dim propertyPage As New TabPage("Propriedades") With {.Padding = New Padding(0)}
            Dim eventsPage As New TabPage("⚡ Eventos") With {.Padding = New Padding(0)}
            propertyPage.Controls.Add(properties)
            eventsPage.Controls.Add(eventInspector)
            propertyTabs.TabPages.Add(propertyPage)
            propertyTabs.TabPages.Add(eventsPage)
            AddHandler eventInspector.EventActivated, AddressOf EventInspectorActivated
            AddHandler propertyTabs.SelectedIndexChanged, AddressOf PropertyTabChanged
            rightSplit.Panel2.Controls.Add(Group("PROPRIEDADES / EVENTOS", propertyTabs)) : outerSplit.Panel2.Controls.Add(rightSplit)

            ' Tema da IDE: Escuro (padrão) / Claro.
            Dim themeParentMenu As ToolStripMenuItem = Nothing
            For Each topItem As ToolStripItem In menuStrip.Items
                Dim candidate As ToolStripMenuItem = TryCast(topItem, ToolStripMenuItem)
                If candidate IsNot Nothing AndAlso candidate.Text.Replace("&", "").Trim().Equals("Exibir", StringComparison.OrdinalIgnoreCase) Then
                    themeParentMenu = candidate
                    Exit For
                End If
            Next
            If themeParentMenu IsNot Nothing Then
                Dim themeMenu As New ToolStripMenuItem("Tema")
                _themeDarkItem = New ToolStripMenuItem("Escuro") With {.Checked = True, .CheckOnClick = False}
                _themeLightItem = New ToolStripMenuItem("Claro") With {.CheckOnClick = False}
                AddHandler _themeDarkItem.Click, Sub(sender As Object, e As EventArgs) ApplyIdeTheme(True)
                AddHandler _themeLightItem.Click, Sub(sender As Object, e As EventArgs) ApplyIdeTheme(False)
                themeMenu.DropDownItems.Add(_themeDarkItem)
                themeMenu.DropDownItems.Add(_themeLightItem)
                themeParentMenu.DropDownItems.Add(New ToolStripSeparator())
                themeParentMenu.DropDownItems.Add(themeMenu)
            End If

            Dim shell As New ToolStripContainer With {.Dock = DockStyle.Fill}

            shell.TopToolStripPanel.Join(menuStrip, New Point(0, 0))

            ' RC: um único ToolStrip físico na primeira linha.
            Dim rcMainTools As New ToolStrip()
            rcMainTools.Name = "RcMainTools"
            _rcMainTools = rcMainTools
            rcMainTools.GripStyle = ToolStripGripStyle.Hidden
            rcMainTools.AllowItemReorder = False
            rcMainTools.AutoSize = True
            rcMainTools.Stretch = False
            rcMainTools.RenderMode = fileTools.RenderMode
            rcMainTools.Renderer = fileTools.Renderer
            rcMainTools.BackColor = fileTools.BackColor
            rcMainTools.ForeColor = fileTools.ForeColor

            MoveToolStripItems(fileTools, rcMainTools)
            MoveToolStripItems(designerTools, rcMainTools)
            MoveToolStripItems(buildTools, rcMainTools)

            Dim toolbarRow1Y As Integer = menuStrip.PreferredSize.Height
            shell.TopToolStripPanel.Join(rcMainTools, New Point(0, toolbarRow1Y))

            ' Segunda e última linha física.
            Dim toolbarRow2Y As Integer = toolbarRow1Y + rcMainTools.PreferredSize.Height
            shell.TopToolStripPanel.Join(projectTools, New Point(0, toolbarRow2Y))

            shell.BottomToolStripPanel.Join(statusBar)
            ApplyIdeTheme(True)

            ' RC: bloqueio REAL das barras. GripStyle.Hidden apenas esconde a alça;
            ' ToolStripPanel.Locked impede que ToolStrips sejam arrastados/reorganizados.
            shell.TopToolStripPanel.Locked = True
            shell.LeftToolStripPanel.Locked = True
            shell.RightToolStripPanel.Locked = True
            shell.BottomToolStripPanel.Locked = True

            ' RC: barras principais ficam bloqueadas no layout padrão.
            ' Não restauramos posições antigas nem salvamos movimentação do usuário.
            shell.ContentPanel.Controls.Add(outerSplit)
            Controls.Add(shell)
            AddHandler designer.SelectionChanged, AddressOf DesignerSelectionChanged
            AddHandler designer.ComponentDoubleClick, Sub(sender, control) OpenEvent(control, DefaultEvent(control))
            AddHandler code.TextChanged, AddressOf CodeTextChanged
            AddHandler code.CaretPositionChanged, Sub(sender, line, column) caretStatus.Text = "Ln " & line & ", Col " & column
        End Sub

        Private Function ToolboxHost() As Control
            Dim panel As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(6, 5, 6, 5), .BackColor = Color.FromArgb(32, 35, 42)}
            toolboxSearch.Tag = "Pesquisar componentes..."
            panel.Controls.Add(toolbox) : panel.Controls.Add(toolboxSearch)
            Return panel
        End Function

        Private Sub PaintCodeRuler(sender As Object, e As PaintEventArgs)
            e.Graphics.Clear(codeRuler.BackColor)
            Using separator As New Pen(Color.FromArgb(62, 68, 80))
                e.Graphics.DrawLine(separator, codeRuler.Width - 1, 0, codeRuler.Width - 1, codeRuler.Height)
            End Using
            If code.Lines.Length = 0 Then Return
            Dim currentLine As Integer = code.GetLineFromCharIndex(code.SelectionStart)
            Dim firstLine As Integer = code.FirstVisibleLine
            For lineIndex As Integer = firstLine To code.Lines.Length - 1
                Dim top As Integer = code.GetLineTop(lineIndex)
                If top = Integer.MinValue OrElse top > codeRuler.ClientSize.Height Then Exit For
                If top + code.Font.Height >= 0 Then
                    If lineIndex = currentLine Then
                        Using highlight As New SolidBrush(Color.FromArgb(48, 55, 68))
                            e.Graphics.FillRectangle(highlight, 0, top, codeRuler.Width - 1, code.Font.Height + 2)
                        End Using
                    End If
                    Dim numberColor As Color = If(lineIndex = currentLine, Color.White, Color.FromArgb(135, 145, 160))
                    TextRenderer.DrawText(e.Graphics, (lineIndex + 1).ToString(), code.Font, New Rectangle(0, top, codeRuler.Width - 8, code.Font.Height + 2), numberColor, TextFormatFlags.Right Or TextFormatFlags.NoPadding)
                End If
            Next
        End Sub

        Private Sub CodeViewportChanged(sender As Object, e As EventArgs)
            codeRuler.Invalidate()
        End Sub

        Private Sub CodeRulerMouseDown(sender As Object, e As MouseEventArgs)
            Dim firstLine As Integer = code.FirstVisibleLine
            For lineIndex As Integer = firstLine To code.Lines.Length - 1
                Dim top As Integer = code.GetLineTop(lineIndex)
                If top = Integer.MinValue OrElse top > codeRuler.ClientSize.Height Then Exit For
                If e.Y >= top AndAlso e.Y < top + code.Font.Height + 2 Then
                    code.GoToEditorLine(lineIndex)
                    codeRuler.Invalidate()
                    Return
                End If
            Next
        End Sub

        Private Sub NormalizeLayout()
            If outerSplit.Width > 900 Then outerSplit.SplitterDistance = Math.Min(260, outerSplit.Width - 650)
            If leftSplit.Height > 400 Then leftSplit.SplitterDistance = Math.Max(180, CInt(leftSplit.Height * 0.48))
            If rightSplit.Width > 650 Then rightSplit.SplitterDistance = Math.Max(350, rightSplit.Width - 310)
        End Sub

        Private Sub RestoreLayout()
            outerSplit.Panel1Collapsed = False : leftSplit.Panel1Collapsed = False : leftSplit.Panel2Collapsed = False : rightSplit.Panel2Collapsed = False
            NormalizeLayout() : status.Text = "Layout restaurado"
        End Sub
        Private Sub ToggleExplorerPanel()
            leftSplit.Panel1Collapsed = Not leftSplit.Panel1Collapsed : status.Text = If(leftSplit.Panel1Collapsed, "Explorador oculto", "Explorador visível")
        End Sub
        Private Sub ToggleToolboxPanel()
            leftSplit.Panel2Collapsed = Not leftSplit.Panel2Collapsed : status.Text = If(leftSplit.Panel2Collapsed, "Caixa de ferramentas oculta", "Caixa de ferramentas visível")
        End Sub
        Private Sub TogglePropertiesPanel()
            rightSplit.Panel2Collapsed = Not rightSplit.Panel2Collapsed : status.Text = If(rightSplit.Panel2Collapsed, "Propriedades ocultas", "Propriedades visíveis")
        End Sub
        Private Sub SetGridSize(value As Integer)
            designer.GridSize = value : designer.Invalidate() : status.Text = "Grade configurada para " & value & " pixels"
        End Sub

        Private Sub DesignerCommand(command As String)
            ShowWorkspaceTab(0)
            Dim changed As Boolean
            Select Case command
                Case "centerh" : changed = designer.CenterSelected(True)
                Case "centerv" : changed = designer.CenterSelected(False)
                Case "front" : changed = designer.ChangeSelectedZOrder(True)
                Case "back" : changed = designer.ChangeSelectedZOrder(False)
                Case "left", "right", "top", "bottom", "hcenter", "vcenter" : changed = designer.AlignSelection(command)
                Case "samewidth" : changed = designer.MakeSameSize(True, False)
                Case "sameheight" : changed = designer.MakeSameSize(False, True)
                Case "samesize" : changed = designer.MakeSameSize(False, False)
                Case "distributeh" : changed = designer.DistributeSelection(True)
                Case "distributev" : changed = designer.DistributeSelection(False)
            End Select
            status.Text = If(changed, "Designer atualizado", "Selecione os controles necessários no Designer (Ctrl/Shift+clique ou retângulo de seleção)")
        End Sub

        Private Sub DuplicateSelectedComponent()
            If tabs.SelectedIndex <> 0 Then status.Text = "Abra o Designer para duplicar componentes" : Return
            SaveCurrent()
            Dim selectedName As String = designer.SelectedComponentName
            Dim designControls As List(Of ControlData) = If(activeUserControlIndex >= 0, project.UserControls(activeUserControlIndex).Controls, CurrentDocument.Controls)
            Dim source As ControlData = designControls.FirstOrDefault(Function(c) c.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase))
            If source Is Nothing Then status.Text = "Selecione um componente para duplicar" : Return
            Dim copy As ControlData
            Using stream As New MemoryStream()
                Dim serializer As New System.Runtime.Serialization.Json.DataContractJsonSerializer(GetType(ControlData))
                serializer.WriteObject(stream, source) : stream.Position = 0 : copy = DirectCast(serializer.ReadObject(stream), ControlData)
            End Using
            Dim baseName As String = Regex.Replace(source.TypeName.Split("."c).Last(), "[^A-Za-z0-9_]", "")
            If baseName = "" Then baseName = "Component"
            Dim index As Integer = 1
            Do
                copy.Name = baseName & index : index += 1
            Loop While designControls.Any(Function(c) c.Name.Equals(copy.Name, StringComparison.OrdinalIgnoreCase))
            copy.Properties("Name") = copy.Name
            Dim location As String = If(copy.Properties.ContainsKey("Location"), copy.Properties("Location"), If(copy.Properties.ContainsKey("__TrayLocation"), copy.Properties("__TrayLocation"), "20, 20"))
            Dim parts As String() = location.Split(","c)
            If parts.Length = 2 Then
                Dim x As Integer, y As Integer
                If Integer.TryParse(parts(0).Trim(), x) AndAlso Integer.TryParse(parts(1).Trim(), y) Then
                    Dim key As String = If(copy.IsNonVisual, "__TrayLocation", "Location") : copy.Properties(key) = (x + 10) & ", " & (y + 10)
                End If
            End If
            designControls.Add(copy)
            If activeUserControlIndex >= 0 Then designer.LoadUserControl(project.UserControls(activeUserControlIndex)) Else designer.LoadDocument(CurrentDocument)
            status.Text = copy.Name & " duplicado"
        End Sub

        Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
            If tabs.SelectedIndex = 0 Then
                Dim stepSize As Integer = If((keyData And Keys.Control) = Keys.Control, Math.Max(1, designer.GridSize), 1)
                Dim resize As Boolean = (keyData And Keys.Shift) = Keys.Shift
                Dim key As Keys = keyData And Keys.KeyCode
                Dim changed As Boolean
                Select Case key
                    Case Keys.Left : changed = designer.NudgeSelected(-stepSize, If(resize, 0, 0), resize)
                    Case Keys.Right : changed = designer.NudgeSelected(stepSize, If(resize, 0, 0), resize)
                    Case Keys.Up : changed = designer.NudgeSelected(If(resize, 0, 0), -stepSize, resize)
                    Case Keys.Down : changed = designer.NudgeSelected(If(resize, 0, 0), stepSize, resize)
                End Select
                If changed Then status.Text = If(resize, "Componente redimensionado", "Componente movido") : Return True
            End If
            Return MyBase.ProcessCmdKey(msg, keyData)
        End Function

        Private Sub ShowWorkspaceTab(index As Integer)
            If index = 0 Then
                If activeUserControlIndex >= 0 Then OpenUserControlDesigner(activeUserControlIndex) Else OpenFormDesigner(activeIndex)
                Return
            End If

            ' F7 mantém o tipo de documento atual. Forms alternam para código;
            ' Classes, Modules e UserControls permanecem no respectivo arquivo VB.
            If activeClassIndex >= 0 Then
                OpenClassDocument(activeClassIndex)
            ElseIf activeModuleIndex >= 0 Then
                OpenModuleDocument(activeModuleIndex)
            ElseIf activeUserControlIndex >= 0 Then
                OpenUserControlDocument(activeUserControlIndex)
            Else
                OpenFormCode(activeIndex)
            End If
        End Sub

        Private Sub UpdateFormViewBar(isForm As Boolean, designMode As Boolean, Optional documentName As String = Nothing)
            formViewBar.Visible = isForm
            If workspaceLayout.RowStyles.Count > 1 Then
                workspaceLayout.RowStyles(1).SizeType = SizeType.Absolute
                workspaceLayout.RowStyles(1).Height = If(isForm, 28.0F, 0.0F)
            End If
            If Not isForm Then Return

            formViewLabel.Text = If(String.IsNullOrWhiteSpace(documentName), CurrentDocument.Name, documentName) & ":"
            Dim activeBack As Color = Color.FromArgb(0, 122, 204)
            Dim inactiveBack As Color = Color.FromArgb(48, 52, 60)
            viewDesignerButton.BackColor = If(designMode, activeBack, inactiveBack)
            viewCodeButton.BackColor = If(designMode, inactiveBack, activeBack)
            viewDesignerButton.ForeColor = Color.White
            viewCodeButton.ForeColor = Color.White
        End Sub

        Private Sub MarkCurrentDocumentDirty()
            If changing OrElse documentTabs.SelectedTab Is Nothing Then Return
            Dim info As DocumentTabInfo = TryCast(documentTabs.SelectedTab.Tag, DocumentTabInfo)
            If info Is Nothing OrElse info.Dirty Then Return
            info.Dirty = True
            documentTabs.SelectedTab.Text = info.BaseTitle & " *"
            UpdateWindowTitle()
        End Sub

        Private Sub ClearDirtyIndicators()
            For Each page As TabPage In documentTabs.TabPages
                Dim info As DocumentTabInfo = TryCast(page.Tag, DocumentTabInfo)
                If info Is Nothing Then Continue For
                info.Dirty = False
                page.Text = info.BaseTitle
            Next
            UpdateWindowTitle()
        End Sub

        Private Function HasDirtyDocuments() As Boolean
            For Each page As TabPage In documentTabs.TabPages
                Dim info As DocumentTabInfo = TryCast(page.Tag, DocumentTabInfo)
                If info IsNot Nothing AndAlso info.Dirty Then Return True
            Next
            Return False
        End Function

        Private Sub UpdateWindowTitle()
            Dim projectName As String = If(project Is Nothing OrElse String.IsNullOrWhiteSpace(project.Name), "Sem projeto", project.Name)
            Text = "FlowForge - " & projectName & If(HasDirtyDocuments(), " *", "")
        End Sub

        Private Sub RenameProject()
            Dim newName As String = Interaction.InputBox("Digite o novo nome do projeto:", "Renomear projeto", project.Name).Trim()
            If newName = "" OrElse newName = project.Name Then Return
            project.Name = newName : UpdateWindowTitle() : RefreshTree() : status.Text = "Projeto renomeado para " & newName
        End Sub

        Private Sub FocusSearch()
            ShowWorkspaceTab(1) : searchBox.Focus() : searchBox.SelectAll()
        End Sub
        Private Sub FocusReplace()
            ShowWorkspaceTab(1) : replaceBox.Focus() : replaceBox.SelectAll()
        End Sub
        Private Sub FindCode(backwards As Boolean)
            Dim term As String = searchBox.Text
            If term = "" Then FocusSearch() : Return
            Dim found As Integer
            If backwards Then
                found = code.Find(term, 0, Math.Max(0, code.SelectionStart - 1), RichTextBoxFinds.Reverse)
                If found < 0 Then found = code.Find(term, 0, code.TextLength, RichTextBoxFinds.Reverse)
            Else
                found = code.Find(term, Math.Min(code.TextLength, code.SelectionStart + code.SelectionLength), code.TextLength, RichTextBoxFinds.None)
                If found < 0 Then found = code.Find(term, 0, code.TextLength, RichTextBoxFinds.None)
            End If
            If found >= 0 Then code.Select(found, term.Length) : code.ScrollToCaret() : code.Focus() Else status.Text = "Texto não encontrado: " & term
        End Sub
        Private Sub ReplaceCode()
            If searchBox.Text = "" Then FocusSearch() : Return
            If code.SelectionLength > 0 AndAlso code.SelectedText.Equals(searchBox.Text, StringComparison.OrdinalIgnoreCase) Then
                code.SelectedText = replaceBox.Text
            Else
                FindCode(False)
            End If
        End Sub
        Private Sub ReplaceAllCode()
            If searchBox.Text = "" Then FocusSearch() : Return
            Dim matches As Integer = Regex.Matches(code.Text, Regex.Escape(searchBox.Text), RegexOptions.IgnoreCase).Count
            If matches = 0 Then status.Text = "Texto não encontrado: " & searchBox.Text : Return
            Dim caret As Integer = code.SelectionStart
            code.Text = Regex.Replace(code.Text, Regex.Escape(searchBox.Text), Function(m) replaceBox.Text, RegexOptions.IgnoreCase)
            code.SelectionStart = Math.Min(caret, code.TextLength) : code.Focus() : status.Text = matches & " ocorrência(s) substituída(s)"
        End Sub
        Private Sub FindInProject()
            SaveCurrent()
            Dim term As String = Interaction.InputBox("Texto a localizar em todos os arquivos VB do projeto:", "Localizar no projeto", searchBox.Text).Trim()
            If term = "" Then Return

            Dim results As New List(Of ProjectSearchResult)()
            For i As Integer = 0 To project.Forms.Count - 1
                AddProjectSearchMatches(results, term, "Form", i, project.Forms(i).Name & ".vb", project.Forms(i).Code)
            Next
            If project.Classes IsNot Nothing Then
                For i As Integer = 0 To project.Classes.Count - 1
                    AddProjectSearchMatches(results, term, "Class", i, project.Classes(i).Name & ".vb", project.Classes(i).Code)
                Next
            End If
            If project.Modules IsNot Nothing Then
                For i As Integer = 0 To project.Modules.Count - 1
                    AddProjectSearchMatches(results, term, "Module", i, project.Modules(i).Name & ".vb", project.Modules(i).Code)
                Next
            End If
            If project.UserControls IsNot Nothing Then
                For i As Integer = 0 To project.UserControls.Count - 1
                    AddProjectSearchMatches(results, term, "UserControl", i, project.UserControls(i).Name & ".vb", project.UserControls(i).Code)
                Next
            End If

            If results.Count = 0 Then
                MessageBox.Show("Nenhuma ocorrência encontrada para '" & term & "'.", "Localizar no projeto", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Using dialog As New FindInProjectForm(term, results)
                If dialog.ShowDialog(Me) = DialogResult.OK AndAlso dialog.SelectedResult IsNot Nothing Then
                    OpenSearchResult(dialog.SelectedResult)
                End If
            End Using
        End Sub

        Private Sub AddProjectSearchMatches(results As List(Of ProjectSearchResult), term As String, kind As String, index As Integer, fileName As String, source As String)
            If String.IsNullOrEmpty(source) Then Return
            Dim lines As String() = source.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Split(ControlChars.Lf)
            For lineIndex As Integer = 0 To lines.Length - 1
                If lines(lineIndex).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 Then
                    Dim preview As String = lines(lineIndex).Trim()
                    If preview.Length > 180 Then preview = preview.Substring(0, 177) & "..."
                    results.Add(New ProjectSearchResult With {.Kind = kind, .Index = index, .FileName = fileName, .LineNumber = lineIndex + 1, .Preview = preview})
                End If
            Next
        End Sub

        Private Sub OpenSearchResult(result As ProjectSearchResult)
            Select Case result.Kind
                Case "Form"
                    OpenFormCode(result.Index)
                Case "Class"
                    OpenClassDocument(result.Index)
                Case "Module"
                    OpenModuleDocument(result.Index)
                Case "UserControl"
                    OpenUserControlDocument(result.Index)
                Case Else
                    Return
            End Select
            Dim targetLine As Integer = Math.Max(0, Math.Min(result.LineNumber - 1, Math.Max(0, code.Lines.Length - 1)))
            Dim position As Integer = code.GetFirstCharIndexFromLine(targetLine)
            If position < 0 Then position = 0
            code.Select(position, 0)
            code.ScrollToCaret()
            code.Focus()
            status.Text = result.FileName & " — linha " & result.LineNumber.ToString()
        End Sub

        Private Sub GoToLine()
            ShowWorkspaceTab(1)
            Dim answer As String = Interaction.InputBox("Número da linha:", "Ir para linha", (code.GetLineFromCharIndex(code.SelectionStart) + 1).ToString())
            Dim line As Integer
            If Not Integer.TryParse(answer, line) OrElse line < 1 OrElse line > code.Lines.Length Then MessageBox.Show("Número de linha inválido.") : Return
            Dim position As Integer = code.GetFirstCharIndexFromLine(line - 1)
            code.Select(Math.Max(0, position), 0) : code.ScrollToCaret() : code.Focus()
        End Sub
        Private Sub ToggleComment(addComment As Boolean)
            ShowWorkspaceTab(1)
            Dim firstLine As Integer = code.GetLineFromCharIndex(code.SelectionStart)
            Dim lastLine As Integer = code.GetLineFromCharIndex(code.SelectionStart + code.SelectionLength)
            Dim start As Integer = code.GetFirstCharIndexFromLine(firstLine)
            Dim finish As Integer = If(lastLine + 1 < code.Lines.Length, code.GetFirstCharIndexFromLine(lastLine + 1), code.TextLength)
            Dim block As String = code.Text.Substring(start, finish - start)
            If addComment Then block = Regex.Replace(block, "(?m)^(\s*)", "$1' ") Else block = Regex.Replace(block, "(?m)^(\s*)'\s?", "$1")
            code.Select(start, finish - start) : code.SelectedText = block : code.Select(start, block.Length) : code.Focus()
        End Sub

        Private Sub FocusProjectPanel()
            projectTree.Focus()
        End Sub

        Private Sub EditCommand(command As String)
            If tabs.SelectedIndex = 0 AndAlso (command = "undo" OrElse command = "redo") Then
                Dim changed As Boolean = If(command = "undo", designer.UndoDesigner(), designer.RedoDesigner())
                status.Text = If(changed, If(command = "undo", "Ação do Designer desfeita", "Ação do Designer refeita"), "Não há mais ações no histórico do Designer")
                Return
            End If
            If tabs.SelectedIndex <> 1 Then
                status.Text = "Este comando está disponível no editor de código."
                Return
            End If
            Select Case command
                Case "undo" : If code.CanUndo Then code.Undo()
                Case "redo" : If code.CanRedo Then code.Redo()
                Case "cut" : code.Cut()
                Case "copy" : code.Copy()
                Case "paste" : code.Paste()
                Case "selectall" : code.SelectAll()
            End Select
            code.Focus()
        End Sub

        Private Sub ToggleGrid()
            designer.ShowGrid = Not designer.ShowGrid
            designer.Invalidate()
            status.Text = If(designer.ShowGrid, "Grade visível", "Grade oculta")
        End Sub

        Private Sub ToggleSnap()
            designer.SnapToGrid = Not designer.SnapToGrid
            status.Text = If(designer.SnapToGrid, "Encaixe na grade ativado", "Encaixe na grade desativado")
        End Sub

        Private Sub ShowAbout()
            MessageBox.Show("FlowForge Studio 0.39.1" & vbCrLf & vbCrLf & "IDE visual para criar aplicativos VB.NET Windows Forms." & vbCrLf & "Inclui 24 controles FlowForge, editor rico e editor de código com sintaxe e linhas." & vbCrLf & ".NET Framework 4.8" & vbCrLf & "Imagens, recursos e DLLs podem ser incorporados ao EXE final.", "Sobre o FlowForge Studio", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        Private Sub ManageProjectResources()
            SaveCurrent()
            Using dialog As New ResourceManagerForm(project)
                dialog.ShowDialog(Me)
            End Using
            savedProjectState = If(currentFile Is Nothing, savedProjectState, savedProjectState)
            status.Text = "Recursos do projeto atualizados"
        End Sub

        Private Sub OpenArduinoTools()
            Using dialog As New ArduinoToolsForm()
                dialog.ShowDialog(Me)
            End Using
        End Sub

        Private Sub OpenFlowchartEditor()
            Using dialog As New FlowchartEditorForm()
                If dialog.ShowDialog(Me) <> DialogResult.OK Then Return
                If project.Modules Is Nothing Then project.Modules = New List(Of ModuleData)()
                Dim index As Integer = 1
                Dim name As String = "Fluxograma" & index.ToString()
                While project.Modules.Any(Function(m) m.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    index += 1 : name = "Fluxograma" & index.ToString()
                End While
                Dim generated As String = dialog.GeneratedCode.Replace("Public Module Fluxograma1", "Public Module " & name)
                project.Modules.Add(New ModuleData With {.Name = name, .Code = generated})
                RefreshProject()
                OpenModuleDocument(project.Modules.Count - 1)
                status.Text = name & ".vb criado a partir do fluxograma"
            End Using
        End Sub

        Private Sub OpenDatabaseDesigner()
            Using dialog As New DatabaseDesignerForm()
                If dialog.ShowDialog(Me) <> DialogResult.OK Then Return
                If project.Modules Is Nothing Then project.Modules = New List(Of ModuleData)()
                Dim existing As ModuleData = project.Modules.FirstOrDefault(Function(m) m.Name.Equals("BancoDados", StringComparison.OrdinalIgnoreCase))
                If existing Is Nothing Then
                    existing = New ModuleData With {.Name = "BancoDados"}
                    project.Modules.Add(existing)
                End If
                existing.Code = dialog.GeneratedModule
                RefreshProject()
                OpenModuleDocument(project.Modules.IndexOf(existing))
                status.Text = "Módulo BancoDados.vb gerado"
            End Using
        End Sub

        Private Sub ManageProjectLibraries()
            SaveCurrent()
            Using dialog As New ProjectLibrariesForm(project)
                dialog.ShowDialog(Me)
            End Using
            MaterializeProjectLibrariesForDesigner()
            RefreshTree()
            status.Text = project.Libraries.Count.ToString() & " biblioteca(s) incorporada(s)"
        End Sub

        Private Sub ToggleFormChrome()
            Dim newValue As Boolean = Not designer.ShowFormChrome
            designer.SetShowFormChrome(newValue)
            status.Text = If(newValue, "Moldura do Form visível", "Moldura do Form oculta")
        End Sub

        Private Sub PropertyGridChanged(sender As Object, e As PropertyValueChangedEventArgs)
            MarkCurrentDocumentDirty()
            designer.RefreshFromPropertyGrid() : designer.UpdateFormChrome() : status.Text = "Propriedade alterada"
            changing = True : formsBox.DataSource = Nothing : formsBox.DisplayMember = "Name" : formsBox.DataSource = project.Forms : formsBox.SelectedIndex = activeIndex : RefreshTree() : UpdateAutocomplete() : changing = False
        End Sub

        Private Sub DesignerSelectionChanged(sender As Object, selected As Object)
            properties.SelectedObject = If(selected Is Nothing, Nothing, New DesignObjectAdapter(selected))
            If selected Is Nothing Then
                eventInspector.ClearTarget()
            Else
                Dim designName As String = If(activeUserControlIndex >= 0, project.UserControls(activeUserControlIndex).Name, CurrentDocument.Name)
                Dim designCode As String = If(activeUserControlIndex >= 0, project.UserControls(activeUserControlIndex).Code, CurrentDocument.Code)
                Dim componentName As String = If(TypeOf selected Is Form, designName, designer.SelectedComponentName)
                eventInspector.SetTarget(selected, componentName, designCode)
            End If
            UpdateAutocomplete()
            selectionStatus.Text = If(selected Is Nothing, "Sem seleção", designer.SelectedComponentName & " : " & selected.GetType().Name)
            Dim visualControl As Control = designer.SelectedControl
            If visualControl Is Nothing Then visualControl = TryCast(selected, Control)
            If visualControl Is Nothing Then Return
            Dim menu As New ContextMenuStrip(), eventsMenu As New ToolStripMenuItem("Eventos")
            Dim defaultName As String = DefaultEvent(selected)
            For Each ev As System.ComponentModel.EventDescriptor In System.ComponentModel.TypeDescriptor.GetEvents(selected).Cast(Of System.ComponentModel.EventDescriptor)().OrderBy(Function(x) x.Name)
                Dim item As New ToolStripMenuItem(If(ev.Name = defaultName, "★ ", "") & ev.Name) With {.Tag = ev.Name, .Font = New Font(menu.Font, If(ev.Name = defaultName, FontStyle.Bold, FontStyle.Regular))}
                AddHandler item.Click, Sub() OpenEvent(selected, CStr(item.Tag))
                eventsMenu.DropDownItems.Add(item)
            Next
            menu.Items.Add(eventsMenu)
            If TypeOf selected Is ToolStrip OrElse TypeOf selected Is ToolStripItem Then
                menu.Items.Add(New ToolStripSeparator())
                menu.Items.Add(CreateAddItemsMenu())
                menu.Items.Add(Item("Editar itens da barra...", AddressOf EditToolStripItems, "properties"))
            End If
            If Not TypeOf selected Is Form Then
                menu.Items.Add(New ToolStripSeparator())
                Dim remove As New ToolStripMenuItem("Excluir")
                AddHandler remove.Click, Sub() designer.DeleteSelected()
                menu.Items.Add(remove)
            End If
            If TypeOf selected Is Form Then designer.FormCanvas.ContextMenuStrip = menu Else visualControl.ContextMenuStrip = menu
        End Sub

        Private Sub NewProject()
            If Not ConfirmDiscardChanges() Then Return
            project = New FlowProject() : project.Forms.Add(FormData.CreateDefault("Form1", True)) : currentFile = Nothing : activeIndex = 0 : activeClassIndex = -1 : activeModuleIndex = -1 : activeUserControlIndex = -1 : documentTabs.TabPages.Clear() : RefreshProject() : savedProjectState = ProjectState()
        End Sub

        Private Function ProjectState() As String
            If project Is Nothing Then Return ""
            SaveCurrent()
            Using stream As New MemoryStream()
                Dim serializer As New System.Runtime.Serialization.Json.DataContractJsonSerializer(GetType(FlowProject))
                serializer.WriteObject(stream, project)
                Return Convert.ToBase64String(stream.ToArray())
            End Using
        End Function

        Private Function ConfirmDiscardChanges() As Boolean
            If project Is Nothing OrElse ProjectState() = savedProjectState Then Return True
            Dim answer As DialogResult = MessageBox.Show("O projeto possui alterações não salvas. Deseja salvar antes de continuar?", "FlowForge", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning)
            If answer = DialogResult.Cancel Then Return False
            If answer = DialogResult.Yes Then
                SaveProject()
                Return currentFile IsNot Nothing AndAlso ProjectState() = savedProjectState
            End If
            Return True
        End Function

        Private Sub MainFormClosingCheck(sender As Object, e As FormClosingEventArgs)
            If Not ConfirmDiscardChanges() Then e.Cancel = True
        End Sub
        Private Sub RefreshProject()
            activeClassIndex = -1 : activeModuleIndex = -1 : activeUserControlIndex = -1
            changing = True : formsBox.DataSource = Nothing : formsBox.DisplayMember = "Name" : formsBox.DataSource = project.Forms : formsBox.SelectedIndex = activeIndex
            designer.LoadDocument(CurrentDocument) : code.Text = CurrentDocument.Code : code.Colorize() : RefreshTree() : LoadToolbox() : UpdateAutocomplete() : UpdateWindowTitle() : changing = False : status.Text = CurrentDocument.Name : formStatus.Text = CurrentDocument.Name
            tabs.SelectedIndex = 0
            UpdateFormViewBar(True, True)
            OpenDocumentTab("FormDesign", CurrentDocument, CurrentDocument.Name & " [Design]")
        End Sub
        Private Sub UpdateAutocomplete()
            If project Is Nothing Then Return
            Dim names As New List(Of String) From {"Show", "ShowDialog", "Close", "Hide", "Enabled", "Visible", "Text", "Name", "Location", "Size", "MessageBox.Show"}
            Dim symbols As New Dictionary(Of String, Type)(StringComparer.OrdinalIgnoreCase)
            If project.Classes IsNot Nothing Then
                For Each classFile As ClassData In project.Classes
                    names.Add(classFile.Name)
                Next
            End If
            If project.Modules IsNot Nothing Then
                For Each moduleFile As ModuleData In project.Modules
                    names.Add(moduleFile.Name)
                Next
            End If
            If project.UserControls IsNot Nothing Then
                For Each controlFile As UserControlData In project.UserControls
                    names.Add(controlFile.Name)
                Next
            End If
            If project.UserControls IsNot Nothing Then
                For Each userControlFile As UserControlData In project.UserControls
                    For Each item As ControlData In userControlFile.Controls
                        names.Add(item.Name)
                        Dim componentType As Type = ResolveComponentType(item)
                        If componentType IsNot Nothing Then symbols(item.Name) = componentType
                        AddToolStripAutocomplete(item.Items, names, symbols)
                    Next
                Next
            End If
            For Each form As FormData In project.Forms
                names.Add(form.Name)
                names.Add(form.Name & ".Show")
                names.Add(form.Name & ".ShowDialog")
                symbols(form.Name) = GetType(Form)
                For Each item As ControlData In form.Controls
                    names.Add(item.Name)
                    names.Add(item.Name & ".Text")
                    names.Add(item.Name & ".ShowDialog")
                    Dim componentType As Type = ResolveComponentType(item)
                    If componentType IsNot Nothing Then symbols(item.Name) = componentType
                    AddToolStripAutocomplete(item.Items, names, symbols)
                Next
            Next
            code.SetProjectSuggestions(names)
            code.SetProjectSymbols(symbols)
        End Sub

        Private Shared Sub AddToolStripAutocomplete(items As List(Of ToolStripItemData), names As List(Of String), symbols As Dictionary(Of String, Type))
            If items Is Nothing Then Return
            For Each item As ToolStripItemData In items
                names.Add(item.Name)
                Dim itemType As Type = GetType(Form).Assembly.GetType("System.Windows.Forms." & item.TypeName, False, True)
                If itemType IsNot Nothing Then symbols(item.Name) = itemType
                AddToolStripAutocomplete(item.DropDownItems, names, symbols)
            Next
        End Sub

        Private Shared Function ResolveComponentType(item As ControlData) As Type
            Try
                If Not String.IsNullOrWhiteSpace(item.AssemblyPath) AndAlso File.Exists(item.AssemblyPath) Then Return Assembly.LoadFrom(item.AssemblyPath).GetType(item.TypeName, False, True)
                If item.TypeName.Equals("ColorComboBox", StringComparison.OrdinalIgnoreCase) Then Return GetType(ColorComboBox)
                If item.TypeName.Equals("RoundedButton", StringComparison.OrdinalIgnoreCase) Then Return GetType(RoundedButton)
                If item.TypeName.Equals("GradientPanel", StringComparison.OrdinalIgnoreCase) Then Return GetType(GradientPanel)
                If item.TypeName.Equals("LedIndicator", StringComparison.OrdinalIgnoreCase) Then Return GetType(LedIndicator)
                If item.TypeName.Equals("ToggleSwitch", StringComparison.OrdinalIgnoreCase) Then Return GetType(ToggleSwitch)
                If item.TypeName.Equals("DigitalDisplay", StringComparison.OrdinalIgnoreCase) Then Return GetType(DigitalDisplay)
                If item.TypeName.Equals("CircularProgress", StringComparison.OrdinalIgnoreCase) Then Return GetType(CircularProgress)
                If item.TypeName.Equals("LevelMeter", StringComparison.OrdinalIgnoreCase) Then Return GetType(LevelMeter)
                If item.TypeName.Equals("BadgeLabel", StringComparison.OrdinalIgnoreCase) Then Return GetType(BadgeLabel)
                If item.TypeName.Equals("SeparatorLine", StringComparison.OrdinalIgnoreCase) Then Return GetType(SeparatorLine)
                If item.TypeName.Equals("StarRating", StringComparison.OrdinalIgnoreCase) Then Return GetType(StarRating)
                If item.TypeName.Equals("NumericKnob", StringComparison.OrdinalIgnoreCase) Then Return GetType(NumericKnob)
                If item.TypeName.Equals("CardPanel", StringComparison.OrdinalIgnoreCase) Then Return GetType(CardPanel)
                If item.TypeName.Equals("BatteryIndicator", StringComparison.OrdinalIgnoreCase) Then Return GetType(BatteryIndicator)
                If item.TypeName.Equals("SignalStrength", StringComparison.OrdinalIgnoreCase) Then Return GetType(SignalStrength)
                If item.TypeName.Equals("ThermometerGauge", StringComparison.OrdinalIgnoreCase) Then Return GetType(ThermometerGauge)
                If item.TypeName.Equals("AnalogGauge", StringComparison.OrdinalIgnoreCase) Then Return GetType(AnalogGauge)
                If item.TypeName.Equals("LoadingSpinner", StringComparison.OrdinalIgnoreCase) Then Return GetType(LoadingSpinner)
                If item.TypeName.Equals("NotificationBanner", StringComparison.OrdinalIgnoreCase) Then Return GetType(NotificationBanner)
                If item.TypeName.Equals("ToggleButton", StringComparison.OrdinalIgnoreCase) Then Return GetType(ToggleButton)
                If item.TypeName.Equals("ColorSwatch", StringComparison.OrdinalIgnoreCase) Then Return GetType(ColorSwatch)
                If item.TypeName.Equals("NavigationButton", StringComparison.OrdinalIgnoreCase) Then Return GetType(NavigationButton)
                If item.TypeName.Equals("MarqueeLabel", StringComparison.OrdinalIgnoreCase) Then Return GetType(MarqueeLabel)
                If item.TypeName.Equals("FontPreviewComboBox", StringComparison.OrdinalIgnoreCase) Then Return GetType(FontPreviewComboBox)
                If item.TypeName.Equals("GlyphImageList", StringComparison.OrdinalIgnoreCase) Then Return GetType(GlyphImageList)
                If item.TypeName.Equals("ToastNotification", StringComparison.OrdinalIgnoreCase) Then Return GetType(ToastNotification)
                If item.TypeName.Equals("Accordion", StringComparison.OrdinalIgnoreCase) Then Return GetType(Accordion)
                If item.TypeName.Equals("ProgressStepper", StringComparison.OrdinalIgnoreCase) Then Return GetType(ProgressStepper)
                If item.TypeName.Equals("TerminalView", StringComparison.OrdinalIgnoreCase) Then Return GetType(TerminalView)
                If item.TypeName.Equals("RichTextEditor", StringComparison.OrdinalIgnoreCase) Then Return GetType(RichTextEditor)
                If item.TypeName.Equals("SyntaxCodeEditor", StringComparison.OrdinalIgnoreCase) Then Return GetType(SyntaxCodeEditor)
                If item.TypeName.Equals("Sparkline", StringComparison.OrdinalIgnoreCase) Then Return GetType(Sparkline)
                If item.TypeName.Equals("ImageButton", StringComparison.OrdinalIgnoreCase) Then Return GetType(ImageButton)
                If item.TypeName.Equals("SearchBox", StringComparison.OrdinalIgnoreCase) Then Return GetType(SearchBox)
                If item.TypeName.Equals("PasswordBox", StringComparison.OrdinalIgnoreCase) Then Return GetType(PasswordBox)
                If item.TypeName.Equals("IPAddressBox", StringComparison.OrdinalIgnoreCase) Then Return GetType(IPAddressBox)
                If item.TypeName.Equals("ModernDatePicker", StringComparison.OrdinalIgnoreCase) Then Return GetType(ModernDatePicker)
                If item.TypeName.Equals("SimpleChart", StringComparison.OrdinalIgnoreCase) Then Return GetType(SimpleChart)
                If item.TypeName.Equals("VirtualJoystick", StringComparison.OrdinalIgnoreCase) Then Return GetType(VirtualJoystick)
                If item.TypeName.Equals("LcdDisplay", StringComparison.OrdinalIgnoreCase) Then Return GetType(LcdDisplay)
                If item.TypeName.Equals("LedMatrix", StringComparison.OrdinalIgnoreCase) Then Return GetType(LedMatrix)
                If item.TypeName.Equals("TrafficLight", StringComparison.OrdinalIgnoreCase) Then Return GetType(TrafficLight)
                If item.TypeName.Equals("SevenSegmentDigit", StringComparison.OrdinalIgnoreCase) Then Return GetType(SevenSegmentDigit)
                If item.TypeName.Equals("ArduinoPin", StringComparison.OrdinalIgnoreCase) Then Return GetType(ArduinoPin)
                If item.TypeName.Equals("IoTSensor", StringComparison.OrdinalIgnoreCase) Then Return GetType(IoTSensor)
                If item.TypeName.Equals("TagInput", StringComparison.OrdinalIgnoreCase) Then Return GetType(TagInput)
                If item.TypeName.Equals("TabStripCustom", StringComparison.OrdinalIgnoreCase) Then Return GetType(TabStripCustom)
                If item.TypeName.Equals("JsonTreeViewer", StringComparison.OrdinalIgnoreCase) Then Return GetType(JsonTreeViewer)
                If item.TypeName.Equals("SerialConnection", StringComparison.OrdinalIgnoreCase) Then Return GetType(SerialConnection)
                Return GetType(Form).Assembly.GetType("System.Windows.Forms." & item.TypeName, False, True)
            Catch
                Return Nothing
            End Try
        End Function
        Private Sub RefreshTree()
            projectTree.Nodes.Clear()
            Dim root As New TreeNode(project.Name) With {.Tag = New ProjectNodeInfo("Project", -1, "")}
            projectTree.Nodes.Add(root)

            Dim formsNode As New TreeNode("Forms") With {.Tag = New ProjectNodeInfo("Category", -1, "")}
            For i As Integer = 0 To project.Forms.Count - 1
                If String.IsNullOrWhiteSpace(project.Forms(i).FolderPath) Then AddFormTreeNode(formsNode, i)
            Next
            root.Nodes.Add(formsNode)

            Dim classesNode As New TreeNode("Classes") With {.Tag = New ProjectNodeInfo("Category", -1, "")}
            If project.Classes IsNot Nothing Then
                For i As Integer = 0 To project.Classes.Count - 1
                    If String.IsNullOrWhiteSpace(project.Classes(i).FolderPath) Then classesNode.Nodes.Add(New TreeNode(project.Classes(i).Name & ".vb") With {.Tag = New ProjectNodeInfo("Class", i, "")})
                Next
            End If
            root.Nodes.Add(classesNode)

            Dim modulesNode As New TreeNode("Modules") With {.Tag = New ProjectNodeInfo("Category", -1, "")}
            If project.Modules IsNot Nothing Then
                For i As Integer = 0 To project.Modules.Count - 1
                    If String.IsNullOrWhiteSpace(project.Modules(i).FolderPath) Then modulesNode.Nodes.Add(New TreeNode(project.Modules(i).Name & ".vb") With {.Tag = New ProjectNodeInfo("Module", i, "")})
                Next
            End If
            root.Nodes.Add(modulesNode)

            Dim userControlsNode As New TreeNode("UserControls") With {.Tag = New ProjectNodeInfo("Category", -1, "")}
            If project.UserControls IsNot Nothing Then
                For i As Integer = 0 To project.UserControls.Count - 1
                    If String.IsNullOrWhiteSpace(project.UserControls(i).FolderPath) Then userControlsNode.Nodes.Add(New TreeNode(project.UserControls(i).Name & ".vb") With {.Tag = New ProjectNodeInfo("UserControl", i, "")})
                Next
            End If
            root.Nodes.Add(userControlsNode)

            If project.Folders IsNot Nothing AndAlso project.Folders.Count > 0 Then
                Dim foldersRoot As New TreeNode("Pastas") With {.Tag = New ProjectNodeInfo("FoldersRoot", -1, "")}
                For Each folder As ProjectFolderData In project.Folders.Where(Function(x) Not String.IsNullOrWhiteSpace(x.Path) AndAlso Not x.Path.Contains("/"c)).OrderBy(Function(x) x.Path)
                    AddFolderTreeNode(foldersRoot, folder.Path)
                Next
                root.Nodes.Add(foldersRoot)
            End If

            If project.Libraries IsNot Nothing AndAlso project.Libraries.Count > 0 Then
                Dim librariesNode As New TreeNode("Bibliotecas (" & project.Libraries.Count.ToString() & ")")
                For Each library As ProjectLibrary In project.Libraries.OrderBy(Function(x) x.RelativePath)
                    librariesNode.Nodes.Add(New TreeNode(library.RelativePath & If(library.IsReference, " [referência]", " [runtime]")))
                Next
                root.Nodes.Add(librariesNode)
            End If
            root.ExpandAll()
        End Sub

        Private Sub AddFormTreeNode(parent As TreeNode, index As Integer)
            Dim f As FormData = project.Forms(index)
            Dim formNode As New TreeNode(If(f.IsStartup, "★ ", "") & f.Name) With {.Tag = New ProjectNodeInfo("FormDesign", index, f.FolderPath)}
            formNode.Nodes.Add(New TreeNode(f.Name & ".vb") With {.Tag = New ProjectNodeInfo("FormCode", index, f.FolderPath)})
            formNode.Nodes.Add(New TreeNode(f.Name & ".Designer.vb") With {.Tag = New ProjectNodeInfo("FormDesign", index, f.FolderPath)})
            parent.Nodes.Add(formNode)
        End Sub

        Private Sub AddFolderTreeNode(parent As TreeNode, folderPath As String)
            Dim name As String = folderPath.Substring(folderPath.LastIndexOf("/"c) + 1)
            Dim node As New TreeNode(name) With {.Tag = New ProjectNodeInfo("Folder", -1, folderPath)}
            parent.Nodes.Add(node)
            For i As Integer = 0 To project.Forms.Count - 1
                If String.Equals(project.Forms(i).FolderPath, folderPath, StringComparison.OrdinalIgnoreCase) Then AddFormTreeNode(node, i)
            Next
            If project.Classes IsNot Nothing Then
                For i As Integer = 0 To project.Classes.Count - 1
                    If String.Equals(project.Classes(i).FolderPath, folderPath, StringComparison.OrdinalIgnoreCase) Then node.Nodes.Add(New TreeNode(project.Classes(i).Name & ".vb") With {.Tag = New ProjectNodeInfo("Class", i, folderPath)})
                Next
            End If
            If project.Modules IsNot Nothing Then
                For i As Integer = 0 To project.Modules.Count - 1
                    If String.Equals(project.Modules(i).FolderPath, folderPath, StringComparison.OrdinalIgnoreCase) Then node.Nodes.Add(New TreeNode(project.Modules(i).Name & ".vb") With {.Tag = New ProjectNodeInfo("Module", i, folderPath)})
                Next
            End If
            If project.UserControls IsNot Nothing Then
                For i As Integer = 0 To project.UserControls.Count - 1
                    If String.Equals(project.UserControls(i).FolderPath, folderPath, StringComparison.OrdinalIgnoreCase) Then node.Nodes.Add(New TreeNode(project.UserControls(i).Name & ".vb") With {.Tag = New ProjectNodeInfo("UserControl", i, folderPath)})
                Next
            End If
            Dim prefix As String = folderPath & "/"
            For Each child As ProjectFolderData In project.Folders.Where(Function(x) x.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) AndAlso Not x.Path.Substring(prefix.Length).Contains("/"c)).OrderBy(Function(x) x.Path)
                AddFolderTreeNode(node, child.Path)
            Next
        End Sub
        Private Sub FormChanged(sender As Object, e As EventArgs)
            If changing OrElse formsBox.SelectedIndex < 0 Then Return
            SaveCurrent() : activeIndex = formsBox.SelectedIndex : activeClassIndex = -1 : activeModuleIndex = -1 : activeUserControlIndex = -1 : RefreshProject()
        End Sub

        Private Sub CodeTextChanged(sender As Object, e As EventArgs)
            If changing OrElse project Is Nothing Then Return
            MarkCurrentDocumentDirty()
            If activeClassIndex >= 0 AndAlso project.Classes IsNot Nothing AndAlso activeClassIndex < project.Classes.Count Then
                project.Classes(activeClassIndex).Code = code.Text
            ElseIf activeModuleIndex >= 0 AndAlso project.Modules IsNot Nothing AndAlso activeModuleIndex < project.Modules.Count Then
                project.Modules(activeModuleIndex).Code = code.Text
            ElseIf activeUserControlIndex >= 0 AndAlso project.UserControls IsNot Nothing AndAlso activeUserControlIndex < project.UserControls.Count Then
                project.UserControls(activeUserControlIndex).Code = code.Text
                If propertyTabs.SelectedIndex = 1 Then eventInspector.UpdateCode(project.UserControls(activeUserControlIndex).Code)
            ElseIf project.Forms.Count > 0 Then
                CurrentDocument.Code = code.Text
                If propertyTabs.SelectedIndex = 1 Then eventInspector.UpdateCode(CurrentDocument.Code)
            End If
        End Sub

        Private Sub SaveCurrent()
            If project Is Nothing Then Return
            If activeClassIndex >= 0 AndAlso project.Classes IsNot Nothing AndAlso activeClassIndex < project.Classes.Count Then
                project.Classes(activeClassIndex).Code = code.Text
                Return
            End If
            If activeModuleIndex >= 0 AndAlso project.Modules IsNot Nothing AndAlso activeModuleIndex < project.Modules.Count Then
                project.Modules(activeModuleIndex).Code = code.Text
                Return
            End If
            If activeUserControlIndex >= 0 AndAlso project.UserControls IsNot Nothing AndAlso activeUserControlIndex < project.UserControls.Count Then
                designer.Snapshot()
                project.UserControls(activeUserControlIndex).Code = code.Text
                Return
            End If
            If project.Forms.Count = 0 Then Return
            CurrentDocument.Code = code.Text : designer.Snapshot()
        End Sub

        Private Sub AddForm()
            SaveCurrent()
            Dim i As Integer = 1, name As String
            Do : name = "Form" & i : i += 1 : Loop While NameExists(name)
            Dim item As FormData = FormData.CreateDefault(name, False)
            item.FolderPath = SelectedFolderPath()
            project.Forms.Add(item) : activeIndex = project.Forms.Count - 1 : activeClassIndex = -1 : activeModuleIndex = -1 : activeUserControlIndex = -1 : RefreshProject()
        End Sub

        Private Sub AddClass()
            SaveCurrent()
            If project.Classes Is Nothing Then project.Classes = New List(Of ClassData)()
            Dim i As Integer = 1
            Dim suggested As String
            Do
                suggested = "Class" & i.ToString()
                i += 1
            Loop While NameExists(suggested)
            Dim className As String = Interaction.InputBox("Nome da nova classe:", "Adicionar Classe", suggested).Trim()
            If className = "" Then Return
            If Not Regex.IsMatch(className, "^[A-Za-z_][A-Za-z0-9_]*$") Then
                MessageBox.Show("Use um nome VB.NET válido, começando por letra ou _." , "Adicionar Classe", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            If NameExists(className) Then
                MessageBox.Show("Já existe um Form ou uma classe com esse nome.", "Adicionar Classe", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            Dim item As ClassData = ClassData.CreateDefault(className)
            item.FolderPath = SelectedFolderPath()
            project.Classes.Add(item)
            RefreshTree()
            OpenClassDocument(project.Classes.Count - 1)
        End Sub

        Private Sub AddModule()
            SaveCurrent()
            If project.Modules Is Nothing Then project.Modules = New List(Of ModuleData)()
            Dim i As Integer = 1, suggested As String
            Do : suggested = "Module" & i.ToString() : i += 1 : Loop While NameExists(suggested)
            Dim itemName As String = AskValidItemName("Nome do novo módulo:", "Adicionar Módulo", suggested)
            If itemName = "" Then Return
            Dim item As ModuleData = ModuleData.CreateDefault(itemName)
            item.FolderPath = SelectedFolderPath()
            project.Modules.Add(item) : RefreshTree() : OpenModuleDocument(project.Modules.Count - 1)
        End Sub

        Private Sub AddUserControl()
            SaveCurrent()
            If project.UserControls Is Nothing Then project.UserControls = New List(Of UserControlData)()
            Dim i As Integer = 1, suggested As String
            Do : suggested = "UserControl" & i.ToString() : i += 1 : Loop While NameExists(suggested)
            Dim itemName As String = AskValidItemName("Nome do novo UserControl:", "Adicionar UserControl", suggested)
            If itemName = "" Then Return
            Dim item As UserControlData = UserControlData.CreateDefault(itemName)
            item.FolderPath = SelectedFolderPath()
            project.UserControls.Add(item) : RefreshTree() : LoadToolbox() : OpenUserControlDesigner(project.UserControls.Count - 1)
        End Sub

        Private Function AskValidItemName(prompt As String, title As String, suggested As String) As String
            Dim value As String = Interaction.InputBox(prompt, title, suggested).Trim()
            If value = "" Then Return ""
            If Not Regex.IsMatch(value, "^[A-Za-z_][A-Za-z0-9_]*$") Then MessageBox.Show("Use um nome VB.NET válido, começando por letra ou _.", title, MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return ""
            If NameExists(value) Then MessageBox.Show("Já existe um item com esse nome no projeto.", title, MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return ""
            Return value
        End Function

        Private Sub AddProjectFolder()
            If project.Folders Is Nothing Then project.Folders = New List(Of ProjectFolderData)()
            Dim basePath As String = SelectedFolderPath()
            Dim name As String = Interaction.InputBox("Nome da nova pasta:", "Nova Pasta", "Pasta1").Trim()
            If name = "" Then Return
            If name.Contains("/") OrElse name.Contains("\") Then MessageBox.Show("O nome da pasta não pode conter / ou \.") : Return
            Dim fullPath As String = If(String.IsNullOrWhiteSpace(basePath), name, basePath & "/" & name)
            If project.Folders.Any(Function(f) f.Path.Equals(fullPath, StringComparison.OrdinalIgnoreCase)) Then MessageBox.Show("Essa pasta já existe.") : Return
            project.Folders.Add(New ProjectFolderData With {.Path = fullPath}) : RefreshTree()
        End Sub

        Private Function SelectedFolderPath() As String
            Dim info As ProjectNodeInfo = If(projectTree.SelectedNode Is Nothing, Nothing, TryCast(projectTree.SelectedNode.Tag, ProjectNodeInfo))
            If info IsNot Nothing AndAlso info.Kind = "Folder" Then Return info.PathValue
            Return ""
        End Function

        Private Function NameExists(value As String) As Boolean
            If project.Forms.Any(Function(f) f.Name.Equals(value, StringComparison.OrdinalIgnoreCase)) Then Return True
            If project.Classes IsNot Nothing AndAlso project.Classes.Any(Function(c) c.Name.Equals(value, StringComparison.OrdinalIgnoreCase)) Then Return True
            If project.Modules IsNot Nothing AndAlso project.Modules.Any(Function(m) m.Name.Equals(value, StringComparison.OrdinalIgnoreCase)) Then Return True
            Return project.UserControls IsNot Nothing AndAlso project.UserControls.Any(Function(u) u.Name.Equals(value, StringComparison.OrdinalIgnoreCase))
        End Function
        Private Sub DuplicateForm()
            SaveCurrent() : Dim copy As FormData = CloneForm(CurrentDocument) : copy.Name &= "Copia" : copy.IsStartup = False : copy.Code = Regex.Replace(copy.Code, "(?i)(Partial\s+Public\s+Class\s+)\w+", "$1" & copy.Name) : project.Forms.Add(copy) : activeIndex = project.Forms.Count - 1 : activeClassIndex = -1 : activeModuleIndex = -1 : activeUserControlIndex = -1 : RefreshProject()
        End Sub
        Private Sub DeleteForm()
            If project.Forms.Count <= 1 Then MessageBox.Show("O projeto precisa ter pelo menos um Form.") : Return
            Dim deleted As FormData = CurrentDocument
            Dim wasStartup = deleted.IsStartup : project.Forms.RemoveAt(activeIndex) : CloseDocumentTabsFor(deleted) : activeIndex = Math.Max(0, activeIndex - 1) : activeClassIndex = -1 : If wasStartup Then project.Forms(0).IsStartup = True
            RefreshProject()
        End Sub
        Private Sub SetStartup()
            For Each f In project.Forms : f.IsStartup = False : Next : CurrentDocument.IsStartup = True : RefreshTree() : status.Text = CurrentDocument.Name & " é o Form inicial"
        End Sub
        Private Sub ProjectNodeDoubleClick(sender As Object, e As TreeNodeMouseClickEventArgs)
            Dim info As ProjectNodeInfo = TryCast(e.Node.Tag, ProjectNodeInfo)
            If info Is Nothing Then Return
            Select Case info.Kind
                Case "FormDesign" : OpenFormDesigner(info.Index)
                Case "FormCode" : OpenFormCode(info.Index)
                Case "Class" : OpenClassDocument(info.Index)
                Case "Module" : OpenModuleDocument(info.Index)
                Case "UserControl" : OpenUserControlDesigner(info.Index)
            End Select
        End Sub

        Private Sub OpenFormDesigner(index As Integer)
            If index < 0 OrElse index >= project.Forms.Count Then Return
            SaveCurrent() : activeIndex = index : activeClassIndex = -1 : activeModuleIndex = -1 : activeUserControlIndex = -1
            changing = True : formsBox.SelectedIndex = activeIndex : designer.LoadDocument(CurrentDocument) : code.Text = CurrentDocument.Code : code.Colorize() : changing = False
            RefreshTree() : UpdateAutocomplete() : UpdateWindowTitle()
            tabs.SelectedIndex = 0
            UpdateFormViewBar(True, True)
            OpenDocumentTab("FormDesign", CurrentDocument, CurrentDocument.Name & " [Design]")
            status.Text = CurrentDocument.Name & " — Designer" : formStatus.Text = CurrentDocument.Name
            designer.Focus()
        End Sub

        Private Sub OpenFormCode(index As Integer)
            If index < 0 OrElse index >= project.Forms.Count Then Return
            SaveCurrent() : activeIndex = index : activeClassIndex = -1 : activeModuleIndex = -1 : activeUserControlIndex = -1
            changing = True : formsBox.SelectedIndex = activeIndex : designer.LoadDocument(CurrentDocument) : code.Text = CurrentDocument.Code : code.Colorize() : changing = False
            RefreshTree() : UpdateAutocomplete() : UpdateWindowTitle()
            tabs.SelectedIndex = 1
            UpdateFormViewBar(True, False)
            OpenDocumentTab("FormCode", CurrentDocument, CurrentDocument.Name & ".vb")
            status.Text = CurrentDocument.Name & ".vb" : formStatus.Text = CurrentDocument.Name
            code.Focus()
        End Sub

        Private Sub OpenClassDocument(index As Integer)
            If project.Classes Is Nothing OrElse index < 0 OrElse index >= project.Classes.Count Then Return
            SaveCurrent() : activeClassIndex = index : activeModuleIndex = -1 : activeUserControlIndex = -1
            changing = True : code.Text = project.Classes(index).Code : code.Colorize() : changing = False
            properties.SelectedObject = Nothing : selectionStatus.Text = "Classe"
            RefreshTree() : UpdateAutocomplete() : UpdateWindowTitle()
            tabs.SelectedIndex = 1
            UpdateFormViewBar(False, False)
            OpenDocumentTab("Class", project.Classes(index), project.Classes(index).Name & ".vb")
            status.Text = project.Classes(index).Name & ".vb" : formStatus.Text = project.Classes(index).Name
            code.Focus()
        End Sub

        Private Sub OpenModuleDocument(index As Integer)
            If project.Modules Is Nothing OrElse index < 0 OrElse index >= project.Modules.Count Then Return
            SaveCurrent() : activeModuleIndex = index : activeClassIndex = -1 : activeUserControlIndex = -1
            changing = True : code.Text = project.Modules(index).Code : code.Colorize() : changing = False
            properties.SelectedObject = Nothing : selectionStatus.Text = "Módulo"
            RefreshTree() : UpdateAutocomplete() : UpdateWindowTitle() : tabs.SelectedIndex = 1
            UpdateFormViewBar(False, False)
            OpenDocumentTab("Module", project.Modules(index), project.Modules(index).Name & ".vb")
            status.Text = project.Modules(index).Name & ".vb" : formStatus.Text = project.Modules(index).Name : code.Focus()
        End Sub

        Private Sub OpenUserControlDesigner(index As Integer)
            If project.UserControls Is Nothing OrElse index < 0 OrElse index >= project.UserControls.Count Then Return
            SaveCurrent() : activeUserControlIndex = index : activeClassIndex = -1 : activeModuleIndex = -1
            changing = True : designer.LoadUserControl(project.UserControls(index)) : code.Text = project.UserControls(index).Code : code.Colorize() : changing = False
            RefreshTree() : UpdateAutocomplete() : UpdateWindowTitle()
            tabs.SelectedIndex = 0
            UpdateFormViewBar(True, True, project.UserControls(index).Name)
            OpenDocumentTab("UserControlDesign", project.UserControls(index), project.UserControls(index).Name & " [Design]")
            status.Text = project.UserControls(index).Name & " — Designer" : formStatus.Text = project.UserControls(index).Name
            designer.SelectForm() : designer.Focus()
        End Sub

        Private Sub OpenUserControlDocument(index As Integer)
            If project.UserControls Is Nothing OrElse index < 0 OrElse index >= project.UserControls.Count Then Return
            SaveCurrent() : activeUserControlIndex = index : activeClassIndex = -1 : activeModuleIndex = -1
            changing = True : designer.LoadUserControl(project.UserControls(index)) : code.Text = project.UserControls(index).Code : code.Colorize() : changing = False
            RefreshTree() : UpdateAutocomplete() : UpdateWindowTitle() : tabs.SelectedIndex = 1
            UpdateFormViewBar(True, False, project.UserControls(index).Name)
            OpenDocumentTab("UserControlCode", project.UserControls(index), project.UserControls(index).Name & ".vb")
            status.Text = project.UserControls(index).Name & ".vb" : formStatus.Text = project.UserControls(index).Name : code.Focus()
        End Sub

        Private Sub OpenDocumentTab(kind As String, document As Object, title As String)
            Dim page As TabPage = Nothing
            For Each candidate As TabPage In documentTabs.TabPages
                Dim info As DocumentTabInfo = TryCast(candidate.Tag, DocumentTabInfo)
                If info IsNot Nothing AndAlso info.Kind = kind AndAlso Object.ReferenceEquals(info.Document, document) Then page = candidate : Exit For
            Next
            If page Is Nothing Then
                page = New TabPage(title) With {.Tag = New DocumentTabInfo(kind, document, title)}
                documentTabs.TabPages.Add(page)
            Else
                Dim existingInfo As DocumentTabInfo = TryCast(page.Tag, DocumentTabInfo)
                If existingInfo IsNot Nothing Then
                    existingInfo.BaseTitle = title
                    page.Text = title & If(existingInfo.Dirty, " *", "")
                Else
                    page.Text = title
                End If
            End If
            changingDocumentTabs = True : documentTabs.SelectedTab = page : changingDocumentTabs = False
        End Sub

        Private Sub DocumentTabChanged(sender As Object, e As EventArgs)
            If changingDocumentTabs OrElse documentTabs.SelectedTab Is Nothing Then Return
            Dim info As DocumentTabInfo = TryCast(documentTabs.SelectedTab.Tag, DocumentTabInfo)
            If info Is Nothing Then Return
            If TypeOf info.Document Is FormData Then
                Dim index As Integer = project.Forms.IndexOf(DirectCast(info.Document, FormData))
                If index < 0 Then Return
                If info.Kind = "FormDesign" Then OpenFormDesigner(index) Else OpenFormCode(index)
            ElseIf TypeOf info.Document Is ClassData Then
                Dim index As Integer = project.Classes.IndexOf(DirectCast(info.Document, ClassData))
                If index >= 0 Then OpenClassDocument(index)
            ElseIf TypeOf info.Document Is ModuleData Then
                Dim index As Integer = project.Modules.IndexOf(DirectCast(info.Document, ModuleData))
                If index >= 0 Then OpenModuleDocument(index)
            ElseIf TypeOf info.Document Is UserControlData Then
                Dim index As Integer = project.UserControls.IndexOf(DirectCast(info.Document, UserControlData))
                If index >= 0 Then
                    If info.Kind = "UserControlDesign" Then OpenUserControlDesigner(index) Else OpenUserControlDocument(index)
                End If
            End If
        End Sub

        Private Sub DocumentTabsDrawItem(sender As Object, e As DrawItemEventArgs)
            Dim rect As Rectangle = documentTabs.GetTabRect(e.Index)
            Dim selected As Boolean = (e.Index = documentTabs.SelectedIndex)
            Using back As New SolidBrush(If(selected, Color.FromArgb(55, 59, 68), Color.FromArgb(38, 41, 48)))
                e.Graphics.FillRectangle(back, rect)
            End Using
            Dim closeRect As New Rectangle(rect.Right - 18, rect.Top + 7, 11, 11)
            Dim textRect As New Rectangle(rect.Left + 7, rect.Top + 3, Math.Max(10, rect.Width - 29), rect.Height - 5)
            TextRenderer.DrawText(e.Graphics, documentTabs.TabPages(e.Index).Text, documentTabs.Font, textRect, Color.WhiteSmoke, TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)
            Using pen As New Pen(Color.Gainsboro, 1.5F)
                e.Graphics.DrawLine(pen, closeRect.Left + 1, closeRect.Top + 1, closeRect.Right - 1, closeRect.Bottom - 1)
                e.Graphics.DrawLine(pen, closeRect.Right - 1, closeRect.Top + 1, closeRect.Left + 1, closeRect.Bottom - 1)
            End Using
        End Sub

        Private Function DocumentCloseRect(index As Integer) As Rectangle
            Dim rect As Rectangle = documentTabs.GetTabRect(index)
            Return New Rectangle(rect.Right - 22, rect.Top + 3, 20, Math.Max(18, rect.Height - 6))
        End Function

        Private Sub DocumentTabsMouseDown(sender As Object, e As MouseEventArgs)
            For i As Integer = 0 To documentTabs.TabPages.Count - 1
                If Not documentTabs.GetTabRect(i).Contains(e.Location) Then Continue For

                If e.Button = MouseButtons.Middle OrElse (e.Button = MouseButtons.Left AndAlso DocumentCloseRect(i).Contains(e.Location)) Then
                    Dim closing As TabPage = documentTabs.TabPages(i)
                    documentTabs.TabPages.Remove(closing)
                    closing.Dispose()
                    Return
                End If

                If e.Button = MouseButtons.Right Then
                    documentTabs.SelectedIndex = i
                    Dim menu As New ContextMenuStrip()
                    Dim capturedIndex As Integer = i
                    menu.Items.Add("Fechar", Nothing, Sub() CloseDocumentTabAt(capturedIndex))
                    menu.Items.Add("Fechar outras", Nothing, Sub() CloseOtherDocumentTabs(capturedIndex))
                    menu.Items.Add("Fechar todas", Nothing, Sub() CloseAllDocumentTabs())
                    menu.Show(documentTabs, e.Location)
                    Return
                End If
                Return
            Next
        End Sub

        Private Sub CloseDocumentTabAt(index As Integer)
            If index < 0 OrElse index >= documentTabs.TabPages.Count Then Return
            Dim page As TabPage = documentTabs.TabPages(index)
            documentTabs.TabPages.RemoveAt(index)
            page.Dispose()
        End Sub

        Private Sub CloseOtherDocumentTabs(keepIndex As Integer)
            If keepIndex < 0 OrElse keepIndex >= documentTabs.TabPages.Count Then Return
            Dim keepPage As TabPage = documentTabs.TabPages(keepIndex)
            For i As Integer = documentTabs.TabPages.Count - 1 To 0 Step -1
                If Object.ReferenceEquals(documentTabs.TabPages(i), keepPage) Then Continue For
                Dim page As TabPage = documentTabs.TabPages(i)
                documentTabs.TabPages.RemoveAt(i)
                page.Dispose()
            Next
            changingDocumentTabs = True
            documentTabs.SelectedTab = keepPage
            changingDocumentTabs = False
        End Sub

        Private Sub CloseAllDocumentTabs()
            For i As Integer = documentTabs.TabPages.Count - 1 To 0 Step -1
                Dim page As TabPage = documentTabs.TabPages(i)
                documentTabs.TabPages.RemoveAt(i)
                page.Dispose()
            Next
        End Sub

        Private Sub CloseDocumentTabsFor(document As Object)
            For i As Integer = documentTabs.TabPages.Count - 1 To 0 Step -1
                Dim info As DocumentTabInfo = TryCast(documentTabs.TabPages(i).Tag, DocumentTabInfo)
                If info IsNot Nothing AndAlso Object.ReferenceEquals(info.Document, document) Then documentTabs.TabPages.RemoveAt(i)
            Next
        End Sub

        Private NotInheritable Class ProjectNodeInfo
            Public ReadOnly Kind As String
            Public ReadOnly Index As Integer
            Public ReadOnly PathValue As String
            Public Sub New(nodeKind As String, nodeIndex As Integer, Optional nodePath As String = "")
                Kind = nodeKind : Index = nodeIndex : PathValue = nodePath
            End Sub
        End Class

        Private Sub ProjectTreeKeyDown(sender As Object, e As KeyEventArgs)
            If projectTree.SelectedNode Is Nothing Then Return
            Dim info As ProjectNodeInfo = TryCast(projectTree.SelectedNode.Tag, ProjectNodeInfo)
            If info Is Nothing Then Return
            If e.KeyCode = Keys.F2 Then
                If info.Kind = "Folder" Then
                    RenameProjectFolder(info.PathValue)
                ElseIf info.Kind = "FormDesign" OrElse info.Kind = "FormCode" OrElse info.Kind = "Class" OrElse info.Kind = "Module" OrElse info.Kind = "UserControl" Then
                    RenameProjectItem(info)
                End If
                e.Handled = True
            ElseIf e.KeyCode = Keys.Delete Then
                If info.Kind = "Folder" Then
                    DeleteProjectFolder(info.PathValue)
                ElseIf info.Kind = "FormDesign" OrElse info.Kind = "FormCode" OrElse info.Kind = "Class" OrElse info.Kind = "Module" OrElse info.Kind = "UserControl" Then
                    DeleteProjectItem(info)
                End If
                e.Handled = True
            End If
        End Sub

        Private Sub ProjectNodeMouseClick(sender As Object, e As TreeNodeMouseClickEventArgs)
            If e.Button <> MouseButtons.Right Then Return
            projectTree.SelectedNode = e.Node
            Dim info As ProjectNodeInfo = TryCast(e.Node.Tag, ProjectNodeInfo)
            If info Is Nothing Then Return
            Dim menu As New ContextMenuStrip()
            If info.Kind = "Project" OrElse info.Kind = "Folder" OrElse info.Kind = "FoldersRoot" Then
                menu.Items.Add("Nova pasta...", Nothing, Sub() AddProjectFolder())
                menu.Items.Add(New ToolStripSeparator())
                menu.Items.Add("Adicionar Form", Nothing, Sub() AddForm())
                menu.Items.Add("Adicionar Classe...", Nothing, Sub() AddClass())
                menu.Items.Add("Adicionar Módulo...", Nothing, Sub() AddModule())
                menu.Items.Add("Adicionar UserControl...", Nothing, Sub() AddUserControl())
                If info.Kind = "Folder" Then
                    menu.Items.Add(New ToolStripSeparator())
                    menu.Items.Add("Renomear pasta...", Nothing, Sub() RenameProjectFolder(info.PathValue))
                    menu.Items.Add("Excluir pasta...", Nothing, Sub() DeleteProjectFolder(info.PathValue))
                End If
            ElseIf info.Kind = "FormDesign" OrElse info.Kind = "FormCode" OrElse info.Kind = "Class" OrElse info.Kind = "Module" OrElse info.Kind = "UserControl" Then
                If info.Kind = "FormDesign" OrElse info.Kind = "FormCode" Then
                    menu.Items.Add("Exibir Designer", Nothing, Sub() OpenFormDesigner(info.Index))
                    menu.Items.Add("Exibir Código", Nothing, Sub() OpenFormCode(info.Index))
                    menu.Items.Add(New ToolStripSeparator())
                ElseIf info.Kind = "UserControl" Then
                    menu.Items.Add("Exibir Designer", Nothing, Sub() OpenUserControlDesigner(info.Index))
                    menu.Items.Add("Exibir Código", Nothing, Sub() OpenUserControlDocument(info.Index))
                    menu.Items.Add(New ToolStripSeparator())
                Else
                    menu.Items.Add("Abrir", Nothing, Sub() OpenProjectItem(info))
                End If
                menu.Items.Add("Renomear...", Nothing, Sub() RenameProjectItem(info))
                menu.Items.Add(CreateMoveToFolderMenu(info))
                menu.Items.Add(New ToolStripSeparator())
                menu.Items.Add("Excluir", Nothing, Sub() DeleteProjectItem(info))
            End If
            If menu.Items.Count > 0 Then menu.Show(projectTree, e.Location)
        End Sub

        Private Sub OpenProjectItem(info As ProjectNodeInfo)
            Select Case info.Kind
                Case "FormDesign" : OpenFormDesigner(info.Index)
                Case "FormCode" : OpenFormCode(info.Index)
                Case "Class" : OpenClassDocument(info.Index)
                Case "Module" : OpenModuleDocument(info.Index)
                Case "UserControl" : OpenUserControlDocument(info.Index)
            End Select
        End Sub

        Private Function CreateMoveToFolderMenu(info As ProjectNodeInfo) As ToolStripMenuItem
            Dim rootItem As New ToolStripMenuItem("Mover para pasta")
            rootItem.DropDownItems.Add("(Raiz)", Nothing, Sub() MoveProjectItem(info, ""))
            If project.Folders IsNot Nothing Then
                For Each folder As ProjectFolderData In project.Folders.OrderBy(Function(x) x.Path)
                    Dim path As String = folder.Path
                    rootItem.DropDownItems.Add(path, Nothing, Sub() MoveProjectItem(info, path))
                Next
            End If
            Return rootItem
        End Function

        Private Sub MoveProjectItem(info As ProjectNodeInfo, folderPath As String)
            Select Case info.Kind
                Case "FormDesign", "FormCode" : project.Forms(info.Index).FolderPath = folderPath
                Case "Class" : project.Classes(info.Index).FolderPath = folderPath
                Case "Module" : project.Modules(info.Index).FolderPath = folderPath
                Case "UserControl" : project.UserControls(info.Index).FolderPath = folderPath
            End Select
            RefreshTree()
        End Sub

        Private Sub RenameProjectItem(info As ProjectNodeInfo)
            SaveCurrent()
            Dim oldName As String = ""
            Select Case info.Kind
                Case "FormDesign", "FormCode" : oldName = project.Forms(info.Index).Name
                Case "Class" : oldName = project.Classes(info.Index).Name
                Case "Module" : oldName = project.Modules(info.Index).Name
                Case "UserControl" : oldName = project.UserControls(info.Index).Name
            End Select
            If oldName = "" Then Return
            Dim newName As String = Interaction.InputBox("Novo nome do arquivo (sem .vb):", "Renomear", oldName).Trim()
            If newName = "" OrElse newName.Equals(oldName, StringComparison.OrdinalIgnoreCase) Then Return
            If Not Regex.IsMatch(newName, "^[A-Za-z_][A-Za-z0-9_]*$") Then MessageBox.Show("Nome VB.NET inválido.") : Return
            If NameExists(newName) Then MessageBox.Show("Já existe um item com esse nome.") : Return
            Select Case info.Kind
                Case "FormDesign", "FormCode"
                    Dim item As FormData = project.Forms(info.Index) : item.Name = newName : item.Code = Regex.Replace(item.Code, "(?i)(Partial\s+Public\s+Class\s+)" & Regex.Escape(oldName) & "\b", "$1" & newName)
                    CloseDocumentTabsFor(item)
                Case "Class"
                    Dim item As ClassData = project.Classes(info.Index) : item.Name = newName : item.Code = Regex.Replace(item.Code, "(?i)(Public\s+Class\s+)" & Regex.Escape(oldName) & "\b", "$1" & newName) : CloseDocumentTabsFor(item)
                Case "Module"
                    Dim item As ModuleData = project.Modules(info.Index) : item.Name = newName : item.Code = Regex.Replace(item.Code, "(?i)(Public\s+Module\s+)" & Regex.Escape(oldName) & "\b", "$1" & newName) : CloseDocumentTabsFor(item)
                Case "UserControl"
                    Dim item As UserControlData = project.UserControls(info.Index) : item.Name = newName : item.Code = Regex.Replace(item.Code, "(?i)(Public\s+Class\s+)" & Regex.Escape(oldName) & "\b", "$1" & newName) : CloseDocumentTabsFor(item)
            End Select
            RefreshTree() : UpdateAutocomplete() : LoadToolbox()
        End Sub

        Private Sub DeleteProjectItem(info As ProjectNodeInfo)
            Dim label As String = If(projectTree.SelectedNode Is Nothing, "item", projectTree.SelectedNode.Text)
            If MessageBox.Show("Excluir '" & label & "' do projeto?", "Excluir", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then Return
            Select Case info.Kind
                Case "FormDesign", "FormCode"
                    If project.Forms.Count <= 1 Then MessageBox.Show("O projeto precisa ter pelo menos um Form.") : Return
                    Dim item As FormData = project.Forms(info.Index) : Dim wasStartup As Boolean = item.IsStartup : CloseDocumentTabsFor(item) : project.Forms.RemoveAt(info.Index) : If wasStartup Then project.Forms(0).IsStartup = True
                    activeIndex = Math.Max(0, Math.Min(activeIndex, project.Forms.Count - 1)) : activeClassIndex = -1 : activeModuleIndex = -1 : activeUserControlIndex = -1 : RefreshProject() : Return
                Case "Class"
                    Dim item As ClassData = project.Classes(info.Index) : CloseDocumentTabsFor(item) : project.Classes.RemoveAt(info.Index) : activeClassIndex = -1
                Case "Module"
                    Dim item As ModuleData = project.Modules(info.Index) : CloseDocumentTabsFor(item) : project.Modules.RemoveAt(info.Index) : activeModuleIndex = -1
                Case "UserControl"
                    Dim item As UserControlData = project.UserControls(info.Index) : CloseDocumentTabsFor(item) : project.UserControls.RemoveAt(info.Index) : activeUserControlIndex = -1
            End Select
            RefreshTree() : UpdateAutocomplete() : LoadToolbox()
        End Sub

        Private Sub RenameProjectFolder(oldPath As String)
            Dim oldName As String = oldPath.Substring(oldPath.LastIndexOf("/"c) + 1)
            Dim parent As String = If(oldPath.Contains("/"c), oldPath.Substring(0, oldPath.LastIndexOf("/"c)), "")
            Dim newName As String = Interaction.InputBox("Novo nome da pasta:", "Renomear Pasta", oldName).Trim()
            If newName = "" OrElse newName = oldName Then Return
            If newName.Contains("/") OrElse newName.Contains("\") Then MessageBox.Show("Nome de pasta inválido.") : Return
            Dim newPath As String = If(parent = "", newName, parent & "/" & newName)
            If project.Folders.Any(Function(f) f.Path.Equals(newPath, StringComparison.OrdinalIgnoreCase)) Then MessageBox.Show("Já existe uma pasta com esse nome.") : Return
            For Each folder As ProjectFolderData In project.Folders
                If folder.Path.Equals(oldPath, StringComparison.OrdinalIgnoreCase) OrElse folder.Path.StartsWith(oldPath & "/", StringComparison.OrdinalIgnoreCase) Then folder.Path = newPath & folder.Path.Substring(oldPath.Length)
            Next
            ReplaceFolderPathOnItems(oldPath, newPath) : RefreshTree()
        End Sub

        Private Sub ReplaceFolderPathOnItems(oldPath As String, newPath As String)
            For Each f As FormData In project.Forms : If f.FolderPath.Equals(oldPath, StringComparison.OrdinalIgnoreCase) OrElse f.FolderPath.StartsWith(oldPath & "/", StringComparison.OrdinalIgnoreCase) Then f.FolderPath = newPath & f.FolderPath.Substring(oldPath.Length)
            Next
            For Each c As ClassData In project.Classes : If c.FolderPath.Equals(oldPath, StringComparison.OrdinalIgnoreCase) OrElse c.FolderPath.StartsWith(oldPath & "/", StringComparison.OrdinalIgnoreCase) Then c.FolderPath = newPath & c.FolderPath.Substring(oldPath.Length)
            Next
            For Each m As ModuleData In project.Modules : If m.FolderPath.Equals(oldPath, StringComparison.OrdinalIgnoreCase) OrElse m.FolderPath.StartsWith(oldPath & "/", StringComparison.OrdinalIgnoreCase) Then m.FolderPath = newPath & m.FolderPath.Substring(oldPath.Length)
            Next
            For Each u As UserControlData In project.UserControls : If u.FolderPath.Equals(oldPath, StringComparison.OrdinalIgnoreCase) OrElse u.FolderPath.StartsWith(oldPath & "/", StringComparison.OrdinalIgnoreCase) Then u.FolderPath = newPath & u.FolderPath.Substring(oldPath.Length)
            Next
        End Sub

        Private Sub DeleteProjectFolder(folderPath As String)
            If MessageBox.Show("Excluir a pasta '" & folderPath & "' e todos os itens contidos nela?", "Excluir Pasta", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then Return
            Dim prefix As String = folderPath & "/"
            project.Forms.RemoveAll(Function(f) f.FolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase) OrElse f.FolderPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            If project.Forms.Count = 0 Then
                project.Forms.Add(FormData.CreateDefault("Form1", True))
            ElseIf Not project.Forms.Any(Function(f) f.IsStartup) Then
                project.Forms(0).IsStartup = True
            End If
            project.Classes.RemoveAll(Function(c) c.FolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase) OrElse c.FolderPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            project.Modules.RemoveAll(Function(m) m.FolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase) OrElse m.FolderPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            project.UserControls.RemoveAll(Function(u) u.FolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase) OrElse u.FolderPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            project.Folders.RemoveAll(Function(f) f.Path.Equals(folderPath, StringComparison.OrdinalIgnoreCase) OrElse f.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            documentTabs.TabPages.Clear() : activeIndex = 0 : activeClassIndex = -1 : activeModuleIndex = -1 : activeUserControlIndex = -1 : RefreshProject()
        End Sub

        Private NotInheritable Class DocumentTabInfo
            Public ReadOnly Kind As String
            Public ReadOnly Document As Object
            Public BaseTitle As String
            Public Dirty As Boolean
            Public Sub New(documentKind As String, documentObject As Object, title As String)
                Kind = documentKind
                Document = documentObject
                BaseTitle = title
                Dirty = False
            End Sub
        End Class

        Private Sub LoadToolbox()
            Dim entries = {
                Tool("Comuns", "Button", "Botão", "▣", "Click"), Tool("Comuns", "Label", "Label", "T", "Click"), Tool("Entrada", "TextBox", "TextBox", "ab", "TextChanged"), Tool("Entrada", "CheckBox", "CheckBox", "☑", "CheckedChanged"),
                Tool("Texto", "RichTextBox", "RichTextBox", "RT", "TextChanged"), Tool("Texto", "MaskedTextBox", "MaskedTextBox", "##", "TextChanged"), Tool("Comuns", "LinkLabel", "LinkLabel", "↗", "LinkClicked"), Tool("Seleção", "RadioButton", "RadioButton", "◉", "CheckedChanged"),
                Tool("Seleção", "ComboBox", "ComboBox", "⌄", "SelectedIndexChanged"), Tool("Seleção", "ListBox", "ListBox", "☷", "SelectedIndexChanged"), Tool("Mídia", "PictureBox", "PictureBox", "▧", "Click"), Tool("Entrada", "TrackBar", "TrackBar", "━", "ValueChanged"),
                Tool("Seleção", "ColorComboBox", "Color ComboBox", "🎨", "SelectedColorChanged"), Tool("Seleção", "CheckedListBox", "CheckedListBox", "☑", "ItemCheck"), Tool("Dados", "TreeView", "TreeView", "🌳", "AfterSelect"), Tool("Dados", "ListView", "ListView", "☷", "SelectedIndexChanged"),
                Tool("Dados", "ProgressBar", "ProgressBar", "▰", "ValueChanged"), Tool("Entrada", "DateTimePicker", "DateTimePicker", "▦", "ValueChanged"), Tool("Dados", "DataGridView", "DataGridView", "▦", "CellClick"), Tool("Contêiner", "Panel", "Panel", "□", "Click"), Tool("Internet", "WebBrowser", "WebBrowser", "◎", "DocumentCompleted"),
                Tool("Entrada", "NumericUpDown", "NumericUpDown", "±", "ValueChanged"), Tool("Entrada", "DomainUpDown", "DomainUpDown", "↕", "SelectedItemChanged"), Tool("Entrada", "MonthCalendar", "MonthCalendar", "▦", "DateChanged"), Tool("Entrada", "HScrollBar", "ScrollBar horizontal", "↔", "Scroll"), Tool("Entrada", "VScrollBar", "ScrollBar vertical", "↕", "Scroll"),
                Tool("Contêiner", "GroupBox", "GroupBox", "▢", "Enter"), Tool("Contêiner", "TabControl", "TabControl", "▤", "SelectedIndexChanged"), Tool("Contêiner", "FlowLayoutPanel", "FlowLayoutPanel", "⇥", "ControlAdded"), Tool("Contêiner", "TableLayoutPanel", "TableLayoutPanel", "▦", "ControlAdded"), Tool("Contêiner", "SplitContainer", "SplitContainer", "◫", "SplitterMoved"),
                Tool("Menus e barras", "MenuStrip", "MenuStrip", "M", "ItemClicked"), Tool("Menus e barras", "ToolStrip", "ToolStrip", "Tb", "ItemClicked"), Tool("Menus e barras", "StatusStrip", "StatusStrip", "St", "ItemClicked"),
                ComponentTool("Diálogos", "OpenFileDialog", "OpenFileDialog", "O", "FileOk"), ComponentTool("Diálogos", "SaveFileDialog", "SaveFileDialog", "S", "FileOk"), ComponentTool("Diálogos", "FolderBrowserDialog", "FolderBrowserDialog", "F", "HelpRequest"), ComponentTool("Diálogos", "ColorDialog", "ColorDialog", "C", "HelpRequest"), ComponentTool("Diálogos", "FontDialog", "FontDialog", "A", "Apply"), ComponentTool("Diálogos", "PrintDialog", "PrintDialog", "P", "HelpRequest"), ComponentTool("Diálogos", "PageSetupDialog", "PageSetupDialog", "Pg", "HelpRequest"), ComponentTool("Diálogos", "PrintPreviewDialog", "PrintPreviewDialog", "Pr", "Load")}
            entries = entries.Concat(New ToolboxEntry() {
                ComponentTool("Componentes", "ToolTip", "Dica / ToolTip", "TIP", "Popup"),
                ComponentTool("Componentes", "NotifyIcon", "Ícone da bandeja", "NI", "MouseClick"),
                ComponentTool("Componentes", "ErrorProvider", "Validação / ErrorProvider", "ERR", "RightToLeftChanged"),
                ComponentTool("Componentes", "ImageList", "Lista de imagens", "IMG", "RecreateHandle"),
                ComponentTool("Componentes", "BindingSource", "Fonte de dados / BindingSource", "DB", "CurrentChanged"),
                ComponentTool("Componentes", "HelpProvider", "Ajuda contextual", "?", "HelpRequested"),
                ComponentTool("Menus e barras", "ContextMenuStrip", "Menu de contexto", "CM", "ItemClicked"),
                Tool("Desenvolvimento", "PropertyGrid", "Inspetor de propriedades", "PG", "PropertyValueChanged"),
                Tool("Dados", "BindingNavigator", "Navegador de dados", "BN", "ItemClicked"),
                Tool("Contêiner", "Splitter", "Divisor / Splitter", "SPL", "SplitterMoved")
            }).ToArray()
            entries = entries.Concat(New ToolboxEntry() {Tool("FlowForge", "RoundedButton", "Botão arredondado", "RB", "Click"), Tool("FlowForge", "GradientPanel", "Painel gradiente", "GP", "Click"), Tool("FlowForge", "LedIndicator", "LED indicador", "LED", "StateChanged"), Tool("FlowForge", "ToggleSwitch", "Chave liga/desliga", "SW", "CheckedChanged"), Tool("FlowForge", "DigitalDisplay", "Display 7 segmentos / matriz", "88", "ValueChanged"), Tool("FlowForge", "CircularProgress", "Progresso circular", "%", "ValueChanged"), Tool("FlowForge", "LevelMeter", "Medidor de nível", "LV", "ValueChanged"), Tool("FlowForge", "BadgeLabel", "Etiqueta / Badge", "BG", "Click"), Tool("FlowForge", "SeparatorLine", "Linha separadora", "—", "Click"), Tool("FlowForge", "StarRating", "Avaliação por estrelas", "★", "RatingChanged"), Tool("FlowForge", "NumericKnob", "Botão giratório", "KN", "ValueChanged"), Tool("FlowForge", "CardPanel", "Painel cartão", "CD", "Click"), Tool("Instrumentos", "BatteryIndicator", "Indicador de bateria", "BAT", "ValueChanged"), Tool("Instrumentos", "SignalStrength", "Intensidade de sinal", "SIG", "ValueChanged"), Tool("Instrumentos", "ThermometerGauge", "Termômetro", "°C", "ValueChanged"), Tool("Instrumentos", "AnalogGauge", "Medidor analógico", "GA", "ValueChanged"), Tool("Interface", "LoadingSpinner", "Indicador de carregamento", "SP", "ActiveChanged"), Tool("Interface", "NotificationBanner", "Faixa de notificação", "!", "Click"), Tool("Interface", "ToggleButton", "Botão alternável", "TB", "CheckedChanged"), Tool("Interface", "ColorSwatch", "Seletor de cor", "CLR", "SelectedColorChanged"), Tool("Navegação", "NavigationButton", "Botão de navegação", "→", "Click"), Tool("Interface", "MarqueeLabel", "Texto em movimento", "TXT", "TextChanged"), Tool("Interface", "ImageButton", "Botão com imagem", "IMG", "Click"), Tool("Entrada", "SearchBox", "Caixa de pesquisa", "SRCH", "Search"), Tool("Entrada", "PasswordBox", "Campo de senha", "PWD", "TextChanged"), Tool("Entrada", "IPAddressBox", "Endereço IP", "IP", "TextChanged"), Tool("Entrada", "ModernDatePicker", "Seletor de data moderno", "DATE", "ValueChanged"), Tool("Gráficos", "SimpleChart", "Gráfico linha / barras", "CH", "DataChanged"), Tool("Controles", "VirtualJoystick", "Joystick virtual", "JOY", "PositionChanged"), Tool("Displays", "LcdDisplay", "Display LCD", "LCD", "ValueChanged"), Tool("Displays", "LedMatrix", "Matriz de LEDs", "8x8", "CellChanged"), Tool("Instrumentos", "TrafficLight", "Semáforo", "TL", "StateChanged"), Tool("Displays", "SevenSegmentDigit", "Dígito 7 segmentos", "7S", "ValueChanged"), Tool("Arduino / IoT", "ArduinoPin", "Pino Arduino / GPIO", "PIN", "ValueChanged"), Tool("Arduino / IoT", "IoTSensor", "Sensor IoT", "IOT", "ValueChanged"), ComponentTool("Lógica", "Timer", "Timer / Relógio", "⏱", "Tick"), ComponentTool("Comunicação", "SerialConnection", "Porta Serial / Arduino", "COM", "DataReceived"), ComponentTool("Imagens", "GlyphImageList", "Banco de 480 glyphs dinâmicos", "GLY", "RecreateHandle")}).ToArray()
            entries = entries.Concat(New ToolboxEntry() {Tool("Editores", "FontPreviewComboBox", "Seletor de fontes com pré-visualização", "Aa", "SelectedIndexChanged"), Tool("Editores", "RichTextEditor", "Editor de texto rico", "RTF", "ContentChanged"), Tool("Editores", "SyntaxCodeEditor", "Editor de código com linhas", "</>", "CodeChanged")}).ToArray()
            entries = entries.Concat(New ToolboxEntry() {Tool("Feedback", "ToastNotification", "Aviso temporário", "TST", "Dismissed"), Tool("Layout", "Accordion", "Painéis expansíveis", "ACC", "SectionToggled"), Tool("Feedback", "ProgressStepper", "Progresso em etapas", "STP", "StepChanged"), Tool("Editores", "TerminalView", "Console/terminal com histórico", ">_", "CommandEntered")}).ToArray()
            entries = entries.Concat(New ToolboxEntry() {Tool("Dados", "Sparkline", "Mini gráfico com título, legenda e indicadores", "SPK", "DataChanged")}).ToArray()
            entries = entries.Concat(New ToolboxEntry() {Tool("Entrada", "TagInput", "Campo de tags", "TAG", "TagsChanged"), Tool("Navegação", "TabStripCustom", "Abas estilizadas", "TAB", "SelectedIndexChanged"), Tool("Dados", "JsonTreeViewer", "Visualizador de JSON em árvore", "JSN", "JsonParsed")}).ToArray()
            If project IsNot Nothing AndAlso project.UserControls IsNot Nothing Then
                Dim projectControls As New List(Of ToolboxEntry)()
                For Each uc As UserControlData In project.UserControls
                    projectControls.Add(New ToolboxEntry With {.Category = "Projeto / UserControls", .TypeName = uc.Name, .DisplayName = uc.Name, .Glyph = "UC", .DefaultEvent = "Load", .AssemblyPath = "project://" & uc.Name})
                Next
                entries = entries.Concat(projectControls).ToArray()
            End If
            allTools.Clear() : allTools.AddRange(entries) : FilterToolbox(Nothing, EventArgs.Empty)
        End Sub
        Private Sub FilterToolbox(sender As Object, e As EventArgs)
            Dim term As String = toolboxSearch.Text.Trim()
            Dim filtered = allTools.Where(Function(x) term = "" OrElse x.DisplayName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 OrElse x.TypeName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 OrElse x.Category.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0).OrderBy(Function(x) x.Category).ThenBy(Function(x) x.DisplayName).Cast(Of Object).ToArray()
            toolbox.BeginUpdate() : toolbox.Items.Clear() : toolbox.Items.AddRange(filtered) : toolbox.EndUpdate()
            If toolbox.Items.Count > 0 Then toolbox.SelectedIndex = 0
            selectionStatus.Text = toolbox.Items.Count & " ferramenta(s)"
        End Sub
        Private Sub ToolboxSearchKeyDown(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Enter Then
                AddSelectedTool()
                e.SuppressKeyPress = True
            ElseIf e.KeyCode = Keys.Escape Then
                toolboxSearch.Clear()
                e.SuppressKeyPress = True
            End If
        End Sub
        Private Sub AddSelectedTool()
            Dim entry = TryCast(toolbox.SelectedItem, ToolboxEntry) : If entry IsNot Nothing Then designer.AddComponent(entry)
        End Sub
        Private Sub DrawToolbox(sender As Object, e As DrawItemEventArgs)
            If e.Index < 0 Then Return
            e.DrawBackground() : Dim entry = DirectCast(toolbox.Items(e.Index), ToolboxEntry), icon As New Rectangle(e.Bounds.X + 5, e.Bounds.Y + 5, 28, 28)
            Using brush As New SolidBrush(Color.FromArgb(55, 65, 80)) : e.Graphics.FillRectangle(brush, icon) : End Using
            TextRenderer.DrawText(e.Graphics, entry.Glyph, New Font("Segoe UI", 11), icon, Color.White, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
            TextRenderer.DrawText(e.Graphics, entry.DisplayName, Font, New Point(e.Bounds.X + 40, e.Bounds.Y + 5), Color.White)
            TextRenderer.DrawText(e.Graphics, entry.Category, New Font(Font.FontFamily, 7.5F), New Point(e.Bounds.X + 40, e.Bounds.Y + 21), Color.Silver)
        End Sub

        Private Sub EventInspectorActivated(sender As Object, eventName As String)
            Dim component As Object = designer.SelectedComponent
            If component Is Nothing Then Return
            OpenEvent(component, eventName)
            If activeUserControlIndex >= 0 Then
                eventInspector.UpdateCode(project.UserControls(activeUserControlIndex).Code)
            Else
                eventInspector.UpdateCode(CurrentDocument.Code)
            End If
        End Sub

        Private Sub PropertyTabChanged(sender As Object, e As EventArgs)
            If propertyTabs.SelectedIndex <> 1 Then Return
            Dim component As Object = designer.SelectedComponent
            If component Is Nothing Then
                eventInspector.ClearTarget()
                Return
            End If
            Dim designName As String = If(activeUserControlIndex >= 0, project.UserControls(activeUserControlIndex).Name, CurrentDocument.Name)
            Dim designCode As String = If(activeUserControlIndex >= 0, project.UserControls(activeUserControlIndex).Code, CurrentDocument.Code)
            Dim componentName As String = If(TypeOf component Is Form, designName, designer.SelectedComponentName)
            eventInspector.SetTarget(component, componentName, designCode)
        End Sub

        Private Sub OpenEvent(component As Object, eventName As String)
            SaveCurrent()
            Dim isUserControl As Boolean = activeUserControlIndex >= 0 AndAlso project.UserControls IsNot Nothing AndAlso activeUserControlIndex < project.UserControls.Count
            Dim documentName As String = If(isUserControl, project.UserControls(activeUserControlIndex).Name, CurrentDocument.Name)
            Dim sourceCode As String = If(isUserControl, project.UserControls(activeUserControlIndex).Code, CurrentDocument.Code)
            Dim componentName As String = If(TypeOf component Is Form, documentName, designer.SelectedComponentName)
            Dim method As String = componentName & "_" & eventName
            If Not Regex.IsMatch(sourceCode, "(?i)Sub\s+" & Regex.Escape(method) & "\s*\(") Then
                Dim target As String = If(TypeOf component Is Form, "MyBase", componentName)
                Dim body As String = "        ' Escreva seu código aqui" & vbCrLf
                ' SerialConnection é compilado, no app final, como o SerialPort puro do .NET,
                ' cujos eventos (DataReceived/ErrorReceived/PinChanged) disparam em uma thread
                ' de segundo plano própria. Sem essa proteção, qualquer código do aluno que
                ' tente atualizar um controle aqui dentro lançaria "cross-thread operation not
                ' valid". O guard abaixo reencaminha a execução pra thread de UI automaticamente,
                ' então o código que o aluno escrever a seguir já roda seguro.
                If TypeOf component Is SerialConnection Then
                    body = "        If InvokeRequired Then" & vbCrLf &
                           "            BeginInvoke(New System.Windows.Forms.MethodInvoker(AddressOf " & method & "))" & vbCrLf &
                           "            Return" & vbCrLf &
                           "        End If" & vbCrLf &
                           body
                End If
                Dim handler As String = "    Private Sub " & method & "() Handles " & target & "." & eventName & vbCrLf & body & "    End Sub" & vbCrLf & vbCrLf
                Dim at As Integer = sourceCode.LastIndexOf("End Class", StringComparison.OrdinalIgnoreCase)
                sourceCode = If(at >= 0, sourceCode.Insert(at, handler), sourceCode & vbCrLf & handler)
                If isUserControl Then project.UserControls(activeUserControlIndex).Code = sourceCode Else CurrentDocument.Code = sourceCode
            End If
            changing = True : code.Text = sourceCode : changing = False : tabs.SelectedIndex = 1
            If isUserControl Then
                UpdateFormViewBar(True, False, documentName)
                OpenDocumentTab("UserControlCode", project.UserControls(activeUserControlIndex), documentName & ".vb")
            Else
                UpdateFormViewBar(True, False, documentName)
                OpenDocumentTab("FormCode", CurrentDocument, documentName & ".vb")
            End If
            Dim methodPosition As Integer = code.Text.IndexOf(method, StringComparison.OrdinalIgnoreCase)
            If methodPosition >= 0 Then
                Dim bodyPosition As Integer = code.Text.IndexOf(vbLf, methodPosition)
                If bodyPosition >= 0 Then code.Select(Math.Min(bodyPosition + 1, code.TextLength), 0)
                code.ScrollToCaret() : code.Focus()
            End If
            eventInspector.UpdateCode(sourceCode)
        End Sub
        Private Shared Function DefaultEvent(c As Object) As String
            If TypeOf c Is Form Then Return "Load"
            If TypeOf c Is TextBox Then Return "TextChanged"
            If TypeOf c Is CheckBox Then Return "CheckedChanged"
            If TypeOf c Is ComboBox OrElse TypeOf c Is ListBox Then Return "SelectedIndexChanged"
            If TypeOf c Is TrackBar OrElse TypeOf c Is ProgressBar OrElse TypeOf c Is DateTimePicker Then Return "ValueChanged"
            If TypeOf c Is DataGridView Then Return "CellClick"
            If TypeOf c Is WebBrowser Then Return "DocumentCompleted"
            If TypeOf c Is OpenFileDialog OrElse TypeOf c Is SaveFileDialog Then Return "FileOk"
            If TypeOf c Is FontDialog Then Return "Apply"
            If TypeOf c Is CommonDialog Then Return "HelpRequest"
            Dim defaultDescriptor As System.ComponentModel.EventDescriptor = System.ComponentModel.TypeDescriptor.GetDefaultEvent(c)
            If defaultDescriptor IsNot Nothing Then Return defaultDescriptor.Name
            Dim firstEvent As System.ComponentModel.EventDescriptor = System.ComponentModel.TypeDescriptor.GetEvents(c).Cast(Of System.ComponentModel.EventDescriptor)().FirstOrDefault()
            Return If(firstEvent Is Nothing, "Disposed", firstEvent.Name)
        End Function
        Private Sub InsertSample()
            Dim sample As String
            Select Case CStr(sampleBox.SelectedItem)
                Case "MessageBox" : sample = "MessageBox.Show(""Concluído!"", ""Aviso"", MessageBoxButtons.OK, MessageBoxIcon.Information)"
                Case "Try / Catch" : sample = "Try" & vbCrLf & "    ' Código" & vbCrLf & "Catch ex As Exception" & vbCrLf & "    MessageBox.Show(ex.Message)" & vbCrLf & "End Try"
                Case "For Each" : sample = "Dim minhaLista As String() = {""Item 1"", ""Item 2""}" & vbCrLf & "For Each item As String In minhaLista" & vbCrLf & "    Debug.WriteLine(item)" & vbCrLf & "Next"
                Case "While" : sample = "Dim contador As Integer = 0" & vbCrLf & "While contador < 10" & vbCrLf & "    contador += 1" & vbCrLf & "End While"
                Case "Select Case" : sample = "Dim valor As Integer = 1" & vbCrLf & "Select Case valor" & vbCrLf & "    Case 1 : MessageBox.Show(""Um"")" & vbCrLf & "    Case Else : MessageBox.Show(""Outro"")" & vbCrLf & "End Select"
                Case "Abrir outro Form"
                    Dim target = project.Forms.FirstOrDefault(Function(f) Not f.Name.Equals(CurrentDocument.Name, StringComparison.OrdinalIgnoreCase))
                    If target Is Nothing Then
                        MessageBox.Show("Adicione outro Form ao projeto antes de inserir este exemplo.", "Abrir outro Form", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Return
                    End If
                    sample = "' Usa a instância padrão, como no Visual Basic:" & vbCrLf & target.Name & ".Show()" & vbCrLf & vbCrLf & "' Para abrir uma nova instância independente:" & vbCrLf & "' Dim janela As New " & target.Name & "()" & vbCrLf & "' janela.Show()"
                Case "Ler arquivo" : sample = "Dim caminho As String = ""C:\dados\arquivo.txt""" & vbCrLf & "If File.Exists(caminho) Then MessageBox.Show(File.ReadAllText(caminho))"
                Case "HTTP GET" : sample = "Using cliente As New HttpClient()" & vbCrLf & "    Dim resposta As String = cliente.GetStringAsync(""https://api.exemplo.com"").Result" & vbCrLf & "    MessageBox.Show(resposta)" & vbCrLf & "End Using"
                Case Else : sample = "Dim condicao As Boolean = True" & vbCrLf & "If condicao Then" & vbCrLf & "    MessageBox.Show(""Verdadeiro"")" & vbCrLf & "Else" & vbCrLf & "    MessageBox.Show(""Falso"")" & vbCrLf & "End If"
            End Select
            InsertCodeSample(sample)
        End Sub

        Private Sub InsertCodeSample(sample As String)
            Dim caret As Integer = code.SelectionStart
            Dim beforeCaret As String = code.Text.Substring(0, caret)
            Dim lastSub As Match = Regex.Match(beforeCaret, "(?im)^\s*(Public|Private|Protected|Friend)?\s*(Async\s+)?Sub\s+\w+\s*\([^\r\n]*\).*$", RegexOptions.RightToLeft)
            Dim lastEndSub As Match = Regex.Match(beforeCaret, "(?im)^\s*End\s+Sub\s*$", RegexOptions.RightToLeft)
            Dim insideMethod As Boolean = lastSub.Success AndAlso (Not lastEndSub.Success OrElse lastSub.Index > lastEndSub.Index)
            If insideMethod Then
                Dim indented As String = "        " & sample.Replace(vbCrLf, vbCrLf & "        ")
                code.SelectionLength = 0
                code.SelectedText = indented & vbCrLf
            Else
                Dim methodName As String = "Exemplo" & DateTime.Now.ToString("HHmmss")
                Dim block As String = "    Private Sub " & methodName & "()" & vbCrLf & "        " & sample.Replace(vbCrLf, vbCrLf & "        ") & vbCrLf & "    End Sub" & vbCrLf & vbCrLf
                Dim insertAt As Integer = code.Text.LastIndexOf("End Class", StringComparison.OrdinalIgnoreCase)
                If insertAt < 0 Then
                    MessageBox.Show("Não foi possível localizar 'End Class'. Corrija a estrutura do código antes de inserir o exemplo.", "Código VB.NET", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
                code.Select(insertAt, 0)
                code.SelectedText = block
            End If
            code.Focus()
        End Sub

        Private Sub RunProject()
            SaveCurrent() : status.Text = "Compilando..." : Cursor = Cursors.WaitCursor
            Try
                Dim errors As String = PreviewCompiler.CompileAndRun(project)
                If errors <> "" Then
                    ShowOutput(errors)
                Else
                    status.Text = "Aplicativo executado"
                End If
            Catch ex As Exception
                ShowOutput(ex.ToString())
            Finally
                Cursor = Cursors.Default
            End Try
        End Sub
        Private Sub BuildProject()
            SaveCurrent()
            Using dialog As New BuildOptionsForm(project)
                If dialog.ShowDialog(Me) <> DialogResult.OK Then Return
                project.ApplicationIcon = dialog.IconPath
                project.AssemblyTitle = dialog.AssemblyTitleValue
                project.AssemblyDescription = dialog.DescriptionValue
                project.AssemblyCompany = dialog.CompanyValue
                project.AssemblyProduct = dialog.ProductValue
                project.AssemblyCopyright = dialog.CopyrightValue
                project.AssemblyTrademark = dialog.TrademarkValue
                project.AssemblyVersion = dialog.AssemblyVersionValue
                project.FileVersion = dialog.FileVersionValue
                If currentFile IsNot Nothing Then ProjectStorage.Save(currentFile, project)
                status.Text = "Gerando executável..." : Cursor = Cursors.WaitCursor
                Try
                    Dim errors As String = PreviewCompiler.BuildExecutable(project, dialog.OutputPath)
                    If errors <> "" Then
                        ShowOutput(errors)
                        status.Text = "Falha na compilação"
                    Else
                        status.Text = "Executável gerado: " & dialog.OutputPath
                        MessageBox.Show("Aplicativo compilado com sucesso:" & vbCrLf & vbCrLf & dialog.OutputPath, "Gerar EXE", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    End If
                Catch ex As Exception
                    ShowOutput(ex.ToString())
                    status.Text = "Falha na compilação"
                Finally
                    Cursor = Cursors.Default
                End Try
            End Using
        End Sub
        Private Sub ShowOutput(text As String)
            ideOutput.SetCompilerOutput(text)
            ShowOutputPanel()
            If ideOutput.ErrorCount > 0 Then
                status.Text = ideOutput.ErrorCount & If(ideOutput.ErrorCount = 1, " erro de compilação", " erros de compilação")
            End If
        End Sub

        Private Sub ShowOutputPanel()
            outputPanelVisible = True
            ideOutput.Visible = True
            workspaceOutputLayout.RowStyles(1).SizeType = SizeType.Absolute
            workspaceOutputLayout.RowStyles(1).Height = Math.Max(100.0F, outputPanelHeight)
        End Sub

        Private Sub HideOutputPanel()
            If workspaceOutputLayout.RowStyles(1).Height > 0.0F Then
                outputPanelHeight = workspaceOutputLayout.RowStyles(1).Height
            End If
            outputPanelVisible = False
            ideOutput.Visible = False
            workspaceOutputLayout.RowStyles(1).SizeType = SizeType.Absolute
            workspaceOutputLayout.RowStyles(1).Height = 0.0F
        End Sub

        Private Sub OutputPanelCloseRequested(sender As Object, e As EventArgs)
            HideOutputPanel()
        End Sub

        Private Sub ToggleOutputPanel()
            If outputPanelVisible Then HideOutputPanel() Else ShowOutputPanel()
        End Sub

        Private Sub CompilerIssueActivated(sender As Object, e As CompilerIssueEventArgs)
            Dim fileName As String = Path.GetFileName(e.FileName)
            If String.IsNullOrWhiteSpace(fileName) Then Return

            If fileName.EndsWith(".Designer.vb", StringComparison.OrdinalIgnoreCase) Then
                Dim formName As String = fileName.Substring(0, fileName.Length - ".Designer.vb".Length)
                Dim formIndex As Integer = project.Forms.FindIndex(Function(f) f.Name.Equals(formName, StringComparison.OrdinalIgnoreCase))
                If formIndex >= 0 Then
                    OpenFormDesigner(formIndex)
                    status.Text = fileName & " (código gerado pelo Designer) — linha " & e.LineNumber
                    Return
                End If
            End If

            Dim baseName As String = Path.GetFileNameWithoutExtension(fileName)
            Dim index As Integer = project.Forms.FindIndex(Function(f) f.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase))
            If index >= 0 Then
                OpenFormCode(index)
                SelectEditorLocation(e.LineNumber, e.ColumnNumber)
                Return
            End If
            If project.Classes IsNot Nothing Then
                index = project.Classes.FindIndex(Function(c) c.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase))
                If index >= 0 Then OpenClassDocument(index) : SelectEditorLocation(e.LineNumber, e.ColumnNumber) : Return
            End If
            If project.Modules IsNot Nothing Then
                index = project.Modules.FindIndex(Function(m) m.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase))
                If index >= 0 Then OpenModuleDocument(index) : SelectEditorLocation(e.LineNumber, e.ColumnNumber) : Return
            End If
            If project.UserControls IsNot Nothing Then
                index = project.UserControls.FindIndex(Function(u) u.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase))
                If index >= 0 Then OpenUserControlDocument(index) : SelectEditorLocation(e.LineNumber, e.ColumnNumber) : Return
            End If
            status.Text = "O erro está em um arquivo gerado internamente: " & fileName
        End Sub

        Private Sub SelectEditorLocation(lineNumber As Integer, columnNumber As Integer)
            If lineNumber < 1 Then lineNumber = 1
            Dim lineIndex As Integer = Math.Min(lineNumber - 1, Math.Max(0, code.Lines.Length - 1))
            Dim start As Integer = code.GetFirstCharIndexFromLine(lineIndex)
            If start < 0 Then start = 0
            Dim offset As Integer = Math.Max(0, columnNumber - 1)
            Dim lineLength As Integer = If(code.Lines.Length > lineIndex, code.Lines(lineIndex).Length, 0)
            code.SelectionStart = Math.Min(code.TextLength, start + Math.Min(offset, lineLength))
            code.SelectionLength = 0
            code.ScrollToCaret()
            code.Focus()
            status.Text = "Erro: " & Path.GetFileName(If(documentTabs.SelectedTab Is Nothing, "", documentTabs.SelectedTab.Text)) & " — linha " & lineNumber & ", coluna " & columnNumber
        End Sub
        Private Sub SaveAll()
            SaveProject()
            If currentFile IsNot Nothing Then status.Text = "Tudo salvo: " & Path.GetFileName(currentFile)
        End Sub

        Private Sub SaveProject()
            If currentFile Is Nothing Then SaveProjectAs() : Return
            SaveCurrent() : ProjectStorage.Save(currentFile, project) : savedProjectState = ProjectState() : ClearDirtyIndicators() : RecentProjectStore.Add("FlowForgeStudio", currentFile) : RefreshRecentProjectsMenu() : status.Text = "Salvo: " & Path.GetFileName(currentFile)
        End Sub
        Private Sub SaveProjectAs()
            Using d As New SaveFileDialog With {.Filter = "Projeto FlowForge|*.flowapp", .FileName = project.Name & ".flowapp"}
                If d.ShowDialog() = DialogResult.OK Then
                    currentFile = d.FileName
                    project.Name = Path.GetFileNameWithoutExtension(d.FileName)
                    UpdateWindowTitle() : RefreshTree() : SaveProject()
                End If
            End Using
        End Sub

        Private Sub OpenProjectFolder()
            If String.IsNullOrWhiteSpace(currentFile) Then MessageBox.Show("Salve o projeto primeiro.", "Pasta do projeto", MessageBoxButtons.OK, MessageBoxIcon.Information) : Return
            Process.Start("explorer.exe", "/select," & QuoteArgument(currentFile))
        End Sub

        Private Sub CopyProjectPath()
            If String.IsNullOrWhiteSpace(currentFile) Then MessageBox.Show("Salve o projeto primeiro.", "Caminho do projeto", MessageBoxButtons.OK, MessageBoxIcon.Information) : Return
            Clipboard.SetText(currentFile)
            status.Text = "Caminho copiado"
        End Sub
        Private Sub OpenProject()
            Using d As New OpenFileDialog With {.Filter = "Projeto FlowForge|*.flowapp"}
                If d.ShowDialog() = DialogResult.OK Then LoadProjectFromPath(d.FileName)
            End Using
        End Sub

        Private Sub LoadProjectFromPath(filePath As String)
            If Not ConfirmDiscardChanges() Then Return
            Try
                project = ProjectStorage.Load(filePath)
                currentFile = filePath
                If String.IsNullOrWhiteSpace(project.Name) OrElse project.Name.Equals("NovoProjeto", StringComparison.OrdinalIgnoreCase) Then project.Name = Path.GetFileNameWithoutExtension(filePath)
                MaterializeProjectLibrariesForDesigner()
                activeIndex = 0 : activeClassIndex = -1 : activeModuleIndex = -1 : activeUserControlIndex = -1 : documentTabs.TabPages.Clear() : RefreshProject() : savedProjectState = ProjectState()
                RecentProjectStore.Add("FlowForgeStudio", filePath)
                RefreshRecentProjectsMenu()
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Abrir projeto", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub RefreshRecentProjectsMenu()
            recentProjectsMenu.DropDownItems.Clear()
            Dim files As List(Of String) = RecentProjectStore.Load("FlowForgeStudio")
            For Each filePath As String In files
                Dim capturedPath As String = filePath
                Dim item As New ToolStripMenuItem(Path.GetFileNameWithoutExtension(filePath)) With {.ToolTipText = filePath}
                AddHandler item.Click, Sub() LoadProjectFromPath(capturedPath)
                recentProjectsMenu.DropDownItems.Add(item)
            Next
            If files.Count = 0 Then recentProjectsMenu.DropDownItems.Add(New ToolStripMenuItem("Nenhum projeto recente") With {.Enabled = False})
            recentProjectsMenu.DropDownItems.Add(New ToolStripSeparator())
            recentProjectsMenu.DropDownItems.Add(Item("Limpar lista", AddressOf ClearRecentProjects, "delete"))
        End Sub

        Private Sub ClearRecentProjects()
            RecentProjectStore.Clear("FlowForgeStudio")
            RefreshRecentProjectsMenu()
        End Sub

        Private Sub MainDragEnter(sender As Object, e As DragEventArgs)
            If e.Data.GetDataPresent(DataFormats.FileDrop) Then e.Effect = DragDropEffects.Copy
        End Sub

        Private Sub MainDragDrop(sender As Object, e As DragEventArgs)
            Dim files As String() = TryCast(e.Data.GetData(DataFormats.FileDrop), String())
            If files Is Nothing OrElse files.Length = 0 Then Return
            If Path.GetExtension(files(0)).Equals(".flowapp", StringComparison.OrdinalIgnoreCase) Then LoadProjectFromPath(files(0))
        End Sub

        Private Sub OpenExample(fileName As String)
            Dim examplePath As String = IO.Path.Combine(Application.StartupPath, "Examples", fileName)
            If Not File.Exists(examplePath) Then
                MessageBox.Show("O exemplo não foi encontrado:" & vbCrLf & examplePath, "Exemplos", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            If Not ConfirmDiscardChanges() Then Return
            Try
                project = ProjectStorage.Load(examplePath)
                currentFile = Nothing
                MaterializeProjectLibrariesForDesigner()
                activeIndex = 0 : activeClassIndex = -1 : activeModuleIndex = -1 : activeUserControlIndex = -1 : documentTabs.TabPages.Clear() : RefreshProject() : savedProjectState = ProjectState()
                status.Text = "Exemplo carregado. Use Salvar como para criar sua cópia."
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Abrir exemplo", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub ImportDotNetLibrary()
            Using dialog As New OpenFileDialog With {.Filter = "Bibliotecas .NET (*.dll)|*.dll", .Title = "Adicionar controles Windows Forms de uma biblioteca .NET"}
                If dialog.ShowDialog(Me) = DialogResult.OK Then ImportDotNetAssembly(dialog.FileName)
            End Using
        End Sub

        Private Sub ImportDotNetAssembly(assemblyFile As String)
            Try
                EmbedLibrary(assemblyFile, True)
                Dim assembly As Assembly = Assembly.LoadFrom(assemblyFile)
                Dim types As Type()
                Try
                    types = assembly.GetExportedTypes()
                Catch ex As ReflectionTypeLoadException
                    types = ex.Types.Where(Function(t) t IsNot Nothing).ToArray()
                End Try
                Dim componentTypes = types.Where(Function(t) GetType(System.ComponentModel.Component).IsAssignableFrom(t) AndAlso Not t.IsAbstract AndAlso t.GetConstructor(Type.EmptyTypes) IsNot Nothing).OrderBy(Function(t) t.FullName).ToArray()
                If componentTypes.Length = 0 Then
                    MessageBox.Show("A biblioteca não contém controles ou componentes Windows Forms públicos com construtor vazio.", "Biblioteca .NET", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                Dim selectedType As Type = SelectExternalType(componentTypes)
                If selectedType Is Nothing Then Return
                Dim entry As New ToolboxEntry With {.Category = "Componentes externos", .TypeName = selectedType.FullName, .DisplayName = selectedType.Name, .Glyph = "DLL", .DefaultEvent = "Click", .AssemblyPath = assemblyFile, .IsNonVisual = Not GetType(Control).IsAssignableFrom(selectedType)}
                allTools.Add(entry) : toolboxSearch.Clear() : FilterToolbox(Nothing, EventArgs.Empty) : toolbox.SelectedItem = entry
                status.Text = selectedType.FullName & " adicionado à caixa de ferramentas"
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Carregar biblioteca .NET", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub EmbedLibrary(filePath As String, isReference As Boolean)
            If project.Libraries Is Nothing Then project.Libraries = New List(Of ProjectLibrary)()
            Dim relative As String = IO.Path.GetFileName(filePath)
            Dim existing As ProjectLibrary = project.Libraries.FirstOrDefault(Function(x) x.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase))
            If existing Is Nothing Then
                existing = New ProjectLibrary()
                project.Libraries.Add(existing)
            End If
            Dim bytes As Byte() = File.ReadAllBytes(filePath)
            existing.FileName = IO.Path.GetFileName(filePath)
            existing.RelativePath = relative
            existing.DataBase64 = Convert.ToBase64String(bytes)
            existing.IsReference = isReference
            existing.EmbedInExecutable = True
            existing.FileSize = bytes.LongLength
        End Sub

        Private Sub MaterializeProjectLibrariesForDesigner()
            If project Is Nothing OrElse project.Libraries Is Nothing OrElse project.Libraries.Count = 0 Then Return
            Dim safeProject As String = Regex.Replace(project.Name, "[^A-Za-z0-9_.-]", "_")
            Dim folder As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlowForgeStudio", "ProjectLibraries", safeProject)
            Directory.CreateDirectory(folder)
            For Each library As ProjectLibrary In project.Libraries
                If String.IsNullOrWhiteSpace(library.DataBase64) OrElse String.IsNullOrWhiteSpace(library.RelativePath) Then Continue For
                Dim relative As String = library.RelativePath.Replace("/"c, Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar)
                If Path.IsPathRooted(relative) OrElse relative.Split(Path.DirectorySeparatorChar).Any(Function(part) part = "..") Then Continue For
                Dim output As String = Path.Combine(folder, relative)
                Dim outputFolder As String = Path.GetDirectoryName(output)
                If Not Directory.Exists(outputFolder) Then Directory.CreateDirectory(outputFolder)
                File.WriteAllBytes(output, Convert.FromBase64String(library.DataBase64))
                For Each form As FormData In project.Forms
                    For Each component As ControlData In form.Controls
                        If Not String.IsNullOrWhiteSpace(component.AssemblyPath) AndAlso Path.GetFileName(component.AssemblyPath).Equals(library.FileName, StringComparison.OrdinalIgnoreCase) Then component.AssemblyPath = output
                    Next
                Next
            Next
        End Sub

        Private Function SelectExternalType(types As Type()) As Type
            Dim dialog As New Form With {.Text = "Selecionar controle .NET", .Width = 560, .Height = 430, .StartPosition = FormStartPosition.CenterParent, .MinimizeBox = False, .MaximizeBox = False}
            Dim list As New ListBox With {.Dock = DockStyle.Fill, .DisplayMember = "FullName"}
            list.Items.AddRange(types.Cast(Of Object).ToArray())
            Dim buttons As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .Height = 45, .FlowDirection = FlowDirection.RightToLeft, .Padding = New Padding(6)}
            Dim ok As New System.Windows.Forms.Button With {.Text = "Adicionar", .DialogResult = DialogResult.OK, .Width = 95}
            Dim cancel As New System.Windows.Forms.Button With {.Text = "Cancelar", .DialogResult = DialogResult.Cancel, .Width = 95}
            buttons.Controls.Add(ok) : buttons.Controls.Add(cancel) : dialog.Controls.Add(list) : dialog.Controls.Add(buttons)
            dialog.AcceptButton = ok : dialog.CancelButton = cancel
            If list.Items.Count > 0 Then list.SelectedIndex = 0
            Dim result As Type = Nothing
            If dialog.ShowDialog(Me) = DialogResult.OK Then result = TryCast(list.SelectedItem, Type)
            dialog.Dispose()
            Return result
        End Function

        Private Sub ImportOcx()
            Using dialog As New OpenFileDialog With {.Filter = "Controles ActiveX (*.ocx)|*.ocx", .Title = "Importar controle ActiveX/OCX"}
                If dialog.ShowDialog(Me) <> DialogResult.OK Then Return
                Dim axImp As String = FindAxImp()
                If axImp = "" Then
                    MessageBox.Show("AxImp.exe não foi encontrado. Instale no Visual Studio o componente '.NET Framework 4.8 SDK' e as ferramentas de desenvolvimento para desktop.", "Importar OCX", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
                Try
                    Dim outputFolder As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlowForgeStudio", "ImportedControls", Guid.NewGuid().ToString("N"))
                    Directory.CreateDirectory(outputFolder)
                    Dim outputFile As String = Path.Combine(outputFolder, "AxInterop." & Path.GetFileNameWithoutExtension(dialog.FileName) & ".dll")
                    Dim info As New ProcessStartInfo With {.FileName = axImp, .Arguments = QuoteArgument(dialog.FileName) & " /out:" & QuoteArgument(outputFile), .UseShellExecute = False, .CreateNoWindow = True, .WorkingDirectory = outputFolder}
                    Using process As Process = Process.Start(info)
                        If Not process.WaitForExit(120000) Then
                            MessageBox.Show("A importação do OCX excedeu dois minutos.", "Importar OCX", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                            Return
                        End If
                        If process.ExitCode <> 0 Then Throw New InvalidOperationException("O AxImp terminou com o código " & process.ExitCode & ". Verifique se o OCX está registrado e se a arquitetura x86/x64 é compatível.")
                    End Using
                    If Not File.Exists(outputFile) Then outputFile = Directory.GetFiles(outputFolder, "AxInterop*.dll").FirstOrDefault()
                    If String.IsNullOrWhiteSpace(outputFile) Then Throw New FileNotFoundException("O AxImp não gerou a biblioteca wrapper do ActiveX.")
                    ImportDotNetAssembly(outputFile)
                Catch ex As Exception
                    MessageBox.Show(ex.Message, "Importar OCX", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End Using
        End Sub

        Private Shared Function FindAxImp() As String
            Dim roots As New List(Of String) From {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft SDKs", "Windows"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Windows Kits")}
            For Each root As String In roots
                If Not Directory.Exists(root) Then Continue For
                Try
                    Dim matches As String() = Directory.GetFiles(root, "AxImp.exe", SearchOption.AllDirectories)
                    If matches.Length > 0 Then Return matches.OrderByDescending(Function(p) p).First()
                Catch
                End Try
            Next
            Return ""
        End Function

        Private Shared Function QuoteArgument(value As String) As String
            Return ChrW(34) & value & ChrW(34)
        End Function

        Private Sub EditToolStripItems()
            designer.EditSelectedToolStripItems(Me)

            Dim component As Object = designer.SelectedComponent
            properties.SelectedObject = component

            If component Is Nothing Then
                eventInspector.ClearTarget()
            Else
                Dim designName As String = If(activeUserControlIndex >= 0, project.UserControls(activeUserControlIndex).Name, CurrentDocument.Name)
                Dim designCode As String = If(activeUserControlIndex >= 0, project.UserControls(activeUserControlIndex).Code, CurrentDocument.Code)
                Dim componentName As String = If(TypeOf component Is Form, designName, designer.SelectedComponentName)
                eventInspector.SetTarget(component, componentName, designCode)
            End If

            status.Text = "Itens da barra atualizados"
        End Sub

        Private Sub EditSelectedCollection()
            If Not designer.CanEditSelectedCollection() Then
                MessageBox.Show("Selecione um ComboBox, ListBox, CheckedListBox, DataGridView, TabControl ou TreeView.", "Editor de Coleções", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            designer.EditSelectedCollection(Me)
            MarkCurrentDocumentDirty()

            Dim component As Object = designer.SelectedComponent
            properties.SelectedObject = If(component Is Nothing, Nothing, New DesignObjectAdapter(component))
            status.Text = "Coleção atualizada"
        End Sub

        Private Function CreateAddItemsMenu() As ToolStripMenuItem
            Dim menu As New ToolStripMenuItem("Adicionar item à barra", IconFactory.Create("toolbox"))
            menu.DropDownItems.AddRange(CreateToolStripItemCommands())
            Return menu
        End Function

        Private Function CreateAddItemsButton() As ToolStripDropDownButton
            Dim button As New ToolStripDropDownButton("Adicionar item", IconFactory.Create("toolbox", 18)) With {.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText, .ToolTipText = "Adicionar item ao MenuStrip, ToolStrip ou StatusStrip selecionado"}
            button.DropDownItems.AddRange(CreateToolStripItemCommands())
            Return button
        End Function

        Private Function CreateToolStripItemCommands() As ToolStripItem()
            Return New ToolStripItem() {
                Item("Item de menu", Sub() designer.AddToolStripItem("ToolStripMenuItem"), "form"),
                Item("Botão", Sub() designer.AddToolStripItem("ToolStripButton"), "run"),
                Item("Separador", Sub() designer.AddToolStripItem("ToolStripSeparator")),
                Item("Label", Sub() designer.AddToolStripItem("ToolStripLabel"), "code"),
                Item("Caixa de texto", Sub() designer.AddToolStripItem("ToolStripTextBox"), "code"),
                Item("ComboBox", Sub() designer.AddToolStripItem("ToolStripComboBox"), "toolbox"),
                Item("Botão suspenso", Sub() designer.AddToolStripItem("ToolStripDropDownButton"), "open"),
                Item("SplitButton", Sub() designer.AddToolStripItem("ToolStripSplitButton"), "open"),
                New ToolStripSeparator(),
                Item("StatusLabel", Sub() designer.AddToolStripItem("ToolStripStatusLabel"), "properties"),
                Item("ProgressBar", Sub() designer.AddToolStripItem("ToolStripProgressBar"), "build")}
        End Function

        Private Shared Function Tool(category As String, typeName As String, display As String, glyph As String, defaultEvent As String) As ToolboxEntry
            Return New ToolboxEntry With {.Category = category, .TypeName = typeName, .DisplayName = display, .Glyph = glyph, .DefaultEvent = defaultEvent}
        End Function
        Private Shared Function ComponentTool(category As String, typeName As String, display As String, glyph As String, defaultEvent As String) As ToolboxEntry
            Return New ToolboxEntry With {.Category = category, .TypeName = typeName, .DisplayName = display, .Glyph = glyph, .DefaultEvent = defaultEvent, .IsNonVisual = True}
        End Function
        Private Shared Function CloneForm(source As FormData) As FormData
            Using ms As New MemoryStream() : Dim s As New System.Runtime.Serialization.Json.DataContractJsonSerializer(GetType(FormData)) : s.WriteObject(ms, source) : ms.Position = 0 : Return DirectCast(s.ReadObject(ms), FormData) : End Using
        End Function
        Private Shared Function Group(title As String, child As Control) As Control
            Dim panel As New Panel With {.Dock = DockStyle.Fill}, label As New Label With {.Text = title, .Dock = DockStyle.Top, .Height = 26, .ForeColor = Color.White, .Padding = New Padding(6), .BackColor = Color.FromArgb(40, 44, 54)} : child.Dock = DockStyle.Fill : panel.Controls.Add(child) : panel.Controls.Add(label) : Return panel
        End Function
        Private Shared Function Item(text As String, action As Action, Optional iconName As String = "", Optional shortcut As Keys = Keys.None) As ToolStripItem
            Dim m As New ToolStripMenuItem(text)
            If iconName <> "" Then m.Image = IconFactory.Create(iconName)
            If shortcut <> Keys.None Then m.ShortcutKeys = shortcut
            AddHandler m.Click, Sub() action()
            Return m
        End Function
        Private Shared Function CheckItem(text As String, initialValue As Boolean, action As Action, iconName As String) As ToolStripItem
            Dim m As New ToolStripMenuItem(text) With {.Checked = initialValue, .CheckOnClick = True, .Image = IconFactory.Create(iconName)}
            AddHandler m.Click, Sub() action()
            Return m
        End Function
        Private Shadows Shared Function Menu(text As String, ParamArray items() As ToolStripItem) As ToolStripMenuItem
            Dim m As New ToolStripMenuItem(text) : m.DropDownItems.AddRange(items) : Return m
        End Function
        Private Shared Function Button(text As String, action As Action, Optional iconName As String = "") As ToolStripButton
            Dim b As New ToolStripButton(text) With {.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText, .ImageTransparentColor = Color.Magenta, .ToolTipText = text}
            If iconName <> "" Then b.Image = IconFactory.Create(iconName, 18)
            AddHandler b.Click, Sub() action()
            Return b
        End Function
    
        ' =========================
        ' Layout das barras da IDE
        ' =========================
        ' O layout é salvo em um arquivo simples na pasta de dados do usuário.
        ' Assim cada usuário mantém sua organização mesmo após fechar e abrir a IDE.
        Private Function ToolbarLayoutFile() As String
            Dim baseFolder As String = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FlowForge")
            System.IO.Directory.CreateDirectory(baseFolder)
            Return System.IO.Path.Combine(baseFolder, "toolbar-layout.ini")
        End Function

        Private Sub MainForm_SaveToolStripLayout(sender As Object, e As FormClosingEventArgs)
            SaveToolStripLayout()
        End Sub

        Private Sub SaveToolStripLayout()
            Try
                Dim lines As New List(Of String)()
                SaveOneToolStripLayout(lines, fileTools)
                SaveOneToolStripLayout(lines, projectTools)
                SaveOneToolStripLayout(lines, designerTools)
                SaveOneToolStripLayout(lines, buildTools)
                System.IO.File.WriteAllLines(ToolbarLayoutFile(), lines.ToArray())
            Catch
                ' Configuração de interface nunca deve impedir o fechamento da IDE.
            End Try
        End Sub

        Private Sub SaveOneToolStripLayout(lines As List(Of String), bar As ToolStrip)
            If bar Is Nothing OrElse bar.Parent Is Nothing Then Return
            Dim panel As ToolStripPanel = TryCast(bar.Parent, ToolStripPanel)
            If panel Is Nothing Then Return

            lines.Add(String.Join("|", New String() {
                bar.Name,
                panel.Dock.ToString(),
                bar.Location.X.ToString(Globalization.CultureInfo.InvariantCulture),
                bar.Location.Y.ToString(Globalization.CultureInfo.InvariantCulture)
            }))
        End Sub

        Private Sub RestoreToolStripLayout(shell As ToolStripContainer)
            Try
                Dim fileName As String = ToolbarLayoutFile()
                If Not System.IO.File.Exists(fileName) Then Return

                Dim bars As New Dictionary(Of String, ToolStrip)(StringComparer.OrdinalIgnoreCase)
                AddToolbarToMap(bars, fileTools)
                AddToolbarToMap(bars, projectTools)
                AddToolbarToMap(bars, designerTools)
                AddToolbarToMap(bars, buildTools)

                For Each raw As String In System.IO.File.ReadAllLines(fileName)
                    If String.IsNullOrWhiteSpace(raw) Then Continue For
                    Dim parts As String() = raw.Split("|"c)
                    If parts.Length <> 4 Then Continue For

                    Dim bar As ToolStrip = Nothing
                    If Not bars.TryGetValue(parts(0), bar) OrElse bar Is Nothing Then Continue For

                    Dim x As Integer
                    Dim y As Integer
                    If Not Integer.TryParse(parts(2), Globalization.NumberStyles.Integer, Globalization.CultureInfo.InvariantCulture, x) Then Continue For
                    If Not Integer.TryParse(parts(3), Globalization.NumberStyles.Integer, Globalization.CultureInfo.InvariantCulture, y) Then Continue For

                    ' Hoje as barras principais ficam no painel superior.
                    ' Mantemos o campo Dock no arquivo para permitir evolução futura sem quebrar o formato.
                    shell.TopToolStripPanel.Join(bar, New Point(Math.Max(0, x), Math.Max(0, y)))
                Next
            Catch
                ' Arquivo antigo/corrompido: usamos o layout padrão sem interromper a IDE.
            End Try
        End Sub

        Private Shared Sub AddToolbarToMap(map As Dictionary(Of String, ToolStrip), bar As ToolStrip)
            If bar Is Nothing OrElse String.IsNullOrWhiteSpace(bar.Name) Then Return
            map(bar.Name) = bar
        End Sub

End Class
End Namespace
