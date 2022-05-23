using JetBrains.Annotations;

namespace QuickTrack.Win32;

public static partial class Interop
{
    [PublicAPI]
    public static class SendKeyboardInput
    {
        /// <summary>
        /// For each character, send the unicode key code
        /// </summary>
        public static void Char(params char[] unicodeCharacters)
        {
            if (!unicodeCharacters.Any())
                throw new ArgumentException("At least one keycode has to be provided", nameof(unicodeCharacters));
            unsafe
            {
                var result = Raw.SendInput(
                    (uint) unicodeCharacters.Length * 2,
                    unicodeCharacters.Select((c) =>
                        new INPUT
                        {
                            Type = (int) EInputType.Keyboard,
                            Data = new INPUTUNION
                            {
                                KeyboardInput = new KEYBDINPUT
                                {
                                    wVk = 0,
                                    wScan = (short) c,
                                    dwFlags = (int) EKeyBdInputFlag.Unicode,
                                },
                            }
                        }).ToArray(),
                    sizeof(INPUT));
            }
        }

        private static bool RequiresExtendedFlag(EVirtualKeyCode virtualKeyCode)
        {
            return virtualKeyCode switch
            {
                EVirtualKeyCode.UpArrow => true,
                EVirtualKeyCode.DownArrow => true,
                EVirtualKeyCode.LeftArrow => true,
                EVirtualKeyCode.RightArrow => true,
                EVirtualKeyCode.Home => true,
                EVirtualKeyCode.End => true,
                EVirtualKeyCode.Prior => true,
                EVirtualKeyCode.Next => true,
                EVirtualKeyCode.Insert => true,
                EVirtualKeyCode.Delete => true,
                _ => false,
            };
        }

        /// <summary>
        /// For each key, sends a key-down event.
        /// </summary>
        public static void KeyDown(params EVirtualKeyCode[] virtualKeyCodes)
        {
            if (!virtualKeyCodes.Any())
                throw new ArgumentException("At least one keycode has to be provided", nameof(virtualKeyCodes));
            unsafe
            {
                var result = Raw.SendInput(
                    (uint) virtualKeyCodes.Length,
                    virtualKeyCodes.Select((vk) =>
                        new INPUT
                        {
                            Type = (int) EInputType.Keyboard,
                            Data = new INPUTUNION
                            {
                                KeyboardInput = new KEYBDINPUT
                                {
                                    wVk = (short) vk,
                                    dwFlags = RequiresExtendedFlag(vk)
                                        ? (int) EKeyBdInputFlag.ExtendedKey
                                        : (int) EKeyBdInputFlag.Empty,
                                }
                            }
                        }).ToArray(),
                    sizeof(INPUT));
            }
        }

        /// <summary>
        /// For each key, sends a key-down and key-up event.
        /// </summary>
        public static void KeyPress(params EVirtualKeyCode[] virtualKeyCodes)
        {
            if (!virtualKeyCodes.Any())
                throw new ArgumentException("At least one keycode has to be provided", nameof(virtualKeyCodes));
            unsafe
            {
                var result = Raw.SendInput(
                    (uint) virtualKeyCodes.Length * 2,
                    virtualKeyCodes.SelectMany((vk) =>
                        new[]
                        {
                            new INPUT
                            {
                                Type = (int) EInputType.Keyboard,
                                Data = new INPUTUNION
                                {
                                    KeyboardInput = new KEYBDINPUT
                                    {
                                        wVk = (short) vk,
                                        dwFlags = RequiresExtendedFlag(vk)
                                            ? (int) EKeyBdInputFlag.ExtendedKey
                                            : (int) EKeyBdInputFlag.Empty,
                                    }
                                }
                            },
                            new INPUT
                            {
                                Type = (int) EInputType.Keyboard,
                                Data = new INPUTUNION
                                {
                                    KeyboardInput = new KEYBDINPUT
                                    {
                                        wVk = (short) vk,
                                        dwFlags = RequiresExtendedFlag(vk)
                                            ? (int) (EKeyBdInputFlag.ExtendedKey | EKeyBdInputFlag.KeyUp)
                                            : (int) EKeyBdInputFlag.KeyUp,
                                    }
                                }
                            }
                        }).ToArray(),
                    sizeof(INPUT));
            }
        }

        /// <summary>
        /// For each key, sends a key-up event.
        /// </summary>
        public static void KeyUp(params EVirtualKeyCode[] virtualKeyCodes)
        {
            if (!virtualKeyCodes.Any())
                throw new ArgumentException("At least one keycode has to be provided", nameof(virtualKeyCodes));
            unsafe
            {
                var result = Raw.SendInput(
                    (uint) virtualKeyCodes.Length,
                    virtualKeyCodes.Select((vk) =>
                        new INPUT
                        {
                            Type = (int) EInputType.Keyboard,
                            Data = new INPUTUNION
                            {
                                KeyboardInput = new KEYBDINPUT
                                {
                                    wVk = (short) vk,
                                    dwFlags = RequiresExtendedFlag(vk)
                                        ? (int) (EKeyBdInputFlag.ExtendedKey | EKeyBdInputFlag.KeyUp)
                                        : (int) EKeyBdInputFlag.KeyUp,
                                }
                            }
                        }).ToArray(),
                    sizeof(INPUT));
            }
        }
    }
}