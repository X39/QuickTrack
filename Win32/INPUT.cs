using System.Runtime.InteropServices;
using JetBrains.Annotations;

// ReSharper disable IdentifierTypo
// ReSharper disable EnumUnderlyingTypeIsInt

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable InconsistentNaming
// ReSharper disable BuiltInTypeReferenceStyle

namespace QuickTrack.Win32;

[PublicAPI]
public struct INPUT
{
    public Int32 Type;
    public INPUTUNION Data;
}

[PublicAPI]
public enum EInputType : Int32
{
    /// <summary>
    /// The event is a mouse event. Use the mi structure of the union.
    /// </summary>
    Mouse = 0,

    /// <summary>
    /// The event is a keyboard event. Use the ki structure of the union.
    /// </summary>
    Keyboard = 1,

    /// <summary>
    /// The event is a hardware event. Use the hi structure of the union.
    /// </summary>
    Hardware = 2,
}

[PublicAPI]
[StructLayout(LayoutKind.Explicit)]
public struct INPUTUNION
{
    [FieldOffset(0)] public MOUSEINPUT MouseInput;
    [FieldOffset(0)] public KEYBDINPUT KeyboardInput;
    [FieldOffset(0)] public HARDWAREINPUT HardwareInput;
}

[PublicAPI]
[StructLayout(LayoutKind.Sequential)]
public struct MOUSEINPUT
{
    /// <remarks>
    /// LONG dx
    /// </remarks>
    public Int32 dx;

    /// <remarks>
    /// LONG dy
    /// </remarks>
    public Int32 dy;

    /// <remarks>
    /// DWORD mouseData
    /// </remarks>
    public UInt32 mouseData;

    /// <remarks>
    /// DWORD dwFlags
    /// </remarks>
    public UInt32 dwFlags;

    /// <remarks>
    /// DWORD time
    /// </remarks>
    public UInt32 time;

    /// <remarks>
    /// ULONG_PTR dwExtraInfo
    /// </remarks>
    public IntPtr dwExtraInfo;
    /*
  LONG      dx;
  LONG      dy;
  DWORD     mouseData;
  DWORD     dwFlags;
  DWORD     time;
  ULONG_PTR dwExtraInfo;
     */
}

[PublicAPI]
[StructLayout(LayoutKind.Sequential)]
public struct KEYBDINPUT
{
    /// <remarks>
    /// WORD wVk
    /// </remarks>
    public Int16 wVk;

    /// <remarks>
    /// WORD      wScan
    /// </remarks>
    public Int16 wScan;

    /// <remarks>
    /// DWORD     dwFlags
    /// </remarks>
    public Int32 dwFlags;

    /// <remarks>
    /// DWORD     time
    /// </remarks>
    public Int32 time;

    /// <remarks>
    /// ULONG_PTR dwExtraInfo
    /// </remarks>
    public IntPtr dwExtraInfo;
}

[Flags]
[PublicAPI]
public enum EKeyBdInputFlag : Int32
{
    Empty = 0,

    /// <sumary>
    /// If specified, the scan code was preceded by a prefix byte that has the value 0xE0 (224).
    /// </sumary>
    /// <remarks>
    /// KEYEVENTF_EXTENDEDKEY
    /// </remarks>
    ExtendedKey = 0x0001,

    /// <sumary>
    /// If specified, the key is being released. If not specified, the key is being pressed.
    /// </sumary>
    /// <remarks>
    /// KEYEVENTF_KEYUP
    /// </remarks>
    KeyUp = 0x0002,

    /// <sumary>
    /// If specified, the system synthesizes a PACKET keystroke.
    /// The wVk parameter must be zero.
    /// This flag can only be combined with the <see cref="KeyUp"/> flag.
    /// For more information, see the Remarks section.
    /// </sumary>
    /// <remarks>
    /// KEYEVENTF_UNICODE
    /// </remarks>
    Unicode = 0x0004,

    /// <sumary>
    /// If specified, wScan identifies the key and wVk is ignored.
    /// </sumary>
    /// <remarks>
    /// KEYEVENTF_SCANCODE
    /// </remarks>
    ScanCode = 0x0008,
}

[PublicAPI]
[StructLayout(LayoutKind.Sequential)]
public struct HARDWAREINPUT
{
    /// <remarks>
    /// DWORD uMsg
    /// </remarks>
    public Int32 uMsg;

