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
    public class DifficultyMod2 : IUserMod
    {
        internal const string MOD_NAME = "Proper Hardness Mod - KHFork";
        internal const string MOD_DESC ="Increased costs, unlock costs (25 tiles), traffic disappear timer, workers need to reach work, and offices spawn more workers."; 
        internal const string MOD_DLL_NAME = "DifficultyMod";
        internal const string MOD_LOG_PREFIX = "ProperHardness";
        internal const string MOD_ORG_CONFIGNAME = "";
        internal const string MOD_CONFIG_FILENAME = "ProperHardness_KH_config.xml";

        internal static Configuration config;
        internal static string MOD_VERSION = "";
        internal static bool DEBUG_LOG_ON = false;

        public string Name
        {
            get
            {
                return MOD_NAME; 
            }
        }
        public string Description
        {
            get
            {
                return MOD_DESC ;
            }
        }

        public void OnEnabled()
        {
            try
            {
                config = Configuration.Deserialize(MOD_CONFIG_FILENAME);
                if (config == null)
                {
                    config = new Configuration();
                    Configuration.Serialize(MOD_CONFIG_FILENAME, config);
                }
                DEBUG_LOG_ON = config.DebugLogging;
                Logger.dbgLog(string.Concat("v", MOD_VERSION, " has been enabled."));
            }
            catch (Exception ex)
            { Logger.dbgLog("",ex,true); }
        }

        public void OnRemoved()
        {
            try
            {
                Logger.dbgLog(String.Concat("v", MOD_VERSION, " has been disabled or unloaded."));
            }
            catch(Exception ex)
            { Logger.dbgLog("", ex, true); }
        }

        public DifficultyMod2()
        {
            try
            {
                var attr2 = Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(),
                    typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
                if (attr2 != null)
                {
                    MOD_VERSION = attr2.InformationalVersion; 
                }
            }
            catch(Exception ex)
            { Debug.Log("ProperHardness: (harmless) Could not get version stamp from assembly: " + ex.Message.ToString()); }
        }
    }

    public class LoadingExtension : LoadingExtensionBase
    {
        static GameObject modGameObject;
        static GameObject buildingWindowGameObject;
        static OptionsWindow2 optionsWindow;
        static BuildingInfoWindow buildingWindow;
        static ServiceInfoWindow3 serviceWindow;

        private static LoadMode _mode;

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
            //KH Just wrapped in try catch, don't want this mod fking it up for others due to co's lack of same.
            try
            {
                if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame)
                    return;
                _mode = mode;

                SaveData2.ResetData();
                modGameObject = new GameObject("DifficultyMod");

                buildingWindowGameObject = new GameObject("BuildingWindow");
                Logger.dbgLog("initializing windows.");
                InitWindows();
                Logger.dbgLog("initializing windows completed.");

                if (SaveData2.MustInitialize())
                {
                    optionsWindow.Show();
                }
                else
                {
                    Logger.dbgLog("loading staring.");
                    LoadMod(SaveData2.saveData);
                    Logger.dbgLog("loading completed.");
                }
            }
            catch (Exception ex)
            {
                Logger.dbgLog("OnLevelLoad Exception",ex,true);
            }
        }

        private void InitWindows()
        {
            var buildingInfo = UIView.Find<UIPanel>("(Library) ZonedBuildingWorldInfoPanel");
            buildingWindow = buildingWindowGameObject.AddComponent<BuildingInfoWindow>();
            buildingWindow.transform.parent = buildingInfo.transform;
            buildingWindow.size = new Vector3(buildingInfo.size.x, buildingInfo.size.y);
            buildingWindow.baseBuildingWindow = buildingInfo.gameObject.transform.GetComponentInChildren<ZonedBuildingWorldInfoPanel>();
            buildingWindow.position = new Vector3(0, 12);

            var serviceBuildingInfo = UIView.Find<UIPanel>("(Library) CityServiceWorldInfoPanel");
            serviceWindow = buildingWindowGameObject.AddComponent<ServiceInfoWindow3>();
            serviceWindow.servicePanel = serviceBuildingInfo.gameObject.transform.GetComponentInChildren<CityServiceWorldInfoPanel>();

            serviceBuildingInfo.eventVisibilityChanged += serviceBuildingInfo_eventVisibilityChanged;

            var view = UIView.GetAView();
            optionsWindow = modGameObject.AddComponent<OptionsWindow2>();
            optionsWindow.transform.parent = view.transform;
            optionsWindow.Hide();

            buildingInfo.eventVisibilityChanged += buildingInfo_eventVisibilityChanged;
        }

        private void serviceBuildingInfo_eventVisibilityChanged(UIComponent component, bool value)
        {
            serviceWindow.Update();
        }
        void buildingInfo_eventVisibilityChanged(UIComponent component, bool value)
        {
            buildingWindow.isEnabled = value;
            if (value)
            {
                buildingWindow.Show();
            }
            else
            {
                buildingWindow.Hide();
            }
        }

        public static void LoadMod(SaveData2 sd)
        {
            var loaded = new HashSet<string>();

            if (sd.disastersEnabled)
            {
                modGameObject.AddComponent<Disasters2>();
            }

            var mapping = new Dictionary<Type, Type>
            {
                {typeof (ResidentialBuildingAI), typeof (WBBResidentialBuildingAI2)},
                {typeof (CommercialBuildingAI), typeof (WBCommercialBuildingAI2)},
                {typeof (IndustrialBuildingAI), typeof (WBIndustrialBuildingAI2)},
                {typeof (OfficeBuildingAI), typeof (WBOfficeBuildingAI2)},
                {typeof (IndustrialExtractorAI), typeof (WBIndustrialExtractorAI)},

            };

            for (uint i = 0; i < PrefabCollection<BuildingInfo>.PrefabCount(); i++)
            {
                var vi = PrefabCollection<BuildingInfo>.GetPrefab(i);
                AdjustBuildingAI(vi, mapping, loaded);
            }

            if (sd.DifficultyLevel == DifficultyLevel.Vanilla)
            {
                return;
            }
            
            if (sd.DifficultyLevel == DifficultyLevel.Hard && _mode == LoadMode.NewGame)
            {
                Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.LoanAmount, 3000000, ItemClass.Service.Education, ItemClass.SubService.None, ItemClass.Level.None);
            }

            if (sd.DifficultyLevel == DifficultyLevel.DwarfFortress && _mode == LoadMode.NewGame)
            {
                Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.LoanAmount, 4000000, ItemClass.Service.Education, ItemClass.SubService.None, ItemClass.Level.None);
            }


            mapping = new Dictionary<Type, Type>
            {
                {typeof (CargoTruckAI), typeof (WBCargoTruckAI)},
                {typeof (PassengerCarAI), typeof (WBPassengerCarAI)},
            };

            for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
            {
                var vi = PrefabCollection<VehicleInfo>.GetPrefab(i);
                if (vi.m_vehicleAI.GetType().Equals(typeof(PassengerTrainAI)))
                {
                    ((PassengerTrainAI)vi.m_vehicleAI).m_passengerCapacity = 70;
                }
                else {
                    AdjustVehicleAI(vi, mapping, loaded);
                }
            }
            mapping = new Dictionary<Type, Type>
            {
                {typeof (ResidentAI), typeof (WBResidentAI6)},
            };


            for (uint i = 0; i < PrefabCollection<CitizenInfo>.PrefabCount(); i++)
            {
                var vi = PrefabCollection<CitizenInfo>.GetPrefab(i);
                AdjustResidentAI(vi, mapping, loaded);
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

            if (loaded.Count() < 8)
            {
                optionsWindow.ShowError();
            }
            //Debug.Log(loaded.Count().ToString());
            //Debug.Log(string.Join(",", loaded.ToArray()));
        }

        private static void AdjustResidentAI(CitizenInfo bi, Dictionary<Type, Type> componentRemap, HashSet<string> loaded)
        {
            if (bi == null)
            {
                return;
            }

            var oldAI = bi.GetComponent<CitizenAI>();
            if (oldAI == null)
                return;
            var compType = oldAI.GetType();

            Type newCompType = GetValue(componentRemap,compType);            
            if (newCompType == null)
            {
                return;
            }

            //UnityEngine.Object.Destroy(oldAI,1f);
            CitizenAI newAI = bi.gameObject.AddComponent(newCompType) as CitizenAI;

            if (newAI != null)
            {            
                    newAI.m_info = bi;
                    bi.m_citizenAI = newAI;
                    newAI.InitializeAI();
            }
            loaded.Add(newCompType.Name);
        }

        private static void AdjustBuildingAI(BuildingInfo bi, Dictionary<Type, Type> componentRemap, HashSet<string> loaded)
        {
            if (bi == null)
            {
                return;
            }
            var oldAI = bi.m_buildingAI;

            if (oldAI == null)
                return;
            var compType = oldAI.GetType();

            Type newCompType = GetValue(componentRemap, compType);
            if (newCompType == null)
            {
                return;
            }

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
            loaded.Add(newCompType.Name);
        }

        private static void AdjustNetAI(NetInfo bi, Dictionary<Type, Type> componentRemap, HashSet<string> loaded)
        {
            if (bi == null)
            {
                return;
            }
            var oldAI = bi.m_netAI;

            if (oldAI == null)
                return;
            var compType = oldAI.GetType();

            Type newCompType = GetValue(componentRemap, compType);
            if (newCompType == null)
            {
                return;
            }

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
            loaded.Add(newCompType.Name);
        }

        private static void AdjustVehicleAI(VehicleInfo vi, Dictionary<Type, Type> componentRemap, HashSet<string> loaded)
        {
            if (vi == null)
            {
                return;
            }
            var oldAI = vi.GetComponent<VehicleAI>();
            if (oldAI == null)
                return;
            var compType = oldAI.GetType();

            Type newCompType = GetValue(componentRemap, compType);
            if (newCompType == null)
            {
                return;
            }

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
            loaded.Add(newCompType.Name);
        }

        private static Type GetValue(Dictionary<Type,Type> componentRemap, Type oldType)
        {
            foreach (var kvp in componentRemap)
            {
                if (kvp.Key.IsAssignableFrom(oldType))
                {
                    return kvp.Value;
                }
            }
            return null;
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
        public override void OnLevelUnloading()
        {
            //KH Just wrapped in try catch, don't want this mod fking it up for others due to co's lack of same.
            try
            {
                if (_mode != LoadMode.LoadGame && _mode != LoadMode.NewGame)
                    return;

                if (buildingWindow != null)
                {
                    if (buildingWindow.parent != null)
                    {
                        buildingWindow.parent.eventVisibilityChanged -= buildingInfo_eventVisibilityChanged;
                    }
                }

                if (modGameObject != null)
                {
                    GameObject.Destroy(modGameObject);
                }
                if (buildingWindowGameObject != null)
                {
                    GameObject.Destroy(buildingWindowGameObject);
                }
            }
            catch (Exception ex)
            {
                Logger.dbgLog("OnLevelUnLoading Exception!", ex, true);
            }

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
