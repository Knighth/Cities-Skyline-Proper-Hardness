//using ColossalFramework;
//using ColossalFramework.Math;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;

//namespace DifficultyMod
//{
//    class WBTransportLineAI2: TransportLineAI
//    {
//        private static float multiplierNormal = 0.01f;
//        private static float multiplierFree = 0.005f;

//        public override void SimulationStep(ushort segmentID, ref NetSegment data)
//        {
//            NetManager instance = Singleton<NetManager>.instance;
//            if ((instance.m_nodes.m_buffer[(int)data.m_startNode].m_flags & NetNode.Flags.Temporary) == NetNode.Flags.None)
//            {
//                if (data.m_path == 0u || (ulong)(Singleton<SimulationManager>.instance.m_currentFrameIndex >> 8 & 15u) == (ulong)((long)(segmentID & 15)))
//                {
//                    TransportLineAI.StartPathFind(segmentID, ref data, this.m_netService, this.m_vehicleType, false);
//                }
//                else
//                {
//                    WBUpdatePath(segmentID, ref data, this.m_netService, this.m_vehicleType, false);
//                }
//            }
//        }

//        public static bool WBUpdatePath(ushort segmentID, ref NetSegment data, ItemClass.Service netService, VehicleInfo.VehicleType vehicleType, bool skipQueue)
//        {
//            if (data.m_path == 0u)
//            {
//                return TransportLineAI.StartPathFind(segmentID, ref data, netService, vehicleType, skipQueue);
//            }
//            if ((data.m_flags & NetSegment.Flags.WaitingPath) == NetSegment.Flags.None)
//            {
//                return true;
//            }
//            PathManager instance = Singleton<PathManager>.instance;
//            NetManager instance2 = Singleton<NetManager>.instance;
//            byte pathFindFlags = instance.m_pathUnits.m_buffer[(int)((UIntPtr)data.m_path)].m_pathFindFlags;
//            if ((pathFindFlags & 4) != 0)
//            {
//                bool flag = false;
//                PathUnit.Position pathPos;
//                if (instance.m_pathUnits.m_buffer[(int)((UIntPtr)data.m_path)].GetPosition(0, out pathPos))
//                {
//                    flag = TransportLineAI.CheckNodePosition(data.m_startNode, pathPos);
//                }
//                if (instance.m_pathUnits.m_buffer[(int)((UIntPtr)data.m_path)].GetLastPosition(out pathPos))
//                {
//                    TransportLineAI.CheckNodePosition(data.m_endNode, pathPos);
//                }
//                data.m_averageLength = instance.m_pathUnits.m_buffer[(int)((UIntPtr)data.m_path)].m_length;
//                if (data.m_lanes != 0u)
//                {
//                    instance2.m_lanes.m_buffer[(int)((UIntPtr)data.m_lanes)].m_length = data.m_averageLength * ((!flag) ? multiplierNormal : multiplierFree);
//                }
//                data.m_flags &= ~NetSegment.Flags.WaitingPath;
//                data.m_flags &= ~NetSegment.Flags.PathFailed;
//                return true;
//            }
//            if ((pathFindFlags & 8) != 0)
//            {
//                if (data.m_averageLength == 0f)
//                {
//                    Vector3 position = instance2.m_nodes.m_buffer[(int)data.m_startNode].m_position;
//                    Vector3 position2 = instance2.m_nodes.m_buffer[(int)data.m_endNode].m_position;
//                    data.m_averageLength = Vector3.Distance(position, position2);
//                }
//                data.m_flags &= ~NetSegment.Flags.WaitingPath;
//                data.m_flags |= NetSegment.Flags.PathFailed;
//                return true;
//            }
//            return false;
//        }


//        public override void UpdateLanes(ushort segmentID, ref NetSegment data, bool loading)
//        {
//            NetManager instance = Singleton<NetManager>.instance;
//            uint num = 0u;
//            uint num2 = data.m_lanes;
//            Vector3 position = instance.m_nodes.m_buffer[(int)data.m_startNode].m_position;
//            Vector3 position2 = instance.m_nodes.m_buffer[(int)data.m_endNode].m_position;
//            Vector3 b = Vector3.Lerp(position, position2, 0.333333343f);
//            Vector3 c = Vector3.Lerp(position, position2, 0.6666667f);
//            if (data.m_averageLength == 0f)
//            {
//                data.m_averageLength = Vector3.Distance(position, position2);
//            }
//            DistrictManager instance2 = Singleton<DistrictManager>.instance;
//            byte district = instance2.GetDistrict(position);
//            DistrictPolicies.Services servicePolicies = instance2.m_districts.m_buffer[(int)district].m_servicePolicies;
//            bool flag = (servicePolicies & DistrictPolicies.Services.FreeTransport) != DistrictPolicies.Services.None;
            
//            float length = data.m_averageLength * ((!flag) ? multiplierNormal : multiplierFree );
//            for (int i = 0; i < this.m_info.m_lanes.Length; i++)
//            {
//                if (num2 == 0u)
//                {
//                    if (!Singleton<NetManager>.instance.CreateLanes(out num2, ref Singleton<SimulationManager>.instance.m_randomizer, segmentID, 1))
//                    {
//                        break;
//                    }
//                    instance.m_lanes.m_buffer[(int)((UIntPtr)num)].m_nextLane = num2;
//                }
//                instance.m_lanes.m_buffer[(int)((UIntPtr)num2)].m_bezier = new Bezier3(position, b, c, position2);
//                instance.m_lanes.m_buffer[(int)((UIntPtr)num2)].m_segment = segmentID;
//                instance.m_lanes.m_buffer[(int)((UIntPtr)num2)].m_firstTarget = 0;
//                instance.m_lanes.m_buffer[(int)((UIntPtr)num2)].m_lastTarget = 255;
//                instance.m_lanes.m_buffer[(int)((UIntPtr)num2)].m_length = length;
//                num = num2;
//                num2 = instance.m_lanes.m_buffer[(int)((UIntPtr)num2)].m_nextLane;
//            }
//            if (data.m_path != 0u)
//            {
//                Singleton<PathManager>.instance.ReleasePath(data.m_path);
//                data.m_path = 0u;
//            }
//        }
//    }
//}
