using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class PassengerCarAIMod : CarAIMod
{
    public override bool ArriveAtDestination(ushort vehicleID, ref Vehicle vehicleData)
    {
        return this.ArriveAtTarget(vehicleID, ref vehicleData);
    }

    private bool ArriveAtTarget(ushort vehicleID, ref Vehicle data)
    {
        if ((data.m_flags & Vehicle.Flags.Parking) != Vehicle.Flags.None)
        {
            VehicleManager instance = Singleton<VehicleManager>.instance;
            CitizenManager manager2 = Singleton<CitizenManager>.instance;
            ushort driverInstance = this.GetDriverInstance(vehicleID, ref data);
            if (driverInstance != 0)
            {
                uint citizen = manager2.m_instances.m_buffer[driverInstance].m_citizen;
                if (citizen != 0)
                {
                    ushort parkedVehicle = manager2.m_citizens.m_buffer[citizen].m_parkedVehicle;
                    if (parkedVehicle != 0)
                    {
                        Vehicle.Frame lastFrameData = data.GetLastFrameData();
                        instance.m_parkedVehicles.m_buffer[parkedVehicle].m_travelDistance = lastFrameData.m_travelDistance;
                        instance.m_parkedVehicles.m_buffer[parkedVehicle].m_flags = (ushort)(instance.m_parkedVehicles.m_buffer[parkedVehicle].m_flags & 0xfff7);
                        InstanceID empty = InstanceID.Empty;
                        empty.Vehicle = vehicleID;
                        InstanceID newID = InstanceID.Empty;
                        newID.ParkedVehicle = parkedVehicle;
                        Singleton<InstanceManager>.instance.ChangeInstance(empty, newID);
                    }
                }
            }
        }
        this.UnloadPassengers(vehicleID, ref data);
        if (data.m_targetBuilding != 0)
        {
            data.m_targetPos0 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding].CalculateSidewalkPosition();
            data.m_targetPos0.w = 2f;
            data.m_targetPos1 = data.m_targetPos0;
            data.m_targetPos2 = data.m_targetPos0;
            data.m_targetPos3 = data.m_targetPos0;
            this.RemoveTarget(vehicleID, ref data);
        }
        return true;
    }

    public override void BuildingRelocated(ushort vehicleID, ref Vehicle data, ushort building)
    {
        base.BuildingRelocated(vehicleID, ref data, building);
        if (building == data.m_targetBuilding)
        {
            this.InvalidPath(vehicleID, ref data, vehicleID, ref data);
        }
    }

    protected override float CalculateTargetSpeed(ushort vehicleID, ref Vehicle data, float speedLimit, float curve)
    {
        return base.CalculateTargetSpeed(vehicleID, ref data, speedLimit, curve);
    }

    public override bool CanLeave(ushort vehicleID, ref Vehicle vehicleData)
    {
        if (vehicleData.m_waitCounter < 2)
        {
            return false;
        }
        return base.CanLeave(vehicleID, ref vehicleData);
    }

    private static bool CheckOverlap(ushort ignoreParked, Segment3 segment)
    {
        VehicleManager instance = Singleton<VehicleManager>.instance;
        Vector3 vector = segment.Min();
        Vector3 vector2 = segment.Max();
        int num = Mathf.Max((int)(((vector.x - 10f) / 32f) + 270f), 0);
        int num2 = Mathf.Max((int)(((vector.z - 10f) / 32f) + 270f), 0);
        int num3 = Mathf.Min((int)(((vector2.x + 10f) / 32f) + 270f), 0x21b);
        int num4 = Mathf.Min((int)(((vector2.z + 10f) / 32f) + 270f), 0x21b);
        bool overlap = false;
        for (int i = num2; i <= num4; i++)
        {
            for (int j = num; j <= num3; j++)
            {
                ushort otherID = instance.m_parkedGrid[(i * 540) + j];
                int num8 = 0;
                while (otherID != 0)
                {
                    otherID = CheckOverlap(ignoreParked, segment, otherID, ref instance.m_parkedVehicles.m_buffer[otherID], ref overlap);
                    if (++num8 > 0x8000)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                        break;
                    }
                }
            }
        }
        return overlap;
    }

    private static ushort CheckOverlap(ushort ignoreParked, Segment3 segment, ushort otherID, ref VehicleParked otherData, ref bool overlap)
    {
        if (otherID != ignoreParked)
        {
            float num;
            float num2;
            VehicleInfo info = otherData.Info;
            Vector3 vector = (Vector3)(otherData.m_rotation * new Vector3(0f, 0f, (info.m_generatedInfo.m_size.z * 0.5f) - 1f));
            Segment3 segment2 = new Segment3(otherData.m_position + vector, otherData.m_position - vector);
            if (segment.DistanceSqr(segment2, out num, out num2) < 1f)
            {
                overlap = true;
            }
        }
        return otherData.m_nextGridParked;
    }

    private static bool CheckOverlap(ushort ignoreParked, ref Bezier3 bezier, float offset, float length, out float minPos, out float maxPos)
    {
        VehicleManager instance = Singleton<VehicleManager>.instance;
        float t = bezier.Travel(offset, length * -0.5f);
        float num2 = bezier.Travel(offset, length * 0.5f);
        bool overlap = false;
        minPos = offset;
        maxPos = offset;
        if (t < 0.001f)
        {
            overlap = true;
            t = 0f;
            minPos = -1f;
            maxPos = Mathf.Max(maxPos, bezier.Travel(0f, (length * 0.5f) + 0.5f));
        }
        if (num2 > 0.999f)
        {
            overlap = true;
            num2 = 1f;
            maxPos = 2f;
            minPos = Mathf.Min(minPos, bezier.Travel(1f, (length * -0.5f) - 0.5f));
        }
        Vector3 pos = bezier.Position(offset);
        Vector3 dir = bezier.Tangent(offset);
        Vector3 lhs = bezier.Position(t);
        Vector3 rhs = bezier.Position(num2);
        Vector3 vector5 = Vector3.Min(lhs, rhs);
        Vector3 vector6 = Vector3.Max(lhs, rhs);
        int num3 = Mathf.Max((int)(((vector5.x - 10f) / 32f) + 270f), 0);
        int num4 = Mathf.Max((int)(((vector5.z - 10f) / 32f) + 270f), 0);
        int num5 = Mathf.Min((int)(((vector6.x + 10f) / 32f) + 270f), 0x21b);
        int num6 = Mathf.Min((int)(((vector6.z + 10f) / 32f) + 270f), 0x21b);
        for (int i = num4; i <= num6; i++)
        {
            for (int j = num3; j <= num5; j++)
            {
                ushort otherID = instance.m_parkedGrid[(i * 540) + j];
                int num10 = 0;
                while (otherID != 0)
                {
                    otherID = CheckOverlap(ignoreParked, ref bezier, pos, dir, offset, length, otherID, ref instance.m_parkedVehicles.m_buffer[otherID], ref overlap, ref minPos, ref maxPos);
                    if (++num10 > 0x8000)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                        break;
                    }
                }
            }
        }
        return overlap;
    }

    private static ushort CheckOverlap(ushort ignoreParked, ref Bezier3 bezier, Vector3 pos, Vector3 dir, float offset, float length, ushort otherID, ref VehicleParked otherData, ref bool overlap, ref float minPos, ref float maxPos)
    {
        if (otherID != ignoreParked)
        {
            VehicleInfo info = otherData.Info;
            Vector3 lhs = otherData.m_position - pos;
            float num = ((length + info.m_generatedInfo.m_size.z) * 0.5f) + 1f;
            float magnitude = lhs.magnitude;
            if (magnitude < (num - 0.5f))
            {
                float num3;
                float num4;
                overlap = true;
                if (Vector3.Dot(lhs, dir) >= 0f)
                {
                    num3 = num + magnitude;
                    num4 = num - magnitude;
                }
                else
                {
                    num3 = num - magnitude;
                    num4 = num + magnitude;
                }
                maxPos = Mathf.Max(maxPos, bezier.Travel(offset, num3));
                minPos = Mathf.Min(minPos, bezier.Travel(offset, -num4));
            }
        }
        return otherData.m_nextGridParked;
    }

    public override void CreateVehicle(ushort vehicleID, ref Vehicle data)
    {
        base.CreateVehicle(vehicleID, ref data);
        Singleton<CitizenManager>.instance.CreateUnits(out data.m_citizenUnits, ref Singleton<SimulationManager>.instance.m_randomizer, 0, vehicleID, 0, 0, 0, 5, 0);
    }

    private static bool FindParkingSpace(ushort homeID, Vector3 refPos, Vector3 searchDir, ushort segment, float width, float length, out Vector3 parkPos, out Quaternion parkRot, out float parkOffset)
    {
        Vector3 vector = refPos + ((Vector3)(searchDir * 16f));
        if (Singleton<SimulationManager>.instance.m_randomizer.Int32(3) == 0)
        {
            if (FindParkingSpaceRoadSide(0, segment, refPos, width - 0.2f, length, out parkPos, out parkRot, out parkOffset))
            {
                return true;
            }
            if (FindParkingSpaceBuilding(homeID, 0, vector, width, length, 16f, out parkPos, out parkRot))
            {
                parkOffset = -1f;
                return true;
            }
        }
        else
        {
            if (FindParkingSpaceBuilding(homeID, 0, vector, width, length, 16f, out parkPos, out parkRot))
            {
                parkOffset = -1f;
                return true;
            }
            if (FindParkingSpaceRoadSide(0, segment, refPos, width - 0.2f, length, out parkPos, out parkRot, out parkOffset))
            {
                return true;
            }
        }
        return false;
    }

    private static bool FindParkingSpaceBuilding(ushort homeID, ushort ignoreParked, Vector3 refPos, float width, float length, float maxDistance, out Vector3 parkPos, out Quaternion parkRot)
    {
        parkPos = Vector3.zero;
        parkRot = Quaternion.identity;
        float num = refPos.x - maxDistance;
        float num2 = refPos.z - maxDistance;
        float num3 = refPos.x + maxDistance;
        float num4 = refPos.z + maxDistance;
        int num5 = Mathf.Max((int)(((num - 72f) / 64f) + 135f), 0);
        int num6 = Mathf.Max((int)(((num2 - 72f) / 64f) + 135f), 0);
        int num7 = Mathf.Min((int)(((num3 + 72f) / 64f) + 135f), 0x10d);
        int num8 = Mathf.Min((int)(((num4 + 72f) / 64f) + 135f), 0x10d);
        BuildingManager instance = Singleton<BuildingManager>.instance;
        bool flag = false;
        for (int i = num6; i <= num8; i++)
        {
            for (int j = num5; j <= num7; j++)
            {
                ushort buildingID = instance.m_buildingGrid[(i * 270) + j];
                int num12 = 0;
                while (buildingID != 0)
                {
                    if (FindParkingSpaceBuilding(homeID, ignoreParked, buildingID, ref instance.m_buildings.m_buffer[buildingID], refPos, width, length, ref maxDistance, ref parkPos, ref parkRot))
                    {
                        flag = true;
                    }
                    buildingID = instance.m_buildings.m_buffer[buildingID].m_nextGridBuilding;
                    if (++num12 >= 0x8000)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                        break;
                    }
                }
            }
        }
        return flag;
    }

    private static bool FindParkingSpaceBuilding(ushort homeID, ushort ignoreParked, ushort buildingID, ref Building building, Vector3 refPos, float width, float length, ref float maxDistance, ref Vector3 parkPos, ref Quaternion parkRot)
    {
        int num = building.Width;
        int num2 = building.Length;
        float num3 = Mathf.Sqrt((float)((num * num) + (num2 * num2))) * 8f;
        if (VectorUtils.LengthXZ(building.m_position - refPos) >= (maxDistance + num3))
        {
            return false;
        }
        BuildingInfo info = building.Info;
        Matrix4x4 matrixx = new Matrix4x4();
        bool flag = false;
        bool flag2 = false;
        if ((info.m_class.m_service != ItemClass.Service.Residential) || (buildingID == homeID))
        {
            if (info.m_props == null)
            {
                return flag2;
            }
            for (int i = 0; i < info.m_props.Length; i++)
            {
                BuildingInfo.Prop prop = info.m_props[i];
                Randomizer r = new Randomizer((buildingID << 6) | prop.m_index);
                if ((r.Int32(100) < prop.m_probability) && (num2 >= prop.m_requiredLength))
                {
                    PropInfo finalProp = prop.m_finalProp;
                    if (finalProp != null)
                    {
                        finalProp = finalProp.GetVariation(ref r);
                        if ((finalProp.m_parkingSpaces != null) && (finalProp.m_parkingSpaces.Length != 0))
                        {
                            if (!flag)
                            {
                                flag = true;
                                Vector3 pos = Building.CalculateMeshPosition(info, building.m_position, building.m_angle, building.Length);
                                Quaternion q = Quaternion.AngleAxis(building.m_angle * 57.29578f, Vector3.down);
                                matrixx.SetTRS(pos, q, Vector3.one);
                            }
                            Vector3 position = matrixx.MultiplyPoint(prop.m_position);
                            if (FindParkingSpaceProp(ignoreParked, finalProp, position, building.m_angle + prop.m_radAngle, prop.m_fixedHeight, refPos, width, length, ref maxDistance, ref parkPos, ref parkRot))
                            {
                                flag2 = true;
                            }
                        }
                    }
                }
            }
        }
        return flag2;
    }

    private static bool FindParkingSpaceProp(ushort ignoreParked, PropInfo info, Vector3 position, float angle, bool fixedHeight, Vector3 refPos, float width, float length, ref float maxDistance, ref Vector3 parkPos, ref Quaternion parkRot)
    {
        bool flag = false;
        Matrix4x4 matrixx = new Matrix4x4();
        Quaternion q = Quaternion.AngleAxis(angle * 57.29578f, Vector3.down);
        matrixx.SetTRS(position, q, Vector3.one);
        for (int i = 0; i < info.m_parkingSpaces.Length; i++)
        {
            Vector3 a = matrixx.MultiplyPoint(info.m_parkingSpaces[i].m_position);
            float num2 = Vector3.Distance(a, refPos);
            if (num2 < maxDistance)
            {
                float num3 = (info.m_parkingSpaces[i].m_size.z - length) * 0.5f;
                Vector3 forward = matrixx.MultiplyVector(info.m_parkingSpaces[i].m_direction);
                a += (Vector3)(forward * num3);
                if (fixedHeight)
                {
                    Vector3 vector3 = (Vector3)(forward * ((length * 0.5f) - 1f));
                    Segment3 segment = new Segment3(a + vector3, a - vector3);
                    if (!CheckOverlap(ignoreParked, segment))
                    {
                        parkPos = a;
                        parkRot = Quaternion.LookRotation(forward);
                        maxDistance = num2;
                        flag = true;
                    }
                }
                else
                {
                    Vector3 worldPos = a + new Vector3(((forward.x * length) * 0.25f) + ((forward.z * width) * 0.4f), 0f, ((forward.z * length) * 0.25f) - ((forward.x * width) * 0.4f));
                    Vector3 vector5 = a + new Vector3(((forward.x * length) * 0.25f) - ((forward.z * width) * 0.4f), 0f, ((forward.z * length) * 0.25f) + ((forward.x * width) * 0.4f));
                    Vector3 vector6 = a - new Vector3(((forward.x * length) * 0.25f) - ((forward.z * width) * 0.4f), 0f, ((forward.z * length) * 0.25f) + ((forward.x * width) * 0.4f));
                    Vector3 vector7 = a - new Vector3(((forward.x * length) * 0.25f) + ((forward.z * width) * 0.4f), 0f, ((forward.z * length) * 0.25f) - ((forward.x * width) * 0.4f));
                    worldPos.y = Singleton<TerrainManager>.instance.SampleDetailHeight(worldPos);
                    vector5.y = Singleton<TerrainManager>.instance.SampleDetailHeight(vector5);
                    vector6.y = Singleton<TerrainManager>.instance.SampleDetailHeight(vector6);
                    vector7.y = Singleton<TerrainManager>.instance.SampleDetailHeight(vector7);
                    a.y = (((worldPos.y + vector5.y) + vector6.y) + vector7.y) * 0.25f;
                    Vector3 vector11 = ((worldPos + vector5) - vector6) - vector7;
                    Vector3 normalized = vector11.normalized;
                    Vector3 vector9 = (Vector3)(normalized * ((length * 0.5f) - 1f));
                    Segment3 segment2 = new Segment3(a + vector9, a - vector9);
                    if (!CheckOverlap(ignoreParked, segment2))
                    {
                        Vector3 rhs = ((worldPos + vector6) - vector5) - vector7;
                        parkPos = a;
                        parkRot = Quaternion.LookRotation(normalized, Vector3.Cross(normalized, rhs));
                        maxDistance = num2;
                        flag = true;
                    }
                }
            }
        }
        return flag;
    }

    private static bool FindParkingSpaceRoadSide(ushort ignoreParked, ushort requireSegment, Vector3 refPos, float width, float length, out Vector3 parkPos, out Quaternion parkRot, out float parkOffset)
    {
        PathUnit.Position position;
        parkPos = Vector3.zero;
        parkRot = Quaternion.identity;
        parkOffset = 0f;
        if (PathManager.FindPathPosition(refPos, ItemClass.Service.Road, NetInfo.LaneType.None | NetInfo.LaneType.Parking, VehicleInfo.VehicleType.Car, 32f, out position))
        {
            float num5;
            float num6;
            if ((requireSegment != 0) && (position.m_segment != requireSegment))
            {
                return false;
            }
            NetManager instance = Singleton<NetManager>.instance;
            NetInfo info = instance.m_segments.m_buffer[position.m_segment].Info;
            uint laneID = PathManager.GetLaneID(position);
            uint lanes = instance.m_segments.m_buffer[position.m_segment].m_lanes;
            for (int i = 0; (i < info.m_lanes.Length) && (lanes != 0); i++)
            {
                if (((instance.m_lanes.m_buffer[lanes].m_flags & 0x100) != 0) && ((info.m_lanes[position.m_lane].m_position >= 0f) == (info.m_lanes[i].m_position >= 0f)))
                {
                    return false;
                }
                lanes = instance.m_lanes.m_buffer[lanes].m_nextLane;
            }
            bool flag = (instance.m_segments.m_buffer[position.m_segment].m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
            bool flag2 = ((byte)(info.m_lanes[position.m_lane].m_finalDirection & NetInfo.Direction.Forward)) == 0;
            bool flag3 = info.m_lanes[position.m_lane].m_position < 0f;
            float offset = position.m_offset * 0.003921569f;
            if (CheckOverlap(ignoreParked, ref instance.m_lanes.m_buffer[laneID].m_bezier, offset, length, out num5, out num6))
            {
                offset = -1f;
                for (int j = 0; j < 6; j++)
                {
                    float num8;
                    float num9;
                    if (num6 <= 1f)
                    {
                        if (CheckOverlap(ignoreParked, ref instance.m_lanes.m_buffer[laneID].m_bezier, num6, length, out num8, out num9))
                        {
                            num6 = num9;
                        }
                        else
                        {
                            offset = num6;
                            break;
                        }
                    }
                    if (num5 >= 0f)
                    {
                        if (CheckOverlap(ignoreParked, ref instance.m_lanes.m_buffer[laneID].m_bezier, num5, length, out num8, out num9))
                        {
                            num5 = num8;
                        }
                        else
                        {
                            offset = num5;
                            break;
                        }
                    }
                }
            }
            if (offset >= 0f)
            {
                Vector3 vector;
                Vector3 vector2;
                instance.m_lanes.m_buffer[laneID].CalculatePositionAndDirection(offset, out vector, out vector2);
                float num10 = (info.m_lanes[position.m_lane].m_width - width) * 0.5f;
                vector2.Normalize();
                if (flag != flag3)
                {
                    parkPos.x = vector.x - (vector2.z * num10);
                    parkPos.y = vector.y;
                    parkPos.z = vector.z + (vector2.x * num10);
                }
                else
                {
                    parkPos.x = vector.x + (vector2.z * num10);
                    parkPos.y = vector.y;
                    parkPos.z = vector.z - (vector2.x * num10);
                }
                if (flag != flag2)
                {
                    parkRot = Quaternion.LookRotation(-vector2);
                }
                else
                {
                    parkRot = Quaternion.LookRotation(vector2);
                }
                parkOffset = offset;
                return true;
            }
        }
        return false;
    }

    public override Color GetColor(ushort vehicleID, ref Vehicle data, InfoManager.InfoMode infoMode)
    {
        if (infoMode == InfoManager.InfoMode.Connections)
        {
            if (Singleton<InfoManager>.instance.CurrentSubMode == InfoManager.SubInfoMode.WindPower)
            {
                CitizenManager instance = Singleton<CitizenManager>.instance;
                uint citizenUnits = data.m_citizenUnits;
                int num2 = 0;
                while (citizenUnits != 0)
                {
                    uint nextUnit = instance.m_units.m_buffer[citizenUnits].m_nextUnit;
                    for (int i = 0; i < 5; i++)
                    {
                        uint citizen = instance.m_units.m_buffer[citizenUnits].GetCitizen(i);
                        if ((citizen != 0) && ((instance.m_citizens.m_buffer[citizen].m_flags & Citizen.Flags.Tourist) != Citizen.Flags.None))
                        {
                            return Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_targetColor;
                        }
                    }
                    citizenUnits = nextUnit;
                    if (++num2 > 0x80000)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                        break;
                    }
                }
            }
        }
        else
        {
            if (infoMode != InfoManager.InfoMode.None)
            {
                return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
            }
            if (base.m_info.m_useColorVariations)
            {
                Randomizer randomizer = new Randomizer((uint)data.m_transferSize);
                switch (randomizer.Int32(4))
                {
                    case 0:
                        return base.m_info.m_color0;

                    case 1:
                        return base.m_info.m_color1;

                    case 2:
                        return base.m_info.m_color2;

                    case 3:
                        return base.m_info.m_color3;
                }
            }
            return base.m_info.m_color0;
        }
        return base.GetColor(vehicleID, ref data, infoMode);
    }

    public override Color GetColor(ushort parkedVehicleID, ref VehicleParked data, InfoManager.InfoMode infoMode)
    {
        if (infoMode != InfoManager.InfoMode.None)
        {
            return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
        }
        if (base.m_info.m_useColorVariations)
        {
            Randomizer randomizer = new Randomizer(data.m_ownerCitizen & 0xffff);
            switch (randomizer.Int32(4))
            {
                case 0:
                    return base.m_info.m_color0;

                case 1:
                    return base.m_info.m_color1;

                case 2:
                    return base.m_info.m_color2;

                case 3:
                    return base.m_info.m_color3;
            }
        }
        return base.m_info.m_color0;
    }

    private ushort GetDriverInstance(ushort vehicleID, ref Vehicle data)
    {
        CitizenManager instance = Singleton<CitizenManager>.instance;
        uint citizenUnits = data.m_citizenUnits;
        int num2 = 0;
        while (citizenUnits != 0)
        {
            uint nextUnit = instance.m_units.m_buffer[citizenUnits].m_nextUnit;
            for (int i = 0; i < 5; i++)
            {
                uint citizen = instance.m_units.m_buffer[citizenUnits].GetCitizen(i);
                if (citizen != 0)
                {
                    ushort num6 = instance.m_citizens.m_buffer[citizen].m_instance;
                    if (num6 != 0)
                    {
                        return num6;
                    }
                }
            }
            citizenUnits = nextUnit;
            if (++num2 > 0x80000)
            {
                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                break;
            }
        }
        return 0;
    }

    public override string GetLocalizedStatus(ushort vehicleID, ref Vehicle data, out InstanceID target)
    {
        CitizenManager instance = Singleton<CitizenManager>.instance;
        ushort driverInstance = this.GetDriverInstance(vehicleID, ref data);
        ushort index = 0;
        if (driverInstance != 0)
        {
            if ((data.m_flags & Vehicle.Flags.Parking) != Vehicle.Flags.None)
            {
                uint citizen = instance.m_instances.m_buffer[driverInstance].m_citizen;
                if ((citizen != 0) && (instance.m_citizens.m_buffer[citizen].m_parkedVehicle != 0))
                {
                    target = InstanceID.Empty;
                    return Locale.Get("VEHICLE_STATUS_PARKING");
                }
            }
            index = instance.m_instances.m_buffer[driverInstance].m_targetBuilding;
        }
        if (index != 0)
        {
            if ((Singleton<BuildingManager>.instance.m_buildings.m_buffer[index].m_flags & Building.Flags.IncomingOutgoing) != Building.Flags.None)
            {
                target = InstanceID.Empty;
                return Locale.Get("VEHICLE_STATUS_LEAVING");
            }
            target = InstanceID.Empty;
            target.Building = index;
            return Locale.Get("VEHICLE_STATUS_GOINGTO");
        }
        target = InstanceID.Empty;
        return Locale.Get("VEHICLE_STATUS_CONFUSED");
    }

    public override string GetLocalizedStatus(ushort parkedVehicleID, ref VehicleParked data, out InstanceID target)
    {
        target = InstanceID.Empty;
        return Locale.Get("VEHICLE_STATUS_PARKED");
    }

    public override InstanceID GetOwnerID(ushort vehicleID, ref Vehicle vehicleData)
    {
        InstanceID eid = new InstanceID();
        ushort driverInstance = this.GetDriverInstance(vehicleID, ref vehicleData);
        if (driverInstance != 0)
        {
            eid.Citizen = Singleton<CitizenManager>.instance.m_instances.m_buffer[driverInstance].m_citizen;
        }
        return eid;
    }

    public override InstanceID GetTargetID(ushort vehicleID, ref Vehicle vehicleData)
    {
        InstanceID eid = new InstanceID();
        ushort driverInstance = this.GetDriverInstance(vehicleID, ref vehicleData);
        if (driverInstance != 0)
        {
            eid.Building = Singleton<CitizenManager>.instance.m_instances.m_buffer[driverInstance].m_targetBuilding;
        }
        return eid;
    }

    public override void LoadVehicle(ushort vehicleID, ref Vehicle data)
    {
        base.LoadVehicle(vehicleID, ref data);
        if (data.m_targetBuilding != 0)
        {
            Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding].AddGuestVehicle(vehicleID, ref data);
        }
    }

    protected override bool ParkVehicle(ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint nextPath, int nextPositionIndex, out byte segmentOffset)
    {
        PathManager instance = Singleton<PathManager>.instance;
        CitizenManager manager2 = Singleton<CitizenManager>.instance;
        NetManager manager3 = Singleton<NetManager>.instance;
        VehicleManager manager4 = Singleton<VehicleManager>.instance;
        uint index = 0;
        uint citizenUnits = vehicleData.m_citizenUnits;
        int num3 = 0;
        while ((citizenUnits != 0) && (index == 0))
        {
            uint nextUnit = manager2.m_units.m_buffer[citizenUnits].m_nextUnit;
            for (int i = 0; i < 5; i++)
            {
                uint citizen = manager2.m_units.m_buffer[citizenUnits].GetCitizen(i);
                if (citizen != 0)
                {
                    ushort num7 = manager2.m_citizens.m_buffer[citizen].m_instance;
                    if (num7 != 0)
                    {
                        index = manager2.m_instances.m_buffer[num7].m_citizen;
                        break;
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
        if (index != 0)
        {
            Vector3 vector;
            Vector3 vector2;
            Vector3 vector3;
            Vector3 vector4;
            Quaternion quaternion;
            float num10;
            ushort num11;
            uint laneID = PathManager.GetLaneID(pathPos);
            segmentOffset = (byte)Singleton<SimulationManager>.instance.m_randomizer.Int32(1, 0xfe);
            manager3.m_lanes.m_buffer[laneID].CalculatePositionAndDirection(((float)segmentOffset) * 0.003921569f, out vector, out vector2);
            NetInfo info = manager3.m_segments.m_buffer[pathPos.m_segment].Info;
            bool flag = (manager3.m_segments.m_buffer[pathPos.m_segment].m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
            bool flag2 = info.m_lanes[pathPos.m_lane].m_position < 0f;
            vector2.Normalize();
            if (flag != flag2)
            {
                vector3.x = -vector2.z;
                vector3.y = 0f;
                vector3.z = vector2.x;
            }
            else
            {
                vector3.x = vector2.z;
                vector3.y = 0f;
                vector3.z = -vector2.x;
            }
            ushort homeID = 0;
            if (index != 0)
            {
                homeID = Singleton<CitizenManager>.instance.m_citizens.m_buffer[index].m_homeBuilding;
            }
            if (FindParkingSpace(homeID, vector, vector3, pathPos.m_segment, base.m_info.m_generatedInfo.m_size.x, base.m_info.m_generatedInfo.m_size.z, out vector4, out quaternion, out num10) && manager4.CreateParkedVehicle(out num11, ref Singleton<SimulationManager>.instance.m_randomizer, base.m_info, vector4, quaternion, index))
            {
                manager2.m_citizens.m_buffer[index].SetParkedVehicle(index, num11);
                if (num10 >= 0f)
                {
                    segmentOffset = (byte)(num10 * 255f);
                }
            }
        }
        else
        {
            segmentOffset = pathPos.m_offset;
        }
        if (index != 0)
        {
            uint num12 = vehicleData.m_citizenUnits;
            int num13 = 0;
            while (num12 != 0)
            {
                uint num14 = manager2.m_units.m_buffer[num12].m_nextUnit;
                for (int j = 0; j < 5; j++)
                {
                    uint num16 = manager2.m_units.m_buffer[num12].GetCitizen(j);
                    if (num16 != 0)
                    {
                        ushort num17 = manager2.m_citizens.m_buffer[num16].m_instance;
                        if ((num17 != 0) && instance.AddPathReference(nextPath))
                        {
                            if (manager2.m_instances.m_buffer[num17].m_path != 0)
                            {
                                instance.ReleasePath(manager2.m_instances.m_buffer[num17].m_path);
                            }
                            manager2.m_instances.m_buffer[num17].m_path = nextPath;
                            manager2.m_instances.m_buffer[num17].m_pathPositionIndex = (byte)nextPositionIndex;
                            manager2.m_instances.m_buffer[num17].m_lastPathOffset = segmentOffset;
                        }
                    }
                }
                num12 = num14;
                if (++num13 > 0x80000)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                    break;
                }
            }
        }
        return true;
    }

    public override void ReleaseVehicle(ushort vehicleID, ref Vehicle data)
    {
        this.RemoveTarget(vehicleID, ref data);
        base.ReleaseVehicle(vehicleID, ref data);
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
        }
    }

    public override void SetTarget(ushort vehicleID, ref Vehicle data, ushort targetBuilding)
    {
        this.RemoveTarget(vehicleID, ref data);
        data.m_targetBuilding = targetBuilding;
        if (targetBuilding != 0)
        {
            Singleton<BuildingManager>.instance.m_buildings.m_buffer[targetBuilding].AddGuestVehicle(vehicleID, ref data);
        }
        if (!this.StartPathFind(vehicleID, ref data))
        {
            data.Unspawn(vehicleID);
        }
    }

    public override void SimulationStep(ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
    {
        if ((data.m_flags & Vehicle.Flags.Congestion) != Vehicle.Flags.None)
        {
            Singleton<VehicleManager>.instance.ReleaseVehicle(vehicleID);
        }
        else
        {
            base.SimulationStep(vehicleID, ref data, physicsLodRefPos);
        }
    }

    public override void SimulationStep(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
    {
        if ((vehicleData.m_flags & Vehicle.Flags.Stopped) != Vehicle.Flags.None)
        {
            vehicleData.m_waitCounter = (byte)(vehicleData.m_waitCounter + 1);
            if (this.CanLeave(vehicleID, ref vehicleData))
            {
                vehicleData.m_flags &= ~Vehicle.Flags.Stopped;
                vehicleData.m_waitCounter = 0;
            }
        }
        base.SimulationStep(vehicleID, ref vehicleData, ref frameData, leaderID, ref leaderData, lodPhysics);
    }

    protected override bool StartPathFind(ushort vehicleID, ref Vehicle vehicleData)
    {
        ushort driverInstance = this.GetDriverInstance(vehicleID, ref vehicleData);
        if (driverInstance != 0)
        {
            ushort targetBuilding = Singleton<CitizenManager>.instance.m_instances.m_buffer[driverInstance].m_targetBuilding;
            if (targetBuilding != 0)
            {
                Vector3 vector;
                Vector3 vector2;
                BuildingManager instance = Singleton<BuildingManager>.instance;
                BuildingInfo info = instance.m_buildings.m_buffer[targetBuilding].Info;
                Randomizer randomizer = new Randomizer((int)vehicleID);
                info.m_buildingAI.CalculateUnspawnPosition(targetBuilding, ref instance.m_buildings.m_buffer[targetBuilding], ref randomizer, base.m_info, out vector, out vector2);
                return this.StartPathFind(vehicleID, ref vehicleData, (Vector3)vehicleData.m_targetPos3, vector2);
            }
        }
        return false;
    }

    protected override bool StartPathFind(ushort vehicleID, ref Vehicle vehicleData, Vector3 startPos, Vector3 endPos, bool startBothWays, bool endBothWays)
    {
        VehicleInfo info = base.m_info;
        ushort driverInstance = this.GetDriverInstance(vehicleID, ref vehicleData);
        if (driverInstance != 0)
        {
            PathUnit.Position position;
            PathUnit.Position position2;
            PathUnit.Position position3;
            float num2;
            float num3;
            CitizenManager instance = Singleton<CitizenManager>.instance;
            CitizenInfo info2 = instance.m_instances.m_buffer[driverInstance].Info;
            NetInfo.LaneType laneTypes = NetInfo.LaneType.None | NetInfo.LaneType.Pedestrian | NetInfo.LaneType.Vehicle;
            VehicleInfo.VehicleType vehicleType = base.m_info.m_vehicleType;
            if (PathManager.FindPathPosition(startPos, ItemClass.Service.Road, NetInfo.LaneType.None | NetInfo.LaneType.Vehicle, info.m_vehicleType, 32f, out position, out position2, out num2, out num3) && info2.m_citizenAI.FindPathPosition(driverInstance, ref instance.m_instances.m_buffer[driverInstance], endPos, laneTypes, vehicleType, out position3))
            {
                uint num4;
                if (!startBothWays || (num2 < 10f))
                {
                    position2 = new PathUnit.Position();
                }
                PathUnit.Position endPosB = new PathUnit.Position();
                SimulationManager manager2 = Singleton<SimulationManager>.instance;
                if (Singleton<PathManager>.instance.CreatePath(out num4, ref manager2.m_randomizer, manager2.m_currentBuildIndex, position, position2, position3, endPosB, laneTypes, vehicleType, 20000f))
                {
                    if (vehicleData.m_path != 0)
                    {
                        Singleton<PathManager>.instance.ReleasePath(vehicleData.m_path);
                    }
                    vehicleData.m_path = num4;
                    vehicleData.m_flags |= Vehicle.Flags.WaitingPath;
                    return true;
                }
            }
        }
        return false;
    }

    private void UnloadPassengers(ushort vehicleID, ref Vehicle data)
    {
        CitizenManager instance = Singleton<CitizenManager>.instance;
        uint citizenUnits = data.m_citizenUnits;
        int num2 = 0;
        while (citizenUnits != 0)
        {
            uint nextUnit = instance.m_units.m_buffer[citizenUnits].m_nextUnit;
            for (int i = 0; i < 5; i++)
            {
                uint citizen = instance.m_units.m_buffer[citizenUnits].GetCitizen(i);
                if (citizen != 0)
                {
                    ushort instanceID = instance.m_citizens.m_buffer[citizen].m_instance;
                    if (instanceID != 0)
                    {
                        instance.m_instances.m_buffer[instanceID].Info.m_citizenAI.SetCurrentVehicle(instanceID, ref instance.m_instances.m_buffer[instanceID], 0, 0, (Vector3)data.m_targetPos0);
                    }
                }
            }
            citizenUnits = nextUnit;
            if (++num2 > 0x80000)
            {
                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                break;
            }
        }
    }

    public override void UpdateBuildingTargetPositions(ushort vehicleID, ref Vehicle vehicleData, Vector3 refPos, ushort leaderID, ref Vehicle leaderData, ref int index, float minSqrDistance)
    {
        if ((leaderData.m_flags & Vehicle.Flags.Parking) != Vehicle.Flags.None)
        {
            ushort driverInstance = this.GetDriverInstance(vehicleID, ref vehicleData);
            if (driverInstance != 0)
            {
                CitizenManager instance = Singleton<CitizenManager>.instance;
                VehicleManager manager2 = Singleton<VehicleManager>.instance;
                uint citizen = instance.m_instances.m_buffer[driverInstance].m_citizen;
                if (citizen != 0)
                {
                    ushort parkedVehicle = instance.m_citizens.m_buffer[citizen].m_parkedVehicle;
                    if (parkedVehicle != 0)
                    {
                        Vector3 position = manager2.m_parkedVehicles.m_buffer[parkedVehicle].m_position;
                        Quaternion rotation = manager2.m_parkedVehicles.m_buffer[parkedVehicle].m_rotation;
                        vehicleData.SetTargetPos(index++, base.CalculateTargetPoint(refPos, position, minSqrDistance, 2f));
                        if (index < 4)
                        {
                            Vector4 pos = position + (rotation * new Vector3(0f, 0f, 0.2f));
                            pos.w = 2f;
                            vehicleData.SetTargetPos(index++, pos);
                        }
                    }
                }
            }
        }
    }

    public override void UpdateParkedVehicle(ushort parkedID, ref VehicleParked parkedData)
    {
        Vector3 vector;
        Quaternion quaternion;
        Vector3 vector2;
        Quaternion quaternion2;
        float num6;
        float x = base.m_info.m_generatedInfo.m_size.x;
        float z = base.m_info.m_generatedInfo.m_size.z;
        float maxDistance = 256f;
        bool flag = false;
        uint ownerCitizen = parkedData.m_ownerCitizen;
        ushort homeID = 0;
        if (ownerCitizen != 0)
        {
            homeID = Singleton<CitizenManager>.instance.m_citizens.m_buffer[ownerCitizen].m_homeBuilding;
        }
        if (FindParkingSpaceRoadSide(parkedID, 0, parkedData.m_position, x - 0.2f, z, out vector, out quaternion, out num6))
        {
            float num7 = Vector3.SqrMagnitude(vector - parkedData.m_position);
            if (num7 < maxDistance)
            {
                maxDistance = num7;
                flag = true;
            }
        }
        if (FindParkingSpaceBuilding(homeID, parkedID, parkedData.m_position, x, z, maxDistance, out vector2, out quaternion2))
        {
            vector = vector2;
            quaternion = quaternion2;
            flag = true;
        }
        if (flag)
        {
            Singleton<VehicleManager>.instance.RemoveFromGrid(parkedID, ref parkedData);
            parkedData.m_position = vector;
            parkedData.m_rotation = quaternion;
            Singleton<VehicleManager>.instance.AddToGrid(parkedID, ref parkedData);
        }
        else
        {
            Singleton<VehicleManager>.instance.ReleaseParkedVehicle(parkedID);
        }
    }
}
