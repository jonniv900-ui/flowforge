# Project Explorer / Classes / Document Tabs

Esta revisão adiciona às edições Studio e Education:

- Classes VB.NET persistidas dentro do arquivo `.flowapp`.
- `Projeto > Adicionar Classe...` e botão `Adicionar Classe` na barra principal.
- `Adicionar Classe...` no menu de contexto do Explorador do Projeto.
- Nós separados `Forms` e `Classes` no Explorador.
- Duplo clique em `FormX.vb` abre o código.
- Duplo clique no Form ou em `FormX.Designer.vb` abre o Designer.
- Duplo clique em uma classe abre seu código.
- Abas de documentos para `FormX [Design]`, `FormX.vb` e `ClassX.vb`.
- Clique numa aba troca de documento mantendo o texto salvo no modelo do projeto.
- Clique do botão do meio fecha uma aba.
- Classes são enviadas ao `PreviewCompiler` e compiladas junto com os Forms no EXE final.
- Projetos antigos continuam compatíveis: se `Classes` não existir no JSON, uma lista vazia é criada ao carregar.

## Expansão do Explorador de Projeto

- Pastas de projeto, inclusive subpastas, persistidas no `.flowapp`.
- `Module` VB.NET como arquivo editável e compilado junto ao aplicativo.
- `UserControl` VB.NET como arquivo editável, herdando de `System.Windows.Forms.UserControl`, e compilado junto ao aplicativo.
- Menu de contexto no Explorador: abrir, renomear, mover para pasta e excluir.
- `F2` renomeia o item selecionado; `Delete` exclui com confirmação.
- Renomear atualiza a declaração principal (`Class`, `Module` ou `Partial Public Class`) para manter o fonte consistente.
- Abas de documentos agora exibem botão `X`; clique do meio continua fechando a aba.
- Itens dentro de pastas continuam participando normalmente da compilação; as pastas são organização do projeto e não alteram namespaces automaticamente.
