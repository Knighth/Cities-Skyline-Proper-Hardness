using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DifficultyMod
{
    class LevelUpHelper3 : MonoBehaviour
    {
        private static LevelUpHelper3 m_instance;
        public static LevelUpHelper3 instance
        {
            get
            {
                if (m_instance == null)
				{
                    m_instance = new LevelUpHelper3();
                    //m_instance = UnityEngine.Object.FindObjectOfType<LevelUpHelper4>();
                    //if (m_instance == null)
                    //{
                    //    GameObject gameObject = new GameObject(typeof(LevelUpHelper4).Name);
                    //    if (!SingletonConfig.hide)
                    //    {
                    //        GameObject gameObject2 = GameObject.Find(SingletonConfig.singletonRootName);
                    //        if (gameObject2 == null)
                    //        {
                    //            gameObject2 = new GameObject(SingletonConfig.singletonRootName);
                    //            UnityEngine.Object.DontDestroyOnLoad(gameObject2);
                    //        }
                    //        gameObject.transform.parent = gameObject2.transform;
                    //    }
                    //    else
                    //    {
                    //        gameObject.hideFlags = HideFlags.HideInHierarchy;
                    //    }
                    //    m_instance = gameObject.AddComponent<LevelUpHelper4>();
                    //    if (m_instance == null)
                    //    {
                    //        m_instance = new LevelUpHelper4;
                    //    }
                    //}
				}
				return m_instance;
            }
        }

        public double GetPollutionFactor(ItemClass.Zone zone)
        {
            if (zone == ItemClass.Zone.ResidentialHigh || zone == ItemClass.Zone.ResidentialLow)
            {
                return -0.2;
            }
            else if (zone == ItemClass.Zone.Office)
            {
                return -0.05;
            }
            return 0;
        }

        public double GetFactor(ItemClass.Zone zone, ImmaterialResourceManager.Resource resource)
        {

            if (zone == ItemClass.Zone.ResidentialLow || zone == ItemClass.Zone.ResidentialHigh)
            {
                switch (resource)
                {
                    case ImmaterialResourceManager.Resource.EducationElementary:
                    case ImmaterialResourceManager.Resource.EducationHighSchool:
                    case ImmaterialResourceManager.Resource.EducationUniversity:
                    case ImmaterialResourceManager.Resource.HealthCare:
                    case ImmaterialResourceManager.Resource.FireDepartment:
                    case ImmaterialResourceManager.Resource.PoliceDepartment:
                    case ImmaterialResourceManager.Resource.PublicTransport:
                    case ImmaterialResourceManager.Resource.DeathCare:
                        return 0.1;
                    case ImmaterialResourceManager.Resource.Entertainment:
                        return 0.2;
                }
            }
            switch (resource)
            {
                case ImmaterialResourceManager.Resource.FireDepartment:
                case ImmaterialResourceManager.Resource.PoliceDepartment:
                case ImmaterialResourceManager.Resource.PublicTransport:
                    return 0.3;
                case ImmaterialResourceManager.Resource.Abandonment:
                    return -0.2;
                case ImmaterialResourceManager.Resource.Entertainment:
                    if (zone == ItemClass.Zone.Office || zone == ItemClass.Zone.CommercialHigh || zone == ItemClass.Zone.CommercialLow)
                    {
                        return 0.1;
                    }
                    break;
                case ImmaterialResourceManager.Resource.NoisePollution:
                    if (zone == ItemClass.Zone.Office || zone == ItemClass.Zone.ResidentialHigh || zone == ItemClass.Zone.ResidentialLow)
                    {
                        return -0.2;
                    }
                    break;
                case ImmaterialResourceManager.Resource.CargoTransport:
                    if (zone == ItemClass.Zone.Industrial)
                    {
                        return 0.1;
                    }
                    break;
            }

            return 0;
        }

        public double GetPollutionScore(Building data, ItemClass.Zone zone)
        {
            byte resourceRate13;
            Singleton<NaturalResourceManager>.instance.CheckPollution(data.m_position, out resourceRate13);
            return ImmaterialResourceManager.CalculateResourceEffect((int)resourceRate13, 50, 255, 50, 100);
        }

        internal float GetEventImpact(ushort buildingID, Building data, ImmaterialResourceManager.Resource resource, float amount)
        {
            var factor = GetFactor(data.Info.m_class.GetZone(), resource);
            var sign = 1;
            if (factor == 0)
            {
                return 0;
            }
            else if (factor < 0)
            {
                sign = -1;
            }
            int num;
            Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(resource, data.m_position, out num);
            var after = GetServiceScore(Mathf.RoundToInt(amount) + num, resource);
            var before = GetServiceScore(num, resource);
            return sign * Mathf.Clamp((float)(after - before) / 100, -1f, 1f);
        }

        public double GetServiceScore(int resourceRate, ImmaterialResourceManager.Resource resource)
        {
            switch (resource)
            {
                case ImmaterialResourceManager.Resource.Entertainment:
                    return ImmaterialResourceManager.CalculateResourceEffect(resourceRate, 140, 340, 50, 100);
                case ImmaterialResourceManager.Resource.EducationElementary:
                case ImmaterialResourceManager.Resource.EducationHighSchool:
                case ImmaterialResourceManager.Resource.EducationUniversity:
                case ImmaterialResourceManager.Resource.PublicTransport:
                    return ImmaterialResourceManager.CalculateResourceEffect(resourceRate, 65, 130, 50, 100);
                case ImmaterialResourceManager.Resource.DeathCare:
                    return ImmaterialResourceManager.CalculateResourceEffect(resourceRate, 35, 90, 50, 100);
            }
            return ImmaterialResourceManager.CalculateResourceEffect(resourceRate, 80, 220, 50, 100);
        }

        public double GetServiceScore(ImmaterialResourceManager.Resource resource, ItemClass.Zone zone, ushort[] array, int num)
        {
            return GetServiceScore(array[num + (int)resource], resource);
        }

        public int GetProperServiceScore(ushort buildingID)
        {
            Building data = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)buildingID];
            ushort[] array;
            int num;
            Singleton<ImmaterialResourceManager>.instance.CheckLocalResources(data.m_position, out array, out num);
            double num2 = 0;
            var zone = data.Info.m_class.GetZone();
            for (var i = 0; i < 20; i += 1)
            {
                var imr = (ImmaterialResourceManager.Resource)i;
                num2 += GetServiceScore(imr, zone, array, num) * GetFactor(zone, imr);
            }

            num2 -= GetPollutionScore(data, zone) * GetPollutionFactor(zone);

            return Math.Max(0, (int)num2);
        }

        public void GetEducationHappyScore(ushort buildingID, out float education, out float happy, out float commute)
        {
            Citizen.BehaviourData behaviourData = default(Citizen.BehaviourData);
            Building data = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)buildingID];
            ItemClass.Zone zone = data.Info.m_class.GetZone();

            int alive = 0;
            int total = 0;
            int homeCount = 0;
            int aliveHomeCount = 0;
            int emptyHome = 0;

            if (zone == ItemClass.Zone.ResidentialLow || zone == ItemClass.Zone.ResidentialHigh)
            {
                CitizenHelper.instance.GetHomeBehaviour(buildingID, data, ref behaviourData, ref alive, ref total, ref homeCount, ref aliveHomeCount, ref emptyHome);
                if (alive > 0)
                {
                    int num = behaviourData.m_educated1Count + behaviourData.m_educated2Count * 3 / 2 + behaviourData.m_educated3Count * 2;
                    int num2 = behaviourData.m_teenCount + behaviourData.m_youngCount + behaviourData.m_adultCount + behaviourData.m_seniorCount;
                    education = (100 * num) / (float)(num2 * 2f);
                    happy = 100 * behaviourData.m_wellbeingAccumulation / (float)(alive * 255);
                    GetCommute(buildingID, data, out commute);
                    return;
                }
            }
            else if (zone == ItemClass.Zone.CommercialHigh || zone == ItemClass.Zone.CommercialLow)
            {
                CitizenHelper.instance.GetVisitBehaviour(buildingID, data, ref behaviourData, ref alive, ref total);
                if (alive > 0)
                {
                    int num = behaviourData.m_wealth1Count + behaviourData.m_wealth2Count * 2 + behaviourData.m_wealth3Count * 2;
                    education = (100 * num) / (float)(alive * 2f);
                    happy = 100 * behaviourData.m_wellbeingAccumulation / (float)(alive * 255);
                    GetCommute(buildingID, data, out commute);
                    return;
                }
            }
            else
            {
                CitizenHelper.instance.GetWorkBehaviour(buildingID, data, ref behaviourData, ref alive, ref total);
                if (alive > 0)
                {
                    int num = behaviourData.m_educated1Count + behaviourData.m_educated2Count * 3 / 2 + behaviourData.m_educated3Count * 2;
                    education = (100 * num) / (float)(alive * 2f);
                    happy = 100 * behaviourData.m_wellbeingAccumulation / (float)(alive * 255);
                    GetCommute(buildingID, data, out commute);
                    return;
                }
            }

            education = 0;
            happy = 0;
            commute = 0;
        }

        public void GetCommute(ushort buildingID, Building buildingData, out float commute)
        {
            int count = 0;
            int commuteTotal = 0;
            CitizenManager instance = Singleton<CitizenManager>.instance;
            uint num = buildingData.m_citizenUnits;
            int num2 = 0;
            while (num != 0u)
            {
                GetCommute(instance.m_units.m_buffer[(int)((UIntPtr)num)], ref commuteTotal, ref count);
                num = instance.m_units.m_buffer[(int)((UIntPtr)num)].m_nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
            if (count <= 0)
            {
                commute = 0;
            }
            else
            {
                commute = (commuteTotal * 100) / (float)(count * 255);
            }
        }

        public void GetCommute(CitizenUnit unit, ref int commute, ref int count)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            if (unit.m_citizen0 != 0u)
            {
                GetCommute(unit.m_citizen0, instance.m_citizens.m_buffer[unit.m_citizen0], ref commute, ref count);
            }
            if (unit.m_citizen1 != 0u)
            {
                GetCommute(unit.m_citizen1, instance.m_citizens.m_buffer[unit.m_citizen1], ref commute, ref count);
            }
            if (unit.m_citizen2 != 0u)
            {
                GetCommute(unit.m_citizen2, instance.m_citizens.m_buffer[unit.m_citizen2], ref commute, ref count);
            }
            if (unit.m_citizen3 != 0u)
            {
                GetCommute(unit.m_citizen3, instance.m_citizens.m_buffer[unit.m_citizen3], ref commute, ref count);
            }
            if (unit.m_citizen4 != 0u)
            {
                GetCommute(unit.m_citizen4, instance.m_citizens.m_buffer[unit.m_citizen4], ref commute, ref count);
            }
        }

        public void GetCommute(uint citizenID, Citizen cit, ref int commute, ref int count)
        {
            if (!cit.Dead)
            {
                var comm = WBResidentAI6.GetCommute(citizenID);
                if (comm > 0)
                {
                    commute += comm;
                    count += 1;
                }
            }
        }

        public int GetServiceThreshhold(ItemClass.Level level, ItemClass.Zone zone)
        {
            var sd = SaveData2.saveData;
            switch (zone)
            {
                case ItemClass.Zone.Office:
                    if (level == ItemClass.Level.None)
                    {
                        return 0;
                    }
                    else if (level == ItemClass.Level.Level1)
                    {
                        if (sd.DifficultyLevel == DifficultyLevel.Normal)
                        {
                            return 25;
                        }
                        else
                        {
                            return 35;
                        }
                    }
                    else if (level == ItemClass.Level.Level2)
                    {
                        if (sd.DifficultyLevel == DifficultyLevel.Normal)
                        {
                            return 75;
                        }
                        else
                        {
                            return 85;
                        }
                    }
                    else
                    {
                        return int.MaxValue;
                    }
                case ItemClass.Zone.Industrial:
                    if (level == ItemClass.Level.None)
                    {
                        return 0;
                    }
                    else if (level == ItemClass.Level.Level1)
                    {
                        if (sd.DifficultyLevel == DifficultyLevel.Normal)
                        {
                            return 30;
                        }
                        else
                        {
                            return 40;
                        }
                    }
                    else if (level == ItemClass.Level.Level2)
                    {

                        if (sd.DifficultyLevel == DifficultyLevel.Normal)
                        {
                            return 60;
                        }
                        else
                        {
                            return 85;
                        }
                    }
                    else
                    {
                        return int.MaxValue;
                    }
                case ItemClass.Zone.ResidentialLow:
                    if (level == ItemClass.Level.None)
                    {
                        return 0;
                    }
                    else if (level == ItemClass.Level.Level1)
                    {

                        if (sd.DifficultyLevel == DifficultyLevel.Normal)
                        {
                            return 15;
                        }
                        else
                        {
                            return 25;
                        }
                    }
                    else if (level == ItemClass.Level.Level2)
                    {

                        if (sd.DifficultyLevel == DifficultyLevel.Normal)
                        {
                            return 25;
                        }
                        else
                        {
                            return 35;
                        }
                    }

                    else if (level == ItemClass.Level.Level3)
                    {

                        if (sd.DifficultyLevel == DifficultyLevel.Normal)
                        {
                            return 45;
                        }
                        else
                        {
                            return 50;
                        }
                    }

                    else if (level == ItemClass.Level.Level4)
                    {

                        if (sd.DifficultyLevel == DifficultyLevel.Normal)
                        {
                            return 50;
                        }
                        else
                        {
                            return 65;
                        }
                    }
                    else
                    {
                        return int.MaxValue;
                    }
                case ItemClass.Zone.ResidentialHigh:
                    if (level == ItemClass.Level.None)
                    {
                        return 0;
                    }
                    else if (level == ItemClass.Level.Level1)
                    {

                        if (sd.DifficultyLevel == DifficultyLevel.Normal)
                        {
                            return 15;
                        }
                        else
                        {
                            return 25;
                        }
                    }
                    else if (level == ItemClass.Level.Level2)
                    {

                        if (sd.DifficultyLevel == DifficultyLevel.Normal)
                        {
                            return 30;
                        }
                        else
                        {
                            return 50;
                        }
                    }

                    else if (level == ItemClass.Level.Level3)
                    {

                        if (sd.DifficultyLevel == DifficultyLevel.Normal)
                        {
                            return 50;
                        }
                        else
                        {
                            return 70;
                        }
                    }

                    else if (level == ItemClass.Level.Level4)
                    {

                        if (sd.DifficultyLevel == DifficultyLevel.Normal)
                        {
                            return 65;
                        }
                        else
                        {
                            return 88;
                        }
                    }
                    else
                    {
                        return int.MaxValue;
                    }
                case ItemClass.Zone.CommercialLow:
                    if (level == ItemClass.Level.None)
                    {
                        return 0;
                    }
                    else if (level == ItemClass.Level.Level1)
                    {

                        if (sd.DifficultyLevel == DifficultyLevel.Normal)
                        {
                            return 25;
                        }
                        else
                        {
                            return 40;
                        }
                    }
                    else if (level == ItemClass.Level.Level2)
                    {
                        if (sd.DifficultyLevel == DifficultyLevel.Normal)
                        {
                            return 35;
                        }
                        else
                        {
                            return 60;
                        }
                    }
                    else
                    {
                        return int.MaxValue;
                    }
                case ItemClass.Zone.CommercialHigh:
                    if (level == ItemClass.Level.None)
                    {
                        return 0;
                    }
                    else if (level == ItemClass.Level.Level1)
                    {

                        if (sd.DifficultyLevel == DifficultyLevel.Normal)
                        {
                            return 30;
                        }
                        else
                        {
                            return 45;
                        }
                    }
                    else if (level == ItemClass.Level.Level2)
                    {
                        if (sd.DifficultyLevel == DifficultyLevel.Normal)
                        {
                            return 65;
                        }
                        else
                        {
                            return 88;
                        }
                    }
                    else
                    {
                        return int.MaxValue;
                    }
            }
            return int.MaxValue;
        }

        public int GetEducationThreshhold(ItemClass.Level level, ItemClass.Zone zone)
        {
            var sd = SaveData2.saveData;
            if (level == ItemClass.Level.None)
            {
                return 0;
            }
            if (zone == ItemClass.Zone.ResidentialHigh || zone == ItemClass.Zone.ResidentialLow)
            {
                if (level == ItemClass.Level.Level1)
                {
                    if (sd.DifficultyLevel == DifficultyLevel.Normal)
                    {
                        return 20;
                    }
                    else
                    {
                        return 30;
                    }
                }
                else if (level == ItemClass.Level.Level2)
                {
                    if (sd.DifficultyLevel == DifficultyLevel.Normal)
                    {
                        return 35;
                    }
                    else
                    {
                        return 50;
                    }
                }
                else if (level == ItemClass.Level.Level3)
                {
                    if (sd.DifficultyLevel == DifficultyLevel.Normal)
                    {
                        return 50;
                    }
                    else
                    {
                        return 68;
                    }
                }
                else if (level == ItemClass.Level.Level4)
                {
                    if (sd.DifficultyLevel == DifficultyLevel.Normal)
                    {
                        return 60;
                    }
                    else
                    {
                        return 83;
                    }
                }
                else
                {
                    return int.MaxValue;
                }
            }
            else if (zone == ItemClass.Zone.Industrial)
            {
                if (level == ItemClass.Level.Level1)
                {
                    if (sd.DifficultyLevel == DifficultyLevel.Normal)
                    {
                        return 20;
                    }
                    else
                    {
                        return 35;
                    }
                }
                else if (level == ItemClass.Level.Level2)
                {
                    if (sd.DifficultyLevel == DifficultyLevel.Normal)
                    {
                        return 40;
                    }
                    else
                    {
                        return 65;
                    }
                }
                else
                {
                    return int.MaxValue;
                }
            }
            else
            {

                if (level == ItemClass.Level.Level1)
                {
                    if (sd.DifficultyLevel == DifficultyLevel.Normal)
                    {
                        return 25;
                    }
                    else
                    {
                        return 50;
                    }
                }
                else if (level == ItemClass.Level.Level2)
                {
                    if (sd.DifficultyLevel == DifficultyLevel.Normal)
                    {
                        return 50;
                    }
                    else
                    {
                        return 85;
                    }
                }
                else
                {
                    return int.MaxValue;
                }
            }
        }


        public int GetWealthThreshhold(ItemClass.Level level, ItemClass.Zone zone)
        {
            var sd = SaveData2.saveData;
            if (zone == ItemClass.Zone.CommercialLow || zone == ItemClass.Zone.CommercialHigh)
            {
                if (level == ItemClass.Level.None)
                {
                    return 0;
                }
                else if (level == ItemClass.Level.Level1)
                {
                    if (sd.DifficultyLevel == DifficultyLevel.Normal)
                    {
                        return 30;
                    }
                    else
                    {
                        return 45;
                    }
                }
                else if (level == ItemClass.Level.Level2)
                {
                    if (sd.DifficultyLevel == DifficultyLevel.Normal)
                    {
                        return 55;
                    }
                    else
                    {
                        return 80;
                    }
                }
                else
                {
                    return int.MaxValue;
                }
            }
            else
            {
                if (level == ItemClass.Level.None)
                {
                    return 0;
                }
                else if (level == ItemClass.Level.Level1)
                {
                    return 350;
                }
                else if (level == ItemClass.Level.Level2)
                {
                    return 700;
                }
                else if (level == ItemClass.Level.Level3)
                {
                    return 1100;
                }
                else if (level == ItemClass.Level.Level4)
                {
                    return 1600;
                }
                else
                {
                    return int.MaxValue;
                }
            }
        }

    }
}
