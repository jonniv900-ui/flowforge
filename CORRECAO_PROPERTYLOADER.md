# Correção PropertyLoader

A geração do `PropertyLoader.vb` do aplicativo compilado foi reescrita.

O problema era o uso de blocos VB.NET (`For Each`, `If`, `Select Case`) comprimidos
em uma única linha. Isso fazia o compilador interpretar incorretamente `Next`,
`End If` e variáveis locais como `p`.

Agora o código gerado usa blocos VB.NET completos, com uma instrução por linha,
para Items, DataGridView.Columns, TabPages e TreeView.Nodes.
