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
    public class DifficultyMod : IUserMod
    {
        public static GameObject modObject;

        public string Name
        {
            get
            {
#if easyMode != true
                return "Proper Hardness Mod"; 
#else
                return "Proper Hardness Mod (Without Hardness)";
#endif
            }
        }
        public string Description
        {
            get
            {
#if easyMode != true
                return "Increased costs, unlock costs (25 tiles), traffic disappear timer, workers need to reach work, and offices spawn more workers."; 
#else
                return "Increased traffic disappear timer, workers need to reach work, and offices spawn more workers.";
#endif
            }
        }


    }

    public class LoadingExtension : LoadingExtensionBase
    {
        //GameObject m_initializer;
        //public override void OnCreated(ILoading loading)
        //{
        //    base.OnCreated(loading);

        //    if (GameObject.Find("Initializer") == null)
        //    {
        //        m_initializer = new GameObject("Custom Prefabs");
        //        m_initializer.AddComponent<Initializer>();
        //    }
        //}

        //public override void OnLevelUnloading()
        //{
        //    base.OnLevelUnloading();

        //    GameObject.Destroy(m_initializer);
        //}

        public override void OnLevelLoaded(LoadMode mode)
        {
            if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame)
                return;

            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Loading difficulty Mod.");
            var mapping = new Dictionary<Type, Type>
            {
                {typeof (AmbulanceAI), typeof (AmbulanceAIMod)},
                {typeof (BusAI), typeof (BusAIMod)},
                {typeof (CargoTruckAI), typeof (CargoTruckAIMod)},
                {typeof (FireTruckAI), typeof (FireTruckAIMod)},
                {typeof (GarbageTruckAI), typeof (GarbageTruckAIMod)},
                {typeof (HearseAI), typeof (HearseAIMod)},
                {typeof (PassengerCarAI), typeof (PassengerCarAIMod)},
                {typeof (PoliceCarAI), typeof (PoliceCarAIMod)},
            };


            for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
            {                
                var vi = PrefabCollection<VehicleInfo>.GetPrefab(i);
                AdjustVehicleAI(vi, mapping);

                //VehicleAI oldAI = null;
                //var newAI = CopyComponent<VehicleAI>(vi, mapping,ref oldAI);
                //if (newAI != null)
                //{
                //    newAI.m_info = vi;
                //    vi.m_vehicleAI = newAI;
                //    newAI.InitializeAI();
                //}
            }
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Replaced vehicle AIs.");

            mapping = new Dictionary<Type, Type>
            {
                {typeof (ResidentAI), typeof (WBResidentAI)},
            };


            for (uint i = 0; i < PrefabCollection<CitizenInfo>.PrefabCount(); i++)
            {
                var vi = PrefabCollection<CitizenInfo>.GetPrefab(i);
                AdjustResidentAI(vi, mapping);
                //CitizenAI oldAI = null;
                //var newAI = CopyComponent<CitizenAI>(vi, mapping, ref oldAI);
                //if (newAI != null)
                //{
                //    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, ((CitizenInfo)vi).name);
                //    newAI.m_info = vi;
                //    vi.m_citizenAI = newAI;
                //    newAI.InitializeAI();
                //}
            }            

            mapping = new Dictionary<Type, Type>
            {
                {typeof (ResidentialBuildingAI), typeof (WBResidentialBuildingAI)},
            };

            for (uint i = 0; i < PrefabCollection<BuildingInfo>.PrefabCount(); i++)
            {
            //    BuildingAI oldAI = null;
                var vi = PrefabCollection<BuildingInfo>.GetPrefab(i);
            //    var newAI = CopyComponent<BuildingAI>(vi, mapping,ref oldAI);
            //    if (newAI != null)
            //    {
            //        newAI.m_info = vi;
            //        vi.m_buildingAI = newAI;
            //    }
                AdjustBuildingAI(vi, mapping);
            }
            
#if easyMode != true
            if (mode == LoadMode.NewGame)
            {
                Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.LoanAmount, 3000000, ItemClass.Service.Education, ItemClass.SubService.None, ItemClass.Level.None);
            }
