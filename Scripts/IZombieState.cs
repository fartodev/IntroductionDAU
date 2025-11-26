using UnityEngine;

/// <summary>
/// Base interface for zombie AI states.
/// Implements the State Pattern for clean, modular behavior architecture.
/// Each state handles its own logic independently.
/// </summary>
public interface IZombieState
{
    /// <summary>
    /// Called once when entering this state.
    /// Use for initialization, playing sounds, setting animations, etc.
    /// </summary>
    void Enter(ZombieAI zombie);

    /// <summary>
    /// Called every frame while in this state.
    /// Contains the main state logic.
    /// </summary>
    void Execute(ZombieAI zombie);

    /// <summary>
    /// Called once when exiting this state.
    /// Use for cleanup, stopping sounds, resetting values, etc.
    /// </summary>
    void Exit(ZombieAI zombie);

    /// <summary>
    /// Returns the state name for debugging.
    /// </summary>
    string GetStateName();
}