    /// <remarks>
    /// WORD  wParamL
    /// </remarks>
    public Int16 wParamL;

    /// <remarks>
    /// WORD  wParamH
    /// </remarks>
    public Int16 wParamH;
}

[PublicAPI]
public enum EVirtualKeyCode
{
    ///<sumary>
    ///Left mouse button
    ///</sumary>
    LBUTTON = 0x01,

    ///<sumary>
    ///Right mouse button
    ///</sumary>
    RBUTTON = 0x02,

    ///<sumary>
    ///Control-break processing
    ///</sumary>
    CANCEL = 0x03,

    ///<sumary>
    ///Middle mouse button (three-button mouse)
    ///</sumary>
    MBUTTON = 0x04,

    ///<sumary>
    ///X1 mouse button
    ///</sumary>
    XBUTTON1 = 0x05,

    ///<sumary>
    ///X2 mouse button
    ///</sumary>
    XBUTTON2 = 0x06,

    ///<sumary>
    ///Undefined
    ///</sumary>
    UNDEFINED0 = 0x07,

    ///<sumary>
    ///BACKSPACE key
    ///</sumary>
    Backspace = 0x08,

    ///<sumary>
    ///TAB key
    ///</sumary>
    Tab = 0x09,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED0 = 0x0A,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED1 = 0x0B,

    ///<sumary>
    ///CLEAR key
    ///</sumary>
    CLEAR = 0x0C,

    ///<sumary>
    ///ENTER key
    ///</sumary>
    RETURN = 0x0D,

    ///<sumary>
    ///ENTER key
    ///</sumary>
    Enter = RETURN,

    ///<sumary>
    ///Undefined
    ///</sumary>
    UNDEFINED1 = 0x0E,

    ///<sumary>
    ///Undefined
    ///</sumary>
    UNDEFINED2 = 0x0F,

    ///<sumary>
    ///SHIFT key
    ///</sumary>
    Shift = 0x10,

    ///<sumary>
    ///CTRL key
    ///</sumary>
    Control = 0x11,

    ///<sumary>
    ///ALT key
    ///</sumary>
    Menu = 0x12,
    
    ///<sumary>
    ///ALT key
    ///</sumary>
    Alt = Menu,

    ///<sumary>
    ///PAUSE key
    ///</sumary>
    PAUSE = 0x13,

    ///<sumary>
    ///CAPS LOCK key
    ///</sumary>
    CAPITAL = 0x14,

    ///<sumary>
    ///IME Kana mode
    ///</sumary>
    KANA = 0x15,

    ///<sumary>
    ///IME Hanguel mode (maintained for compatibility; use HANGUL)
    ///</sumary>
    HANGUEL = 0x15,

    ///<sumary>
    ///IME Hangul mode
    ///</sumary>
    HANGUL = 0x15,

    ///<sumary>
    ///IME On
    ///</sumary>
    IME_ON = 0x16,

    ///<sumary>
    ///IME Junja mode
    ///</sumary>
    JUNJA = 0x17,

    ///<sumary>
    ///IME final mode
    ///</sumary>
    FINAL = 0x18,

    ///<sumary>
    ///IME Hanja mode
    ///</sumary>
    HANJA = 0x19,

    ///<sumary>
    ///IME Kanji mode
    ///</sumary>
    KANJI = 0x19,

    ///<sumary>
    ///IME Off
    ///</sumary>
    IME_OFF = 0x1A,

    ///<sumary>
    ///ESC key
    ///</sumary>
    Escape = 0x1B,

    ///<sumary>
    ///IME convert
    ///</sumary>
    CONVERT = 0x1C,

    ///<sumary>
    ///IME nonconvert
    ///</sumary>
    NONCONVERT = 0x1D,

    ///<sumary>
    ///IME accept
    ///</sumary>
    ACCEPT = 0x1E,

    ///<sumary>
    ///IME mode change request
    ///</sumary>
    MODECHANGE = 0x1F,

    ///<sumary>
    ///SPACEBAR
    ///</sumary>
    SpaceBar = 0x20,

    ///<sumary>
    ///PAGE UP key
    ///</sumary>
    Prior = 0x21,
    
    ///<sumary>
    ///PAGE UP key
    ///</sumary>
    PageUp = Prior,

