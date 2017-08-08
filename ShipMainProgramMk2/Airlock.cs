using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
    partial class Program {
        /// <summary>
        /// Airlock system.  Default side A = space, side B = inside.
        /// </summary>
        public class Airlock {

            public string ZoneName { get; set; }
            public List<IMyAirVent> Vents { get; set; }
            public List<IMyInteriorLight> Lights { get; set; }
            public List<IMyDoor> DoorsA { get; set; }
            public List<IMyDoor> DoorsB { get; set; }
            public List<IMyAirVent> VentsA { get; set; }
            public List<IMyAirVent> VentsB { get; set; }
            public List<IMyGasTank> O2Tanks { get; set; }
            public string Status {
                get { return StatusBuilder.ToString(); }
                set {
                    StatusBuilder.Clear();
                    StatusBuilder.Append(value);
                }
            }
            private StringBuilder StatusBuilder = new StringBuilder();
            public string AirlockStatus { get; private set; }
            public int AirLimit { get; set; }

            public float AirlockPressure { get; private set; } = 0;
            private bool toBreakOut = true;
            public bool AirSideA { get; private set; } = false;
            public bool AirSideB { get; private set; } = true;
            public float O2TanksFill { get; private set; } = 0;
            public int AirTanksCount { get; private set; } = 0;
            private MyResourceSinkComponent sink;
            private ListReader<MyDefinitionId> list;
            private bool hasO2;

            public Airlock(string zoneName, List<IMyAirVent> vents, List<IMyInteriorLight> lights, List<IMyDoor> doorsSideA, List<IMyDoor> doorsSideB,
                List<IMyGasTank> o2Tanks, int airLimit = 5) : this(zoneName, vents, lights, doorsSideA, doorsSideB, o2Tanks, airLimit,
                    new List<IMyAirVent>(), new List<IMyAirVent>()) {
            }

            public Airlock(string zoneName, List<IMyAirVent> vents, List<IMyInteriorLight> lights, List<IMyDoor> doorsSideA, List<IMyDoor> doorsSideB,
                List<IMyGasTank> o2Tanks, int airLimit, List<IMyAirVent> ventsSideA, List<IMyAirVent> ventsSideB) {
                ZoneName = zoneName;
                Vents = new List<IMyAirVent>(vents);
                Lights = new List<IMyInteriorLight>(lights);
                DoorsA = new List<IMyDoor>(doorsSideA);
                DoorsB = new List<IMyDoor>(doorsSideB);
                VentsA = new List<IMyAirVent>(ventsSideA);
                VentsB = new List<IMyAirVent>(ventsSideB);
                O2Tanks = new List<IMyGasTank>(o2Tanks);
                AirLimit = airLimit;
                Status = "";
                //RoomPressure = new RoomPressure();
            }

            public bool Update() {
                if (Vents.Count == 0 || Lights.Count == 0 || DoorsA.Count == 0 || DoorsB.Count == 0) return false;
                toBreakOut = true;
                //RoomPressure handling
                AirlockPressure = Vents[0].GetOxygenLevel() * 100;
                if (AirlockPressure > AirLimit) {
                    foreach (IMyInteriorLight de in Lights) de.Color = Color.White;
                } else {
                    foreach (IMyInteriorLight de in Lights) de.Color = Color.Red;
                    if (DoorsA[0].Status == DoorStatus.Open && DoorsB[0].Status == DoorStatus.Open && !Vents[0].Depressurize) {
                        DoorClose(DoorsA);
                        DoorClose(DoorsB);
                    }
                }
                    StatusBuilder.Clear();
                    StatusBuilder.Append(ZoneName);
                    StatusBuilder.Append($" O2 {AirlockPressure}% {AirlockStatus}");

                foreach (IMyLightingBlock de in Lights) if (de.CustomName.Contains("Control") && !de.Enabled) toBreakOut = false;
                if (toBreakOut) return true;
                GetAirTankFill(O2Tanks);
                //get Airpressure
                if (VentsA.Count < 1 || (VentsA[0].GetOxygenLevel() * 100) < AirLimit) {
                    AirSideA = false;
                } else {
                    AirSideA = true;
                }
                if (VentsB.Count < 1 || (VentsB[0].GetOxygenLevel() * 100) > AirLimit) {
                    AirSideB = true;
                } else {
                    AirSideB = false;
                }
                //special cases
                if ((AirSideA && AirSideB && !Vents[0].Depressurize) || (!AirSideA && !AirSideB && Vents[0].Depressurize)) {
                    DoorOn(DoorsA);
                    DoorOn(DoorsB);
                    DoorOpen(DoorsA);
                    DoorOpen(DoorsB);
                    AirlockStatus =" Both Doors Open";
                    ControlOn(Lights);
                    return true;
                }
                if ((DoorsA[0].Enabled && DoorsB[0].Enabled) || (!DoorsA[0].Enabled && !DoorsB[0].Enabled)) {
                    if (DoorsB[0].Status == DoorStatus.Closed) {
                        DoorOff(DoorsB);
                        AirlockStatus =" Unknown state";
                        return true;
                    } else {
                        DoorOn(DoorsA);
                        DoorOn(DoorsB);
                        DoorClose(DoorsA);
                        DoorClose(DoorsB);
                        AirlockStatus = " Unknown state";
                        return true;
                    }
                }

                if (!AirLockMirror(DoorsA, DoorsB, AirSideA)) { AirLockMirror(DoorsB, DoorsA, AirSideB); }
                return true;
            }

            private bool AirLockMirror(List<IMyDoor> doors1, List<IMyDoor> doors2, bool airSide1) {
                if (!doors1[0].Enabled) {
                    if (!airSide1) {
                        if (!Vents[0].Depressurize) {
                            foreach (IMyAirVent de in Vents) de.Depressurize = true;
                            DoorClose(doors2);
                            AirlockStatus = " Depressurizing";
                            return true;
                        } else {
                            if (O2TanksFill == 100 || AirlockPressure == 0) {
                                DoorOff(doors2);
                                DoorOn(doors1);
                                DoorOpen(doors1);
                                AirlockStatus = " Depressurized";
                                ControlOn(Lights);
                                return true;
                            }
                        }
                    } else {
                        if (!Vents[0].Depressurize) {
                            if (O2TanksFill == 0 || AirlockPressure > AirLimit) {
                                DoorOff(doors2);
                                DoorOn(doors1);
                                DoorOpen(doors1);
                                AirlockStatus = " Pressurized";
                                ControlOn(Lights);
                                return true;
                            }
                        } else {
                            foreach (IMyAirVent de in Vents) de.Depressurize = false;
                            DoorClose(doors2);
                            AirlockStatus = " Pressurizing";
                            return true;
                        }
                    }
                }
                return false;
            }

            private void ControlOn(List<IMyInteriorLight> lights) {
                foreach (IMyLightingBlock de in Lights) if (de.CustomName.Contains("Control")) de.Enabled = true;
            }

            private void DoorClose(List<IMyDoor> doors) {
                foreach (IMyDoor de in doors) de.CloseDoor();
            }

            private void DoorOn(List<IMyDoor> doors) {
                foreach (IMyDoor de in doors) de.Enabled = true;
            }

            private void DoorOpen(List<IMyDoor> doors) {
                foreach (IMyDoor de in doors) de.OpenDoor();
            }

            private void DoorOff(List<IMyDoor> doors) {
                foreach (IMyDoor de in doors) de.Enabled = false;
            }

            float GetAirTankFill(List<IMyGasTank> airTanks) {
                AirTanksCount = 0;
                //get percent in tanks 
                O2TanksFill = 0;
                foreach (IMyGasTank de in airTanks) {
                    de.Components.TryGet<MyResourceSinkComponent>(out sink);
                    list = sink.AcceptedResources;
                    hasO2 = false;
                    for (int j = 0; j < list.Count; ++j) {
                        hasO2 = list[j].SubtypeId.ToString() == "Oxygen";
                        if (hasO2) { break; }
                    }
                    if (hasO2) {
                        O2TanksFill += de.FilledRatio * 100;
                        ++AirTanksCount;
                    }
                }
                O2TanksFill /= AirTanksCount;
                return O2TanksFill;
            }
        }
    }
}
