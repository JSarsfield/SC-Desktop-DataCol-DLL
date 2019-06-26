using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Math;
using Accord.Video;
using Accord.Video.FFMPEG;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SC_Desktop_DataCol_DLL
{

    /// <summary>
    /// Record the screen and save to encoded video format at desired FPS
    /// </summary>
    /// 
    class ScreenCapture
    {
        // These native references are for capturing the mouse cursor position and icon for drawing onto screen capture

        [StructLayout(LayoutKind.Sequential)]
        struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINTAPI ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINTAPI
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        const Int32 CURSOR_SHOWING = 0x00000001;
        
        // Get system timestamp
        [DllImport("kernel32")]
        extern static UInt64 GetTickCount64();

        ScreenCaptureStream stream;
        VideoFileWriter writer;
        EventManager em;  // Reference to eventmanager instance
        int frameNum = 0;
        string savePath;
        TimeSpan firstFrameTimestamp;

        public ScreenCapture(EventManager _em, string _savePath)
        {
            em = _em;  // reference to EM
            savePath = _savePath;
            Rectangle resolution = new Rectangle(0, 0, Convert.ToInt32(System.Windows.SystemParameters.PrimaryScreenWidth), Convert.ToInt32(System.Windows.SystemParameters.PrimaryScreenHeight));
            stream = new ScreenCaptureStream(resolution,33); // 33ms screenshot interval is slightly faster than 30 FPS which is 33.333...ms interval
            stream.NewFrame += NewFrameArrived;
            int bitRate = ((Convert.ToInt32(System.Windows.SystemParameters.PrimaryScreenWidth) * Convert.ToInt32(System.Windows.SystemParameters.PrimaryScreenHeight)) * 30);
            writer = new VideoFileWriter();
            /*
            {
                Width = Convert.ToInt32(System.Windows.SystemParameters.PrimaryScreenWidth),
                Height = Convert.ToInt32(System.Windows.SystemParameters.PrimaryScreenHeight),
                //FrameRate = new Rational(30),
                VideoCodec = VideoCodec.H264,
                BitRate = bitRate
            };
            writer.VideoOptions["vsync"] = "drop";*/
            //writer.VideoOptions["copyts"] = "";
            //writer.VideoOptions["preset"] = "ultrafast";
            //writer.VideoOptions["tune"] = "zerolatency";
        }

        public void NewFrameArrived(object sender, NewFrameEventArgs eventArgs)
        {
            // Get Frame
            Bitmap bitmap = eventArgs.Frame;

            // Draw cursor
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                CURSORINFO pci;
                pci.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CURSORINFO));
                
                if (GetCursorInfo(out pci))
                {
                    if (pci.flags == CURSOR_SHOWING)
                    {
                        DrawIcon(g.GetHdc(), pci.ptScreenPos.x, pci.ptScreenPos.y, pci.hCursor);
                        g.ReleaseHdc();
                    }
                }
            }

            // Add frame to writer
            try
            {
                // Get timestamp
                TimeSpan frameTimestamp = TimeSpan.FromMilliseconds((long)GetTickCount64());
                //string frameTS = frameTimestamp.
                if (frameNum == 0)
                    firstFrameTimestamp = frameTimestamp;
                TimeSpan frameTs = frameTimestamp - firstFrameTimestamp;
                //TimeSpan frameVidTime = frameTimestamp - firstFrameTimestamp;
                writer.WriteVideoFrame(bitmap, frameTs); // , TimeSpan.FromMilliseconds((long)GetTickCount64()) - firstFrameTimestamp , frameTimestamp - firstFrameTimestamp
                if (frameNum % 120 == 0) // Flush every ~4 seconds
                {
                    writer.Flush();
                }
                // Save screen capture metadata to csv
                em.screenCaptureMetaData.Add(TimestampToSessTime(frameTimestamp) + "," + frameTs.TotalMilliseconds.ToString());
                frameNum += 1;
            }
            catch
            {
                Console.WriteLine("Frame failed");
            }
        }

        public string TimestampToSessTime(TimeSpan ts)
        {
            return (ts-TimeSpan.FromMilliseconds((long)(em.sessSysStartTime))).TotalMilliseconds.ToString();
        }

        public bool BeginRecordingScreen()
        {
            int bitRate = ((Convert.ToInt32(System.Windows.SystemParameters.PrimaryScreenWidth) * Convert.ToInt32(System.Windows.SystemParameters.PrimaryScreenHeight)) * 30);
            writer.Open(savePath + "screenCaptureVideo.mov", 
                Convert.ToInt32(System.Windows.SystemParameters.PrimaryScreenWidth), 
                Convert.ToInt32(System.Windows.SystemParameters.PrimaryScreenHeight),
                 new Rational(30),
                 VideoCodec.MPEG4,
                 bitRate);
            stream.Start();
            return true;
        }

        public bool StopRecordingScreen()
        {
            stream.SignalToStop();
            stream.WaitForStop();
            writer.Flush();
            writer.Close();
            writer.Dispose();
            return true;
        }
    }
}
