using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DifficultyMod
{
    public class Disasters2 : MonoBehaviour
    {
        private static ushort[] originalLevels = new ushort[2000];
        private static float originalSeaLevel = 40f;
        private static float currentDisaster = -1f;
        private static float disasterMagnitude = 0f;
        void Update()
        {

            SimulationManager instance = Singleton<SimulationManager>.instance;

            if (currentDisaster == -1f)
            {
                if (instance.m_randomizer.Int32(1, 400000) == 42)
                {
                    if (!instance.SimulationPaused)
                    {
                        instance.SelectedSimulationSpeed = 1;
                    }

                    var ws = Singleton<TerrainManager>.instance.WaterSimulation;
                    var message = new SimpleMessage(Singleton<MessageManager>.instance.GetRandomResidentID(), "aaa", "bbb");
                    Singleton<MessageManager>.instance.QueueMessage(message);
                    currentDisaster = 0f;

                    if (instance.m_randomizer.Int32(1, 40) == 2)
                    {
                        disasterMagnitude = instance.m_randomizer.Int32(20, 40);
                    }
                    else
                    {
                        disasterMagnitude = instance.m_randomizer.Int32(14, 24);
                    }

                    
                    originalSeaLevel = ws.m_currentSeaLevel;
                    for (int i = 0; i < ws.m_waterSources.m_size; i++)
                    {
                        if (ws.m_waterSources.m_buffer[i].m_type == 1)
                        {
                            var source = (ushort)(i + 1);
                            WaterSource sourceData = ws.LockWaterSource(source);
                            try
                            {
                                originalLevels[source] = sourceData.m_target += 1;
                            }
                            finally
                            {
                                ws.UnlockWaterSource(source, sourceData);
                            }
                        }
                    }
                }
                
            }
            else if (currentDisaster >= 0f)
            {
                var sign = 1f;
                if (disasterMagnitude <= 0f)
                {
                    sign = -1f;
                }
                else if (currentDisaster > disasterMagnitude)
                {
                    disasterMagnitude -= 0.05f;
                    return;
                }

                currentDisaster += 0.015625f * sign;
                var ws = Singleton<TerrainManager>.instance.WaterSimulation;
                ws.m_nextSeaLevel += 0.015625f * sign;
                for (int i = 0; i < ws.m_waterSources.m_size; i++)
                {
                    if (ws.m_waterSources.m_buffer[i].m_type == 1)
                    {
                        var source = (ushort)(i + 1);
                        WaterSource sourceData = ws.LockWaterSource(source);
                        try
                        {
                            sourceData.m_target = (ushort)Math.Max(0, (int)sourceData.m_target +  sign);
                        }
                        finally
                        {
                            ws.UnlockWaterSource(source, sourceData);
                        }
                    }
                }
            }
            else if (currentDisaster < 0f)
            {
                ResetSeaLevel();
            }

        }

        public void ResetSeaLevel()
        {
            if (currentDisaster == -1f)
            {
                return;
            }
            currentDisaster = -1f;
            var ws = Singleton<TerrainManager>.instance.WaterSimulation;
            ws.m_nextSeaLevel = originalSeaLevel;
            for (int i = 0; i < ws.m_waterSources.m_size; i++)
            {
                if (ws.m_waterSources.m_buffer[i].m_type == 1)
                {
                    var source = (ushort)(i + 1);
                    WaterSource sourceData = ws.LockWaterSource(source);
                    try
                    {
                        sourceData.m_target = originalLevels[source];
                    }
                    finally
                    {
                        ws.UnlockWaterSource(source, sourceData);
                    }
                }
            }
        }
    }


    public class SimpleMessage : CitizenMessage
    {
        public SimpleMessage(uint senderID, string messageID, string keyID) : base(senderID, messageID, keyID) { }


        public override string GetText()
        {
            return "Strange phenomenon are causing #water levels to #rise. For your #safety, don't save the game while waters are rising.";
        }

        public override string GetSenderName()
        {
            return "EMERGENCY ALERT SYSTEM";
        }
    }

}