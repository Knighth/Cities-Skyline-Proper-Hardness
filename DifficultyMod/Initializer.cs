////From https://github.com/joaofarias/csl-traffic
//using System;
//using System.Collections.Generic;
//using System.Reflection;
//using UnityEngine;
//using ColossalFramework.Plugins;

//namespace DifficultyMod
//{
//    class Initializer : MonoBehaviour
//    {
//        bool m_initialized;

//        void Awake()
//        {
//            DontDestroyOnLoad(this);
//        }

//        void Start()
//        {
//        }

//        void Update()
//        {
//            if (!m_initialized)
//            {
//                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Try resident AIs.");
//                TryReplacePrefabs();
//            }
//        }

//        void TryReplacePrefabs()
//        {
//            try
//            {
//                var mapping = new Dictionary<Type, Type>
//                {
//                    {typeof (ResidentAI), typeof (WBResidentAI)},
//                };


//                for (uint i = 0; i < PrefabCollection<CitizenInfo>.PrefabCount(); i++)
//                {
//                    var vi = PrefabCollection<CitizenInfo>.GetPrefab(i);
//                    //CitizenAI oldAI = null;
//                    //var newAI = CopyComponent<CitizenAI>(vi, mapping, ref oldAI);
//                    //if (newAI != null)
//                    //{
//                    //    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, ((CitizenInfo)vi).name);
//                    //    newAI.m_info = vi;
//                    //    vi.m_citizenAI = newAI;
//                    //    newAI.InitializeAI();
//                    //}
//                    AdjustResidentAI(vi, mapping);
//                }
//                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Replaced resident AIs.");

//                m_initialized = true; 
//            }
//            catch (Exception)
//            {

//            }

//        }

//        private void AdjustResidentAI(CitizenInfo originalAmbulance, Dictionary<Type, Type> componentRemap)
//        {
//            GameObject instance = GameObject.Instantiate<GameObject>(originalAmbulance.gameObject);
//            instance.name = originalAmbulance.name;
//            instance.transform.SetParent(transform);
//            GameObject.Destroy(instance.GetComponent<ResidentAI>());
//            instance.AddComponent<WBResidentAI>();
//            CitizenInfo resident = instance.GetComponent<CitizenInfo>();
//            resident.m_prefabInitialized = false;
//            resident.m_citizenAI = null;


//            if (bi == null)
//            {
//                return;
//            }

//            var oldAI = bi.GetComponent<CitizenAI>();
//            if (oldAI == null)
//                return;
//            var compType = oldAI.GetType();
//            Type newCompType;
//            if (!componentRemap.TryGetValue(compType, out newCompType))
//                return;

//            UnityEngine.Object.Destroy(oldAI, 1f);
//            CitizenAI newAI = bi.gameObject.AddComponent(newCompType) as CitizenAI;

//            if (newAI != null)
//            {
//                newAI.m_info = bi;
//                bi.m_citizenAI = newAI;
//                newAI.InitializeAI();
//            }
//        }


//    }
//}
