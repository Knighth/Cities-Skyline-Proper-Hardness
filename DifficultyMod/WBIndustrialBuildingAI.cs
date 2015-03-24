using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace DifficultyMod
{
    public class WBIndustrialBuildingAI7 : IndustrialBuildingAI
    {
        private FireSpread fs = new FireSpread();
        protected override void SimulationStepActive(ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
        {
            if (Singleton<SimulationManager>.instance.m_randomizer.Int32(12u) == 0)
            {
                int num16;
                int num17;
                this.GetPollutionRates(10, out num16, out num17);
                Singleton<NaturalResourceManager>.instance.TryDumpResource(NaturalResourceManager.Resource.Pollution, num16, num16, buildingData.m_position, 250f);
            }

            base.SimulationStepActive(buildingID, ref buildingData, ref frameData);

            if (buildingData.m_fireIntensity != 0 && frameData.m_fireDamage > 12 && SaveData2.saveData.disastersEnabled)
            {
                fs.ExtraFireSpread(buildingID, ref buildingData, 60, this.m_info.m_size.y);
            }

            if ((buildingData.m_flags & Building.Flags.BurnedDown) != Building.Flags.None || (buildingData.m_flags & Building.Flags.Abandoned) != Building.Flags.None)
            {
                float radius = (float)(buildingData.Width + buildingData.Length) * 15.0f;
                Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Abandonment, 20, buildingData.m_position, radius);
            }
            else if (buildingData.m_fireIntensity == 0)
            {
                DistrictManager instance = Singleton<DistrictManager>.instance;
                byte district = instance.GetDistrict(buildingData.m_position);
                DistrictPolicies.Taxation taxationPolicies = instance.m_districts.m_buffer[(int)district].m_taxationPolicies;
                DistrictPolicies.Services servicePolicies = instance.m_districts.m_buffer[(int)district].m_servicePolicies;

                int baseIncome = CitizenHelper.GetBaseIncome(buildingData.Info.m_class.m_level, buildingData.Info.m_class.GetZone());
                if ((servicePolicies & DistrictPolicies.Services.Recycling) != DistrictPolicies.Services.None)
                {
                    baseIncome = baseIncome * 95 / 100;
                }

                int income = 0;
                GetCitizenIncome(buildingID, ref buildingData, ref income);

                income = (income * baseIncome + 9999) / 10000;
                int percentage = 100;
                if (buildingData.m_electricityProblemTimer >= 1 || buildingData.m_waterProblemTimer >= 1 || buildingData.m_waterProblemTimer >= 1 || buildingData.m_garbageBuffer > 60000 || buildingData.m_outgoingProblemTimer >= 128 || buildingData.m_customBuffer1 == 0)
                {
                    percentage = 0;
                }
                if (this.CanSufferFromFlood())
                {
                    float num18 = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(buildingData.m_position));
                    if (num18 > buildingData.m_position.y)
                    {
                        percentage = 0;
                    }
                }

                income = (income * percentage + 99) / 100;
                if (income > 0)
                {
                    Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.PrivateIncome, -income, this.m_info.m_class, taxationPolicies);
                }
            }
        }

        private void GetCitizenIncome(ushort buildingID, ref Building buildingData, ref int income)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            uint num = buildingData.m_citizenUnits;
            int num2 = 0;
            while (num != 0u)
            {
                if ((ushort)(instance.m_units.m_buffer[(int)((UIntPtr)num)].m_flags & CitizenUnit.Flags.Work) != 0)
                {
                    CitizenHelper.GetCitizenIncome(instance.m_units.m_buffer[(int)((UIntPtr)num)], ref income);
                }
                num = instance.m_units.m_buffer[(int)((UIntPtr)num)].m_nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }

        public override float GetEventImpact(ushort buildingID, ref Building data, ImmaterialResourceManager.Resource resource, float amount)
        {
            if ((data.m_flags & (Building.Flags.Abandoned | Building.Flags.BurnedDown)) != Building.Flags.None)
            {
                return 0f;
            }
            float result = WBLevelUp9.GetEventImpact(buildingID, data, resource, amount);
            if (result != 0)
            {
                return result;
            }
            else {                
                return base.GetEventImpact(buildingID, ref data, resource, amount);
            }            
        }
    }
}
