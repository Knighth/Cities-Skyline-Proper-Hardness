using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DifficultyMod
{
    public class FireSpread
    {
        private static Queue<ushort> fireQueue = new Queue<ushort>();
        private static HashSet<ushort> fireSet = new HashSet<ushort>();

        public void ExtraFireSpread(ushort buildingID, ref Building buildingData, int damageAccumulation, float sizeY)
        {

            int width = buildingData.Width;
            int length = buildingData.Length;
            Vector2 a = VectorUtils.XZ(buildingData.m_position);
            Vector2 a2 = new Vector2(Mathf.Cos(buildingData.m_angle), Mathf.Sin(buildingData.m_angle)) * 8f;
            Vector2 a3 = new Vector2(a2.y, -a2.x);
            Quad2 quad;
            quad.a = a - ((float)width * 0.5f + 1.5f) * a2 - ((float)length * 0.5f + 1.5f) * a3;
            quad.b = a + ((float)width * 0.5f + 1.5f) * a2 - ((float)length * 0.5f + 1.5f) * a3;
            quad.c = a + ((float)width * 0.5f + 1.5f) * a2 + ((float)length * 0.5f + 1.5f) * a3;
            quad.d = a - ((float)width * 0.5f + 1.5f) * a2 + ((float)length * 0.5f + 1.5f) * a3;
            Vector2 vector = quad.Min();
            Vector2 vector2 = quad.Max();
            vector.y -= (float)buildingData.m_baseHeight;
            vector2.y += sizeY;
            int num = Mathf.Max((int)((vector.x - 72f) / 64f + 135f), 0);
            int num2 = Mathf.Max((int)((vector.y - 72f) / 64f + 135f), 0);
            int num3 = Mathf.Min((int)((vector2.x + 72f) / 64f + 140f), 269);
            int num4 = Mathf.Min((int)((vector2.y + 72f) / 64f + 140f), 269);
            BuildingManager instance = Singleton<BuildingManager>.instance;
            for (int i = num2; i <= num4; i++)
            {
                for (int j = num; j <= num3; j++)
                {
                    ushort num5 = instance.m_buildingGrid[i * 270 + j];
                    int num6 = 0;
                    while (num5 != 0)
                    {
                        var fireChance = 220u;
                        if (SaveData2.saveData.DifficultyLevel == DifficultyLevel.Hard)
                        {
                            fireChance = 170u;
                        }
                        else if (SaveData2.saveData.DifficultyLevel == DifficultyLevel.DwarfFortress)
                        {
                             fireChance = 100u;
                        }

                        if (num5 != buildingID && Singleton<SimulationManager>.instance.m_randomizer.Int32(fireChance) < damageAccumulation)
                        {
                            this.ExtraTrySpreadFire(quad, vector.y, vector2.y, num5, ref instance.m_buildings.m_buffer[(int)num5]);
                        }
                        num5 = instance.m_buildings.m_buffer[(int)num5].m_nextGridBuilding;
                        if (++num6 >= 32768)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
        }

        private void ExtraTrySpreadFire(Quad2 quad, float minY, float maxY, ushort buildingID, ref Building buildingData)
        {
            BuildingInfo info = buildingData.Info;
            int num;
            int num2;
            int num3;
            info.m_buildingAI.GetFireParameters(buildingID, ref buildingData, out num, out num2, out num3);
            ItemClass.CollisionType tmpIC = 0;
            if (num != 0 && (buildingData.m_flags & (Building.Flags.Completed | Building.Flags.Abandoned)) == Building.Flags.Completed
                && buildingData.m_fireIntensity == 0 && buildingData.GetLastFrameData().m_fireDamage == 0 && buildingData.OverlapQuad( buildingID, quad, minY, maxY,tmpIC))
            {
                if (fireSet.Contains(buildingID))
                {
                    return;
                }
                else
                {
                    var temp = "";
                    foreach (ushort fs in fireSet)
                    {
                        temp += fs.ToString();
                    }
                    fireQueue.Enqueue(buildingID);
                    fireSet.Add(buildingID);
                    if (fireSet.Count > 50)
                    {
                        fireSet.Remove(fireQueue.Dequeue());
                    }
                }
                float num4 = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(buildingData.m_position));
                if (num4 <= buildingData.m_position.y)
                {
                    Building.Flags flags = buildingData.m_flags;
                    buildingData.m_fireIntensity = (byte)num2;
                    info.m_buildingAI.BuildingDeactivated(buildingID, ref buildingData);
                    Building.Flags flags2 = buildingData.m_flags;
                    Singleton<BuildingManager>.instance.UpdateBuildingRenderer(buildingID, true);
                    if (flags2 != flags)
                    {
                        Singleton<BuildingManager>.instance.UpdateFlags(buildingID, flags2 ^ flags);
                    }
                }
            }
        }

    }

}