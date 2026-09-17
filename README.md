# FlowForge Suite

Solução conjunta para Visual Studio 2026 contendo as duas edições do FlowForge, ambas em VB.NET Windows Forms para .NET Framework 4.8.

FlowForge Studio é um ambiente de desenvolvimento integrado criado para tornar o desenvolvimento WinForms mais visual, rápido e acessível.

O projeto combina um designer visual de interfaces, editor de código VB.NET, gerenciamento de projetos, componentes personalizados e ferramentas auxiliares em uma única aplicação.

> **Status atual:** Beta  
> Esta é uma versão inicial destinada a testes, desenvolvimento e coleta de feedback.

## Projetos

| Projeto | Executável | Finalidade |
| --- | --- | --- |
| FlowForge Studio 0.39.1 | `FlowForgeStudio.exe` | IDE completa com 24 controles customizados, projetos recentes e proteção de alterações |
| FlowForge Education 0.10.1 Preview | `FlowForgeEducation.exe` | Fork educacional com 24 controles customizados, quiz, caderno e progresso |

Os dois projetos abrem o mesmo formato `.flowapp`, mas possuem código-fonte, identidade, configurações e diretórios de saída separados.

## Sobre o projeto

O FlowForge nasceu da ideia de criar uma IDE independente voltada ao desenvolvimento visual em VB.NET, mantendo a simplicidade que tornou o Visual Basic uma excelente porta de entrada para programação, mas acrescentando recursos modernos de desenvolvimento.

Um projeto do FlowForge é armazenado no formato `.flowapp` e pode conter múltiplos Forms, classes, módulos, UserControls, recursos e referências.

O próprio FlowForge pode transformar o projeto em um aplicativo Windows executável.

## Compilar os dois

Abra `FlowForgeSuite.sln` e use **Compilar solução**, ou execute `build_all.bat`.

As saídas serão geradas em:

- `FlowForgeStudio\bin\Release\FlowForgeStudio.exe`
- `FlowForgeEducation\bin\Release\FlowForgeEducation.exe`

É necessário ter o Visual Studio 2026 com desenvolvimento para desktop e o Developer Pack/Targeting Pack do .NET Framework 4.8.

Cada subpasta também mantém sua solução e seu script individual, permitindo compilar somente uma edição quando necessário.

## Principais recursos

### Designer visual WinForms

Crie interfaces arrastando componentes para o formulário.

O Designer possui:

- seleção simples e múltipla;
- redimensionamento visual;
- movimentação de controles;
- Undo/Redo;
- alinhamento e distribuição;
- guias e Snap;
- controle de ordem Z;
- duplicação de componentes;
- edição pelo PropertyGrid;
- painel de eventos;
- visualização Designer/Código;
- moldura visual do Form.

### Editor VB.NET

Editor integrado para Forms, Classes, Modules e UserControls.

Inclui recursos como:

- destaque de sintaxe;
- numeração de linhas;
- IntelliSense básico;
- identificação de variáveis locais;
- navegação entre código e Designer;
- criação automática de manipuladores de eventos;
- busca no projeto;
- painel de saída;
- lista de erros com navegação para arquivo e linha.

### Gerenciamento de projetos

O Project Explorer suporta:

- múltiplos Forms;
- Classes;
- Modules;
- UserControls;
- pastas e subpastas;
- renomear itens;
- mover itens;
- excluir itens;
- duplicar Forms;
- selecionar o Form inicial;
- recursos do projeto;
- referências e DLLs.

### Formato `.flowapp`

O FlowForge utiliza seu próprio formato de projeto.

O formato possui versionamento interno e mantém compatibilidade com projetos criados em versões anteriores sempre que possível.

Arquivos `.flowapp` também podem ser associados ao FlowForge no Windows.

### Componentes personalizados

Além dos controles WinForms tradicionais, o FlowForge inclui uma biblioteca de componentes próprios, incluindo:

