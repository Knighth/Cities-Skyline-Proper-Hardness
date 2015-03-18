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
    public class WBLevelUp : LevelUpExtensionBase
    {

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
                return 2000;
            }
        }
    public override ResidentialLevelUp OnCalculateResidentialLevelUp(ResidentialLevelUp levelUp, int averageEducation, int landValue, ushort buildingID, Service service, SubService subService, Level currentLevel)
        {
            var instance = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)buildingID];
            int buildingWealth = instance.m_customBuffer1;

            if (levelUp.landValueProgress != 0)
            {                
                Level targetLevel;

                if (landValue < 15 || (buildingWealth != 0 && buildingWealth < GetWealthThreshhold(ItemClass.Level.Level1)))
                {
                    targetLevel = Level.Level1;
                    levelUp.landValueProgress = 1 + (landValue * 15 + 7) / 15;
                }
                else if (landValue < 35 || (buildingWealth != 0 && buildingWealth < GetWealthThreshhold(ItemClass.Level.Level2)))
                {
                    targetLevel = Level.Level2;
                    levelUp.landValueProgress = 1 + ((landValue - 15) * 15 + 10) / 20;
                }
                else if (landValue < 60 || (buildingWealth != 0 && buildingWealth < GetWealthThreshhold(ItemClass.Level.Level3)))
                {
                    targetLevel = Level.Level3;
                    levelUp.landValueProgress = 1 + ((landValue - 35) * 15 + 12) / 25;
                }
                else if (landValue < 85 || (buildingWealth != 0 && buildingWealth < GetWealthThreshhold(ItemClass.Level.Level4)))
                {
                    targetLevel = Level.Level4;
                    levelUp.landValueProgress = 1 + ((landValue - 60) * 15 + 12) / 25;
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
                if (landValue == 0) { 
                    levelUp.landValueTooLow = true;
                }
            }
            else if (currentLevel == Level.Level3)
            {
                if (landValue < 21 || (buildingWealth != 0 && buildingWealth < GetWealthThreshhold(ItemClass.Level.Level1)))
                {
                    levelUp.landValueTooLow = true;
                }                
            }
            else if (currentLevel == Level.Level4)
            {
                if (landValue < 46 || (buildingWealth != 0 && buildingWealth < GetWealthThreshhold(ItemClass.Level.Level2)))
                {
                    levelUp.landValueTooLow = true;
                }                
            }
            else if (currentLevel == Level.Level5)
            {
                if (landValue < 70 || (buildingWealth != 0 && buildingWealth < GetWealthThreshhold(ItemClass.Level.Level3)))
                {
                    levelUp.landValueTooLow = true;
                }                
            }

            return levelUp;
        }

#if easyMode != true
        public override CommercialLevelUp OnCalculateCommercialLevelUp(CommercialLevelUp levelUp, int averageWealth, int landValue, ushort buildingID, Service service, SubService subService, Level currentLevel)
        {
            if (levelUp.landValueProgress != 0)
            {
                Level targetLevel;

                if (landValue < 30)
                {
                    targetLevel = Level.Level1;
                    levelUp.landValueProgress = 1 + (landValue * 15 + 15) / 30;
                }
                else if (landValue < 60)
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
#endif
    }
#if easyMode != true
    
    public class HardModeEconomy : EconomyExtensionBase
    {

        public override int OnGetConstructionCost(int originalConstructionCost, Service service, SubService subService, Level level)
        {
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
                            multiplier = 6;
                        }
                        else
                        {
                            multiplier = 3;
                        }
                        break;
                }
            }            
            return (int)Math.Min(Math.Round((originalConstructionCost * multiplier),2),int.MaxValue);
        }

        public override int OnGetMaintenanceCost(int originalMaintenanceCost, Service service, SubService subService, Level level)
        {
            var multiplier = 1.4;

            switch (service)
            {
                case Service.Education:
                    multiplier = 2.0;
                    break;
                case Service.Road:
                    multiplier = 1.8;
                break;
            }
            return (int)(originalMaintenanceCost * multiplier);
        }

        public override int OnGetRelocationCost(int constructionCost, int relocationCost, Service service, SubService subService, Level level)
        {
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
            return (int)Math.Min(int.MaxValue,Math.Round(Math.Pow(originalPrice * 2.0,1.2),3));
        }

        public void OnUnlockArea(int x, int z)
        {

        }
    }
#endif

}