#endif
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Loaded difficulty mod.");
        }

        private void AdjustResidentAI(CitizenInfo bi, Dictionary<Type, Type> componentRemap)
        {
            if (bi == null)
            {
                return;
            }

            var oldAI = bi.GetComponent<CitizenAI>();
            if (oldAI == null)
                return;
            var compType = oldAI.GetType();
            Type newCompType;
            if (!componentRemap.TryGetValue(compType, out newCompType))
                return;

            UnityEngine.Object.Destroy(oldAI,1f);
            CitizenAI newAI = bi.gameObject.AddComponent(newCompType) as CitizenAI;

            if (newAI != null)
            {
                bi.m_prefabInitialized = false;
                bi.m_citizenAI = null;
                bi.InitializePrefab();
                //bi.Initi
                //MethodInfo initMethod = typeof(VehicleCollection).GetMethod("InitializePrefabs", BindingFlags.Static | BindingFlags.NonPublic);
                //Singleton<LoadingManager>.instance.QueueLoadingAction((IEnumerator)initMethod.Invoke(null, new object[] { collection.name, new[] { ambulance }, new string[] { "Ambulance" } }));

                if (bi.m_citizenAI == null)
                {
                    newAI.m_info = bi;
                    bi.m_citizenAI = newAI;
                    newAI.InitializeAI();
                }
            }
        }

        private void AdjustBuildingAI(BuildingInfo bi, Dictionary<Type, Type> componentRemap)
        {
            if (bi == null)
            {
                return;
            }
            var oldAI = bi.GetComponent<BuildingAI>();

            if (oldAI == null)
                return;
            var compType = oldAI.GetType();
            Type newCompType;
            if (!componentRemap.TryGetValue(compType, out newCompType))
                return;
            var fields = ExtractFields(oldAI);
            UnityEngine.Object.Destroy(oldAI, 1f);
            BuildingAI newAI = bi.gameObject.AddComponent(newCompType) as BuildingAI;

            if (fields.Count() > 0)
            {
                SetFields(newAI, fields);
            }
            if (newAI != null)
            {
                newAI.m_info = bi;
                bi.m_buildingAI = newAI;
                newAI.InitializePrefab();
            }

        }

        private void AdjustVehicleAI(VehicleInfo vi, Dictionary<Type, Type> componentRemap)
        {
            if (vi == null)
            {
                return;
            }
            var oldAI = vi.GetComponent<VehicleAI>();
            if (oldAI == null)
                return;
            var compType = oldAI.GetType();
            Type newCompType;
            if (!componentRemap.TryGetValue(compType, out newCompType))
                return;
            var fields = ExtractFields(oldAI);

            UnityEngine.Object.Destroy(oldAI, 1f);

            VehicleAI newAI = vi.gameObject.AddComponent(newCompType) as VehicleAI;
            if (fields.Count() > 0)
            {
                SetFields(newAI, fields);
            }
            if (newAI != null)
            {
                newAI.m_info = vi;
                vi.m_vehicleAI = newAI;
                newAI.InitializeAI();
            }

        }

        //private static T CopyComponent<T>(PrefabInfo infoObject, Dictionary<Type, Type> componentRemap,ref T t ) where T : Component
        //{
        //    if (infoObject == null)
        //    {
        //        return null;
        //    }
          
        //    Type type = typeof(T);
        //    t = infoObject.GetComponent(type) as T;
        //    if (t != null)
        //    {
        //        var compType = t.GetType();
        //        Type newCompType;
        //        if (!componentRemap.TryGetValue(compType, out newCompType))
        //        {
        //            return null;
        //        }                
        //        UnityEngine.Object.DestroyImmediate(t);
        //        T t2 = infoObject.gameObject.AddComponent(newCompType) as T;
        //        FieldInfo[] fields = compType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //        FieldInfo[] array = fields;
        //        for (int i = 0; i < array.Length; i++)
        //        {
        //            FieldInfo fieldInfo = array[i];
        //            if (fieldInfo.FieldType.IsSerializable && !fieldInfo.IsNotSerialized)
        //            {
        //                object value = fieldInfo.GetValue(t);
        //                fieldInfo.SetValue(t2, value);
        //            }
        //        }
        //        return t2;
        //    }
        //    else
        //    {
        //        return null;
        //    }           
        //}

        private Dictionary<string, object> ExtractFields(object a)
        {
            var fields = a.GetType().GetFields();
            var dict = new Dictionary<string, object>(fields.Length);
            for (int i = 0; i < fields.Length; i++)
            {
                var af = fields[i];
                dict[af.Name] = af.GetValue(a);
            }
            return dict;
        }

        private void SetFields(object b, Dictionary<string, object> fieldValues)
        {
            var bType = b.GetType();
            foreach (var kvp in fieldValues)
            {
                // .AddMessage(PluginManager.MessageType.Message, "setting Field: " + kvp.Key + " " + kvp.Value);
                var bf = bType.GetField(kvp.Key);
                if (bf == null)
                {
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning, "Could not find field " + kvp.Key + " in " + b);
                    continue;
                }
                bf.SetValue(b, kvp.Value);
            }
        }
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
                    multiplier = 1.5;
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

    public class HardModeLevelUp : LevelUpExtensionBase
    {

        public override ResidentialLevelUp OnCalculateResidentialLevelUp(ResidentialLevelUp levelUp, int averageEducation, int landValue, ushort buildingID, Service service, SubService subService, Level currentLevel)
        {
            if (levelUp.landValueProgress != 0)
            {
                Level targetLevel;

                if (landValue < 15)
                {
                    targetLevel = Level.Level1;
                    levelUp.landValueProgress = 1 + (landValue * 15 + 7) / 15;
                }
                else if (landValue < 35)
                {
                    targetLevel = Level.Level2;
                    levelUp.landValueProgress = 1 + ((landValue - 15) * 15 + 10) / 20;
                }
                else if (landValue < 60)
                {
                    targetLevel = Level.Level3;
                    levelUp.landValueProgress = 1 + ((landValue - 35) * 15 + 12) / 25;
                }
                else if (landValue < 85)
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
                if (landValue == 0) levelUp.landValueTooLow = true;
            }
            else if (currentLevel == Level.Level3)
            {
                if (landValue < 21) levelUp.landValueTooLow = true;
            }
            else if (currentLevel == Level.Level4)
            {
                if (landValue < 46) levelUp.landValueTooLow = true;
            }
            else if (currentLevel == Level.Level5)
            {
                if (landValue < 71) levelUp.landValueTooLow = true;
            }

            return levelUp;
        }

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
                return amount/2;
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
