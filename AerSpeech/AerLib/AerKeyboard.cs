using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;   //Needed to import your .dll

namespace AerSpeech
{

    /// <summary>
    /// Keyboard Emulation
    /// </summary>
    public class AerKeyboard
    {

        Dictionary<char, short> ScanCodes = new Dictionary<char, short>()
        {
            {'0', 0x0B},
            {'1', 0x02},
            {'2', 0x03},
            {'3', 0x04},
            {'4', 0x05},
            {'5', 0x06},
            {'6', 0x07},
            {'7', 0x08},
            {'8', 0x09},
            {'9', 0x0A},
            {'A', 0x1E},
            {'B', 0x30},
            {'C', 0x2E},
            {'D', 0x20},
            {'E', 0x12},
            {'F', 0x21},
            {'G', 0x22},
            {'H', 0x23},
            {'I', 0x17},
            {'J', 0x24},
            {'K', 0x25},
            {'L', 0x26},
            {'M', 0x32},
            {'N', 0x31},
            {'O', 0x18},
            {'P', 0x19},
            {'Q', 0x10},
            {'R', 0x13},
            {'S', 0x1F},
            {'T', 0x14},
            {'U', 0x16},
            {'V', 0x2F},
            {'W', 0x11},
            {'X', 0x2D},
            {'Y', 0x15},
            {'Z', 0x2C},
            {'\'',0x28},
            {':', 0x92},
            {',', 0x33},
            {' ', 0x39},
            {'-', 0x0C},
            {'\b', 0x0E} //Backspace
        };

        public int _InputDelay;
        DXInputEmulate _Keyboard;

        public AerKeyboard(int inputDelay = 10)
        {
            _Keyboard = new DXInputEmulate();
            _InputDelay = inputDelay;
        }

        public void Type(string typeMe)
        {
            char[] characters = typeMe.ToUpper().ToCharArray();
            foreach (char c in characters)
            {

                if(ScanCodes.ContainsKey(c))
                {
                    _Keyboard.SendKey(ScanCodes[c], DXInputEmulate.KEYEVENTF_KEYDOWN);
                    Thread.Sleep(_InputDelay);
                    _Keyboard.SendKey(ScanCodes[c], DXInputEmulate.KEYEVENTF_KEYUP);
                }
                else
                {
                    AerDebug.LogError("ScanCode for character '" + c + "' does not exists in dictionary");
                }
                
            }
        }

        public void ClearField()
        {
            for (int i = 0; i < 60; i++)
            {

                if (ScanCodes.ContainsKey('\b'))
                {
                    _Keyboard.SendKey(ScanCodes['\b'], DXInputEmulate.KEYEVENTF_KEYDOWN);
                    Thread.Sleep(_InputDelay);
                    _Keyboard.SendKey(ScanCodes['\b'], DXInputEmulate.KEYEVENTF_KEYUP);
                }
                else
                {
                    AerDebug.LogError("ScanCode for backspace character does not exists in dictionary");
                }

            }
        }
    
    }

    public class DXInputEmulate
    {
        [DllImport("user32.dll")]
        static extern UInt32 SendInput(UInt32 nInputs, [MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] INPUT[] pInputs, Int32 cbSize);

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public short wVk;
            public short wScan;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct INPUT
        {
            [FieldOffset(0)]
            public int type;
            [FieldOffset(4)]
            public MOUSEINPUT mi;
            [FieldOffset(4)]
            public KEYBDINPUT ki;
            [FieldOffset(4)]
            public HARDWAREINPUT hi;
        }

        public const int KEYEVENTF_KEYDOWN = 0x0000;
        public const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        public const int KEYEVENTF_KEYUP = 0x0002;
        public const int KEYEVENTF_UNICODE = 0x0004;
        public const int KEYEVENTF_SCANCODE = 0x0008;

        public void SendKey(short Keycode, int KeyUporDown)
        {
            INPUT[] InputData = new INPUT[1];

            InputData[0].type = 1;
            InputData[0].ki.wScan = Keycode;
            InputData[0].ki.dwFlags = KeyUporDown | KEYEVENTF_SCANCODE;
            InputData[0].ki.time = 0;
            InputData[0].ki.dwExtraInfo = IntPtr.Zero;

            SendInput(1, InputData, Marshal.SizeOf(typeof(INPUT)));

            //AerDebug.Log("SendKey result = " + Marshal.GetLastWin32Error());
        }

    }
}

