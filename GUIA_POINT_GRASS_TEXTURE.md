# Guía: Solución "0 Puntos" y Textura del Terreno

Esta guía te ayudará a solucionar el problema de que no aparece la hierba (0 puntos) y a ponerle la misma textura al terreno que la del asset.

## 1. Solucionar el problema de "0 Points" (Zero Points)

El componente `Point Grass Renderer` utiliza las **Capas del Terreno (Terrain Layers)** para saber dónde colocar la hierba. Si dice "Total point count: 0", es porque no has "pintado" el terreno con la capa que el script está buscando.

### Pasos:

1.  Selecciona tu objeto **Terrain** en la jerarquía.
2.  En el Inspector del Terreno, ve a la pestaña **Paint Texture** (el icono del pincel).
3.  Asegúrate de que en "Layer Palette" tienes asignada la capa que estás usando en el `Point Grass Renderer`.
    *   *Nota:* En tu imagen vi que el `Point Grass Renderer` tiene 2 elementos en "Terrain Layers". Asegúrate de pintar con una de esas capas.
4.  Selecciona la capa en la paleta y **pinta sobre el terreno** en la escena (clic izquierdo y arrastrar).
5.  Vuelve a seleccionar el objeto con el `Point Grass Renderer` y dale al botón **"Refresh Points"**.
6.  ¡Deberían aparecer los puntos y la hierba!

---

## 2. Cambiar la Textura del Terreno (Igualar al Asset)

Para que el suelo se vea igual que la hierba, necesitas asignar la textura `PG_EXAMPLE_Grass` a una capa de tu terreno.

### Pasos:

1.  Selecciona tu objeto **Terrain**.
2.  Ve a la pestaña **Paint Texture** (icono del pincel).
3.  Bajo "Layer Palette", haz clic en el botón **Edit Terrain Layers...** -> **Create Layer...**.
4.  Se abrirá una ventana de selección. Busca **`PG_EXAMPLE_Grass`**.
    *   Debería haber una textura llamada así (probablemente verde/hierba). Selecciónala.
5.  Ahora aparecerá una nueva capa ("New Layer") en tu "Layer Palette".
6.  Selecciónala y pinta tu terreno con ella.
    *   *Truco:* Si quieres que sea la textura por defecto de todo el terreno, asegúrate de que sea la **primera** capa en la lista (puedes arrastrarlas o borrar las otras si no las usas).

### Verificar Referencias en Point Grass Renderer

Si creaste una nueva capa, asegúrate de añadir esa nueva capa ("Terrain Layer") a la lista **Terrain Layers** en tu componente `Point Grass Renderer`.

1.  Ve al `Point Grass Renderer`.
2.  En **Terrain Layers**, asegúrate de que la capa que acabas de crear y pintar está en la lista (Element 0 o Element 1).
3.  Dale a **Refresh Points**.

---

## Resumen

1.  **Pintar el Terreno**: La hierba solo aparece donde hay textura pintada.
2.  **Coincidir Capas**: La capa pintada en el terreno debe estar en la lista `Terrain Layers` del script.
3.  **Textura Correcta**: Crea una Layer con la textura `PG_EXAMPLE_Grass` para que combine visualmente.
