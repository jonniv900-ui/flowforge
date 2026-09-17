# Persistência da posição das barras

As barras independentes da IDE agora salvam automaticamente sua posição ao fechar
a janela principal e restauram o layout na próxima execução.

O arquivo de configuração fica em:

`%LOCALAPPDATA%\FlowForge\toolbar-layout.ini`

O arquivo é específico do usuário do Windows e não faz parte do projeto `.flowapp`.
Se estiver ausente ou inválido, a IDE simplesmente usa o layout padrão.

A implementação vale para Studio e Education.
