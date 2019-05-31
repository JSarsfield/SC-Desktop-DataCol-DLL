using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace SC_Desktop_DataCol_DLL
{
    /// <summary>
    /// Windows API keyboard event hooks.
    /// Initial code taken from https://stackoverflow.com/questions/46013287/c-sharp-global-keyboard-hook-that-opens-a-form-from-a-console-application
    /// </summary>
    class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13; // hook procedure that monitors low-level keyboard  input events.
        private const int WM_KEYDOWN = 0x0100; 
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_KEYUP = 0x101;
        private const int WM_SYSKEYUP = 0x105;


        private EventManager.LowLevelHookProc _proc;
        private IntPtr _hookID;

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;

        EventManager em;  // Reference to eventmanager instance
        bool disposed = false;
        SafeHandleGeneric sethook_handle;

        private Dictionary<int, int> timestamps = new Dictionary<int, int>();  // Store timestamps

        public KeyboardHook(EventManager _em)
        {
            this.em = _em;
            this._proc = this.KeyboardHookCallback;
            this.sethook_handle = SetHook(_proc);
        }

        /// <summary>
        /// Create new keyboard activity and send to the activityQueue for processing by the Database Manager
        /// timestamp is stored as Unix timestamp
        /// </summary>
        /*
        void CreateKeyboardActivity(long timestamp, long duration)
        {
            Task.Run(() => {
                em.QueueActivityData(new Dictionary<string, object>() {{ "timestamp", timestamp },
                    { "duration", duration },
                    { "type", "keypress"}
                });
            });
        }*/

        private SafeHandleGeneric SetHook(EventManager.LowLevelHookProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return NativeMethods.SetWindowsHookEx(WH_KEYBOARD_LL, proc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        /// <summary>
        /// The KBDLLHOOKSTRUCT structure contains information about a low-level keyboard input event. 
        /// </summary>
        /// <remarks>
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookstructures/cwpstruct.asp
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardHookStruct
        {
            /// <summary>
            /// Specifies a virtual-key code. The code must be a value in the range 1 to 254. 
            /// </summary>
            public int VirtualKeyCode;
            /// <summary>
            /// Specifies a hardware scan code for the key. 
            /// </summary>
            public int ScanCode;
            /// <summary>
            /// Specifies the extended-key flag, event-injected flag, context code, and transition-state flag.
            /// </summary>
            public int Flags;
            /// <summary>
            /// Specifies the Time stamp for this message.
            /// </summary>
            public int Time;
            /// <summary>
            /// Specifies extra information associated with the message. 
            /// </summary>
            public int ExtraInfo;
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // Get keyboard data from callback
            KeyboardHookStruct keyHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));
            // Check if keydown or keyup event
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                if (!timestamps.ContainsKey(keyHookStruct.VirtualKeyCode))
                {
                    timestamps.Add(keyHookStruct.VirtualKeyCode, keyHookStruct.Time);
                }
            }
            else if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
            {
                if (timestamps.ContainsKey(keyHookStruct.VirtualKeyCode))
                {
                    var duration = (keyHookStruct.Time - timestamps[keyHookStruct.VirtualKeyCode]).ToString();
                    // Add key press data to array
                    em.keypressData.Add(em.ConvertToSessTime(timestamps[keyHookStruct.VirtualKeyCode]) + "," + em.ConvertToSessTime(keyHookStruct.Time) + "," + duration);
                    timestamps.Remove(keyHookStruct.VirtualKeyCode);
                }
            }

            return NativeMethods.CallNextHookEx(this.sethook_handle.DangerousGetHandle(), nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                sethook_handle.Dispose();
            }
            disposed = true;
        }

        ~KeyboardHook()
        {
            Dispose(false);
        }
    }
}
