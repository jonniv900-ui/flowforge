# Novos exemplos de componentes

Foram adicionados quatro projetos `.flowapp` ao menu **Exemplos** das edições Studio e Education.

- `DashboardDados.flowapp`: Sparkline, SimpleChart, LcdDisplay e SevenSegmentDigit.
- `FormularioModerno.flowapp`: SearchBox, PasswordBox, IPAddressBox, ModernDatePicker, TagInput, TabStripCustom, NavigationButton e ImageButton.
- `LaboratorioIoT.flowapp`: VirtualJoystick, ArduinoPin, IoTSensor, LedMatrix, TrafficLight, LcdDisplay e SevenSegmentDigit.
- `JsonExplorer.flowapp`: SyntaxCodeEditor e JsonTreeViewer, incluindo tratamento de JSON válido e inválido.

Os exemplos usam `FormatVersion = 2` e são compartilhados pela edição Education a partir da pasta de exemplos do Studio, seguindo o padrão já usado pelos demais demos de componentes customizados.


## Revisão de validação

Os quatro exemplos novos foram revisados novamente. Foram corrigidos o escape de strings VB.NET no JSON Explorer, a importação de `System.Collections.Generic` no Dashboard e acessos a controles durante eventos disparados no carregamento inicial.


## Network Scanner

Disponível em **Exemplos > Rede > Network Scanner**. Detecta o IPv4 local, varre uma faixa /24 em lotes concorrentes, faz ping, tenta resolver hostname e usa `IPAddressBox`, `SearchBox`, `SignalStrength`, `Sparkline`, `LcdDisplay` e `SimpleChart`. Inclui também teste de download HTTP de aproximadamente 10 MB com Mbps em tempo real.


## Wi-Fi Scanner

Disponível em **Exemplos > Rede > Wi-Fi Scanner**. Usa `netsh wlan` para listar
SSID/BSSID, sinal, canal, banda, segurança e tipo de rádio. Demonstra `SearchBox`,
`SignalStrength`, `LcdDisplay`, `Sparkline` e `SimpleChart`. A conversão percentual
de sinal -> dBm é apenas uma aproximação visual.
> Correção: o parser do Wi-Fi Scanner usa somente APIs `System` para tratar CR/LF, sem depender de `ControlChars`.

