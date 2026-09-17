# Barras de ferramentas separadas

A barra única da janela principal foi dividida em barras independentes.

## Studio
- Arquivo
- Projeto
- Designer
- Execução

## Education
Possui as mesmas barras e ainda:
- Aprender

As barras usam `ToolStripGripStyle.Visible`, portanto continuam independentes dentro
do `ToolStripContainer` e podem ser reposicionadas pelo usuário. Para evitar uma barra
horizontal excessivamente comprida, elas começam organizadas em duas linhas.
