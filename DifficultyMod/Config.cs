using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DifficultyMod
{
    public class Configuration
    {

        public bool DebugLogging = false;
        public byte DebugLoggingLevel = 0;
        public bool UseCustomLogFile = false;
        public string CustomLogFilePath = DifficultyMod2.MOD_CUSTOM_LOG_NAME;

        public static void Serialize(string filename, Configuration config)
        {
            var serializer = new XmlSerializer(typeof(Configuration));
            try
            {
                using (var writer = new StreamWriter(filename))
                {
                    serializer.Serialize(writer, config);
                }
            }
            catch (Exception ex1)
            {
                Logger.dbgLog("Had a problem saving the config file  Error: ", ex1, true);
            }
        }


        public static Configuration Deserialize(string filename)
        {
            var serializer = new XmlSerializer(typeof(Configuration));

            try
            {
                using (var reader = new StreamReader(filename))
                {
                    var config = (Configuration)serializer.Deserialize(reader);
                    return config;
                }
            }
            catch (FileNotFoundException ex1)
            {
                Logger.dbgLog("Config file did not exists, harmless we'll create a new one.", ex1, false);
            }
            catch (Exception ex1)
            {
                string tmpstr = "Could not load configuration file, a new one will be generated.";
                if (DifficultyMod2.DEBUG_LOG_ON)
                { Logger.dbgLog(tmpstr, ex1, true); }
                else
                { Logger.dbgLog(tmpstr, ex1, false); }
            }

            return null;
        }
    }
}
