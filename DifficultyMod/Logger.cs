using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using ColossalFramework.IO;

namespace DifficultyMod
{
    static class Logger
    {
        //should be enough for most log messages and we want this guy in the HFHeap.
        private static StringBuilder logSB = new System.Text.StringBuilder(1024);

        /// <summary>
        /// Our LogWrapper. We use this so our log messages a) look like [MOD_PREFIX_NAME]: message
        /// and b) are optionally output to our specific log as configured.
        /// </summary>
        /// <param name="sText">Text to log</param>
        /// <param name="ex">An Exception - if not null it's basic data will be printed.</param>
        /// <param name="bDumpStack">If an Exception was passed do you want the stack trace?</param>
        /// <param name="bNoIncMethod">If for some reason you don't want the method name prefaced with the log line.</param>
        public static void dbgLog(string sText, Exception ex = null, bool bDumpStack = false, bool bNoIncMethod = false)
        {
            try
            {
                logSB.Length = 0;
                string sPrefix = string.Concat("[", DifficultyMod2.MOD_LOG_PREFIX);
                if (bNoIncMethod) { string.Concat(sPrefix, "]  "); }
                else
                {
                    System.Diagnostics.StackFrame oStack = new System.Diagnostics.StackFrame(1); //pop back one frame, ie our caller.
                    sPrefix = string.Concat(sPrefix, ":", oStack.GetMethod().DeclaringType.Name, ".", oStack.GetMethod().Name, "] ");
                }
                logSB.Append(string.Concat(sPrefix, sText));

                if (ex != null)
                {
                    logSB.Append(string.Concat("\r\nException: ", ex.Message.ToString()));
                }
                if (bDumpStack)
                {
                    logSB.Append(string.Concat("\r\nStackTrace: ", ex.ToString()));
                }
                if (DifficultyMod2.config != null && DifficultyMod2.config.UseCustomLogFile == true)
                {
                    string strPath = System.IO.Directory.Exists(Path.GetDirectoryName(DifficultyMod2.config.CustomLogFilePath)) ? DifficultyMod2.config.CustomLogFilePath.ToString() : Path.Combine(DataLocation.executableDirectory.ToString(), DifficultyMod2.config.CustomLogFilePath);
                    using (StreamWriter streamWriter = new StreamWriter(strPath, true))
                    {
                        streamWriter.WriteLine(logSB.ToString());
                    }
                }
                else
                {
                    Debug.Log(logSB.ToString());
                }
            }
            catch (Exception Exp)
            {
                Debug.Log(string.Concat("[ProperHardness.Logger.dbgLog()] Error in log attempt!  ", Exp.Message.ToString()));
            }
        }

    }
}
