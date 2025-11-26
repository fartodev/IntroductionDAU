# Dead Among Us
### A Performance-First Zombie Survival Framework in Unity (HDRP)

> **⚠️ PROJECT STATUS: PRIVATE DEVELOPMENT / CODE SHOWCASE**
>
> **Dead Among Us** is currently in active development and intended for commercial release.
>
> This repository is made public specifically as a **technical portfolio** to demonstrate software architecture, optimization techniques, and AI systems. It represents a vertical slice of the engineering work. The full game assets, narrative content, and commercial build are proprietary and not licensed for public distribution or reuse.

---

## 📖 Overview

**Dead Among Us** is a TWD-inspired survival horror game built in Unity (HDRP). While the gameplay focuses on organic herd formation and resource scarcity, the *codebase* is a demonstration of scalable game architecture.

This project was engineered to solve specific technical challenges:
* **Zero Garbage Collection (GC)** allocation in core gameplay loops.
* **Modular AI** using SOLID principles and State Patterns.
* **Event-Driven Architecture** for high-performance scaling.

---

## 🏗️ Technical Architecture

### 1. Finite State Machine (Polymorphic Design)
Instead of using a standard `switch` statement or `enum` for AI behavior, I implemented a modular, **class-based State Machine** utilizing the **State Pattern**.

* **Interface-Driven:** Defined an `IZombieState` contract with `Enter`, `Execute`, and `Exit` lifecycle methods.
* **SOLID Principles:** Adheres to the Open/Closed principle—new behaviors (e.g., *StunnedState*) can be added without modifying the core controller.
* **Separation of Concerns:** Each state (Idle, Investigating, Chasing) is encapsulated in its own class, making logic independently testable.

```csharp
// The Controller uses polymorphism to run logic
private IZombieState currentState;

void Update() {
    // Cleaner and more scalable than a massive switch statement
    currentState?.Execute(this);
}
2. Event-Driven "Observer" Architecture
To decouple systems, I utilized the Observer Pattern via C# Actions and Delegates. Systems do not poll each other; they react to events.

Hearing System: The NoiseEmitter does not know about zombies. It simply fires a static event OnNoiseCreated. Zombies subscribe to this event to react instantly.

Day/Night Cycle: Instead of 100+ zombies polling the DayNightCycle singleton every frame to check if it's night, they subscribe to OnDayNightChanged. This fires only twice per 30-minute cycle, resulting in a 54,000x reduction in checks compared to polling.

Memory Management: Strict adherence to OnDestroy unsubscription prevents memory leaks.

⚡ Performance & Optimization
The core goal was to support 50-100+ active agents while maintaining 60 FPS.

"Zero GC" Implementation
Garbage Collection spikes are the enemy of smooth gameplay.

No Coroutines: I avoided IEnumerator and WaitForSeconds (which generate garbage). Instead, I utilized manual timer tracking in Update loops.

Object Pooling: Implemented Unity.ObjectPool for noise spheres and particle effects to prevent instantiation lag.

NonAlloc Physics: Used Physics.OverlapSphereNonAlloc with reused arrays to avoid allocating new collider arrays during vision checks.

Load Spreading (Interval-Based AI)
To prevent frame spikes where all 50 zombies calculate paths simultaneously:

AI decisions occur on a 0.3s interval, not every frame.

Randomized Initialization: Each zombie's internal timer is initialized with a random offset (Random.Range). This distributes the CPU load evenly across frames, ensuring a smooth frame time budget of ~0.11ms per frame for AI.

🧠 Sensory Systems
Trigger-Based Scent Detection
I implemented a priority-based "Scent" system that supports future extensions (like breadcrumb pathfinding).

Physics-Driven: Uses SphereCollider triggers to detect ScentSource components automatically.

Priority Logic: The AI evaluates scent "strength." A blood pool (Strength 3) overrides the player's natural scent (Strength 1).

Hysteresis: Includes a memory system where zombies "remember" a scent for 5 seconds after exiting the trigger to prevent jittery behavior.

HDRP Lighting Integration
The Day/Night cycle controls HDRP light intensity (100k lux vs 1k lux) and fog density.

Zombie vision range changes dynamically (15m Day / 5m Night) via the event system.

🛠️ Tech Stack
Engine: Unity 2021+ (HDRP Pipeline)

Language: C#

Patterns: State, Observer, Singleton, Object Pool, Strategy

Tools: Unity Profiler, ProBuilder, NavMesh Components

🔮 Future Roadmap
Breadcrumb Pathfinding: Upgrading the smell system to follow a linked-list of "scent nodes" for realistic trailing.

Combat System: Melee weapons with stamina costs and directional damage.

Job System: Moving vision raycasts to the Unity Job System for multi-threaded performance.

## 👤 Developer

**Can** - Full-Stack Game Developer

I specialize in building scalable game architectures and performance-critical systems.

[GitHub Profile](https://github.com/fartodev)