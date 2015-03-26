using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace DifficultyMod
{
    public class WBOfficeBuildingAI2 : OfficeBuildingAI
    {
        private FireSpread fs = new FireSpread();
        protected override void SimulationStepActive(ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
        {
            base.SimulationStepActive(buildingID, ref buildingData, ref frameData);
            if (buildingData.m_fireIntensity != 0 && frameData.m_fireDamage > 12 && SaveData2.saveData.disastersEnabled)
            {
                fs.ExtraFireSpread(buildingID, ref buildingData, 60, this.m_info.m_size.y);
            }

            if ((buildingData.m_flags & Building.Flags.BurnedDown) != Building.Flags.None || (buildingData.m_flags & Building.Flags.Abandoned) != Building.Flags.None)
            {
                float radius = (float)(buildingData.Width + buildingData.Length) * 25.0f;
                Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Abandonment, 40, buildingData.m_position, radius);
            }
            else if (buildingData.m_fireIntensity == 0)
            {
                int income = 0;
                int tourists = 0;
                CitizenHelper.instance.GetIncome(buildingID, buildingData, ref income, ref tourists);
                DistrictManager instance = Singleton<DistrictManager>.instance;
                byte district = instance.GetDistrict(buildingData.m_position);
                DistrictPolicies.Taxation taxationPolicies = instance.m_districts.m_buffer[(int)district].m_taxationPolicies;
                if (income > 0)
                {
                    Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.PrivateIncome, -income, this.m_info.m_class, taxationPolicies);
                }
            }
        }

        public override float GetEventImpact(ushort buildingID, ref Building data, ImmaterialResourceManager.Resource resource, float amount)
        {
            if ((data.m_flags & (Building.Flags.Abandoned | Building.Flags.BurnedDown)) != Building.Flags.None)
            {
                return 0f;
            }
            float result = LevelUpHelper3.instance.GetEventImpact(buildingID, data, resource, amount);
            if (result != 0)
            {
                return result;
            }
            else
            {
                return base.GetEventImpact(buildingID, ref data, resource, amount);
            }
        }
        
    }
}

