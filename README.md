![ILERNA TOP DOWN](Assets/README_IMG/titulo.png)

### Eres un **JAMÓN** (sí, has leído bien) encerrado en una jaula. Pero no estás solo: una horda infinita de tigres hambrientos intenta devorarte mientras una multitud eufórica observa el espectáculo desde las gradas, gritando y saltando como si fuera la final de un torneo mortal. 

Tu única defensa es tu arma y tu capacidad para moverte al ritmo de la música.

![](assets/README_IMG/Captura%20de%20pantalla%202026-01-15%20115431.png)

---

## 🎮 Controles

| Acción | Tecla / Input |
| :--- | :--- |
| **Moverse** | `W`, `A`, `S`, `D` o Flechas |
| **Apuntar** | Ratón (Cursor) |
| **Disparar** | `Click Izquierdo` (Automático según BPM) |
| **Dash (Esquiva)** | `Espacio` |
| **Seleccionar Item** | `Rueda del Ratón` (Scroll) |
| **Usar Item (Time Stop)** | `Click Izquierdo` (Si Slot seleccionado) o `Q` |
| **Desplegar Torreta** | `Click Derecho` (Si Slot 5 seleccionado) |
| **Bomba Nuclear** | `Click Derecho` (Si está cargada y no hay torreta) |
| **Pausa** | `ESC` |

---

## 🕹️ Mecánicas de Juego

### 🎵 Sincronización Musical
Todo en el juego ocurre al ritmo de **"Professional Griefers" de Deadmau5** (128 BPM).
- **Disparos**: Tus proyectiles se disparan automáticamente en sincronía con el beat (128 BPM).
- **Aparición**: Los tigres comienzan a salir de sus jaulas justo cuando rompe la música.
- **Ambiente**: Las luces y el público reaccionan a la intensidad de la canción.

### 🕺 Disco Mode
Cuando llega el estribillo, la arena se transforma:
- **Iluminación**: Las luces cambian de color y parpadean al ritmo de la música.
- **Público**: La multitud en las gradas salta más rápido y con más energía.
- **Efecto Visual**: Un efecto de "estroboscopio" negro intenta distraerte, dificultando la visión pero aumentando la adrenalina.

### 👹 Enemigos

![](assets/README_IMG/tiger.png)

No todos los tigres son iguales. Prepárate para enfrentar a:

1.  **Tigres Normales**: 
    - Atacan cuerpo a cuerpo.
    - Su velocidad y vida aumentan progresivamente con cada ronda.
    - Te dañan al tocarte.
2.  **Tigres de Fuego (Fire Tigers)**:
    - Son **más grandes** y de color **negro**.
    - Se mueven **más lento** y no intentan alcanzarte directamente.
    - Se mantienen a distancia para dispararte bolas de fuego.
3.  **Jefes (Boss)**:
    - Tigres **ENORMES**.
    - Atacan cuerpo a cuerpo con una fuerza devastadora.
    - Tienen una vida inmensa.
    - **Peligro**: Rompen tu escudo de un solo golpe y te quitan **3 vidas** de un impacto.

---

## ⚡ Power-Ups y Habilidades

Los enemigos pueden soltar mejoras temporales para ayudarte a sobrevivir. Tienes un **Inventario de 5 Slots** en la parte inferior para ver qué tienes activo.

- **❤️ Salud**: Recupera parte de tu vida.
- **🛡️ Escudo (Slot 2)**: Te hace invulnerable temporalmente (hasta 5 golpes).
- **⚡ Velocidad (Slot 1)**: Aumenta drásticamente tu velocidad de movimiento para huir de los tigres.
- **⏱️ Time Stop (Slot 3)**: Al recogerlo, almacenas una carga. Selecciónalo y pulsa `Click Izquierdo` (o `Q`) para congelar a todos los enemigos.
- **🔫 Torreta Automática (Slot 5)**: Se almacena en tu inventario. Al seleccionarla, verás un **holograma** a tu lado. Pulsa `Click Derecho` para desplegarla. Tiene su propia salud y dispara a los enemigos automáticamente.
- **💣 Bomba Nuclear**: Se carga automáticamente cada **100 bajas**. Al usarla (`Click Derecho`), eliminas a todos los enemigos de la pantalla. ¡La bomba reaparece en el mundo como item si no la has usado!

---

## 🛠️ Detalles Técnicos
- **Desarrollado en**: Unity 2022/2023.
- **Lenguaje**: C#.
- **Arquitectura**:
  - **GameManager**: Gestión centralizada del estado del juego.
  - **WaveSpawner**: Sistema de oleadas procedimental con escalado de dificultad.
  - **Object Pooling**: Optimización de rendimiento para proyectiles y enemigos.
  - **ScriptableObjects**: Configuración modular de datos de enemigos (`EnemyData`).
  - **[Diagrama de Clases (Ejercicio RPG)](RPG_Class_Diagram.md)**: Modelo UML para actividad académica.

---

**Autor**: Alberto
**Versión**: 1.0
