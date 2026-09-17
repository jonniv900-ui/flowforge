# FlowForge Suite — Mega rodada de melhorias

Esta versão parte do pacote com Designer visual de UserControl corrigido e acrescenta uma rodada ampla de recursos nas edições Studio e Education.

## Designer
- Undo/Redo também no Designer (`Ctrl+Z` / `Ctrl+Y`).
- Histórico de alterações de controles e propriedades.
- Seleção múltipla com Ctrl/Shift + clique.
- Seleção de vários controles arrastando um retângulo na área do Form.
- Movimento conjunto dos controles selecionados.
- Alinhar esquerda/direita/topo/base/centros.
- Igualar largura, altura ou ambos.
- Distribuir horizontal ou verticalmente.
- Snap lines inteligentes entre bordas e centros durante o movimento.
- Grid e snap-to-grid continuam disponíveis e convivem com as snap lines.

## IntelliSense
- Mantido o IntelliSense contextual por reflexão para controles do Form e UserControls.
- Controles internos de UserControls também entram nos símbolos do projeto.
- Variáveis locais declaradas como `Dim x As Tipo` passam a receber autocomplete após `x.` para tipos .NET conhecidos.

## Recursos
- `Projeto > Recursos do projeto...`.
- Adiciona imagens, textos, PDFs e outros arquivos ao `.flowapp` e ao EXE final.
- Snippets prontos para `FlowForgeResources.GetPath`, `GetImage` e `GetBytes`.

## Referências / DLLs
- `Projeto > Referências / DLLs...` usa o gerenciador de bibliotecas já existente.
- Referências .NET, DLLs nativas e recursos podem ser incorporados ou copiados ao lado do EXE.

## Arduino / ESP
- `Ferramentas > Arduino / ESP — Monitor e Upload...`.
- Monitor serial integrado, seleção de porta e baud rate.
- Envio de comandos pela serial.
- Perfis Arduino UNO, Nano, ESP32 e ESP8266.
- Compilar/upload de sketch `.ino` por `arduino-cli` quando disponível.

## Fluxograma → VB.NET
- Editor por blocos para atribuição, mensagem, If/Else, For, While e comentários.
- Gera um `Module` VB.NET real dentro do projeto e já abre o arquivo gerado.

## Banco de Dados Visual
- Ferramenta SQL Server sem dependências externas.
- Teste de conexão.
- Leitura de tabelas pelo schema ADO.NET.
- Geração automática de `BancoDados.vb` com rotina de consulta e método para a tabela selecionada.
- Outros providers continuam possíveis via sistema de referências DLL.

## Compatibilidade
- Studio e Education recebem os mesmos recursos estruturais.
- O formato `.flowapp` anterior continua compatível; os novos gerenciadores reutilizam as estruturas de bibliotecas já existentes.