- RoundedButton;
- GradientPanel;
- LED Indicator;
- ToggleSwitch;
- Digital Display;
- display de 7 segmentos;
- matriz de LEDs;
- LCD Display;
- CircularProgress;
- LevelMeter;
- BatteryIndicator;
- SignalStrength;
- ThermometerGauge;
- AnalogGauge;
- LoadingSpinner;
- NotificationBanner;
- Sparkline;
- SimpleChart;
- VirtualJoystick;
- TrafficLight;
- ArduinoPin;
- IoTSensor;
- RichTextEditor;
- SyntaxCodeEditor;
- JsonTreeViewer;
- SearchBox;
- PasswordBox;
- IPAddressBox;
- ModernDatePicker;
- TagInput;
- ImageButton;
- NavigationButton;
- entre outros.

### MenuStrip, ToolStrip e StatusStrip

O FlowForge possui editor visual para itens dessas barras.

É possível trabalhar com menus, submenus, separadores, botões, labels, ComboBoxes e outros ToolStripItems.

### Editor de coleções

Coleções comuns podem ser configuradas diretamente pela IDE:

- `ComboBox.Items`;
- `ListBox.Items`;
- `CheckedListBox.Items`;
- `DataGridView.Columns`;
- `TabControl.TabPages`;
- `TreeView.Nodes`.

Essas informações são armazenadas no projeto e recriadas no aplicativo compilado.

### Ferramentas adicionais

O FlowForge também inclui ferramentas experimentais e auxiliares como:

- Resource Manager;
- monitor serial;
- ferramentas Arduino/ESP;
- integração com `arduino-cli`;
- editor de fluxogramas;
- geração de código VB.NET a partir de fluxogramas;
- designer de banco de dados SQL Server;
- ferramentas de rede;
- exemplos de Network Scanner;
- Wi-Fi Scanner.

## Controles FlowForge

A caixa de ferramentas inclui `RoundedButton`, `GradientPanel`, `LedIndicator`, `ToggleSwitch` e `DigitalDisplay`. Eles são salvos no projeto e incorporados automaticamente ao código-fonte do EXE gerado, sem exigir DLL externa. Abra **Exemplos > Básicos/Iniciante > Painel de controles customizados** para testar propriedades, eventos e métodos.

O `DigitalDisplay` possui os estilos `SevenSegment`, `DotMatrix` e `Text`. A coleção também inclui `CircularProgress`, `LevelMeter`, `BadgeLabel`, `SeparatorLine`, `StarRating`, `NumericKnob` e `CardPanel`. O exemplo **Controles avançados e displays** demonstra os novos eventos e propriedades.

Também foram incluídos `BatteryIndicator`, `SignalStrength`, `ThermometerGauge`, `AnalogGauge`, `LoadingSpinner`, `NotificationBanner`, `ToggleButton`, `ColorSwatch`, `NavigationButton` e `MarqueeLabel`. O exemplo **Painel de telemetria** liga esses componentes como uma interface de sensores/Arduino.

O `RichTextEditor` traz uma barra integrada de formatação e operações RTF/TXT. O `SyntaxCodeEditor` possui contador de linhas, modos Visual Basic, HTML, JSON e texto, destaque atrasado e redesenho suspenso para evitar cintilação. Veja o exemplo **Editores de texto e código**.

## Expansão de componentes FlowForge

Esta versão adiciona componentes customizados compartilhados pelo Studio e Education:
ImageButton, SearchBox, PasswordBox, IPAddressBox, ModernDatePicker, SimpleChart (linha/barras),
VirtualJoystick, LcdDisplay, LedMatrix, TrafficLight, SevenSegmentDigit, ArduinoPin e IoTSensor.
Os controles são incorporados ao CustomControls.vb usado também pelo compilador de preview/build.

## Exemplos

A distribuição inclui diversos projetos `.flowapp`.

