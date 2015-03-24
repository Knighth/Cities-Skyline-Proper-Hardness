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
    public class WBLevelUp9 : LevelUpExtensionBase
    {

        public static double GetPollutionFactor(ItemClass.Zone zone)
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

        public static double GetFactor(ItemClass.Zone zone, ImmaterialResourceManager.Resource resource)
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

        public static double GetPollutionScore(Building data, ItemClass.Zone zone)
        {
            byte resourceRate13;
            Singleton<NaturalResourceManager>.instance.CheckPollution(data.m_position, out resourceRate13);
            return ImmaterialResourceManager.CalculateResourceEffect((int)resourceRate13, 50, 255, 50, 100);
        }

        public static double GetServiceScore(ImmaterialResourceManager.Resource resource, ItemClass.Zone zone, ushort[] array, int num)
        {
            switch (resource)
            {
                case ImmaterialResourceManager.Resource.Entertainment:
                    return ImmaterialResourceManager.CalculateResourceEffect(array[num + (int)resource], 140, 500, 50, 100);
                case ImmaterialResourceManager.Resource.EducationElementary:
                case ImmaterialResourceManager.Resource.EducationHighSchool:
                case ImmaterialResourceManager.Resource.EducationUniversity:
                case ImmaterialResourceManager.Resource.DeathCare:
                    return ImmaterialResourceManager.CalculateResourceEffect(array[num + (int)resource], 35, 90, 50, 100);
            }
            return ImmaterialResourceManager.CalculateResourceEffect(array[num + (int)resource], 70, 240, 50, 100);
        }

        public static int GetProperServiceScore(ushort buildingID)
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

            return (int)num2;
        }

        public static void GetEducationHappyScore(ushort buildingID, out float education, out float happy,out float commute)
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
                GetHomeBehaviour(buildingID, data, ref behaviourData, ref alive, ref total, ref homeCount, ref aliveHomeCount, ref emptyHome);
                if (alive > 0)
                {
                    int num = behaviourData.m_educated1Count + behaviourData.m_educated2Count * 2 + behaviourData.m_educated3Count * 3;
                    int num2 = behaviourData.m_teenCount + behaviourData.m_youngCount * 2 + behaviourData.m_adultCount * 3 + behaviourData.m_seniorCount * 3;
                    education = 100 * num / (float)(alive * 6f);
                    happy = 100 * behaviourData.m_wellbeingAccumulation / (float)(alive * 255);
                    GetCommute(buildingID, data, out commute);
                    return;
                }
            }
            else
            {
                GetWorkBehaviour(buildingID, ref data, ref behaviourData, ref alive, ref total);
                if (alive > 0)
                {
                    int num = behaviourData.m_educated1Count + behaviourData.m_educated2Count * 2 + behaviourData.m_educated3Count * 3;
                    education = 100 * num / (float)(alive * 6f);
                    happy = 100 * behaviourData.m_wellbeingAccumulation / (float)(alive * 255);
                    GetCommute(buildingID, data, out commute);
                    return;
                }
            }

            education = 0;
            happy = 0;
            commute = 0;
        }

        public static void GetHomeBehaviour(ushort buildingID, Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount, ref int homeCount, ref int aliveHomeCount, ref int emptyHomeCount)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            uint num = buildingData.m_citizenUnits;
            int num2 = 0;
            while (num != 0u)
            {
            //    if ((ushort)(instance.m_units.m_buffer[(int)((UIntPtr)num)].m_flags & CitizenUnit.Flags.Home) != 0)
            //    {
                    int num3 = 0;
                    int num4 = 0;
                    instance.m_units.m_buffer[(int)((UIntPtr)num)].GetCitizenHomeBehaviour(ref behaviour, ref num3, ref num4);
                    if (num3 != 0)
                    {
                        aliveHomeCount++;
                        aliveCount += num3;
                    }
                    if (num4 != 0)
                    {
                        totalCount += num4;
                    }
                    else
                    {
                        emptyHomeCount++;
                    }
                    homeCount++;
                //}
                num = instance.m_units.m_buffer[(int)((UIntPtr)num)].m_nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }
        
        public static void GetWorkBehaviour(ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            uint num = buildingData.m_citizenUnits;
            int num2 = 0;
            while (num != 0u)
            {
                if ((ushort)(instance.m_units.m_buffer[(int)((UIntPtr)num)].m_flags & CitizenUnit.Flags.Work) != 0)
                {
                    instance.m_units.m_buffer[(int)((UIntPtr)num)].GetCitizenWorkBehaviour(ref behaviour, ref aliveCount, ref totalCount);
                }
                num = instance.m_units.m_buffer[(int)((UIntPtr)num)].m_nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }

        public static void GetCommute(ushort buildingID, Building buildingData, out float commute)
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

        public static void GetCommute(CitizenUnit unit, ref int commute,ref int count)
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

        public static void GetCommute(uint citizenID, Citizen cit, ref int commute, ref int count)
        {
        //    var ageGroup = Citizen.GetAgeGroup(cit.m_age);
        //    if (!cit.Dead)
        //    {
        //        var comm =  WBResidentAI4.GetCommute(citizenID);
        //        if (comm > 0){
        //            commute += comm;
        //            count += 1;
        //        }
        //    }
        }

        public static int GetServiceThreshhold(ItemClass.Level level, ItemClass.Zone zone)
        {
            switch (zone)
            {
                case ItemClass.Zone.Office:
                    if (level == ItemClass.Level.None)
                    {
                        return 0;
                    }
                    else if (level == ItemClass.Level.Level1)
                    {
                        return 40;
                    }
                    else if (level == ItemClass.Level.Level2)
                    {
                        return 94;
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
                        return 40;
                    }
                    else if (level == ItemClass.Level.Level2)
                    {
                        return 90;
                    }
                    else
                    {
                        return int.MaxValue;
                    }
                case ItemClass.Zone.ResidentialLow:
                case ItemClass.Zone.ResidentialHigh:
                    if (level == ItemClass.Level.None)
                    {
                        return 0;
                    }
                    else if (level == ItemClass.Level.Level1)
                    {
                        return 40;
                    }
                    else if (level == ItemClass.Level.Level2)
                    {
                        return 80;
                    }

                    else if (level == ItemClass.Level.Level3)
                    {
                        return 100;
                    }

                    else if (level == ItemClass.Level.Level4)
                    {
                        return 130;
                    }
                    else
                    {
                        return int.MaxValue;
                    }
                case ItemClass.Zone.CommercialLow:
                case ItemClass.Zone.CommercialHigh:
                    if (level == ItemClass.Level.None)
                    {
                        return 0;
                    }
                    else if (level == ItemClass.Level.Level1)
                    {
                        return 40;
                    }
                    else if (level == ItemClass.Level.Level2)
                    {
                        return 94;
                    }
                    else
                    {
                        return int.MaxValue;
                    }
            }
            return int.MaxValue;
        }

        public static int GetEducationThreshhold(ItemClass.Level level, ItemClass.Zone zone)
        {      
                if (level == ItemClass.Level.None)
                {
                    return 0;
                }
            if (zone == ItemClass.Zone.ResidentialHigh || zone == ItemClass.Zone.ResidentialLow)
            {
                if (level == ItemClass.Level.Level1)
                {
                    return 20;
                }
                else if (level == ItemClass.Level.Level2)
                {
                    return 35;
                }
                else if (level == ItemClass.Level.Level3)
                {
                    return 50;
                }
                else if (level == ItemClass.Level.Level4)
                {
                    return 70;
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
                    return 30;
                }
                else if (level == ItemClass.Level.Level2)
                {
                    return 60;
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
                    return 35;
                }
                else if (level == ItemClass.Level.Level2)
                {
                    return 70;
                }
                else
                {
                    return int.MaxValue;
                }
            }
        }


        public static int GetWealthThreshhold(ItemClass.Level level)
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

        public static int GetLandValueThreshhold(ItemClass.Level level)
        {
            var sd = SaveData2.saveData;
            if (level == ItemClass.Level.None)
            {
                return 0;
            }
            else if (level == ItemClass.Level.Level1)
            {
                if (sd.DifficultyLevel == DifficultyLevel.Normal)
                {
                    return 6;
                }
                else
                {
                    return 15;
                }
            }
            else if (level == ItemClass.Level.Level2)
            {
                if (sd.DifficultyLevel == DifficultyLevel.Normal)
                {
                    return 21;
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
                    return 41;
                }
                else
                {
                    return 60;
                }
            }
            else if (level == ItemClass.Level.Level4)
            {
                if (sd.DifficultyLevel == DifficultyLevel.Normal)
                {
                    return 61;
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

        public override ResidentialLevelUp OnCalculateResidentialLevelUp(ResidentialLevelUp levelUp, int averageEducation, int landValue, ushort buildingID, Service service, SubService subService, Level currentLevel)
        {
            if (SaveData2.saveData.DifficultyLevel == DifficultyLevel.Vanilla)
            {
                return levelUp;
            }

            var instance = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)buildingID];
            int buildingWealth = instance.m_customBuffer1;

            if (levelUp.landValueProgress != 0)
            {
                Level targetLevel = Level.Level5;
                for (var i = 0; i < 5; i += 1)
                {
                    if (landValue < GetLandValueThreshhold((ItemClass.Level)i) || (buildingWealth != 0 && buildingWealth < GetWealthThreshhold((ItemClass.Level)i)))
                    {
                        targetLevel = (Level)i;
                        levelUp.landValueProgress = CalcProgress(landValue, GetLandValueThreshhold((ItemClass.Level)i), GetLandValueThreshhold((ItemClass.Level)(i - 1)), 8) + CalcProgress(buildingWealth, GetWealthThreshhold((ItemClass.Level)i), GetWealthThreshhold((ItemClass.Level)(i - 1)), 8);
                        break;
                    }
                }

                if (targetLevel < currentLevel)
                {
                    levelUp.landValueProgress = 1;
                }
                else if (targetLevel > currentLevel)
                {
                    levelUp.landValueProgress = 15;
                }

                if (targetLevel < levelUp.targetLevel)
                {
                    levelUp.targetLevel = targetLevel;
                }
            }

            levelUp.landValueTooLow = false;
            if (currentLevel == Level.Level2)
            {
                if (landValue == 0)
                {
                    levelUp.landValueTooLow = true;
                }
            }
            else if (currentLevel == Level.Level3)
            {
                if (landValue < GetLandValueThreshhold(ItemClass.Level.Level1) || (buildingWealth != 0 && buildingWealth < GetWealthThreshhold(ItemClass.Level.Level1)))
                {
                    levelUp.landValueTooLow = true;
                }
            }
            else if (currentLevel == Level.Level4)
            {
                if (landValue < GetLandValueThreshhold(ItemClass.Level.Level2) || (buildingWealth != 0 && buildingWealth < GetWealthThreshhold(ItemClass.Level.Level2)))
                {
                    levelUp.landValueTooLow = true;
                }
            }
            else if (currentLevel == Level.Level5)
            {
                if (landValue < GetLandValueThreshhold(ItemClass.Level.Level3) || (buildingWealth != 0 && buildingWealth < GetWealthThreshhold(ItemClass.Level.Level3)))
                {
                    levelUp.landValueTooLow = true;
                }
            }

            return levelUp;
        }

        private int CalcProgress(int val, int max, int previous, int multiplier)
        {
            return Math.Max(0, Math.Min(val - previous, max)) * multiplier / max;
        }

        public override CommercialLevelUp OnCalculateCommercialLevelUp(CommercialLevelUp levelUp, int averageWealth, int landValue, ushort buildingID, Service service, SubService subService, Level currentLevel)
        {
            if (SaveData2.saveData.DifficultyLevel == DifficultyLevel.Vanilla)
            {
                return levelUp;
            }

            if (levelUp.landValueProgress != 0)
            {
                Level targetLevel;

                if (landValue < 30)
                {
                    targetLevel = Level.Level1;
                    levelUp.landValueProgress = 1 + (landValue * 15 + 15) / 30;
                }
                else if (landValue < 80)
                {
                    targetLevel = Level.Level2;
                    levelUp.landValueProgress = 1 + ((landValue - 30) * 15 + 15) / 30;
                }
                else
                {
                    targetLevel = Level.Level5;
                    levelUp.landValueProgress = 1;
                }

                if (targetLevel < currentLevel)
                {
                    levelUp.landValueProgress = 1;
                }
                else if (targetLevel > currentLevel)
                {
                    levelUp.landValueProgress = 15;
                }

                if (targetLevel < levelUp.targetLevel)
                {
                    levelUp.targetLevel = targetLevel;
                }
            }

            levelUp.landValueTooLow = false;
            if (currentLevel == Level.Level2)
            {
                if (landValue < 15) levelUp.landValueTooLow = true;
            }
            else if (currentLevel == Level.Level3)
            {
                if (landValue < 40) levelUp.landValueTooLow = true;
            }

            return levelUp;
        }

        public override OfficeLevelUp OnCalculateOfficeLevelUp(OfficeLevelUp levelUp, int averageEducation, int serviceScore, ushort buildingID, Service service, SubService subService, Level currentLevel)
        {
            if (SaveData2.saveData.DifficultyLevel == DifficultyLevel.Vanilla)
            {
                return levelUp;
            }

            serviceScore = GetProperServiceScore(buildingID);
            Level targetLevel = Level.Level3;

            for (var i = 0; i < 3; i += 1)
            {
                if (serviceScore < GetServiceThreshhold((ItemClass.Level)i,ItemClass.Zone.Office))
                {
                    targetLevel = (Level)i;
                    levelUp.serviceProgress = 1 + CalcProgress(serviceScore, GetServiceThreshhold((ItemClass.Level)i, ItemClass.Zone.Office), GetServiceThreshhold((ItemClass.Level)(i - 1), ItemClass.Zone.Office), 15);
                    break;
                }
            }

            levelUp.tooFewServices = (serviceScore < GetServiceThreshhold((ItemClass.Level)(Math.Max(-1, (int)currentLevel - 2)), ItemClass.Zone.Office));

            if (targetLevel < currentLevel)
            {
                levelUp.serviceProgress = 1;
            }
            else if (targetLevel > currentLevel)
            {
                levelUp.serviceProgress = 15;
            }
            if (targetLevel < levelUp.targetLevel)
            {
                levelUp.targetLevel = targetLevel;
            }
            return levelUp;
        }

        public override IndustrialLevelUp OnCalculateIndustrialLevelUp(IndustrialLevelUp levelUp, int averageEducation, int serviceScore, ushort buildingID, Service service, SubService subService, Level currentLevel)
        {
            if (SaveData2.saveData.DifficultyLevel == DifficultyLevel.Vanilla)
            {
                return levelUp;
            }

            serviceScore = GetProperServiceScore(buildingID);
            Level targetLevel = Level.Level3;

            for (var i = 0; i < 3; i += 1)
            {
                if (serviceScore < GetServiceThreshhold((ItemClass.Level)i,ItemClass.Zone.Industrial))
                {
                    targetLevel = (Level)i;
                    levelUp.serviceProgress = 1 + CalcProgress(serviceScore, GetServiceThreshhold((ItemClass.Level)i, ItemClass.Zone.Industrial), GetServiceThreshhold((ItemClass.Level)(i - 1), ItemClass.Zone.Industrial), 15);
                    break;
                }
            }

            levelUp.tooFewServices = (serviceScore < GetServiceThreshhold((ItemClass.Level)(Math.Max(-1, (int)currentLevel - 2)), ItemClass.Zone.Industrial));

            if (targetLevel < currentLevel)
            {
                levelUp.serviceProgress = 1;
            }
            else if (targetLevel > currentLevel)
            {
                levelUp.serviceProgress = 15;
            }
            if (targetLevel < levelUp.targetLevel)
            {
                levelUp.targetLevel = targetLevel;
            }
            return levelUp;
        }

    }

    public class HardModeEconomy : EconomyExtensionBase
    {

        public override int OnGetConstructionCost(int originalConstructionCost, Service service, SubService subService, Level level)
        {
            if (SaveData2.saveData.DifficultyLevel != DifficultyLevel.Hard)
            {
                return originalConstructionCost;
            }

            var multiplier = 1.4;
            if (originalConstructionCost > 30000000)
            {
                multiplier = 20;
            }
            else if (subService == SubService.PublicTransportMetro)
            {
                if (originalConstructionCost > 10000)
                {
                    multiplier = 3;
                }
                else
                {
                    multiplier = 6;
                }

            }
            else
            {
                switch (service)
                {
                    case Service.Education:
                        multiplier = 2;
                        break;
                    case Service.Monument:
                        multiplier = 2;
                        break;
                    case Service.Road:
                        if (originalConstructionCost >= 7000)
                        {
                            multiplier = 5;
                        }
                        else
                        {
                            multiplier = 2.5;
                        }
                        break;
                }
            }
            return (int)Math.Min(Math.Round((originalConstructionCost * multiplier), 2), int.MaxValue);
        }

        public override int OnGetMaintenanceCost(int originalMaintenanceCost, Service service, SubService subService, Level level)
        {
            if (SaveData2.saveData.DifficultyLevel != DifficultyLevel.Hard)
            {
                return originalMaintenanceCost;
            }

            var multiplier = 1.6;

            switch (service)
            {
                case Service.Education:
                    multiplier = 2.0;
                    break;
                case Service.Road:
                    multiplier = 2.0;
                    break;
                case Service.Garbage:
                    multiplier = 1;
                    break;
            }
            return (int)(originalMaintenanceCost * multiplier);
        }

        public override int OnGetRelocationCost(int constructionCost, int relocationCost, Service service, SubService subService, Level level)
        {
            if (SaveData2.saveData.DifficultyLevel != DifficultyLevel.Hard)
            {
                return constructionCost;
            }
            return constructionCost / 2;
        }
    }

    public class UnlockAllMilestones : MilestonesExtensionBase
    {

        public override void OnRefreshMilestones()
        {
            milestonesManager.UnlockMilestone("Basic Road Created");
        }
    }

    public class NoMoneyFromMilestones : EconomyExtensionBase
    {
        public override int OnAddResource(EconomyResource resource, int amount, Service service, SubService subService, Level level)
        {
            if (SaveData2.saveData.DifficultyLevel != DifficultyLevel.Hard)
            {
                return amount;
            }

            if (resource == EconomyResource.RewardAmount)
            {
                return amount / 4;
            }
            return amount;
        }
    }

    public class UnlockAreas : IAreasExtension
    {
        public void OnCreated(IAreas areas)
        {
            areas.maxAreaCount = 25;
        }

        public void OnReleased()
        {

        }

        public bool OnCanUnlockArea(int x, int z, bool originalResult)
        {
            return originalResult;
        }

        public int OnGetAreaPrice(uint ore, uint oil, uint forest, uint fertility, uint water, bool road, bool train, bool ship, bool plane, float landFlatness, int originalPrice)
        {
            if (SaveData2.saveData.DifficultyLevel != DifficultyLevel.Hard)
            {
                return originalPrice;
            }
            return (int)Math.Min(int.MaxValue, Math.Round(Math.Pow(originalPrice * 2.0, 1.2), 3));
        }

        public void OnUnlockArea(int x, int z)
        {

        }
    }

}
