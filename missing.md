# Elementos Faltantes (Missing Features)

Basado en la lista de requisitos proporcionada y el progreso actual:

## 1. Enemigos e IA
*   [x] **Tipos de Enemigo (Shooter/Variante)**: Implementados Tigres de Fuego y Jefes.
*   [x] **Comportamiento Distinto**: Lógica de disparo y persecución diferenciada implementada.

## 2. Progreso y Puntuación
*   [ ] **Multiplicador de Puntuación**: Falta el sistema de "Combo/Racha".
*   [x] **Persistencia (High Score)**: Implementado guardado de mejores puntuaciones.

## 3. Audio y Feedback
*   [x] **AudioMixer**: Implementado `SoundManager`.
*   [x] **SFX Específicos**: Hooks añadidos.

## 4. Arquitectura y Calidad de Código
*   [x] **ScriptableObjects (Data)**: Implementado `EnemyData`.
*   [x] **Object Pooling**: Implementado `ObjectPoolManager`.
*   [ ] **Sistema de Eventos**: Recomendado para desacoplar UI/GameManager (Parcialmente manejado via GameManager Singleton).

## 5. UI / UX Extras
*   [x] **Menú de Opciones**: Implementado.
*   [x] **Pausa Real**: Implementada con detención de corrutinas y TimeScale.
*   [x] **Pantalla de Controles**: Implementada dentro del menú de Opciones.
*   [x] **Inventario y Torreta**: Sistema de Slots y Torreta desplegable implementados.

## Resumen de Prioridades Actuales
1.  **Multiplicador / Combo** (Opcional).
2.  **Pulido Visual** (Feedback de daño, partículas).