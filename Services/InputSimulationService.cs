using System.Runtime.InteropServices;
using TxtTyper.Helpers;
using TxtTyper.Services.Interfaces;

namespace TxtTyper.Services;

public sealed class InputSimulationService : IInputSimulationService
{
    private const uint InputKeyboard = 1;
    private const uint KeyeventfExtendedKey = 0x0001;
    private const uint KeyeventfKeyUp = 0x0002;
    private const uint KeyeventfUnicode = 0x0004;

    private static readonly HashSet<ushort> ExtendedKeys =
    [
        VirtualKeys.Up,
        VirtualKeys.Down,
        VirtualKeys.Left,
        VirtualKeys.Right,
        VirtualKeys.Home,
        VirtualKeys.End,
        VirtualKeys.PageUp,
        VirtualKeys.PageDown,
        VirtualKeys.Delete
    ];

    public async Task TypeTextAsync(string text, int characterDelayMilliseconds, CancellationToken cancellationToken)
    {
        foreach (var character in text)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (character)
            {
                case '\r':
                    continue;
                case '\n':
                    SendVirtualKeyPress(VirtualKeys.Enter);
                    break;
                case '\t':
                    SendVirtualKeyPress(VirtualKeys.Tab);
                    break;
                default:
                    SendUnicodeCharacter(character);
                    break;
            }

            if (characterDelayMilliseconds > 0)
            {
                await DelayAsync(characterDelayMilliseconds, cancellationToken);
            }
        }
    }

    public Task SendKeyChordAsync(IReadOnlyList<ushort> virtualKeys, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (virtualKeys.Count == 0)
        {
            return Task.CompletedTask;
        }

        SendKeyChordInternal(virtualKeys);
        return Task.CompletedTask;
    }

    public Task DelayAsync(int delayMilliseconds, CancellationToken cancellationToken)
    {
        return delayMilliseconds > 0
            ? Task.Delay(delayMilliseconds, cancellationToken)
            : Task.CompletedTask;
    }

    private static void SendVirtualKeyPress(ushort virtualKey)
    {
        SendInputs(
        [
            CreateVirtualKeyInput(virtualKey, keyUp: false),
            CreateVirtualKeyInput(virtualKey, keyUp: true)
        ]);
    }

    private static void SendKeyChordInternal(IReadOnlyList<ushort> virtualKeys)
    {
        var inputs = new List<Input>(virtualKeys.Count * 2);

        for (var index = 0; index < virtualKeys.Count - 1; index++)
        {
            inputs.Add(CreateVirtualKeyInput(virtualKeys[index], keyUp: false));
        }

        var finalKey = virtualKeys[^1];
        inputs.Add(CreateVirtualKeyInput(finalKey, keyUp: false));
        inputs.Add(CreateVirtualKeyInput(finalKey, keyUp: true));

        for (var index = virtualKeys.Count - 2; index >= 0; index--)
        {
            inputs.Add(CreateVirtualKeyInput(virtualKeys[index], keyUp: true));
        }

        SendInputs(inputs.ToArray());
    }

    private static void SendUnicodeCharacter(char character)
    {
        SendInputs(
        [
            CreateUnicodeInput(character, keyUp: false),
            CreateUnicodeInput(character, keyUp: true)
        ]);
    }

    private static Input CreateVirtualKeyInput(ushort virtualKey, bool keyUp)
    {
        var flags = keyUp ? KeyeventfKeyUp : 0u;
        if (ExtendedKeys.Contains(virtualKey))
        {
            flags |= KeyeventfExtendedKey;
        }

        return new Input
        {
            Type = InputKeyboard,
            Union = new InputUnion
            {
                Keyboard = new KeybdInput
                {
                    VirtualKey = virtualKey,
                    Flags = flags,
                    ExtraInfo = NativeMethods.GetMessageExtraInfo()
                }
            }
        };
    }

    private static Input CreateUnicodeInput(char character, bool keyUp)
    {
        return new Input
        {
            Type = InputKeyboard,
            Union = new InputUnion
            {
                Keyboard = new KeybdInput
                {
                    ScanCode = character,
                    Flags = KeyeventfUnicode | (keyUp ? KeyeventfKeyUp : 0u),
                    ExtraInfo = NativeMethods.GetMessageExtraInfo()
                }
            }
        };
    }

    private static void SendInputs(Input[] inputs)
    {
        var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        if (sent != (uint)inputs.Length)
        {
            throw new InvalidOperationException(
                $"Windows rejected one or more simulated key events (Win32 error {Marshal.GetLastWin32Error()}).");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KeybdInput Keyboard;

        [FieldOffset(0)]
        public MouseInput Mouse;

        [FieldOffset(0)]
        public HardwareInput Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeybdInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareInput
    {
        public uint Message;
        public ushort ParameterLow;
        public ushort ParameterHigh;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint numberOfInputs, Input[] inputs, int sizeOfInputStructure);

        [DllImport("user32.dll")]
        public static extern IntPtr GetMessageExtraInfo();
    }
}
