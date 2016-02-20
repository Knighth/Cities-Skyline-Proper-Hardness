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
    public class WBLevelUp2 : LevelUpExtensionBase
    {
        private int CalcProgress(int val, int max, int previous, int multiplier)
        {
            return Math.Max(0, Math.Min(val - previous, max)) * multiplier / max;
        }

        public override ResidentialLevelUp OnCalculateResidentialLevelUp(ResidentialLevelUp levelUp, int averageEducation, int lv, ushort buildingID, Service service, SubService subService, Level currentLevel)
        {
            if (SaveData2.saveData.DifficultyLevel == DifficultyLevel.Vanilla)
            {
                return levelUp;
            }

            var instance = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)buildingID];
            var zone = instance.Info.m_class.GetZone();
            int buildingWealth = instance.m_customBuffer1;
            float education = 0;
            float happy = 0;
            float commute = 0;
            var levelUpHelper = LevelUpHelper3.instance;

            var serviceScore = levelUpHelper.GetProperServiceScore(buildingID);
            levelUpHelper.GetEducationHappyScore(buildingID, out education, out happy, out commute);

            Level targetLevel = Level.Level5;
            for (var i = 0; i < 5; i += 1)
            {
                if (serviceScore < levelUpHelper.GetServiceThreshhold((ItemClass.Level)i, zone) ||
                    (buildingWealth != 0 && buildingWealth < levelUpHelper.GetWealthThreshhold((ItemClass.Level)i, zone)) ||
                    education < levelUpHelper.GetEducationThreshhold((ItemClass.Level)i, zone))
                {
                    targetLevel = (Level)i;
                    levelUp.landValueProgress = 1 + CalcProgress(serviceScore, levelUpHelper.GetServiceThreshhold((ItemClass.Level)i, zone), levelUpHelper.GetServiceThreshhold((ItemClass.Level)(i - 1), zone), 7) + CalcProgress(buildingWealth, levelUpHelper.GetWealthThreshhold((ItemClass.Level)i, zone), levelUpHelper.GetWealthThreshhold((ItemClass.Level)(i - 1), zone), 8);
                    levelUp.educationProgress = 1 + CalcProgress((int)education, levelUpHelper.GetEducationThreshhold((ItemClass.Level)i, zone), levelUpHelper.GetEducationThreshhold((ItemClass.Level)(i - 1), zone), 15);
                    break;
                }
            }

            levelUp.landValueTooLow = (serviceScore < levelUpHelper.GetServiceThreshhold((ItemClass.Level)(Math.Max(-1, (int)currentLevel - 2)), zone)) ||
                (buildingWealth != 0 && buildingWealth < levelUpHelper.GetWealthThreshhold((ItemClass.Level)(Math.Max(-1, (int)currentLevel - 2)), zone));
            
            if (targetLevel < currentLevel)
            {
                levelUp.landValueProgress = 1;
                levelUp.educationProgress = 1;
            }
            else if (targetLevel > currentLevel)
            {
                levelUp.landValueProgress = 15;
                levelUp.educationProgress = 15;
            }
            if (targetLevel < levelUp.targetLevel)
            {
                levelUp.targetLevel = targetLevel;
            }
            return levelUp;
        }

        public override CommercialLevelUp OnCalculateCommercialLevelUp(CommercialLevelUp levelUp, int averageWealth, int landValue, ushort buildingID, Service service, SubService subService, Level currentLevel)
        {
            if (SaveData2.saveData.DifficultyLevel == DifficultyLevel.Vanilla)
            {
                return levelUp;
            }

            var instance = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)buildingID];
            var zone = instance.Info.m_class.GetZone();
            float wealth = 0;
            float happy = 0;
            float commute = 0;
            var levelUpHelper = LevelUpHelper3.instance;

            var serviceScore = levelUpHelper.GetProperServiceScore(buildingID);
            levelUpHelper.GetEducationHappyScore(buildingID, out wealth, out happy, out commute);
            Level targetLevel = Level.Level3;

            for (var i = 0; i < 3; i += 1)
            {
                if (serviceScore < levelUpHelper.GetServiceThreshhold((ItemClass.Level)i, zone) || wealth < levelUpHelper.GetWealthThreshhold((ItemClass.Level)i, zone))
                {
                    targetLevel = (Level)i;
                    levelUp.landValueProgress = 1 + CalcProgress(serviceScore, levelUpHelper.GetServiceThreshhold((ItemClass.Level)i, ItemClass.Zone.Office), levelUpHelper.GetServiceThreshhold((ItemClass.Level)(i - 1), ItemClass.Zone.Office), 15);
                    levelUp.wealthProgress = 1 + CalcProgress((int)wealth, levelUpHelper.GetWealthThreshhold((ItemClass.Level)i, zone), levelUpHelper.GetWealthThreshhold((ItemClass.Level)(i - 1), zone), 15);
                    break;
                }
            }

            levelUp.landValueTooLow = (serviceScore < levelUpHelper.GetServiceThreshhold((ItemClass.Level)(Math.Max(-1, (int)currentLevel - 2)), zone));

            if (targetLevel < currentLevel)
            {
                levelUp.landValueProgress = 1;
                levelUp.wealthProgress = 1;
            }
            else if (targetLevel > currentLevel)
            {
                levelUp.landValueProgress = 15;
                levelUp.wealthProgress = 15;
            }
            if (targetLevel < levelUp.targetLevel)
            {
                levelUp.targetLevel = targetLevel;
            }
            return levelUp;
        }

        public override OfficeLevelUp OnCalculateOfficeLevelUp(OfficeLevelUp levelUp, int averageEducation, int serviceScore, ushort buildingID, Service service, SubService subService, Level currentLevel)
        {
            if (SaveData2.saveData.DifficultyLevel == DifficultyLevel.Vanilla)
            {
                return levelUp;
            }

            var instance = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)buildingID];
            var zone = instance.Info.m_class.GetZone();
            float education = 0;
            float happy = 0;
            float commute = 0;
            var levelUpHelper = LevelUpHelper3.instance;

            serviceScore = levelUpHelper.GetProperServiceScore(buildingID);
            levelUpHelper.GetEducationHappyScore(buildingID, out education, out happy, out commute);
            Level targetLevel = Level.Level3;

            for (var i = 0; i < 3; i += 1)
            {
                if (serviceScore < levelUpHelper.GetServiceThreshhold((ItemClass.Level)i, zone) || education < levelUpHelper.GetEducationThreshhold((ItemClass.Level)i, zone))
                {
                    targetLevel = (Level)i;
                    levelUp.serviceProgress = 1 + CalcProgress(serviceScore, levelUpHelper.GetServiceThreshhold((ItemClass.Level)i, ItemClass.Zone.Office), levelUpHelper.GetServiceThreshhold((ItemClass.Level)(i - 1), ItemClass.Zone.Office), 15);
                    levelUp.educationProgress = 1 + CalcProgress((int)education, levelUpHelper.GetEducationThreshhold((ItemClass.Level)i, zone), levelUpHelper.GetEducationThreshhold((ItemClass.Level)(i - 1), zone), 15);
                    break;
                }
            }

            levelUp.tooFewServices = (serviceScore < levelUpHelper.GetServiceThreshhold((ItemClass.Level)(Math.Max(-1, (int)currentLevel - 2)), zone));
            
            if (targetLevel < currentLevel)
            {
                levelUp.serviceProgress = 1;
                levelUp.educationProgress = 1;
            }
            else if (targetLevel > currentLevel)
            {
                levelUp.serviceProgress = 15;
                levelUp.educationProgress = 15;
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

            var instance = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)buildingID];
            var zone = instance.Info.m_class.GetZone();
            float education = 0;
            float happy = 0;
            float commute = 0;
            var levelUpHelper = LevelUpHelper3.instance;

            serviceScore = levelUpHelper.GetProperServiceScore(buildingID);
            levelUpHelper.GetEducationHappyScore(buildingID, out education, out happy, out commute);
            Level targetLevel = Level.Level3;

            for (var i = 0; i < 3; i += 1)
            {
                if (serviceScore < levelUpHelper.GetServiceThreshhold((ItemClass.Level)i, zone) || education < levelUpHelper.GetEducationThreshhold((ItemClass.Level)i, zone))
                {
                    targetLevel = (Level)i;
                    levelUp.serviceProgress = 1 + CalcProgress(serviceScore, levelUpHelper.GetServiceThreshhold((ItemClass.Level)i, ItemClass.Zone.Office), levelUpHelper.GetServiceThreshhold((ItemClass.Level)(i - 1), ItemClass.Zone.Office), 15);
                    levelUp.educationProgress = 1 + CalcProgress((int)education, levelUpHelper.GetEducationThreshhold((ItemClass.Level)i, zone), levelUpHelper.GetEducationThreshhold((ItemClass.Level)(i - 1), zone), 15);
                    break;
                }
            }

            levelUp.tooFewServices = (serviceScore < levelUpHelper.GetServiceThreshhold((ItemClass.Level)(Math.Max(-1, (int)currentLevel - 2)), zone));

            if (targetLevel < currentLevel)
            {
                levelUp.serviceProgress = 1;
                levelUp.educationProgress = 1;
            }
            else if (targetLevel > currentLevel)
            {
                levelUp.serviceProgress = 15;
                levelUp.educationProgress = 15;
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
            if (SaveData2.saveData.DifficultyLevel < DifficultyLevel.Hard)
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
                            multiplier = 4;
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
            if (SaveData2.saveData.DifficultyLevel < DifficultyLevel.Hard)
            {
                return originalMaintenanceCost;
            }

            var multiplier = 1.54;

            if (SaveData2.saveData.DifficultyLevel == DifficultyLevel.DwarfFortress)
            {
                multiplier = 2.0;
            }


            switch (service)
            {
                case Service.FireDepartment:
                case Service.PoliceDepartment:
                case Service.HealthCare:
                    if (originalMaintenanceCost > 420000)
                    {
                        multiplier = 1.8;
                    }
                    break;
                case Service.Education:
                    multiplier = 2.0;
                    if (SaveData2.saveData.DifficultyLevel == DifficultyLevel.DwarfFortress)
                    {
                        multiplier = 2.4;
                    }
                    break;
                case Service.Road:
                    multiplier = 2.0;
                    if (SaveData2.saveData.DifficultyLevel == DifficultyLevel.DwarfFortress)
                    {
                        multiplier = 2.4;
                    }
                    break;
                case Service.Garbage:
                    multiplier = 1;
                    break;
            }
            return (int)(originalMaintenanceCost * multiplier);
        }

        public override int OnGetRelocationCost(int constructionCost, int relocationCost, Service service, SubService subService, Level level)
        {
            if (SaveData2.saveData.DifficultyLevel < DifficultyLevel.Hard)
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

    public class DifficultyModEconomy : EconomyExtensionBase
    {
        public override int OnAddResource(EconomyResource resource, int amount, Service service, SubService subService, Level level)
        {
            if (SaveData2.saveData.DifficultyLevel != DifficultyLevel.Vanilla && resource == EconomyResource.PrivateIncome)
            {
                if (amount > 0)
                {
                    return 0;
                }
                else
                {
                    return -amount;
                }
            }
            if (SaveData2.saveData.DifficultyLevel < DifficultyLevel.Hard)
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
            if (SaveData2.saveData.DifficultyLevel < DifficultyLevel.Hard)
            {
                return originalPrice;
            }
            return (int)Math.Min(int.MaxValue, Math.Round(Math.Pow(originalPrice * 2.0, 1.2), 3));
        }

        public void OnUnlockArea(int x, int z)
        {

        }
    }

    public class DifficultyModDemand : DemandExtensionBase
    {
        public override int OnCalculateCommercialDemand(int originalDemand)
        {
            return originalDemand - 22;
        }
    }

}
