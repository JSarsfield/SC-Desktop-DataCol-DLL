using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC_Desktop_DataCol_DLL
{
    class DatabaseManagerLocal
    {
        private string saveFolder;
        private Dictionary<string, TextWriter> streams = new Dictionary<string, TextWriter>();

        public DatabaseManagerLocal()
        {
        }

        /// <summary>
        /// Create required folder and files for data storage
        /// </summary>
        public void SetupFolderAndFiles(List<string> filenames, List<string> headers, string _sessionName, string saveLocation)
        {
            saveFolder = saveLocation + _sessionName + "/";
            System.IO.Directory.CreateDirectory(saveFolder); // Create session folder

            // Create csv files and write headers
            for (int i = 0; i < filenames.Count; i++)
            {
                TextWriter textWriter = TextWriter.Synchronized(new StreamWriter(saveFolder + filenames[i]));
                string fn = filenames[i].Substring(0, filenames[i].Length - 4);
                streams.Add(fn, textWriter);
                textWriter.WriteLine(headers[i]);
                textWriter.Flush();
            }
        }

        /// <summary>
        /// Explicit call to save data to file
        /// </summary>
        public void SaveData(string filename, List<string> data)
        {
            foreach (var line in data)
            {
                streams[filename].WriteLine(line);
            }
            streams[filename].Flush();
        }
    }
}
