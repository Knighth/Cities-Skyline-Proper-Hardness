using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DifficultyMod
{
    class CitizenHelper5
    {

        public static void GetCitizenIncome(CitizenUnit citizenUnit, ref int income)
        {
            int tourists = 0;
            GetCitizenIncome(citizenUnit, ref income,ref tourists);
        }

        public static void GetCitizenIncome(CitizenUnit citizenUnit, ref int income,ref int tourists)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            if (citizenUnit.m_citizen0 != 0u)
            {
                GetCitizenIncome(instance.m_citizens.m_buffer[(int)((UIntPtr)citizenUnit.m_citizen0)], ref income,ref tourists);
            }
            if (citizenUnit.m_citizen1 != 0u)
            {
                GetCitizenIncome(instance.m_citizens.m_buffer[(int)((UIntPtr)citizenUnit.m_citizen1)], ref income, ref tourists);
            }
            if (citizenUnit.m_citizen2 != 0u)
            {
                GetCitizenIncome(instance.m_citizens.m_buffer[(int)((UIntPtr)citizenUnit.m_citizen2)], ref income, ref tourists);
            }
            if (citizenUnit.m_citizen3 != 0u)
            {
                GetCitizenIncome(instance.m_citizens.m_buffer[(int)((UIntPtr)citizenUnit.m_citizen3)], ref income, ref tourists);
            }
            if (citizenUnit.m_citizen4 != 0u)
            {
                GetCitizenIncome(instance.m_citizens.m_buffer[(int)((UIntPtr)citizenUnit.m_citizen4)], ref income, ref tourists);
            }
        }


        public static void GetCitizenIncome(Citizen citizen, ref int income,ref int tourists)
        {
            if ((citizen.m_flags & Citizen.Flags.MovingIn) == Citizen.Flags.None && !citizen.Dead)
            {
                bool tourist = ((citizen.m_flags & Citizen.Flags.Tourist) != Citizen.Flags.None);
                int age = citizen.Age;
                Citizen.Education educationLevel = citizen.EducationLevel;
                Citizen.AgePhase agePhase = Citizen.GetAgePhase(educationLevel, age);
                int unemployed = citizen.Unemployed;
                var result = 0;

                if (citizen.Sick)
                {
                    result -= 50;
                }
                if (unemployed == 0 || tourist)
                {
                    switch (agePhase)
                    {
                        case Citizen.AgePhase.Child:
                            result += 20;
                            break;
                        case Citizen.AgePhase.Teen0:
                            result += 30;
                            break;
                        case Citizen.AgePhase.Teen1:
                            result += 40;
                            break;
                        case Citizen.AgePhase.Young0:
                            result += 50;
                            break;
                        case Citizen.AgePhase.Young1:
                            result += 60;
                            break;
                        case Citizen.AgePhase.Young2:
                            result += 70;
                            break;
                        case Citizen.AgePhase.Adult0:
                            result += 50;
                            break;
                        case Citizen.AgePhase.Adult1:
                            result += 60;
                            break;
                        case Citizen.AgePhase.Adult2:
                            result += 70;
                            break;
                        case Citizen.AgePhase.Adult3:
                            result += 80;
                            break;
                        case Citizen.AgePhase.Senior0:
                            result += 50;
                            break;
                        case Citizen.AgePhase.Senior1:
                            result += 60;
                            break;
                        case Citizen.AgePhase.Senior2:
                            result += 70;
                            break;
                        case Citizen.AgePhase.Senior3:
                            result += 80;
                            break;
                    }
                }
                result += citizen.m_health + citizen.m_wellbeing - 155;
                if (tourist)
                {
                    tourists += result;
                }
                else
                {
                    income += result;
                }                
            }
        }


        public static void GetHomeBehaviour(ushort buildingID, Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount, ref int homeCount, ref int aliveHomeCount, ref int emptyHomeCount)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            uint num = buildingData.m_citizenUnits;
            int num2 = 0;
            while (num != 0u)
            {
                if ((ushort)(instance.m_units.m_buffer[(int)((UIntPtr)num)].m_flags & CitizenUnit.Flags.Home) != 0)
                {
                    int num3 = 0;
                    int num4 = 0;
                    instance.m_units.m_buffer[(int)((UIntPtr)num)].GetCitizenHomeBehaviour(ref behaviour, ref num3, ref num4);
                    if (num3 != 0)
                    {
                        aliveHomeCount++;
                        aliveCount += num3;
                    }
                    if (num4 != 0)
                    {
                        totalCount += num4;
                    }
                    else
                    {
                        emptyHomeCount++;
                    }
                    homeCount++;
                }
                num = instance.m_units.m_buffer[(int)((UIntPtr)num)].m_nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }

        public static void GetWorkBehaviour(ushort buildingID, Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            uint num = buildingData.m_citizenUnits;
            int num2 = 0;
            while (num != 0u)
            {
                if ((ushort)(instance.m_units.m_buffer[(int)((UIntPtr)num)].m_flags & CitizenUnit.Flags.Work) != 0)
                {
                    instance.m_units.m_buffer[(int)((UIntPtr)num)].GetCitizenWorkBehaviour(ref behaviour, ref aliveCount, ref totalCount);
                }
                num = instance.m_units.m_buffer[(int)((UIntPtr)num)].m_nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }

        public static void GetVisitBehaviour(ushort buildingID, Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            uint num = buildingData.m_citizenUnits;
            int num2 = 0;
            while (num != 0u)
            {
                if ((ushort)(instance.m_units.m_buffer[(int)((UIntPtr)num)].m_flags & CitizenUnit.Flags.Visit) != 0)
                {
                    instance.m_units.m_buffer[(int)((UIntPtr)num)].GetCitizenVisitBehaviour(ref behaviour, ref aliveCount, ref totalCount);
                }
                num = instance.m_units.m_buffer[(int)((UIntPtr)num)].m_nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }


        internal static int GetBaseIncome(ItemClass.Level level, ItemClass.Zone zone)
        {
            switch (zone){
                case ItemClass.Zone.CommercialHigh:
                    switch (level)
                    {
                        case ItemClass.Level.Level1:
                            return 155;
                        case ItemClass.Level.Level2:
                            return 150;
                        case ItemClass.Level.Level3:
                            return 150;
                    }
                    break;
                case ItemClass.Zone.CommercialLow:
                    switch (level)
                    {
                        case ItemClass.Level.Level1:
                            return 190;
                        case ItemClass.Level.Level2:
                            return 200;
                        case ItemClass.Level.Level3:
                            return 210;
                    }
                    break;
                case ItemClass.Zone.ResidentialHigh:
                    switch (level)
                    {
                        case ItemClass.Level.Level1:
                            return 145;
                        case ItemClass.Level.Level2:
                            return 145;
                        case ItemClass.Level.Level3:
                            return 145;
                        case ItemClass.Level.Level4:
                            return 145;
                        case ItemClass.Level.Level5:
                            return 145;
                    }
                    break;
                case ItemClass.Zone.ResidentialLow:
                    switch (level)
                    {
                        case ItemClass.Level.Level1:
                            return 170;
                        case ItemClass.Level.Level2:
                            return 180;
                        case ItemClass.Level.Level3:
                            return 190;
                        case ItemClass.Level.Level4:
                            return 195;
                        case ItemClass.Level.Level5:
                            return 195;
                    }
                    break;
                case ItemClass.Zone.Office:
                    switch (level)
                    {
                        case ItemClass.Level.Level1:
                            return 170;
                        case ItemClass.Level.Level2:
                            return 170;
                        case ItemClass.Level.Level3:
                            return 170;
                    }
                    break;
                case ItemClass.Zone.Industrial:
                    switch (level)
                    {
                        case ItemClass.Level.Level1:
                            return 220;
                        case ItemClass.Level.Level2:
                            return 230;
                        case ItemClass.Level.Level3:
                            return 230;
                    }
                    break;
            }
            return 0;
        }
    }
}
