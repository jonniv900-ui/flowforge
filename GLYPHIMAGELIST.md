# GlyphImageList

480 glyphs em 16 categorias, renderizados dinamicamente por GDI+.

Temas: Classic, Office2003, Windows9x, Modern, Dark e Monochrome.

```vb
Button1.Image = GlyphImageList1.GetGlyph(GlyphType.File_Save)
ToolStripButton1.Image = GlyphImageList1.GetGlyph("File.Save")
PictureBox1.Image = GlyphImageList1.GetGlyph(GlyphType.Network_Wifi, 32)
```

Para controles que exigem um ImageList tradicional:
```vb
TreeView1.ImageList = GlyphImageList1.ImageList
ListView1.SmallImageList = GlyphImageList1.ImageList
```