Os exemplos não servem apenas como demonstração: **o código dos projetos de exemplo é comentado e preparado como material de aprendizado**.

Há exemplos de:

- controles básicos;
- componentes personalizados;
- interfaces;
- gráficos;
- IoT;
- Arduino;
- JSON;
- rede;
- Wi-Fi;
- formulários;
- eventos;
- entre outros.

## Compilação de aplicativos

O FlowForge pode gerar aplicações Windows a partir de projetos `.flowapp`.

Durante a compilação, a IDE gera os arquivos necessários do projeto, Forms, Designer, componentes e recursos auxiliares.

O objetivo é permitir o fluxo:

**Criar → Desenhar → Programar → Compilar → Executar**

sem sair do FlowForge.

## Interface

A interface segue o modelo tradicional de uma IDE desktop:

- Menu principal;
- barras de ferramentas independentes;
- Project Explorer;
- Toolbox;
- área de documentos;
- Designer;
- editor de código;
- propriedades;
- eventos;
- saída;
- lista de erros.

As barras de ferramentas podem ser reposicionadas e sua organização é preservada entre execuções.

## Requisitos

- Windows 10 ou Windows 11;
- .NET Framework 4.8;
- ambiente Windows compatível com aplicações WinForms.

## Melhorias de produtividade — setembro/2026

- Salvar tudo no menu Arquivo e na barra de ferramentas (Ctrl+Alt+S).
- Localizar no projeto (Ctrl+Shift+F) em Forms, Classes, Modules e UserControls.
- Resultados de pesquisa exibem arquivo, linha e trecho; duplo clique/Enter abre o arquivo na linha correta.
- Menu de contexto nas abas: Fechar, Fechar outras e Fechar todas.
- Mantida a estrutura de layout corrigida de abas → Designer/Código → conteúdo.

## Melhorias de produtividade desta revisão

- Painel inferior integrado com abas **Saída** e **Lista de Erros**.
- Erros de compilação são convertidos em itens navegáveis com arquivo, linha, coluna, código e descrição.
- Duplo clique/Enter em um erro abre o arquivo `.vb` correspondente e posiciona o cursor na linha/coluna.
- Erros em `.Designer.vb` direcionam para o Form correspondente e indicam que se trata de código gerado.
- `Exibir > Saída / Lista de erros` (`Ctrl+Alt+O`) mostra/oculta o painel sem sobrepor o Designer/editor.
- Abas de código/projeto exibem `*` quando o documento é alterado; ao salvar, o indicador é removido.
- A janela principal também exibe `*` enquanto houver documento aberto marcado como alterado.

## Beta

O FlowForge ainda está em desenvolvimento.

Nesta fase, o objetivo principal é encontrar:

- erros de compilação;
- problemas no Designer;
- componentes incompatíveis;
- problemas de persistência do `.flowapp`;
- diferenças entre o Designer e o executável final;
- problemas de interface;
- situações não previstas no fluxo de trabalho.

Relatórios de bugs e sugestões são bem-vindos através das Issues do projeto.

## Roadmap

Alguns recursos planejados para versões futuras incluem:

- Project Properties avançado;
- melhorias no Resource Manager;
- suporte visual a `My.Settings`;
- IntelliSense mais avançado;
- ferramentas de refatoração;
- templates de projeto;
- melhorias adicionais no Designer;
- página inicial;
- debugger integrado;
- sistema de extensões/plugins;
- assistente de publicação e distribuição.

---

## FlowForge Education

O projeto também possui uma edição voltada ao ensino:

**FlowForge Education**

Ela utiliza a mesma base de desenvolvimento, mas adiciona recursos e uma experiência direcionada a estudantes e iniciantes.

---

## Licença

Consulte o arquivo `LICENSE` do repositório para informações sobre os termos de uso e distribuição.

---

**FlowForge Studio**  
Desenvolvimento visual em VB.NET.

