﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SC_Desktop_DataCol_DLL
{

    /// <summary>
    /// Accessible DLL functions should only exist in Main class
    /// </summary>
    public class DataCol
    {
        private String username;
        private String computername;
        //private static DatabaseManager dm;
        private DatabaseManagerLocal dm;
        private EventManager em;

        //public bool IsRecording { get; set; } = false;

        private Task taskSendActivityData;  // Task involved in uploading activity data to DB
        public CancellationTokenSource taskSADToken;  // Token to cancel Send Activity Data task when stop recording is called

        private Dictionary<string, string> filenames = new Dictionary<string, string>()
        {
            { "keypressData", "keypressData.csv" },
            { "mouseMoveData", "mouseMoveData.csv" },
            { "mouseClickData", "mouseClickData.csv" }
        };
        private List<string> headers = new List<string>() // Header line describing column names at top of data files
        {
            "timestamp_keydown_(ms_since_boot),timestamp_keyup_(ms_since_boot),duration_(ms)",
            "timestamp_mousemove_(ms_since_boot),abs_x,abs_y,duration_since_last_move_(ms)",
            "timestamp_mouseclickdown_(ms_since_boot),timestamp_mouseclickup_(ms_since_boot),duration_(ms)"
        };

        /// <summary>
        /// Setup commands.
        /// Alert program if the cloud database can't be connected to - local data is still stored.
        /// </summary>
        public bool SetupDLL()
        {
            return true;
        }

        /// <summary>
        /// Create session 
        /// </summary>
        /// <returns></returns>
        public void CreateSession(string sessionName, List<string> extraData, List<string> extraHeaders, string saveLocation)
        {
            for (int i = 0; i < extraData.Count; i++)
            {
                filenames.Add(extraData[i], extraData[i] + ".csv");
                headers.Add(extraHeaders[i]);
            }
            dm = new DatabaseManagerLocal();
            Task.Run(() =>
            {
                dm.SetupFolderAndFiles(filenames.Values.ToList(), headers, sessionName, saveLocation);
            });
        }

        /// <summary>
        /// Explicit call to save data to file
        /// </summary>
        public void SaveData(string filename, List<string> data, Action callbackFunc = null, bool runAsync = true)
        {
            if (callbackFunc is null)
            {
                if (runAsync)
                {
                    Task.Run(() =>
                    {
                        dm.SaveData(filename, data);
                    });
                }
                else
                {
                    dm.SaveData(filename, data);
                }
            }
            else
            {
                Task.Run(() =>
                {
                    dm.SaveData(filename, data);
                }).ContinueWith(_ => { callbackFunc(); });
            }
        }

        /// <summary>
        /// Beging recording desktop activity - key presses and mouse movements - store locally and cloud mongodb if possible
        /// </summary>
        /// <returns></returns>
        public void BeginRecording()
        {
            // Create EM
            em = new EventManager();
            em.BeginRecording(); 
        }

        /// <summary>
        /// End session, stop recording and save keyboard/mouse activity to files.
        /// </summary>
        /// <returns></returns>
        public bool EndSession()
        {
            // Dispose of EM and close streams. Consider saving csv files to cloud.
            em.isRecording = false;
            System.Threading.Thread.Sleep(200); // Artifical delay to ensure we have stopped logging - Hacky but does the job
            SaveData("keypressData", new List<string>(em.keypressData),null,false);
            em.mouseMoveData.RemoveAt(0); // Remove first element as we don't have duration since last move
            SaveData("mouseMoveData", new List<string>(em.mouseMoveData), null, false);
            SaveData("mouseClickData", new List<string>(em.mouseClickData), null, false);
            em.Dispose();
            return true;
        }
    }
}