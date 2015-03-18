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

            var mapping = new Dictionary<Type, Type>
            {
                {typeof (CargoTruckAI), typeof (WBCargoTruckAI)},
                {typeof (PassengerCarAI), typeof (WBPassengerCarAI)},
            };


            for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
            {                
                var vi = PrefabCollection<VehicleInfo>.GetPrefab(i);
                AdjustVehicleAI(vi, mapping);
            }
            mapping = new Dictionary<Type, Type>
            {
                {typeof (ResidentAI), typeof (WBResidentAI)},
            };


            for (uint i = 0; i < PrefabCollection<CitizenInfo>.PrefabCount(); i++)
            {
                var vi = PrefabCollection<CitizenInfo>.GetPrefab(i);
                AdjustResidentAI(vi, mapping);
            }

            //for (uint i = 0; i < PrefabCollection<CitizenInfo>.LoadedCount(); i++)
            //{
            //    var vi = PrefabCollection<CitizenInfo>.GetLoaded(i);
            //    AdjustResidentAI(vi, mapping);
            //}

            mapping = new Dictionary<Type, Type>
            {
                {typeof (ResidentialBuildingAI), typeof (WBResidentialBuildingAI)},
                {typeof (CommercialBuildingAI), typeof (WBCommercialBuildingAI)},
                {typeof (IndustrialBuildingAI), typeof (WBIndustrialBuildingAI)},
            };

            for (uint i = 0; i < PrefabCollection<BuildingInfo>.PrefabCount(); i++)
            {            
                var vi = PrefabCollection<BuildingInfo>.GetPrefab(i);
                AdjustBuildingAI(vi, mapping);
            }

            for (uint i = 0; i < PrefabCollection<BuildingInfo>.LoadedCount(); i++)
            {
                var vi = PrefabCollection<BuildingInfo>.GetLoaded(i);
                AdjustBuildingAI(vi, mapping);
            }
            
#if easyMode != true
                if (mode == LoadMode.NewGame)
                {
                    Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.LoanAmount, 3000000, ItemClass.Service.Education, ItemClass.SubService.None, ItemClass.Level.None);
                }
#endif
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Loaded proper hardness.");
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

            //if (bi.name.Contains("Child") || bi.name.Contains("Pensioner"))
            //{
            //    return;
            //}
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, bi.name + " " + bi.tag);
            //UnityEngine.Object.Destroy(oldAI,1f);
            CitizenAI newAI = bi.gameObject.AddComponent(newCompType) as CitizenAI;

            if (newAI != null)
            {
            
                    newAI.m_info = bi;
                    bi.m_citizenAI = newAI;
                    newAI.InitializeAI();
            }
        }

        private void AdjustBuildingAI(BuildingInfo bi, Dictionary<Type, Type> componentRemap)
        {
            if (bi == null)
            {
                return;
            }
            var oldAI = bi.m_buildingAI;

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
}
