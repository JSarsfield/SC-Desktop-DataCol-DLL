using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace SC_Desktop_DataCol_DLL
{
    /// <summary>
    /// Store keyboard activity data. This forms the features for engagement classification.
    /// </summary>
    public struct KeyActivity
    {
        private int timestamp;
        private float duration;

        public KeyActivity(int _timestamp, float _duration)
        {
            timestamp = _timestamp;
            duration = _duration;
        }
    }

    /// <summary>
    /// EventManager manages all hooks from the OS including keyboard, mouse and window hooks.
    /// Responds to events and sends data to DatabaseManager through MainController class.
    /// Releasing unmanaged resources follows the dispose pattern using SafeHandle https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose
    /// </summary>
    public class EventManager : IDisposable
    {
        private KeyboardHook keyboard;
        private MouseHook mouse;
        private WindowHook window;
        public ScreenCapture screenCapture;

        public ulong sessSysStartTime;

        public delegate IntPtr LowLevelHookProc(int nCode, IntPtr wParam, IntPtr lParam);

        //public List<Dictionary<string, object>> activityQueue = new List<Dictionary<string, object>>();

        public List<string> keypressData = new List<string>();
        public List<string> mouseMoveData = new List<string>();
        public List<string> mouseClickData = new List<string>();
        public List<string> screenCaptureMetaData = new List<string>();

        public bool isRecording = true;


        public EventManager(string savePath)
        {
            screenCapture = new ScreenCapture(this, savePath);
        }

        public void BeginRecording(ulong _sessSysStartTime)
        {
            this.sessSysStartTime = _sessSysStartTime;
            this.keyboard = new KeyboardHook(this);
            this.mouse = new MouseHook(this);
            this.screenCapture.BeginRecordingScreen();
            //this.window = new WindowHook(this);
        }

        public void Dispose()
        {
            this.keyboard.Dispose();
            this.mouse.Dispose();
            //this.window.Dispose();
        }

        public String ConvertToSessTime(int time)
        {
            return ((ulong)time-sessSysStartTime).ToString();
        }

        /// <summary>
        /// Queue data in activityQueue for sending to DB
        /// </summary>
        /*
        public void QueueActivityData(Dictionary<string, object> activity)
        {
            lock (activityQueue)
            {
                activityQueue.Add(activity);
            }
        }*/
    }

    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    internal class SafeHandleGeneric : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Create a SafeHandle, informing the base class
        // that this SafeHandle instance "owns" the handle,
        // and therefore SafeHandle should call
        // our ReleaseHandle method when the SafeHandle
        // is no longer in use.
        private SafeHandleGeneric()
            : base(true)
        {
        }
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        override protected bool ReleaseHandle()
        {
            return NativeMethods.UnhookWindowsHookEx(handle);
        }
    }

    static class NativeMethods
    {

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern SafeHandleGeneric SetWindowsHookEx(int idHook, EventManager.LowLevelHookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        /*

        [DllImport("user32.dll")]
        internal static extern SafeHandleZeroOrMinusOneIsInvalid SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        internal static extern delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);*/
    }
}