    ///<sumary>
    ///PAGE DOWN key
    ///</sumary>
    Next = 0x22,
    
    ///<sumary>
    ///PAGE DOWN key
    ///</sumary>
    PageDown = Next,

    ///<sumary>
    ///END key
    ///</sumary>
    End = 0x23,

    ///<sumary>
    ///HOME key
    ///</sumary>
    Home = 0x24,

    ///<sumary>
    ///HOME key
    ///</sumary>
    Pos1 = Home,

    ///<sumary>
    ///LEFT ARROW key
    ///</sumary>
    LeftArrow = 0x25,

    ///<sumary>
    ///UP ARROW key
    ///</sumary>
    UpArrow = 0x26,

    ///<sumary>
    ///RIGHT ARROW key
    ///</sumary>
    RightArrow = 0x27,

    ///<sumary>
    ///DOWN ARROW key
    ///</sumary>
    DownArrow = 0x28,

    ///<sumary>
    ///SELECT key
    ///</sumary>
    SELECT = 0x29,

    ///<sumary>
    ///PRINT key
    ///</sumary>
    PRINT = 0x2A,

    ///<sumary>
    ///EXECUTE key
    ///</sumary>
    EXECUTE = 0x2B,

    ///<sumary>
    ///PRINT SCREEN key
    ///</sumary>
    SNAPSHOT = 0x2C,

    ///<sumary>
    ///INS key
    ///</sumary>
    Insert = 0x2D,

    ///<sumary>
    ///DEL key
    ///</sumary>
    Delete = 0x2E,

    ///<sumary>
    ///HELP key
    ///</sumary>
    HELP = 0x2F,

    ///<sumary>
    ///0 key
    ///</sumary>
    Key0 = 0x30,

    ///<sumary>
    ///1 key
    ///</sumary>
    Key1 = 0x31,

    ///<sumary>
    ///2 key
    ///</sumary>
    Key2 = 0x32,

    ///<sumary>
    ///3 key
    ///</sumary>
    Key3 = 0x33,

    ///<sumary>
    ///4 key
    ///</sumary>
    Key4 = 0x34,

    ///<sumary>
    ///5 key
    ///</sumary>
    Key5 = 0x35,

    ///<sumary>
    ///6 key
    ///</sumary>
    Key6 = 0x36,

    ///<sumary>
    ///7 key
    ///</sumary>
    Key7 = 0x37,

    ///<sumary>
    ///8 key
    ///</sumary>
    Key8 = 0x38,

    ///<sumary>
    ///9 key
    ///</sumary>
    Key9 = 0x39,

    ///<sumary>
    ///Undefined
    ///</sumary>
    UNDEFINED3 = 0x3A,

    ///<sumary>
    ///Undefined
    ///</sumary>
    UNDEFINED4 = 0x3B,

    ///<sumary>
    ///Undefined
    ///</sumary>
    UNDEFINED5 = 0x3C,

    ///<sumary>
    ///Undefined
    ///</sumary>
    UNDEFINED6 = 0x3D,

    ///<sumary>
    ///Undefined
    ///</sumary>
    UNDEFINED7 = 0x3F,

    ///<sumary>
    ///Undefined
    ///</sumary>
    UNDEFINED8 = 0x3E,

    ///<sumary>
    ///Undefined
    ///</sumary>
    UNDEFINED9 = 0x40,

    ///<sumary>
    ///A key
    ///</sumary>
    A = 0x41,

    ///<sumary>
    ///B key
    ///</sumary>
    B = 0x42,

    ///<sumary>
    ///C key
    ///</sumary>
    C = 0x43,

    ///<sumary>
    ///D key
    ///</sumary>
    D = 0x44,

    ///<sumary>
    ///E key
    ///</sumary>
    E = 0x45,

    ///<sumary>
    ///F key
    ///</sumary>
    F = 0x46,

    ///<sumary>
    ///G key
    ///</sumary>
    G = 0x47,

    ///<sumary>
    ///H key
    ///</sumary>
    H = 0x48,

    ///<sumary>
    ///I key
    ///</sumary>
    I = 0x49,

    ///<sumary>
    ///J key
    ///</sumary>
    J = 0x4A,

    ///<sumary>
    ///K key
    ///</sumary>
    K = 0x4B,

