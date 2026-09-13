# PDFium portátil no FlowForge 0.34

O `FlowPDFViewer_PDFium.flowapp` usa o PDFium dentro do aplicativo e não exige que Poppler, Ghostscript, Acrobat ou outro programa seja instalado no Windows.

## Arquivos necessários

- `PdfiumViewer.dll`: adicionar como **referência .NET**.
- `pdfium.dll` de 32 bits: adicionar como **DLL nativa**, com destino `x86\pdfium.dll`.
- `pdfium.dll` de 64 bits: adicionar como **DLL nativa**, com destino `x64\pdfium.dll`.

Use versões compatíveis entre si e preserve os avisos/licenças fornecidos pelo distribuidor dos binários.

## Como incorporar

1. Abra `FlowPDFViewer_PDFium.flowapp` no FlowForge 0.34.
2. Abra **Projeto > Bibliotecas incorporadas**.
3. Clique em **Adicionar referência .NET** e selecione `PdfiumViewer.dll`.
4. Clique em **Adicionar DLL nativa** para cada `pdfium.dll`. Se elas estiverem originalmente em pastas chamadas `x86` e `x64`, o FlowForge sugere os destinos corretos automaticamente.
5. Deixe os três itens no modo **Dentro do EXE** e salve. As DLLs passam a fazer parte do `.flowapp` e do executável final.
6. Use **Gerar EXE**. O FlowForge produzirá um único EXE e extrairá as DLLs automaticamente somente durante a execução.

O resultado distribuído é apenas o EXE. Não há instalador nem ferramenta externa sendo executada para renderizar as páginas.
