using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace DifficultyMod
{
    public class WBPassengerCarAI : PassengerCarAI
    {
        private static byte[] blockCounter = new byte[16384u];
        public override void SimulationStep(ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
        {
//            Logger.dbgLog("called .");
            byte bc = blockCounter[vehicleID];
            
            if (data.m_blockCounter == 0)
            {
                bc = 0;
                blockCounter[vehicleID] = 0;
            }
            else if (data.m_blockCounter > 1)
            {
                bc = (byte)Mathf.Min(bc + 1, 255);
                WBResidentAI6.AddCommuteWait(data, 1);
            }
            
            if ((data.m_flags & Vehicle.Flags.Congestion) != Vehicle.Flags.None)
            {
                bc = (byte)Mathf.Min(bc + 5, 255);
                data.m_flags &= ~Vehicle.Flags.Congestion;
            }

            data.m_blockCounter = 1;
            blockCounter[vehicleID] = bc;

            if (bc == 255)
            {
                blockCounter[vehicleID] = 0;
                Singleton<VehicleManager>.instance.ReleaseVehicle(vehicleID);
            }
            else
            {
                base.SimulationStep(vehicleID, ref data, physicsLodRefPos);
            }
        }

        public override void SimulationStep(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
        {
//            Logger.dbgLog("called ..");
            if ((vehicleData.m_flags & Vehicle.Flags.Stopped) != Vehicle.Flags.None)
            {
                vehicleData.m_waitCounter += 1;
                if (this.CanLeave(vehicleID, ref vehicleData))
                {
                    vehicleData.m_flags &= ~Vehicle.Flags.Stopped;
                    vehicleData.m_waitCounter = 0;
                }
            }
            base.SimulationStep(vehicleID, ref vehicleData, ref frameData, leaderID, ref leaderData, lodPhysics);
        }
    }

}