    ///<sumary>
    ///L key
    ///</sumary>
    L = 0x4C,

    ///<sumary>
    ///M key
    ///</sumary>
    M = 0x4D,

    ///<sumary>
    ///N key
    ///</sumary>
    N = 0x4E,

    ///<sumary>
    ///O key
    ///</sumary>
    O = 0x4F,

    ///<sumary>
    ///P key
    ///</sumary>
    P = 0x50,

    ///<sumary>
    ///Q key
    ///</sumary>
    Q = 0x51,

    ///<sumary>
    ///R key
    ///</sumary>
    R = 0x52,

    ///<sumary>
    ///S key
    ///</sumary>
    S = 0x53,

    ///<sumary>
    ///T key
    ///</sumary>
    T = 0x54,

    ///<sumary>
    ///U key
    ///</sumary>
    U = 0x55,

    ///<sumary>
    ///V key
    ///</sumary>
    V = 0x56,

    ///<sumary>
    ///W key
    ///</sumary>
    W = 0x57,

    ///<sumary>
    ///X key
    ///</sumary>
    X = 0x58,

    ///<sumary>
    ///Y key
    ///</sumary>
    Y = 0x59,

    ///<sumary>
    ///Z key
    ///</sumary>
    Z = 0x5A,

    ///<sumary>
    ///Left Windows key (Natural keyboard)
    ///</sumary>
    LEFT_WINDOWS = 0x5B,

    ///<sumary>
    ///Right Windows key (Natural keyboard)
    ///</sumary>
    RIGHT_WINDOWS = 0x5C,

    ///<sumary>
    ///Applications key (Natural keyboard)
    ///</sumary>
    APPLICATION = 0x5D,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED2 = 0x5E,

    ///<sumary>
    ///Computer Sleep key
    ///</sumary>
    SLEEP = 0x5F,

    ///<sumary>
    ///Numeric keypad 0 key
    ///</sumary>
    NUMPAD0 = 0x60,

    ///<sumary>
    ///Numeric keypad 1 key
    ///</sumary>
    NUMPAD1 = 0x61,

    ///<sumary>
    ///Numeric keypad 2 key
    ///</sumary>
    NUMPAD2 = 0x62,

    ///<sumary>
    ///Numeric keypad 3 key
    ///</sumary>
    NUMPAD3 = 0x63,

    ///<sumary>
    ///Numeric keypad 4 key
    ///</sumary>
    NUMPAD4 = 0x64,

    ///<sumary>
    ///Numeric keypad 5 key
    ///</sumary>
    NUMPAD5 = 0x65,

    ///<sumary>
    ///Numeric keypad 6 key
    ///</sumary>
    NUMPAD6 = 0x66,

    ///<sumary>
    ///Numeric keypad 7 key
    ///</sumary>
    NUMPAD7 = 0x67,

    ///<sumary>
    ///Numeric keypad 8 key
    ///</sumary>
    NUMPAD8 = 0x68,

    ///<sumary>
    ///Numeric keypad 9 key
    ///</sumary>
    NUMPAD9 = 0x69,

    ///<sumary>
    ///Multiply key
    ///</sumary>
    MULTIPLY = 0x6A,

    ///<sumary>
    ///Add key
    ///</sumary>
    ADD = 0x6B,

    ///<sumary>
    ///Separator key
    ///</sumary>
    SEPARATOR = 0x6C,

    ///<sumary>
    ///Subtract key
    ///</sumary>
    SUBTRACT = 0x6D,

    ///<sumary>
    ///Decimal key
    ///</sumary>
    DECIMAL = 0x6E,

    ///<sumary>
    ///Divide key
    ///</sumary>
    DIVIDE = 0x6F,

    ///<sumary>
    ///F1 key
    ///</sumary>
    F1 = 0x70,

    ///<sumary>
    ///F2 key
    ///</sumary>
    F2 = 0x71,

    ///<sumary>
    ///F3 key
    ///</sumary>
    F3 = 0x72,

    ///<sumary>
    ///F4 key
    ///</sumary>
    F4 = 0x73,

    ///<sumary>
    ///F5 key
    ///</sumary>
    F5 = 0x74,

    ///<sumary>
    ///F6 key
    ///</sumary>
    F6 = 0x75,

