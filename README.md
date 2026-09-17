# FlowForge Suite

Solução conjunta para Visual Studio 2026 contendo as duas edições do FlowForge, ambas em VB.NET Windows Forms para .NET Framework 4.8.

## Projetos

| Projeto | Executável | Finalidade |
| --- | --- | --- |
| FlowForge Studio 0.39.1 | `FlowForgeStudio.exe` | IDE completa com 24 controles customizados, projetos recentes e proteção de alterações |
| FlowForge Education 0.10.1 Preview | `FlowForgeEducation.exe` | Fork educacional com 24 controles customizados, quiz, caderno e progresso |

Os dois projetos abrem o mesmo formato `.flowapp`, mas possuem código-fonte, identidade, configurações e diretórios de saída separados.

## Compilar os dois

Abra `FlowForgeSuite.sln` e use **Compilar solução**, ou execute `build_all.bat`.

As saídas serão geradas em:

- `FlowForgeStudio\bin\Release\FlowForgeStudio.exe`
- `FlowForgeEducation\bin\Release\FlowForgeEducation.exe`

É necessário ter o Visual Studio 2026 com desenvolvimento para desktop e o Developer Pack/Targeting Pack do .NET Framework 4.8.

Cada subpasta também mantém sua solução e seu script individual, permitindo compilar somente uma edição quando necessário.

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
