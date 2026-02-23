# 🎮 Guía del Menú Principal

## Instrucciones paso a paso

### Paso 1 — Abrir Unity y esperar la compilación

1. Abre tu proyecto **counter3.5** en Unity
2. Espera a que Unity compile los scripts nuevos (verás la barra de progreso abajo)
3. Si hay errores de compilación, revisa la consola (`Window > Console`)

### Paso 2 — Generar el menú automáticamente

1. Ve al menú superior de Unity: **Tools > Crear Menú Principal**
2. Se abrirá una ventanita con un campo de texto que dice "Escena del juego"
3. Asegúrate de que pone **SampleScene** (o el nombre de tu escena de juego)
4. Pulsa el botón **¡Crear Menú Principal!**
5. ¡Listo! Se ha creado todo automáticamente

### Paso 3 — Verificar Build Settings

1. Ve a **File > Build Settings**
2. Comprueba que aparecen estas escenas:
   - `Scenes/MainMenu` — índice **0** (se carga primero)
   - `Scenes/SampleScene` — índice **1**
3. Si no aparecen, arrastra las escenas desde `Assets/Scenes/` a la lista

### Paso 4 — Probar el menú

1. Abre la escena `Assets/Scenes/MainMenu.unity`
2. Dale a **Play** ▶️
3. Prueba los botones:
   - **JUGAR** → Te lleva a SampleScene
   - **OPCIONES** → Abre el panel de ajustes
   - **SALIR** → Cierra el juego (sale del Play Mode en el editor)

### Paso 5 — Personalizar (Opcional)

#### Cambiar el título del juego
1. En la escena MainMenu, busca `Canvas_Menu > Panel_MenuPrincipal > PanelCentral > Titulo`
2. Cambia el texto "MI JUEGO" por el nombre de tu juego

#### Cambiar colores
1. Los colores se definen en `Assets/Scripts/Editor/MainMenuSetupEditor.cs`
2. Modifica las variables `COLOR_FONDO`, `COLOR_BOTON`, `COLOR_TITULO`, etc.
3. Vuelve a ejecutar **Tools > Crear Menú Principal** para regenerar

---

## ¿Qué incluye el menú de Opciones?

| Ajuste | Control | Descripción |
|--------|---------|-------------|
| Volumen General | Slider | Controla el volumen del audio (0 a 1) |
| Sensibilidad Ratón | Slider | Sensibilidad del ratón (0.1 a 10) |
| Calidad Gráfica | Dropdown | Niveles de calidad de Unity |
| Pantalla Completa | Toggle | Activa/desactiva pantalla completa |
| Resolución | Dropdown | Selecciona la resolución de pantalla |

Todos los ajustes se **guardan automáticamente** y se mantienen entre sesiones.

---

## Estructura de archivos creados

```
Assets/Scripts/
├── Menu/
│   ├── MainMenuController.cs    ← Lógica del menú
│   └── SettingsManager.cs       ← Gestión de ajustes
├── Editor/
│   └── MainMenuSetupEditor.cs   ← Genera la escena del menú
Assets/Scenes/
└── MainMenu.unity               ← Se crea al ejecutar el script
```

## Solución de problemas

| Problema | Solución |
|----------|----------|
| No aparece "Tools > Crear Menú Principal" | Espera a que Unity compile los scripts. Revisa la consola por errores |
| El botón Jugar no hace nada | Verifica que SampleScene está en Build Settings (índice 1) |
| Los ajustes no se guardan | Asegúrate de que el objeto SettingsManager existe en la escena |
