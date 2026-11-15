//=======================================================================
// DefaultSaver.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler;

/// <summary>
/// Provides a reusable save/apply lifecycle base for explicit state transfer helpers.
/// </summary>
/// <remarks>
/// Host integrations can use these hooks to transfer settings and configuration
/// between a library and an editor or game engine. The host owns when each phase
/// runs; this base class has no engine dependency.
/// </remarks>
public abstract class DefaultSaver
{
    /// <summary>Capture the state to transfer.</summary>
    /// <remarks>Calls <see cref="OnSave"/> synchronously. The host decides when to run the phase.</remarks>
    public void Save()
    {
        OnSave();
    }

    /// <summary>Capture the state to transfer.</summary>
    /// <remarks>Override to implement <see cref="Save"/> for the host. The default hook has no effect.</remarks>
    protected virtual void OnSave() { }

    /// <summary>Prepare the destination for applying state.</summary>
    /// <remarks>Calls <see cref="OnEarlyApply"/> synchronously. The host decides when to run the phase.</remarks>
    public void EarlyApply()
    {
        OnEarlyApply();
    }

    /// <summary>Prepare the destination for applying state.</summary>
    /// <remarks>Override to implement <see cref="EarlyApply"/> for the host. The default hook has no effect.</remarks>
    protected virtual void OnEarlyApply() { }

    /// <summary>Apply the transferred state.</summary>
    /// <remarks>Calls <see cref="OnApply"/> synchronously. The host decides when to run the phase.</remarks>
    public void Apply()
    {
        OnApply();
    }

    /// <summary>Apply the transferred state.</summary>
    /// <remarks>Override to implement <see cref="Apply"/> for the host. The default hook has no effect.</remarks>
    protected virtual void OnApply() { }

    /// <summary>Finish applying state and reconnect dependent host state.</summary>
    /// <remarks>Calls <see cref="OnLateApply"/> synchronously. The host decides when to run the phase.</remarks>
    public void LateApply()
    {
        OnLateApply();
    }

    /// <summary>Finish applying state and reconnect dependent host state.</summary>
    /// <remarks>Override to implement <see cref="LateApply"/> for the host. The default hook has no effect.</remarks>
    protected virtual void OnLateApply() { }
}
