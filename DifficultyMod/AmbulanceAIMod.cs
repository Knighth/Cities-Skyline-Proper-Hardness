using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class AmbulanceAIMod : CarAIMod
{
    public int m_paramedicCount = 2;
    public int m_patientCapacity = 2;

    public override bool ArriveAtDestination(ushort vehicleID, ref Vehicle vehicleData)
    {
        if ((vehicleData.m_flags & Vehicle.Flags.WaitingTarget) != Vehicle.Flags.None)
        {
            return false;
        }
        if ((vehicleData.m_flags & Vehicle.Flags.GoingBack) != Vehicle.Flags.None)
        {
            return this.ArriveAtSource(vehicleID, ref vehicleData);
        }
        return this.ArriveAtTarget(vehicleID, ref vehicleData);
    }

    private bool ArriveAtSource(ushort vehicleID, ref Vehicle data)
    {
        if (data.m_sourceBuilding == 0)
        {
            Singleton<VehicleManager>.instance.ReleaseVehicle(vehicleID);
            return true;
        }
        CitizenManager instance = Singleton<CitizenManager>.instance;
        uint citizenUnits = data.m_citizenUnits;
        int num2 = 0;
        while (citizenUnits != 0)
        {
            uint nextUnit = instance.m_units.m_buffer[citizenUnits].m_nextUnit;
            for (int i = 0; i < 5; i++)
            {
                uint citizen = instance.m_units.m_buffer[citizenUnits].GetCitizen(i);
                if ((citizen != 0) && instance.m_citizens.m_buffer[citizen].Sick)
                {
                    instance.m_citizens.m_buffer[citizen].SetVehicle(citizen, 0, 0);
                    instance.m_citizens.m_buffer[citizen].SetVisitplace(citizen, data.m_sourceBuilding, 0);
                    instance.m_citizens.m_buffer[citizen].CurrentLocation = Citizen.Location.Visit;
                }
            }
            citizenUnits = nextUnit;
            if (++num2 > 0x80000)
            {
                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                break;
            }
        }
        this.RemoveSource(vehicleID, ref data);
        Singleton<VehicleManager>.instance.ReleaseVehicle(vehicleID);
        return true;
    }

    private bool ArriveAtTarget(ushort vehicleID, ref Vehicle data)
    {
        if (data.m_targetBuilding == 0)
        {
            Singleton<VehicleManager>.instance.ReleaseVehicle(vehicleID);
            return true;
        }
        CitizenManager instance = Singleton<CitizenManager>.instance;
        uint citizenUnits = data.m_citizenUnits;
        int num2 = 0;
        while (citizenUnits != 0)
        {
            uint nextUnit = instance.m_units.m_buffer[citizenUnits].m_nextUnit;
            for (int j = 0; j < 5; j++)
            {
                uint citizen = instance.m_units.m_buffer[citizenUnits].GetCitizen(j);
                if ((citizen != 0) && (instance.m_citizens.m_buffer[citizen].CurrentLocation != Citizen.Location.Moving))
                {
                    ushort num6 = instance.m_citizens.m_buffer[citizen].m_instance;
                    if (num6 != 0)
                    {
                        instance.ReleaseCitizenInstance(num6);
                    }
                    instance.m_citizens.m_buffer[citizen].CurrentLocation = Citizen.Location.Moving;
                    data.m_transferSize = (ushort)(data.m_transferSize + 1);
                }
            }
            citizenUnits = nextUnit;
            if (++num2 > 0x80000)
            {
                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                break;
            }
        }
        for (int i = 0; i < this.m_paramedicCount; i++)
        {
            this.CreateParamedic(vehicleID, ref data, Citizen.AgePhase.Adult0);
        }
        data.m_flags |= Vehicle.Flags.Stopped;
        data.m_flags &= ~Vehicle.Flags.Emergency2;
        this.SetTarget(vehicleID, ref data, 0);
        return false;
    }

    public override void BuildingRelocated(ushort vehicleID, ref Vehicle data, ushort building)
    {
        base.BuildingRelocated(vehicleID, ref data, building);
        if (building == data.m_sourceBuilding)
        {
            if ((data.m_flags & Vehicle.Flags.GoingBack) != Vehicle.Flags.None)
            {
                this.InvalidPath(vehicleID, ref data, vehicleID, ref data);
            }
        }
        else if ((building == data.m_targetBuilding) && ((data.m_flags & Vehicle.Flags.GoingBack) == Vehicle.Flags.None))
        {
            this.InvalidPath(vehicleID, ref data, vehicleID, ref data);
        }
    }

    protected override float CalculateTargetSpeed(ushort vehicleID, ref Vehicle data, float speedLimit, float curve)
    {
        if ((data.m_flags & Vehicle.Flags.Emergency2) == Vehicle.Flags.None)
        {
            return base.CalculateTargetSpeed(vehicleID, ref data, speedLimit, curve);
        }
        return Mathf.Min(base.CalculateTargetSpeed(vehicleID, ref data, speedLimit * 1.25f, curve * 0.5f), base.m_info.m_maxSpeed);
    }

    public override bool CanLeave(ushort vehicleID, ref Vehicle vehicleData)
    {
        SimulationManager instance = Singleton<SimulationManager>.instance;
        CitizenManager manager2 = Singleton<CitizenManager>.instance;
        bool flag = true;
        bool flag2 = false;
        ushort index = 0;
        uint citizenUnits = vehicleData.m_citizenUnits;
        int num3 = 0;
        while (citizenUnits != 0)
        {
            uint nextUnit = manager2.m_units.m_buffer[citizenUnits].m_nextUnit;
            for (int i = 0; i < 5; i++)
            {
                uint citizen = manager2.m_units.m_buffer[citizenUnits].GetCitizen(i);
                if ((citizen != 0) && !manager2.m_citizens.m_buffer[citizen].Sick)
                {
                    ushort num7 = manager2.m_citizens.m_buffer[citizen].m_instance;
                    if ((num7 != 0) && (manager2.m_instances.m_buffer[num7].Info.m_class.m_service == base.m_info.m_class.m_service))
                    {
                        if ((manager2.m_instances.m_buffer[num7].m_flags & CitizenInstance.Flags.EnteringVehicle) == CitizenInstance.Flags.None)
                        {
                            if ((manager2.m_instances.m_buffer[num7].m_flags & CitizenInstance.Flags.Character) != CitizenInstance.Flags.None)
                            {
                                flag2 = true;
                            }
                            else if (index == 0)
                            {
                                index = num7;
                            }
                            else
                            {
                                manager2.ReleaseCitizenInstance(num7);
                            }
                        }
                        flag = false;
                    }
                }
            }
            citizenUnits = nextUnit;
            if (++num3 > 0x80000)
            {
                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                break;
            }
        }
        if (!flag2 && (index != 0))
        {
            CitizenInfo info = manager2.GetGroupCitizenInfo(ref instance.m_randomizer, base.m_info.m_class.m_service, Citizen.Gender.Female, Citizen.AgePhase.Adult1);
            if (info != null)
            {
                manager2.m_instances.m_buffer[index].Info = info;
            }
            else
            {
                info = manager2.m_instances.m_buffer[index].Info;
            }
            info.m_citizenAI.SetTarget(index, ref manager2.m_instances.m_buffer[index], 0);
        }
        return flag;
    }

    private void CreateParamedic(ushort vehicleID, ref Vehicle data, Citizen.AgePhase agePhase)
    {
        SimulationManager instance = Singleton<SimulationManager>.instance;
        CitizenManager manager2 = Singleton<CitizenManager>.instance;
        CitizenInfo info = manager2.GetGroupCitizenInfo(ref instance.m_randomizer, base.m_info.m_class.m_service, Citizen.Gender.Female, agePhase);
        if (info != null)
        {
            int family = instance.m_randomizer.Int32(0x100);
            uint citizen = 0;
            if (manager2.CreateCitizen(out citizen, 90, family, ref instance.m_randomizer, info.m_gender))
            {
                ushort num3;
                if (manager2.CreateCitizenInstance(out num3, ref instance.m_randomizer, info, citizen))
                {
                    Vector3 randomDoorPosition = data.GetRandomDoorPosition(ref instance.m_randomizer, VehicleInfo.DoorType.Exit);
                    info.m_citizenAI.SetCurrentVehicle(num3, ref manager2.m_instances.m_buffer[num3], 0, 0, randomDoorPosition);
                    info.m_citizenAI.SetTarget(num3, ref manager2.m_instances.m_buffer[num3], data.m_targetBuilding);
                    manager2.m_citizens.m_buffer[citizen].SetVehicle(citizen, vehicleID, 0);
                }
                else
                {
                    manager2.ReleaseCitizen(citizen);
                }
            }
        }
    }

    public override void CreateVehicle(ushort vehicleID, ref Vehicle data)
    {
        base.CreateVehicle(vehicleID, ref data);
        data.m_flags |= Vehicle.Flags.WaitingTarget;
        Singleton<CitizenManager>.instance.CreateUnits(out data.m_citizenUnits, ref Singleton<SimulationManager>.instance.m_randomizer, 0, vehicleID, 0, 0, 0, this.m_patientCapacity + this.m_paramedicCount, 0);
    }

    public override void GetBufferStatus(ushort vehicleID, ref Vehicle data, out string localeKey, out int current, out int max)
    {
        CitizenManager instance = Singleton<CitizenManager>.instance;
        uint citizenUnits = data.m_citizenUnits;
        int num2 = 0;
        int num3 = 0;
        while (citizenUnits != 0)
        {
            uint nextUnit = instance.m_units.m_buffer[citizenUnits].m_nextUnit;
            for (int i = 0; i < 5; i++)
            {
                uint citizen = instance.m_units.m_buffer[citizenUnits].GetCitizen(i);
                if ((citizen != 0) && (instance.m_citizens.m_buffer[citizen].CurrentLocation == Citizen.Location.Moving))
                {
                    num2++;
                }
            }
            citizenUnits = nextUnit;
            if (++num3 > 0x80000)
            {
                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                break;
            }
        }
        localeKey = "Ambulance";
        current = num2;
        max = this.m_patientCapacity;
    }

    public override Color GetColor(ushort vehicleID, ref Vehicle data, InfoManager.InfoMode infoMode)
    {
        if ((infoMode == InfoManager.InfoMode.Health) && (Singleton<InfoManager>.instance.CurrentSubMode == InfoManager.SubInfoMode.Default))
        {
            return Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_activeColor;
        }
        return base.GetColor(vehicleID, ref data, infoMode);
    }

    public override string GetLocalizedStatus(ushort vehicleID, ref Vehicle data, out InstanceID target)
    {
        if ((data.m_flags & Vehicle.Flags.GoingBack) != Vehicle.Flags.None)
        {
            if (data.m_transferSize == 0)
            {
                target = InstanceID.Empty;
                return Locale.Get("VEHICLE_STATUS_AMBULANCE_RETURN_EMPTY");
            }
            target = InstanceID.Empty;
            return Locale.Get("VEHICLE_STATUS_AMBULANCE_RETURN_FULL");
        }
        if ((data.m_flags & Vehicle.Flags.WaitingTarget) != Vehicle.Flags.None)
        {
            target = InstanceID.Empty;
            return Locale.Get("VEHICLE_STATUS_AMBULANCE_WAIT");
        }
        if (((data.m_flags & Vehicle.Flags.Emergency2) != Vehicle.Flags.None) && (data.m_targetBuilding != 0))
        {
            target = InstanceID.Empty;
            target.Building = data.m_targetBuilding;
            return Locale.Get("VEHICLE_STATUS_AMBULANCE_EMERGENCY");
        }
        target = InstanceID.Empty;
        return Locale.Get("VEHICLE_STATUS_CONFUSED");
    }

    public override void GetSize(ushort vehicleID, ref Vehicle data, out int size, out int max)
    {
        size = data.m_transferSize;
        max = this.m_patientCapacity;
    }

    public override InstanceID GetTargetID(ushort vehicleID, ref Vehicle vehicleData)
    {
        InstanceID eid = new InstanceID();
        if ((vehicleData.m_flags & Vehicle.Flags.GoingBack) != Vehicle.Flags.None)
        {
            eid.Building = vehicleData.m_sourceBuilding;
            return eid;
        }
        eid.Building = vehicleData.m_targetBuilding;
        return eid;
    }

    public override void LoadVehicle(ushort vehicleID, ref Vehicle data)
    {
        base.LoadVehicle(vehicleID, ref data);
        if (data.m_sourceBuilding != 0)
        {
            Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_sourceBuilding].AddOwnVehicle(vehicleID, ref data);
        }
        if (data.m_targetBuilding != 0)
        {
            Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding].AddGuestVehicle(vehicleID, ref data);
        }
    }

    public override void ReleaseVehicle(ushort vehicleID, ref Vehicle data)
    {
        this.RemoveOffers(vehicleID, ref data);
        this.RemoveSource(vehicleID, ref data);
        this.RemoveTarget(vehicleID, ref data);
        base.ReleaseVehicle(vehicleID, ref data);
    }

    private void RemoveOffers(ushort vehicleID, ref Vehicle data)
    {
        if ((data.m_flags & Vehicle.Flags.WaitingTarget) != Vehicle.Flags.None)
        {
            TransferManager.TransferOffer offer = new TransferManager.TransferOffer
            {
                Vehicle = vehicleID
            };
            Singleton<TransferManager>.instance.RemoveIncomingOffer((TransferManager.TransferReason)data.m_transferType, offer);
        }
    }

    private void RemoveSource(ushort vehicleID, ref Vehicle data)
    {
        if (data.m_sourceBuilding != 0)
        {
            Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_sourceBuilding].RemoveOwnVehicle(vehicleID, ref data);
            data.m_sourceBuilding = 0;
        }
    }

    private void RemoveTarget(ushort vehicleID, ref Vehicle data)
    {
        if (data.m_targetBuilding != 0)
        {
            Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding].RemoveGuestVehicle(vehicleID, ref data);
            data.m_targetBuilding = 0;
        }
    }

    public override void SetSource(ushort vehicleID, ref Vehicle data, ushort sourceBuilding)
    {
        this.RemoveSource(vehicleID, ref data);
        data.m_sourceBuilding = sourceBuilding;
        if (sourceBuilding != 0)
        {
            Vector3 vector;
            Vector3 vector2;
            BuildingManager instance = Singleton<BuildingManager>.instance;
            BuildingInfo info = instance.m_buildings.m_buffer[sourceBuilding].Info;
            data.Unspawn(vehicleID);
            Randomizer randomizer = new Randomizer((int)vehicleID);
            info.m_buildingAI.CalculateSpawnPosition(sourceBuilding, ref instance.m_buildings.m_buffer[sourceBuilding], ref randomizer, base.m_info, out vector, out vector2);
            Quaternion identity = Quaternion.identity;
            Vector3 forward = vector2 - vector;
            if (forward.sqrMagnitude > 0.01f)
            {
                identity = Quaternion.LookRotation(forward);
            }
            data.m_frame0 = new Vehicle.Frame(vector, identity);
            data.m_frame1 = data.m_frame0;
            data.m_frame2 = data.m_frame0;
            data.m_frame3 = data.m_frame0;
            data.m_targetPos0 = vector;
            data.m_targetPos0.w = 2f;
            data.m_targetPos1 = vector2;
            data.m_targetPos1.w = 2f;
            data.m_targetPos2 = data.m_targetPos1;
            data.m_targetPos3 = data.m_targetPos1;
            this.FrameDataUpdated(vehicleID, ref data, ref data.m_frame0);
            Singleton<BuildingManager>.instance.m_buildings.m_buffer[sourceBuilding].AddOwnVehicle(vehicleID, ref data);
        }
    }

    public override void SetTarget(ushort vehicleID, ref Vehicle data, ushort targetBuilding)
    {
        this.RemoveTarget(vehicleID, ref data);
        data.m_targetBuilding = targetBuilding;
        data.m_flags &= ~Vehicle.Flags.WaitingTarget;
        data.m_waitCounter = 0;
        if (targetBuilding != 0)
        {
            if (data.m_transferType == 2)
            {
                data.m_flags |= Vehicle.Flags.Emergency2;
            }
            Singleton<BuildingManager>.instance.m_buildings.m_buffer[targetBuilding].AddGuestVehicle(vehicleID, ref data);
        }
        else if ((data.m_transferSize < this.m_patientCapacity) && !this.ShouldReturnToSource(vehicleID, ref data))
        {
            TransferManager.TransferOffer offer = new TransferManager.TransferOffer
            {
                Priority = 7,
                Vehicle = vehicleID
            };
            if (data.m_sourceBuilding != 0)
            {
                offer.Position = (Vector3)((data.GetLastFramePosition() + Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_sourceBuilding].m_position) * 0.5f);
            }
            else
            {
                offer.Position = data.GetLastFramePosition();
            }
            offer.Amount = 1;
            offer.Active = true;
            Singleton<TransferManager>.instance.AddIncomingOffer((TransferManager.TransferReason)data.m_transferType, offer);
            data.m_flags |= Vehicle.Flags.WaitingTarget;
        }
        else
        {
            data.m_flags |= Vehicle.Flags.GoingBack;
        }
        if (!this.StartPathFind(vehicleID, ref data))
        {
            data.Unspawn(vehicleID);
        }
    }

    private bool ShouldReturnToSource(ushort vehicleID, ref Vehicle data)
    {
        if (data.m_sourceBuilding != 0)
        {
            BuildingManager instance = Singleton<BuildingManager>.instance;
            if (((instance.m_buildings.m_buffer[data.m_sourceBuilding].m_flags & Building.Flags.Active) == Building.Flags.None) && (instance.m_buildings.m_buffer[data.m_sourceBuilding].m_fireIntensity == 0))
            {
                return true;
            }
        }
        return false;
    }

    public override void SimulationStep(ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
    {
        if (((data.m_flags & Vehicle.Flags.WaitingTarget) != Vehicle.Flags.None) && ((data.m_waitCounter = (byte)(data.m_waitCounter + 1)) > 20))
        {
            this.RemoveOffers(vehicleID, ref data);
            data.m_flags &= ~Vehicle.Flags.WaitingTarget;
            data.m_flags |= Vehicle.Flags.GoingBack;
            data.m_waitCounter = 0;
            if (!this.StartPathFind(vehicleID, ref data))
            {
                data.Unspawn(vehicleID);
            }
        }
        base.SimulationStep(vehicleID, ref data, physicsLodRefPos);
    }

    public override void SimulationStep(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
    {
        frameData.m_blinkState = ((vehicleData.m_flags & Vehicle.Flags.Emergency2) == Vehicle.Flags.None) ? 0f : 10f;
        base.SimulationStep(vehicleID, ref vehicleData, ref frameData, leaderID, ref leaderData, lodPhysics);
        if (((vehicleData.m_flags & Vehicle.Flags.Stopped) != Vehicle.Flags.None) && this.CanLeave(vehicleID, ref vehicleData))
        {
            vehicleData.m_flags &= ~Vehicle.Flags.Stopped;
            vehicleData.m_flags |= Vehicle.Flags.Leaving;
        }
        if (((vehicleData.m_flags & Vehicle.Flags.GoingBack) == Vehicle.Flags.None) && this.ShouldReturnToSource(vehicleID, ref vehicleData))
        {
            this.SetTarget(vehicleID, ref vehicleData, 0);
        }
    }

    protected override bool StartPathFind(ushort vehicleID, ref Vehicle vehicleData)
    {
        if ((vehicleData.m_flags & Vehicle.Flags.WaitingTarget) != Vehicle.Flags.None)
        {
            return true;
        }
        if ((vehicleData.m_flags & Vehicle.Flags.GoingBack) != Vehicle.Flags.None)
        {
            if (vehicleData.m_sourceBuilding != 0)
            {
                Vector3 vector;
                Vector3 vector2;
                BuildingManager instance = Singleton<BuildingManager>.instance;
                BuildingInfo info = instance.m_buildings.m_buffer[vehicleData.m_sourceBuilding].Info;
                Randomizer randomizer = new Randomizer((int)vehicleID);
                info.m_buildingAI.CalculateUnspawnPosition(vehicleData.m_sourceBuilding, ref instance.m_buildings.m_buffer[vehicleData.m_sourceBuilding], ref randomizer, base.m_info, out vector, out vector2);
                return this.StartPathFind(vehicleID, ref vehicleData, (Vector3)vehicleData.m_targetPos3, vector2);
            }
        }
        else if (vehicleData.m_targetBuilding != 0)
        {
            Vector3 vector3;
            Vector3 vector4;
            BuildingManager manager2 = Singleton<BuildingManager>.instance;
            BuildingInfo info2 = manager2.m_buildings.m_buffer[vehicleData.m_targetBuilding].Info;
            Randomizer randomizer2 = new Randomizer((int)vehicleID);
            info2.m_buildingAI.CalculateUnspawnPosition(vehicleData.m_targetBuilding, ref manager2.m_buildings.m_buffer[vehicleData.m_targetBuilding], ref randomizer2, base.m_info, out vector3, out vector4);
            return this.StartPathFind(vehicleID, ref vehicleData, (Vector3)vehicleData.m_targetPos3, vector4);
        }
        return false;
    }

    public override void StartTransfer(ushort vehicleID, ref Vehicle data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
    {
        if (material == ((TransferManager.TransferReason)data.m_transferType))
        {
            if ((data.m_flags & Vehicle.Flags.WaitingTarget) != Vehicle.Flags.None)
            {
                uint citizen = offer.Citizen;
                ushort buildingByLocation = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizen].GetBuildingByLocation();
                this.SetTarget(vehicleID, ref data, buildingByLocation);
                Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizen].SetVehicle(citizen, vehicleID, 0);
            }
        }
        else
        {
            base.StartTransfer(vehicleID, ref data, material, offer);
        }
    }

    public override void UpdateBuildingTargetPositions(ushort vehicleID, ref Vehicle vehicleData, Vector3 refPos, ushort leaderID, ref Vehicle leaderData, ref int index, float minSqrDistance)
    {
        if ((leaderData.m_flags & Vehicle.Flags.WaitingTarget) == Vehicle.Flags.None)
        {
            if ((leaderData.m_flags & Vehicle.Flags.GoingBack) != Vehicle.Flags.None)
            {
                if (leaderData.m_sourceBuilding != 0)
                {
                    Vector3 vector;
                    Vector3 vector2;
                    BuildingManager instance = Singleton<BuildingManager>.instance;
                    BuildingInfo info = instance.m_buildings.m_buffer[leaderData.m_sourceBuilding].Info;
                    Randomizer randomizer = new Randomizer((int)vehicleID);
                    info.m_buildingAI.CalculateUnspawnPosition(vehicleData.m_sourceBuilding, ref instance.m_buildings.m_buffer[leaderData.m_sourceBuilding], ref randomizer, base.m_info, out vector, out vector2);
                    vehicleData.SetTargetPos(index++, base.CalculateTargetPoint(refPos, vector2, minSqrDistance, 2f));
                }
            }
            else if (leaderData.m_targetBuilding != 0)
            {
                Vector3 vector3;
                Vector3 vector4;
                BuildingManager manager2 = Singleton<BuildingManager>.instance;
                BuildingInfo info2 = manager2.m_buildings.m_buffer[leaderData.m_targetBuilding].Info;
                Randomizer randomizer2 = new Randomizer((int)vehicleID);
                info2.m_buildingAI.CalculateUnspawnPosition(vehicleData.m_targetBuilding, ref manager2.m_buildings.m_buffer[leaderData.m_targetBuilding], ref randomizer2, base.m_info, out vector3, out vector4);
                vehicleData.SetTargetPos(index++, base.CalculateTargetPoint(refPos, vector4, minSqrDistance, 2f));
            }
        }
    }
}
