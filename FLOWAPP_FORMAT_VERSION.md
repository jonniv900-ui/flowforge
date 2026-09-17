# Versionamento do formato .flowapp

## Formato atual

`FormatVersion = 2`

`FormatVersion` representa apenas a estrutura interna do arquivo `.flowapp`.
Ele é independente de `Version`, que continua sendo a versão do projeto/aplicativo.

## Compatibilidade com arquivos antigos

Arquivos históricos que não possuem `FormatVersion` são interpretados como
`FormatVersion = 1` e continuam abrindo normalmente.

A migração acontece somente em memória. O arquivo antigo só é gravado no
formato atual quando o usuário escolher Salvar.

## Formatos futuros

Se o arquivo tiver um `FormatVersion` maior que o suportado pela IDE, o
FlowForge interrompe a abertura e informa que é necessário atualizar a IDE,
evitando perda de dados causada por uma leitura parcial de estruturas futuras.

## Migrações

A rotina `MigrateProject` centraliza futuras atualizações:

- v1 -> v2: introdução do versionamento estrutural explícito.
- futuras versões devem acrescentar seus passos de migração sequencialmente.
