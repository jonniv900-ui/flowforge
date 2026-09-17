# Correção do Editor de Coleções

Corrigidos os erros de compilação reportados após a inclusão do Editor de Coleções.

- `CanEditSelectedCollection` e `EditSelectedCollection` agora existem também no
  `DesignerSurface` da edição Education.
- Foram incluídas as rotinas auxiliares de serialização que faltavam:
  `EncodeStringList`, `SerializeGridColumns`, `SerializeTabPages` e
  `SerializeTreeNodes`.
- `ApplyProperties` passou a restaurar as coleções internas `__FF_*`.
- Studio e Education usam a mesma lógica de persistência das coleções.

