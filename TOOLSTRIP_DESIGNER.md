# Editor visual de MenuStrip / ToolStrip / StatusStrip

A IDE agora possui um editor dedicado para os itens de barras do Windows Forms.

## Como abrir

Selecione um `MenuStrip`, `ToolStrip`, `StatusStrip` ou qualquer item pertencente a uma dessas barras e use:

- **Ferramentas > Editar itens da barra...**
- botão **Editar itens** da barra principal da IDE;
- menu de contexto do componente selecionado.

## Recursos

- árvore visual dos itens e submenus;
- criação de `ToolStripMenuItem`, `ToolStripButton`, `ToolStripSeparator`, `ToolStripLabel`, `ToolStripTextBox`, `ToolStripComboBox`, `ToolStripDropDownButton`, `ToolStripSplitButton`, `ToolStripStatusLabel` e `ToolStripProgressBar`;
- alteração de `Name` e `Text`;
- `ShortcutKeys` para itens de menu;
- `Enabled`, `Visible` e `Checked`;
- reordenação para cima/baixo;
- transformação em submenu e retorno para a raiz;
- exclusão de itens;
- persistência dentro do `.flowapp`;
- geração das declarações e inicialização no executável final.

Os eventos continuam sendo criados pela aba **Eventos** do FlowForge, permitindo associar `Click` e os demais eventos da forma normal.
