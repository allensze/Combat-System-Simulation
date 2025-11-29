# Combat System Simulation

## DESCRIPTION
I built a custom 2.5D side-scrolling combat prototype in Unity featuring my first enemy AI state machine and a Gears of War–style active reload that rewards precise timing. By logging enemy actions and combat events, I used basic telemetry to tweak attack patterns, timings, and difficulty until encounters felt fair and responsive. This project became my sandbox for learning gameplay systems, AI behavior, and data-driven combat balancing.

## SHOWCASE

### Combat Wave & Telemetry Orchestrator
#### [CombatManager.cs](https://github.com/allensze/Combat-System-Simulation/blob/main/CombatManager.cs)
- Manages game modes, waves, spawning, telemetry, and autoplay integration.

### Adaptive Smart Autoplay AI (Telemetry-driven player AI)
#### [AutoplayStateManager.cs](https://github.com/allensze/Combat-System-Simulation/blob/main/AutoplayStateManager.cs)
- Shows state pattern and an adaptive Smart AI that reacts to telemetry.

### Player Ability & Targeting System
#### [PlayerActions.cs](https://github.com/allensze/Combat-System-Simulation/blob/main/PlayerActions.cs)
- Includes input handling, ability system, cooldown timers, smart target selection, and autoplay hooks.
