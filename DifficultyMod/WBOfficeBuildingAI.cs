using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace DifficultyMod
{
    public class WBOfficeBuildingAI5 : OfficeBuildingAI
    {
        private FireSpread fs = new FireSpread();
        protected override void SimulationStepActive(ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
        {
            base.SimulationStepActive(buildingID, ref buildingData, ref frameData);
            if (buildingData.m_fireIntensity != 0 && frameData.m_fireDamage > 12 && SaveData2.saveData.disastersEnabled)
            {
                fs.ExtraFireSpread(buildingID, ref buildingData, 60, this.m_info.m_size.y);
            }
        }

        public override float GetEventImpact(ushort buildingID, ref Building data, ImmaterialResourceManager.Resource resource, float amount)
        {
            if ((data.m_flags & (Building.Flags.Abandoned | Building.Flags.BurnedDown)) != Building.Flags.None)
            {
                return 0f;
            }
            switch (resource)
            {
                case ImmaterialResourceManager.Resource.FireDepartment:
                case ImmaterialResourceManager.Resource.PoliceDepartment:
                case ImmaterialResourceManager.Resource.PublicTransport:
                case ImmaterialResourceManager.Resource.Entertainment:
                    {
                        int num;
                        Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(resource, data.m_position, out num);
                        int num2 = ImmaterialResourceManager.CalculateResourceEffect(num, 60, 150, 30, 50);
                        int num3 = ImmaterialResourceManager.CalculateResourceEffect(num + Mathf.RoundToInt(amount), 60, 150, 30, 50);
                        return Mathf.Clamp((float)(num3 - num2) / 250f, -1f, 1f);
                    }
                case ImmaterialResourceManager.Resource.Abandonment:
                    {
                        int num16;
                        Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(resource, data.m_position, out num16);
                        int num17 = ImmaterialResourceManager.CalculateResourceEffect(num16, 60, 150, 30, 50);
                        int num18 = ImmaterialResourceManager.CalculateResourceEffect(num16 + Mathf.RoundToInt(amount), 60, 150, 30, 50);
                        return Mathf.Clamp((float)(num18 - num17) / 150, -1f, 1f);
                    }
                case ImmaterialResourceManager.Resource.NoisePollution:
                    {
                        int num19;
                        Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(resource, data.m_position, out num19);
                        int num20 = ImmaterialResourceManager.CalculateResourceEffect(num19, 60, 150, 30, 50);
                        int num21 = ImmaterialResourceManager.CalculateResourceEffect(num19 + Mathf.RoundToInt(amount), 60, 150, 30, 50);
                        return Mathf.Clamp((float)(num21 - num20) / 50f, -1f, 1f);
                    }

            }
            return base.GetEventImpact(buildingID, ref data, resource, amount);
        }
        
    }
}

