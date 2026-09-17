# Designer visual de UserControl

Esta versão adiciona Designer visual para UserControls do projeto.

- Novo UserControl abre diretamente em modo Designer.
- Alternância Designer/Código usa a mesma barra dos Forms.
- Propriedades e controles do UserControl são serializados no `.flowapp`.
- Eventos funcionam pela aba de Eventos.
- O compilador gera o arquivo `.Designer.vb` do UserControl e o inclui no EXE final.
- UserControls existentes são tratados com compatibilidade: o compilador converte a declaração para `Partial` em memória e injeta `InitializeComponent()` em construtores existentes quando necessário.
- UserControls do projeto aparecem na Caixa de Ferramentas na categoria `Projeto / UserControls`.
- No designer, uma instância de UserControl do projeto é representada por um placeholder com seu nome; no EXE compilado ela é instanciada como o tipo real do projeto.

Duplo clique em um UserControl no Explorador abre o Designer. Clique direito oferece Exibir Designer e Exibir Código.
