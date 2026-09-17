# FlowForge Custom Controls para Visual Studio

Biblioteca WinForms **.NET Framework 4.8** reutilizável. O assembly é `FlowForge.CustomControls.dll`.
O namespace permanece `FlowForgeStudio` para compatibilidade.

## Compilar
Abra `FlowForge.CustomControls.sln` no Visual Studio 2022/2026 e compile em Release, ou execute `build_release.bat`.

## Instalar na Toolbox
No projeto WinForms .NET Framework: Toolbox > botão direito > Escolher Itens... > Procurar >
selecione `bin\Release\FlowForge.CustomControls.dll`. Marque os controles desejados.

Também adicione a DLL em Referências quando necessário.

```vb
Imports FlowForgeStudio
Dim glyphs As New GlyphImageList()
Button1.Image = glyphs.GetGlyph(GlyphType.File_Save)
```

`GlyphImageList` e `SerialConnection` são não visuais e aparecem na bandeja inferior do Designer.
`DemoVB` demonstra GlyphImageList, ToastNotification, Accordion, ProgressStepper e TerminalView.