    ///<sumary>
    ///F7 key
    ///</sumary>
    F7 = 0x76,

    ///<sumary>
    ///F8 key
    ///</sumary>
    F8 = 0x77,

    ///<sumary>
    ///F9 key
    ///</sumary>
    F9 = 0x78,

    ///<sumary>
    ///F10 key
    ///</sumary>
    F10 = 0x79,

    ///<sumary>
    ///F11 key
    ///</sumary>
    F11 = 0x7A,

    ///<sumary>
    ///F12 key
    ///</sumary>
    F12 = 0x7B,

    ///<sumary>
    ///F13 key
    ///</sumary>
    F13 = 0x7C,

    ///<sumary>
    ///F14 key
    ///</sumary>
    F14 = 0x7D,

    ///<sumary>
    ///F15 key
    ///</sumary>
    F15 = 0x7E,

    ///<sumary>
    ///F16 key
    ///</sumary>
    F16 = 0x7F,

    ///<sumary>
    ///F17 key
    ///</sumary>
    F17 = 0x80,

    ///<sumary>
    ///F18 key
    ///</sumary>
    F18 = 0x81,

    ///<sumary>
    ///F19 key
    ///</sumary>
    F19 = 0x82,

    ///<sumary>
    ///F20 key
    ///</sumary>
    F20 = 0x83,

    ///<sumary>
    ///F21 key
    ///</sumary>
    F21 = 0x84,

    ///<sumary>
    ///F22 key
    ///</sumary>
    F22 = 0x85,

    ///<sumary>
    ///F23 key
    ///</sumary>
    F23 = 0x86,

    ///<sumary>
    ///F24 key
    ///</sumary>
    F24 = 0x87,

    ///<sumary>
    ///Unassigned
    ///</sumary>
    UNASSIGNED0 = 0x88,

    ///<sumary>
    ///Unassigned
    ///</sumary>
    UNASSIGNED1 = 0x8F,

    ///<sumary>
    ///NUM LOCK key
    ///</sumary>
    NUMLOCK = 0x90,

    ///<sumary>
    ///SCROLL LOCK key
    ///</sumary>
    SCROLL = 0x91,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_SPECIFIC0 = 0x92,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_SPECIFIC1 = 0x93,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_SPECIFIC2 = 0x94,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_SPECIFIC3 = 0x95,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_SPECIFIC4 = 0x96,

    ///<sumary>
    ///Unassigned
    ///</sumary>
    UNASSIGNED2 = 0x97,

    ///<sumary>
    ///Unassigned
    ///</sumary>
    UNASSIGNED3 = 0x98,

    ///<sumary>
    ///Unassigned
    ///</sumary>
    UNASSIGNED4 = 0x99,

    ///<sumary>
    ///Unassigned
    ///</sumary>
    UNASSIGNED5 = 0x9A,

    ///<sumary>
    ///Unassigned
    ///</sumary>
    UNASSIGNED6 = 0x9B,

    ///<sumary>
    ///Unassigned
    ///</sumary>
    UNASSIGNED7 = 0x9C,

    ///<sumary>
    ///Unassigned
    ///</sumary>
    UNASSIGNED8 = 0x9D,

    ///<sumary>
    ///Unassigned
    ///</sumary>
    UNASSIGNED9 = 0x9E,

    ///<sumary>
    ///Unassigned
    ///</sumary>
    UNASSIGNED10 = 0x9F,

    ///<sumary>
    ///Left SHIFT key
    ///</sumary>
    LeftShift = 0xA0,

    ///<sumary>
    ///Right SHIFT key
    ///</sumary>
    RSHIFT = 0xA1,

    ///<sumary>
    ///Left CONTROL key
    ///</sumary>
    LeftControl = 0xA2,

    ///<sumary>
    ///Right CONTROL key
    ///</sumary>
    RCONTROL = 0xA3,

    ///<sumary>
    ///Left MENU key
    ///</sumary>
    LMENU = 0xA4,

    ///<sumary>
    ///Right MENU key
    ///</sumary>
    RMENU = 0xA5,

    ///<sumary>
    ///Browser Back key
    ///</sumary>
    BROWSER_BACK = 0xA6,

    ///<sumary>
    ///Browser Forward key
    ///</sumary>
    BROWSER_FORWARD = 0xA7,

