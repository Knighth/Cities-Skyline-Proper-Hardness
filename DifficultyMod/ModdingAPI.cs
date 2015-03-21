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
    public class WBLevelUp5 : LevelUpExtensionBase
    {
        public static int GetServiceOfficeThreshhold(ItemClass.Level level)
        {
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

        public static int GetServiceIndustryThreshhold(ItemClass.Level level)
        {
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
                if (sd.DifficultyLevel == DifficultyLevel.Normal){    
                                return 6;
                }
                else {    
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
                        levelUp.landValueProgress = CalcProgress(landValue, GetLandValueThreshhold((ItemClass.Level)i), GetLandValueThreshhold((ItemClass.Level)(i - 1)), 8) + CalcProgress(buildingWealth, GetWealthThreshhold((ItemClass.Level)i), GetWealthThreshhold((ItemClass.Level)(i-1)), 8);
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
                if (landValue == 0) { 
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

    private int CalcProgress(int val, int max,int previous, int multiplier)
    {
        return Math.Max(0,Math.Min(val - previous, max)) * multiplier / max;
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

            serviceScore = GetProperServiceScore(buildingID,true);
            Level targetLevel = Level.Level3;
            
            for (var i = 0; i < 3; i += 1)
            {
                if (serviceScore < GetServiceOfficeThreshhold((ItemClass.Level)i) )
                {
                    targetLevel = (Level)i;
                    levelUp.serviceProgress = 1 + CalcProgress(serviceScore, GetServiceOfficeThreshhold((ItemClass.Level)i), GetServiceOfficeThreshhold((ItemClass.Level)(i - 1)), 15);
                    break;
                }                
            }

            levelUp.tooFewServices = (serviceScore < GetServiceOfficeThreshhold((ItemClass.Level)(Math.Max(-1,(int)currentLevel - 2))));

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

            serviceScore = GetProperServiceScore(buildingID,false);
            Level targetLevel = Level.Level3;

            for (var i = 0; i < 3; i += 1)
            {
                if (serviceScore < GetServiceIndustryThreshhold((ItemClass.Level)i) )
                {
                    targetLevel = (Level)i;
                    levelUp.serviceProgress = 1 + CalcProgress(serviceScore, GetServiceIndustryThreshhold((ItemClass.Level)i), GetServiceIndustryThreshhold((ItemClass.Level)(i - 1)), 15);
                    break;
                }
            }
            
            levelUp.tooFewServices = (serviceScore < GetServiceIndustryThreshhold((ItemClass.Level)(Math.Max(-1,(int)currentLevel - 2))));

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

        public static int GetProperServiceScore(ushort buildingID, bool isOffice)
        {
            Building data = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)buildingID];
            ushort[] array;
            int num;
            Singleton<ImmaterialResourceManager>.instance.CheckLocalResources(data.m_position, out array, out num);
            double num2 = 0;
            num2 += ImmaterialResourceManager.CalculateResourceEffect(array[num + (int)ImmaterialResourceManager.Resource.FireDepartment], 60, 150, 30, 50) / 2.0;
            num2 += ImmaterialResourceManager.CalculateResourceEffect(array[num + (int)ImmaterialResourceManager.Resource.PoliceDepartment], 60, 150, 30, 50) / 2.0;
            num2 += ImmaterialResourceManager.CalculateResourceEffect(array[num + (int)ImmaterialResourceManager.Resource.PublicTransport], 60, 150, 30, 50) / 2.0;
            num2 += ImmaterialResourceManager.CalculateResourceEffect(array[num + (int)ImmaterialResourceManager.Resource.LandValue], 60, 150, 30, 50) / 4.0;
            num2 -= ImmaterialResourceManager.CalculateResourceEffect(array[num + (int)ImmaterialResourceManager.Resource.Abandonment], 60, 150, 30, 50) / 2.0;

            if (isOffice)
            {
                byte resourceRate13;
                Singleton<NaturalResourceManager>.instance.CheckPollution(data.m_position, out resourceRate13);
                num2 -= ImmaterialResourceManager.CalculateResourceEffect((int)resourceRate13, 50, 255, 50, 100) / 4.0;
                num2 += ImmaterialResourceManager.CalculateResourceEffect(array[num + (int)ImmaterialResourceManager.Resource.Entertainment], 60, 150, 30, 50) / 3.0;
                num2 -= ImmaterialResourceManager.CalculateResourceEffect(array[num + (int)ImmaterialResourceManager.Resource.NoisePollution], 60, 150, 30, 50) / 3.0;

            }
            else
            {
                num2 += ImmaterialResourceManager.CalculateResourceEffect(array[num + (int)ImmaterialResourceManager.Resource.CargoTransport], 20, 50, 30, 50) / 4.0;
                num2 += ImmaterialResourceManager.CalculateResourceEffect(array[num + (int)ImmaterialResourceManager.Resource.Entertainment], 60, 150, 30, 50) / 5.0;
            }
            return (int)num2;
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
                if (originalConstructionCost > 10000){
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
            return (int)Math.Min(Math.Round((originalConstructionCost * multiplier),2),int.MaxValue);
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
                return amount/4;
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
            return (int)Math.Min(int.MaxValue,Math.Round(Math.Pow(originalPrice * 2.0,1.2),3));
        }

        public void OnUnlockArea(int x, int z)
        {

        }
    }

}
