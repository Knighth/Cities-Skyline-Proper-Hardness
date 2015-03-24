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
        public string Name
        {
            get
            {
                return "Proper Hardness Mod"; 
            }
        }
        public string Description
        {
            get
            {
                return "Increased costs, unlock costs (25 tiles), traffic disappear timer, workers need to reach work, and offices spawn more workers.";
            }
        }
    }

    public class LoadingExtension8 : LoadingExtensionBase
    {
        static GameObject modGameObject;
        OptionsWindow2 optionsWindow;
        BuildingInfoWindow buildingWindow;

        private Dictionary<GameObject, bool> FindSceneRoots()
        {
            Dictionary<GameObject, bool> roots = new Dictionary<GameObject, bool>();

            GameObject[] objects = GameObject.FindObjectsOfType<GameObject>();
            foreach (var obj in objects)
            {
                if (!roots.ContainsKey(obj.transform.root.gameObject))
                {
                    roots.Add(obj.transform.root.gameObject, true);
                }
            }

            return roots;
        }

        private List<KeyValuePair<GameObject, Component>> FindComponentsOfType(string typeName)
        {
            var roots = FindSceneRoots();
            var list = new List<KeyValuePair<GameObject, Component>>();
            foreach (var root in roots.Keys)
            {
                FindComponentsOfType(typeName, root, list);
            }
            return list;
        }

        private void FindComponentsOfType(string typeName, GameObject gameObject, List<KeyValuePair<GameObject, Component>> list)
        {
            var component = gameObject.GetComponent(typeName);
            if (component != null)
            {
                list.Add(new KeyValuePair<GameObject, Component>(gameObject, component));
            }

            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                FindComponentsOfType(typeName, gameObject.transform.GetChild(i).gameObject, list);
            }
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame)
                return;

            SaveData2.ResetData();
            modGameObject = new GameObject("Difficulty Mod");

            var view = UIView.GetAView();
            //var objects = GameObject.Find("ZonedBuildingWorldInfoPanel");
            //if (objects != null)
            //{
            //    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, objects.name + " " + objects.GetType().Name);
            //}
           

            var buildingInfo = UIView.Find<UIPanel>("(Library) ZonedBuildingWorldInfoPanel");
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, .name);
            
            
            this.buildingWindow = modGameObject.AddComponent<BuildingInfoWindow>();
            this.buildingWindow.transform.parent = buildingInfo.transform;
            this.buildingWindow.size = new Vector3(buildingInfo.size.x,buildingInfo.size.y);
            this.buildingWindow.baseBuildingWindow = buildingInfo.gameObject.transform.GetComponentInChildren<ZonedBuildingWorldInfoPanel>();

            
            buildingInfo.eventPositionChanged += (component, param) =>
            {
                this.buildingWindow.position = new Vector3(0, -10);// new Vector3(component.position.x, component.position.y + component.size.y - 10);
                this.buildingWindow.Update();
            };

            buildingInfo.eventVisibilityChanged += (component, param) =>
            {
                this.buildingWindow.isEnabled = param;
                if (param)
                {
                    this.buildingWindow.Show();
                }
                else
                {
                    this.buildingWindow.Hide();
                }
            };

            if (SaveData2.MustInitialize())
            {
                this.optionsWindow = modGameObject.AddComponent<OptionsWindow2>();
                this.optionsWindow.transform.parent = view.transform;
                this.optionsWindow.mode = mode;
            }
            else
            {
                LoadMod(mode,SaveData2.saveData);
            }
        }

        public static void LoadMod(LoadMode mode, SaveData2 sd)
        {
            if (sd.disastersEnabled)
            {
                modGameObject.AddComponent<Disasters2>();
            }

            var mapping = new Dictionary<Type, Type>
            {
                {typeof (ResidentialBuildingAI), typeof (WBBResidentialBuildingAI3)},
                {typeof (CommercialBuildingAI), typeof (WBCommercialBuildingAI)},
                {typeof (IndustrialBuildingAI), typeof (WBIndustrialBuildingAI6)},
                {typeof (OfficeBuildingAI), typeof (WBOfficeBuildingAI5)},
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

            if (sd.DifficultyLevel == DifficultyLevel.Vanilla)
            {
                return;
            }


            if (sd.DifficultyLevel == DifficultyLevel.Hard && mode == LoadMode.NewGame)
            {
                Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.LoanAmount, 3000000, ItemClass.Service.Education, ItemClass.SubService.None, ItemClass.Level.None);
            }

            mapping = new Dictionary<Type, Type>
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
                {typeof (ResidentAI), typeof (WBResidentAI4)},
            };


            for (uint i = 0; i < PrefabCollection<CitizenInfo>.PrefabCount(); i++)
            {
                var vi = PrefabCollection<CitizenInfo>.GetPrefab(i);
                AdjustResidentAI(vi, mapping);
            }

            //mapping = new Dictionary<Type, Type>
            //{
            //    {typeof (TransportLineAI), typeof (WBTransportLineAI2)},
            //};


            //for (uint i = 0; i < PrefabCollection<NetInfo>.PrefabCount(); i++)
            //{
            //    var vi = PrefabCollection<NetInfo>.GetPrefab(i);
            //    AdjustNetAI(vi, mapping);
            //}

            Singleton<UnlockManager>.instance.MilestonesUpdated();
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Loaded proper hardness. " + sd.DifficultyLevel.ToString());
        }

        private static void AdjustResidentAI(CitizenInfo bi, Dictionary<Type, Type> componentRemap)
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

            //UnityEngine.Object.Destroy(oldAI,1f);
            CitizenAI newAI = bi.gameObject.AddComponent(newCompType) as CitizenAI;

            if (newAI != null)
            {            
                    newAI.m_info = bi;
                    bi.m_citizenAI = newAI;
                    newAI.InitializeAI();
            }
        }

        private static void AdjustBuildingAI(BuildingInfo bi, Dictionary<Type, Type> componentRemap)
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

        private static void AdjustNetAI(NetInfo bi, Dictionary<Type, Type> componentRemap)
        {
            if (bi == null)
            {
                return;
            }
            var oldAI = bi.m_netAI;

            if (oldAI == null)
                return;
            var compType = oldAI.GetType();
            Type newCompType;
            if (!componentRemap.TryGetValue(compType, out newCompType))
                return;
            var fields = ExtractFields(oldAI);
            UnityEngine.Object.Destroy(oldAI, 1f);
            NetAI newAI = bi.gameObject.AddComponent(newCompType) as NetAI;

            if (fields.Count() > 0)
            {
                SetFields(newAI, fields);
            }
            if (newAI != null)
            {
                newAI.m_info = bi;
                bi.m_netAI = newAI;
                newAI.InitializePrefab();
            }

        }
        private static void AdjustVehicleAI(VehicleInfo vi, Dictionary<Type, Type> componentRemap)
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

        private static Dictionary<string, object> ExtractFields(object a)
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

        private static void SetFields(object b, Dictionary<string, object> fieldValues)
        {
            var bType = b.GetType();
            foreach (var kvp in fieldValues)
            {
                var bf = bType.GetField(kvp.Key);
                if (bf == null)
                {
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning, "Could not find field " + kvp.Key + " in " + b);
                    continue;
                }
                bf.SetValue(b, kvp.Value);
            }
        }

        public override void OnReleased()
        {
            base.OnReleased();
            GameObject.Destroy(modGameObject);
        }

        public static List<UIComponent> FindUIComponents(string searchString)
        {
            var uics = new List<UIComponent>();
            var components = UnityEngine.Object.FindObjectsOfType<UIComponent>();

            foreach (var uic in components)
            {
                if (!uic.name.Contains(searchString))
                    continue;
                uics.Add(uic);
            }

            return uics;
        }

        public static UIComponent FindUIComponent(string searchString)
        {
            var components = UnityEngine.Object.FindObjectsOfType<UIComponent>();

            foreach (var uic in components)
            {
                if (uic.name == searchString)
                    return uic;
            }

            return null;
        }
    }
}