    ///<sumary>
    ///Browser Refresh key
    ///</sumary>
    BROWSER_REFRESH = 0xA8,

    ///<sumary>
    ///Browser Stop key
    ///</sumary>
    BROWSER_STOP = 0xA9,

    ///<sumary>
    ///Browser Search key
    ///</sumary>
    BROWSER_SEARCH = 0xAA,

    ///<sumary>
    ///Browser Favorites key
    ///</sumary>
    BROWSER_FAVORITES = 0xAB,

    ///<sumary>
    ///Browser Start and Home key
    ///</sumary>
    BROWSER_HOME = 0xAC,

    ///<sumary>
    ///Volume Mute key
    ///</sumary>
    VOLUME_MUTE = 0xAD,

    ///<sumary>
    ///Volume Down key
    ///</sumary>
    VOLUME_DOWN = 0xAE,

    ///<sumary>
    ///Volume Up key
    ///</sumary>
    VOLUME_UP = 0xAF,

    ///<sumary>
    ///Next Track key
    ///</sumary>
    MEDIA_NEXT_TRACK = 0xB0,

    ///<sumary>
    ///Previous Track key
    ///</sumary>
    MEDIA_PREV_TRACK = 0xB1,

    ///<sumary>
    ///Stop Media key
    ///</sumary>
    MEDIA_STOP = 0xB2,

    ///<sumary>
    ///Play/Pause Media key
    ///</sumary>
    MEDIA_PLAY_PAUSE = 0xB3,

    ///<sumary>
    ///Start Mail key
    ///</sumary>
    LAUNCH_MAIL = 0xB4,

    ///<sumary>
    ///Select Media key
    ///</sumary>
    LAUNCH_MEDIA_SELECT = 0xB5,

    ///<sumary>
    ///Start Application 1 key
    ///</sumary>
    LAUNCH_APP1 = 0xB6,

    ///<sumary>
    ///Start Application 2 key
    ///</sumary>
    LAUNCH_APP2 = 0xB7,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED3 = 0xB8,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED4 = 0xB9,

    ///<sumary>
    ///Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the ';:' key
    ///</sumary>
    OEM_1 = 0xBA,

    ///<sumary>
    ///For any country/region, the '+' key
    ///</sumary>
    OEM_PLUS = 0xBB,

    ///<sumary>
    ///For any country/region, the ',' key
    ///</sumary>
    OEM_COMMA = 0xBC,

    ///<sumary>
    ///For any country/region, the '-' key
    ///</sumary>
    OEM_MINUS = 0xBD,

    ///<sumary>
    ///For any country/region, the '.' key
    ///</sumary>
    OEM_PERIOD = 0xBE,

    ///<sumary>
    ///Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '/?' key
    ///</sumary>
    OEM_2 = 0xBF,

    ///<sumary>
    ///Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '`~' key
    ///</sumary>
    OEM_3 = 0xC0,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED5 = 0xC1,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED6 = 0xC2,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED7 = 0xC3,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED8 = 0xC4,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED9 = 0xC5,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED10 = 0xC6,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED11 = 0xC7,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED12 = 0xC8,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED13 = 0xC9,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED14 = 0xCA,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED15 = 0xCB,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED16 = 0xCD,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED17 = 0xCE,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED18 = 0xCF,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED19 = 0xD0,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED20 = 0xD1,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED21 = 0xD2,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED22 = 0xD3,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED23 = 0xD4,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED24 = 0xD5,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED25 = 0xD6,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED26 = 0xD7,

    ///<sumary>
    ///Unassigned
    ///</sumary>
    UNASSIGNED11 = 0xD8,

    ///<sumary>
    ///Unassigned
    ///</sumary>
    UNASSIGNED12 = 0xD9,

    ///<sumary>
    ///Unassigned
    ///</sumary>
    UNASSIGNED13 = 0xDA,

    ///<sumary>
    ///Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '[{' key
    ///</sumary>
    OEM_4 = 0xDB,

    ///<sumary>
    ///Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '\|' key
    ///</sumary>
    OEM_5 = 0xDC,

    ///<sumary>
    ///Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the ']}' key
    ///</sumary>
    OEM_6 = 0xDD,

    ///<sumary>
    ///Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the 'single-quote/double-quote' key
    ///</sumary>
    OEM_7 = 0xDE,

