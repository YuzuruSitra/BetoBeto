using System;
using System.Collections;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BetoBeto.Tests
{
    /// <summary>Use the actual Input System event path without depending on editor focus.</summary>
    sealed class PadTestInput : IDisposable
    {
        public readonly Gamepad Device;
        readonly InputSettings.BackgroundBehavior background;
        readonly InputSettings.EditorInputBehaviorInPlayMode editorInput;
        readonly Gamepad[] existingPads;
        readonly Gamepad previousCurrent;
        public PadTestInput()
        {
            background = InputSystem.settings.backgroundBehavior;
            editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            previousCurrent = Gamepad.current;
            existingPads = Gamepad.all.Where(p => p.enabled).ToArray();
            foreach (var existing in existingPads) InputSystem.DisableDevice(existing);
            Device = InputSystem.AddDevice<Gamepad>();
            State(new GamepadState());
        }
        public void State(GamepadState state)
        {
            Device.MakeCurrent(); InputSystem.QueueStateEvent(Device, state);
        }
        public IEnumerator Press(GamepadButton button)
        {
            State(new GamepadState().WithButton(button));
            yield return null; yield return null;
            State(new GamepadState());
            yield return null; yield return null;
        }
        public void Dispose()
        {
            if (Device.added) InputSystem.RemoveDevice(Device);
            foreach (var existing in existingPads) if (existing.added) InputSystem.EnableDevice(existing);
            if (previousCurrent != null && previousCurrent.added && previousCurrent.enabled) previousCurrent.MakeCurrent();
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
        }
    }
}
