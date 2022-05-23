using System.Runtime.InteropServices;
using JetBrains.Annotations;
// ReSharper disable BuiltInTypeReferenceStyle

namespace QuickTrack.Win32;

public static partial class Interop
{
    [PublicAPI]
    public static class Raw
    {
        
        /// <summary>
        /// Synthesizes keystrokes, mouse motions, and button clicks.
        /// </summary>
        /// <param name="cInputs">The number of structures in the pInputs array.</param>
        /// <param name="pInputs">An array of INPUT structures. Each structure represents an event to be inserted into the keyboard or mouse input stream.</param>
        /// <param name="cbSize">The size, in bytes, of an INPUT structure. If cbSize is not the size of an INPUT structure, the function fails.</param>
        /// <returns>
        /// The function returns the number of events that it successfully inserted into the keyboard or mouse input stream.
        /// If the function returns zero, the input was already blocked by another thread.
        /// To get extended error information, call GetLastError.
        /// This function fails when it is blocked by UIPI.
        /// Note that neither GetLastError nor the return value will indicate the failure was caused by UIPI blocking.
        /// </returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern UInt32 SendInput(
            [MarshalAs(UnmanagedType.U4)] UInt32 cInputs,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
            INPUT[] pInputs,
            [MarshalAs(UnmanagedType.I4)] int cbSize
        );

        /// <summary>
        /// Translates (maps) a virtual-key code into a scan code or character value, or translates a scan code into a virtual-key code.
        /// To specify a handle to the keyboard layout to use for translating the specified code, use the MapVirtualKeyEx function.
        /// </summary>
        /// <param name="uCode">
        /// The virtual key code or scan code for a key. How this value is interpreted depends on the value of the uMapType parameter.
        /// Starting with Windows Vista, the high byte of the uCode value can contain either 0xe0 or 0xe1 to specify the extended scan code.
        /// </param>
        /// <param name="uMapType">
        /// The translation to be performed. The value of this parameter depends on the value of the uCode parameter.
        /// <list type="bullet">
        /// <item>
        ///     MAPVK_VK_TO_VSC 0
        ///     <para>
        ///         The uCode parameter is a virtual-key code and is translated into a scan code.
        ///         If it is a virtual-key code that does not distinguish between left- and right-hand keys,
        ///         the left-hand scan code is returned.
        ///         If there is no translation, the function returns 0.
        ///     </para>
        /// </item>
        /// <item>
        ///     MAPVK_VSC_TO_VK 1
        ///     <para>
        ///         The uCode parameter is a scan code and is translated into a virtual-key code
        ///         that does not distinguish between left- and right-hand keys.
        ///         If there is no translation, the function returns 0.
        ///     </para>
        /// </item>
        /// <item>
        ///     MAPVK_VK_TO_CHAR 2
        ///     <para>
        ///         The uCode parameter is a virtual-key code and is translated into an un-shifted character value
        ///         in the low order word of the return value.
        ///         Dead keys (diacritics) are indicated by setting the top bit of the return value.
        ///         If there is no translation, the function returns 0.
        ///     </para>
        /// </item>
        /// <item>
        ///     MAPVK_VSC_TO_VK_EX 3
        ///     <para>
        ///         The uCode parameter is a scan code and is translated into a virtual-key code
        ///         that distinguishes between left- and right-hand keys.
        ///         If there is no translation, the function returns 0.
        ///     </para>
        /// </item>
        /// <item>
        ///     MAPVK_VK_TO_VSC_EX 4
        ///     <para>
        ///         Windows Vista and later:
        ///         The uCode parameter is a virtual-key code and is translated into a scan code.
        ///         If it is a virtual-key code that does not distinguish between left- and right-hand keys,
        ///         the left-hand scan code is returned.
        ///         If the scan code is an extended scan code,
        ///         the high byte of the uCode value can contain either 0xe0 or 0xe1 to specify the extended scan code.
        ///         If there is no translation, the function returns 0.
        ///     </para>
        /// </item>
        /// </list>
        /// </param>
        /// <returns>
        /// The return value is either a scan code,
        /// a virtual-key code, or a character value,
        /// depending on the value of uCode and uMapType.
        /// If there is no translation, the return value is zero.
        /// </returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern UInt32 MapVirtualKeyA(
            [MarshalAs(UnmanagedType.U4)] UInt32 uCode,
            [MarshalAs(UnmanagedType.U4)] UInt32 uMapType);

        public static int GetLastError()
        {
            return Marshal.GetLastWin32Error();
        }
    }
}