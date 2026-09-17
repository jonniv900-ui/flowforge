# Ícones e associação .flowapp

Esta versão adiciona identidade visual própria ao FlowForge e suporte a abertura de projetos por duplo clique.

## Ícones
- `FlowForgeStudio.ico` — Studio.
- `FlowForgeEducation.ico` — Education.
- `FlowForgeProject.ico` — arquivos `.flowapp`.

Os executáveis usam seus ícones na compilação e a janela principal carrega o mesmo ícone da pasta de saída.

## Linha de comando
As duas edições aceitam:

    FlowForgeStudio.exe "C:\Projetos\MeuProjeto.flowapp"
    FlowForgeEducation.exe "C:\Projetos\MeuProjeto.flowapp"

## Duplo clique no Windows
1. Compile em Release com `build_all.bat`.
2. Execute `Registrar_Flowapp.bat`.
3. A associação é criada em HKCU, portanto não precisa de administrador.

O Studio fica como padrão. Se Education estiver compilado, aparece também **Abrir no FlowForge Education** no menu de contexto.

Para remover, execute `Remover_Associacao_Flowapp.bat`. Se mover a pasta do FlowForge, execute o registro novamente.
