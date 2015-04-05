using ColossalFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DifficultyMod
{
    public class SaveData2 
    {
        private static SaveData2 m_saveData = new SaveData2();
        public static SaveData2 saveData
        {
            get
            {                
                return m_saveData;
            }
        }

        private const string fileName = "difficultyMod.config";

        private static List<SaveData2> m_saves = null;
        public static  List<SaveData2> saves {
            get{
                if (m_saves == null){
                    if (!File.Exists(fileName))
                    {
                        m_saves = new List<SaveData2>();
                    }
                    else
                    {
                        using (System.IO.StreamReader file = new System.IO.StreamReader(fileName))
                        {
                            System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(List<SaveData2>));
                            m_saves = (List<SaveData2>)reader.Deserialize(file);
                        }
                    }                         
                }
                return m_saves;
            }
        }        


        public bool disastersEnabled = true;
        public DifficultyLevel DifficultyLevel = DifficultyLevel.Hard;
        public string cityId;

        public static bool MustInitialize()
        {
            var tempData =  SaveData2.ReadData(Singleton<SimulationManager>.instance.m_metaData.m_gameInstanceIdentifier);
            if (tempData == null)
            {
                return true;
            }
            else
            {
                m_saveData = tempData;
                return false;
            }            
        }

        public static void WriteData(SaveData2 data)
        {            
            var found = false;
            for (var i = 0; i < saves.Count; i += 1)
            {
                if (saves[i].cityId == data.cityId){
                    saves[i] = data;
                    found = true;
                    break;
                }
            }
            if (!found){
                saves.Add(data);
            }

            System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(List<SaveData2>));
            using(var file  = new StreamWriter(File.Create(fileName))){                
                writer.Serialize(file, saves);
                file.Close();
            }
        }

        public static SaveData2 ReadData(string cityId)
        {
            for (var i = 0; i < saves.Count; i += 1)
            {
                if (saves[i].cityId == cityId){
                    return saves[i];
                }
            }
            return null;
        }


        internal static void ResetData()
        {
            m_saves = null;
        }
    }
    public enum DifficultyLevel{
        Vanilla,
        Normal,
        Hard,        
        DwarfFortress,
    }
}
