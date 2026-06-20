//=======================================================================
// DefaultSaver.cs
//=======================================================================
// MIT License, Copyright (c) 2024–present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler;

/// <summary>
/// Provides a reusable save/apply lifecycle base for explicit state transfer helpers.
/// </summary>
public abstract class DefaultSaver
{
    /// <summary>
    /// Runs the save phase.
    /// </summary>
    public void Save()
    {
        OnSave();
    }

    /// <summary>
    /// Called by <see cref="Save"/>.
    /// </summary>
    protected virtual void OnSave() { }

    /// <summary>
    /// Runs the early apply phase.
    /// </summary>
    public void EarlyApply()
    {
        OnEarlyApply();
    }

    /// <summary>
    /// Called by <see cref="EarlyApply"/>.
    /// </summary>
    protected virtual void OnEarlyApply() { }

    /// <summary>
    /// Runs the apply phase.
    /// </summary>
    public void Apply()
    {
        OnApply();
    }

    /// <summary>
    /// Called by <see cref="Apply"/>.
    /// </summary>
    protected virtual void OnApply() { }

    /// <summary>
    /// Runs the late apply phase.
    /// </summary>
    public void LateApply()
    {
        OnLateApply();
    }

    /// <summary>
    /// Called by <see cref="LateApply"/>.
    /// </summary>
    protected virtual void OnLateApply() { }
}
