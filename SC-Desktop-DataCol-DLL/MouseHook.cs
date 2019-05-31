using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace SC_Desktop_DataCol_DLL
{

    /// <summary>
    /// Windows API mouse event hooks.
    /// </summary>
    class MouseHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;  // hook procedure that monitors low-level mouse input events.
        private const int WH_MOUSE = 7; // hook procedure that monitors mouse messages. For more information, see the MouseProc hook procedure. 
        private const int WM_MOUSEMOVE = 0x200; // WM_MOUSEMOVE message is posted to a window when the cursor moves. 
        private const int WM_LBUTTONDOWN = 0x201; // WM_LBUTTONDOWN message is posted when the user presses the left mouse button 
        private const int WM_RBUTTONDOWN = 0x204; // WM_RBUTTONDOWN message is posted when the user presses the right mouse button
        private const int WM_MBUTTONDOWN = 0x207; // WM_MBUTTONDOWN message is posted when the user presses the middle mouse button 
        private const int WM_LBUTTONUP = 0x202; // WM_LBUTTONUP message is posted when the user releases the left mouse button 
        private const int WM_RBUTTONUP = 0x205; // WM_RBUTTONUP message is posted when the user releases the right mouse button 
        private const int WM_MBUTTONUP = 0x208; // WM_MBUTTONUP message is posted when the user releases the middle mouse button
        private const int WM_LBUTTONDBLCLK = 0x203; // WM_LBUTTONDBLCLK message is posted when the user double-clicks the left mouse button 
        private const int WM_RBUTTONDBLCLK = 0x206; // The WM_RBUTTONDBLCLK message is posted when the user double-clicks the right mouse button 
        private const int WM_MBUTTONDBLCLK = 0x209; // The WM_RBUTTONDOWN message is posted when the user presses the right mouse button 
        private const int WM_MOUSEWHEEL = 0x020A; // WM_MOUSEWHEEL message is posted when the user presses the mouse wheel.

        //public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        int[] mouseClickDownEvents = { WM_LBUTTONDOWN, WM_RBUTTONDOWN, WM_MBUTTONDOWN };
        int[] mouseClickUpEvents = { WM_LBUTTONUP, WM_RBUTTONUP, WM_MBUTTONUP };

        private Dictionary<int, int> mouseClickDurations = new Dictionary<int, int>();

        private EventManager.LowLevelHookProc _proc;

        EventManager em;
        bool disposed = false;
        SafeHandleGeneric sethook_handle;

        int lastMouseMove = 0;


        public MouseHook(EventManager _em)
        {
            this.em = _em;
            this._proc = this.MouseHookCallback;
            this.sethook_handle = SetHook(_proc);
        }


        private SafeHandleGeneric SetHook(EventManager.LowLevelHookProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return NativeMethods.SetWindowsHookEx(WH_MOUSE_LL, proc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        /// <summary>
        /// The Point structure defines the X- and Y- coordinates of a point. 
        /// </summary>
        /// <remarks>
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/gdi/rectangl_0tiq.asp
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        private struct Point
        {
            /// <summary>
            /// Specifies the X-coordinate of the point. 
            /// </summary>
            public int X;
            /// <summary>
            /// Specifies the Y-coordinate of the point. 
            /// </summary>
            public int Y;
        }

        /// <summary>
        /// The MSLLHOOKSTRUCT structure contains information about a low-level keyboard input event. 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MouseLLHookStruct
        {
            /// <summary>
            /// Specifies a Point structure that contains the X- and Y-coordinates of the cursor, in screen coordinates. 
            /// </summary>
            public Point Point;
            /// <summary>
            /// If the message is WM_MOUSEWHEEL, the high-order word of this member is the wheel delta. 
            /// The low-order word is reserved. A positive value indicates that the wheel was rotated forward, 
            /// away from the user; a negative value indicates that the wheel was rotated backward, toward the user. 
            /// One wheel click is defined as WHEEL_DELTA, which is 120. 
            ///If the message is WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP,
            /// or WM_NCXBUTTONDBLCLK, the high-order word specifies which X button was pressed or released, 
            /// and the low-order word is reserved. This value can be one or more of the following values. Otherwise, MouseData is not used. 
            ///XBUTTON1
            ///The first X button was pressed or released.
            ///XBUTTON2
            ///The second X button was pressed or released.
            /// </summary>
            public int MouseData;
            /// <summary>
            /// Specifies the event-injected flag. An application can use the following value to test the mouse Flags. Value Purpose 
            ///LLMHF_INJECTED Test the event-injected flag.  
            ///0
            ///Specifies whether the event was injected. The value is 1 if the event was injected; otherwise, it is 0.
            ///1-15
            ///Reserved.
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
        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                //Get mouse move data from callback.
                MouseLLHookStruct mouseHookStruct = (MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseLLHookStruct));
                int wParamI = (int)wParam; // Cast IntPtr to int
                if (wParamI == WM_MOUSEMOVE) // Mouse move event
                {
                    //Console.WriteLine("mouse X:" + mouseHookStruct.Point.X.ToString() + " Y:" + mouseHookStruct.Point.Y.ToString() + " Time:" + mouseHookStruct.Time.ToString()+" Duration:"+(mouseHookStruct.Time-lastMouseMove).ToString());
                    // Add mouse move data to array
                    em.mouseMoveData.Add(em.ConvertToSessTime(mouseHookStruct.Time) + "," + mouseHookStruct.Point.X.ToString() + "," + mouseHookStruct.Point.Y.ToString() + "," + (mouseHookStruct.Time - lastMouseMove).ToString());
                    lastMouseMove = mouseHookStruct.Time;
                }
                else if (mouseClickDownEvents.Contains(wParamI)) // Mouse left/right/middle button down event
                {
                    if (!mouseClickDurations.ContainsKey(wParamI+1))
                    {
                        mouseClickDurations.Add(wParamI + 1, mouseHookStruct.Time);
                    }
                }
                else if (mouseClickUpEvents.Contains(wParamI)) // Mouse left/right/middle button up event
                {
                    if (mouseClickDurations.ContainsKey(wParamI))
                    {
                        var duration = (mouseHookStruct.Time - mouseClickDurations[wParamI]).ToString();
                        // Add mouse click data to array
                        em.mouseClickData.Add(em.ConvertToSessTime(mouseClickDurations[wParamI]) + "," + em.ConvertToSessTime(mouseHookStruct.Time) + "," + duration);
                        mouseClickDurations.Remove(wParamI);
                    }
                }
                else if (wParamI == WM_MOUSEWHEEL) // Mouse scroll currently ignored
                {
                    //Console.WriteLine("WM_MOUSEWHEEL");
                }
            }
            //call next hook
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

        ~MouseHook()
        {
            Dispose(false);
        }
    }
}