    ///<sumary>
    ///Used for miscellaneous characters; it can vary by keyboard.
    ///</sumary>
    OEM_8 = 0xDF,

    ///<sumary>
    ///Reserved
    ///</sumary>
    RESERVED27 = 0xE0,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_9 = 0xE1,

    ///<sumary>
    ///The &lt;&gt; keys on the US standard keyboard, or the \\| key on the non-US 102-key keyboard
    ///</sumary>
    OEM_102 = 0xE2,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_10 = 0xE3,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_11 = 0xE4,

    ///<sumary>
    ///IME PROCESS key
    ///</sumary>
    PROCESSKEY = 0xE5,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_12 = 0xE6,

    ///<sumary>
    ///Used to pass Unicode characters as if they were keystrokes.
    /// The PACKET key is the low word of a 32-bit Virtual Key value used for non-keyboard input methods.
    /// For more information, see Remark in KEYBDINPUT, SendInput, WM_KEYDOWN, and WM_KEYUP
    ///</sumary>
    PACKET = 0xE7,

    ///<sumary>
    ///Unassigned
    ///</sumary>
    UNASSIGNED14 = 0xE8,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_13 = 0xE9,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_14 = 0xEA,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_15 = 0xEB,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_16 = 0xEC,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_17 = 0xED,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_18 = 0xEE,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_19 = 0xEF,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_20 = 0xF0,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_21 = 0xF1,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_22 = 0xF2,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_23 = 0xF3,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_24 = 0xF4,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_25 = 0xF5,

    ///<sumary>
    ///OEM specific
    ///</sumary>
    OEM_26 = 0xF6,

    ///<sumary>
    ///Attn key
    ///</sumary>
    ATTN = 0xF6,

    ///<sumary>
    ///CrSel key
    ///</sumary>
    CRSEL = 0xF7,

    ///<sumary>
    ///ExSel key
    ///</sumary>
    EXSEL = 0xF8,

    ///<sumary>
    ///Erase EOF key
    ///</sumary>
    EREOF = 0xF9,

    ///<sumary>
    ///Play key
    ///</sumary>
    PLAY = 0xFA,

    ///<sumary>
    ///Zoom key
    ///</sumary>
    ZOOM = 0xFB,

    ///<sumary>
    ///Reserved
    ///</sumary>
    NONAME = 0xFC,

    ///<sumary>
    ///PA1 key
    ///</sumary>
    PA1 = 0xFD,

    ///<sumary>
    ///Clear key
    ///</sumary>
    OEM_CLEAR = 0xFE,
}

public enum EMapVk
{
    /// <summary>
    /// The uCode parameter is a virtual-key code and is translated into a scan code.
    /// If it is a virtual-key code that does not distinguish between left- and right-hand keys,
    /// the left-hand scan code is returned. If there is no translation, the function returns 0.
    /// </summary>
    MAPVK_VK_TO_VSC = 0,

    /// <summary>
    /// The uCode parameter is a scan code and is translated into a virtual-key
    /// code that does not distinguish between left- and right-hand keys.
    /// If there is no translation, the function returns 0.
    /// </summary>
    MAPVK_VSC_TO_VK = 1,

    /// <summary>
    /// The uCode parameter is a virtual-key code and is translated into an un-shifted character value
    /// in the low order word of the return value.
    /// Dead keys (diacritics) are indicated by setting the top bit of the return value.
    /// If there is no translation, the function returns 0.
    /// </summary>
    MAPVK_VK_TO_CHAR = 2,

    /// <summary>
    /// The uCode parameter is a scan code and is translated into a virtual-key code that distinguishes
    /// between left- and right-hand keys. If there is no translation, the function returns 0.
    /// </summary>
    MAPVK_VSC_TO_VK_EX = 3,

    /// <summary>
    /// Windows Vista and later: The uCode parameter is a virtual-key code and is translated into a scan code.
    /// If it is a virtual-key code that does not distinguish between left- and right-hand keys,
    /// the left-hand scan code is returned. If the scan code is an extended scan code,
    /// the high byte of the uCode value can contain either 0xe0 or 0xe1 to specify the extended scan code.
    /// If there is no translation, the function returns 0.
    /// </summary>
    MAPVK_VK_TO_VSC_EX = 4,
}