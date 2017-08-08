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
        public class Hangar {

            public string ZoneName { get; set; }
            public List<IMyInteriorLight> ControlPressurize { get; set; }
            public List<IMyInteriorLight> ControlDepressurize { get; set; }
            public List<IMyAirVent> Vents { get; set; }
            public List<IMyInteriorLight> WarningLights { get; set; }
            public List<IMySoundBlock> WarningSounds { get; set; }
            public List<IMyDoor> HangarDoors { get; set; }
            public List<IMySensorBlock> HangarDoorSensors { get; set; }
            public List<IMyAirVent> OutsideVents { get; set; }
            public List<IMyGasTank> O2Tanks { get; set; }
            public string Status {
                get { return StatusBuilder.ToString(); }
                set {
                    StatusBuilder.Clear();
                    StatusBuilder.Append(value);
                }
            }
            private StringBuilder StatusBuilder = new StringBuilder();
            public int AirLimit { get; set; }

            public string HangarStatus { get; private set; }
            public float HangarPressure { get; private set; } = 0;
            public bool DepressurizeFlag { get; private set; }
            public bool PressurizeFlag { get; private set; }
            public float O2TanksFill { get; private set; } = 0;
            public int AirTanksCount { get; private set; } = 0;
            private MyResourceSinkComponent sink;
            private ListReader<MyDefinitionId> list;
            private bool hasO2;

            public Hangar(string zoneName, List<IMyInteriorLight> controlPressurize, List<IMyInteriorLight> controlDepressurize, List<IMyAirVent> vents,
                List<IMyInteriorLight> warningLights, List<IMySoundBlock> warningSounds, List<IMyDoor> hangarDoors,
                List<IMySensorBlock> bayDoorSensors, List<IMyAirVent> outsideVents, List<IMyGasTank> o2Tanks, int airLimit = 5) {
                ZoneName = zoneName;
                ControlPressurize = new List<IMyInteriorLight>(controlPressurize);
                ControlDepressurize = new List<IMyInteriorLight>(controlDepressurize);
                Vents = new List<IMyAirVent>(vents);
                WarningLights = new List<IMyInteriorLight>(warningLights);
                WarningSounds = new List<IMySoundBlock>(warningSounds);
                HangarDoors = new List<IMyDoor>(hangarDoors);
                HangarDoorSensors = new List<IMySensorBlock>(bayDoorSensors);
                OutsideVents = new List<IMyAirVent>(outsideVents);
                O2Tanks = new List<IMyGasTank>(o2Tanks);
                AirLimit = airLimit;
                Status = "";
            }

            public Hangar(string zoneName, List<IMyInteriorLight> controlPressurize, List<IMyInteriorLight> controlDepressurize, List<IMyAirVent> vents,
                List<IMyDoor> hangarDoors, List<IMySensorBlock> bayDoorSensors, List<IMySoundBlock> sounds, List<IMyGasTank> o2Tanks, int airLimit = 5)
                : this(zoneName, controlPressurize, controlDepressurize, vents, new List<IMyInteriorLight>(), sounds,
                 hangarDoors, bayDoorSensors, new List<IMyAirVent>(), o2Tanks, airLimit) { }

            public Hangar(string zoneName, List<IMyInteriorLight> controlPressurize, List<IMyInteriorLight> controlDepressurize, List<IMyAirVent> vents,
                List<IMyDoor> hangarDoors, List<IMySensorBlock> bayDoorSensors, List<IMyGasTank> o2Tanks, int airLimit = 5)
                : this(zoneName, controlPressurize, controlDepressurize, vents, new List<IMyInteriorLight>(), new List<IMySoundBlock>(),
                      hangarDoors, bayDoorSensors, new List<IMyAirVent>(), o2Tanks, airLimit) { }

            public Hangar(string zoneName, List<IMyInteriorLight> controlPressurize, List<IMyInteriorLight> controlDepressurize, List<IMyAirVent> vents,
                List<IMyDoor> hangarDoors, List<IMyAirVent> outsideVents, List<IMyGasTank> o2Tanks, int airLimit = 5)
                : this(zoneName, controlPressurize, controlDepressurize, vents, new List<IMyInteriorLight>(), new List<IMySoundBlock>(),
                      hangarDoors, new List<IMySensorBlock>(), outsideVents, o2Tanks, airLimit) { }

            public Hangar(string zoneName, List<IMyInteriorLight> controlPressurize, List<IMyInteriorLight> controlDepressurize, List<IMyAirVent> vents,
                List<IMyDoor> hangarDoors, List<IMyGasTank> o2Tanks, int airLimit = 5)
                : this(zoneName, controlPressurize, controlDepressurize, vents, hangarDoors, new List<IMySensorBlock>(), o2Tanks, airLimit) { }

            public bool Update() {
                if (Vents.Count == 0 || (ControlDepressurize.Count == 0 && ControlPressurize.Count == 0)) { return false; }
                PressurizeFlag = false;
                DepressurizeFlag = false;
                foreach (IMyInteriorLight de in ControlPressurize) {
                    if (!de.Enabled) {
                        PressurizeFlag = true;
                        break;
                    }
                }
                foreach (IMyInteriorLight de in ControlDepressurize) {
                    if (!de.Enabled) {
                        DepressurizeFlag = true;
                        break;
                    }
                }
                //RoomPressure handling
                HangarPressure = Vents[0].GetOxygenLevel() * 100;
                if (HangarPressure > AirLimit) {
                    foreach (IMyInteriorLight de in ControlPressurize) de.Color = Color.White;
                    foreach (IMyInteriorLight de in ControlDepressurize) de.Color = Color.White;
                } else {
                    foreach (IMyInteriorLight de in ControlPressurize) de.Color = Color.Red;
                    foreach (IMyInteriorLight de in ControlDepressurize) de.Color = Color.Red;
                }
                StatusBuilder.Clear();
                StatusBuilder.Append(ZoneName);
                StatusBuilder.Append($" O2 {HangarPressure}% {HangarStatus}");
                //hangar
                if (!DepressurizeFlag && !PressurizeFlag) return true;
                if (DepressurizeFlag && !PressurizeFlag) {
                    if (Vents[0].Depressurize) {
                        if (HangarPressure == 0 || GetAirTankFill(O2Tanks) == 100) {
                            foreach (IMySoundBlock de in WarningSounds) de.Stop();
                            foreach (IMySensorBlock de in HangarDoorSensors) de.Enabled = true;
                            HangarStatus = " Hangar doors ready to open";
                            foreach (IMyInteriorLight de in ControlDepressurize) de.Enabled = true;
                            return true;
                        } else return true;
                    } else if (OutsideVents.Count > 0 && OutsideVents[0].GetOxygenLevel() * 100 > AirLimit) {
                        foreach (IMySensorBlock de in HangarDoorSensors) de.Enabled = true;
                        foreach (IMyInteriorLight de in ControlDepressurize) de.Enabled = true;
                        HangarStatus = " Hangar doors ready for Earthside";
                        return true;
                    } else {
                        foreach (IMyDoor de in HangarDoors) de.CloseDoor();
                        foreach (IMySoundBlock de in WarningSounds) de.Play();
                        foreach (IMyInteriorLight de in WarningLights) de.Enabled = true;
                        foreach (IMyAirVent de in Vents) de.Depressurize = true;
                        HangarStatus = " Hangar bay depressurizing";
                        return true;
                    }
                } else if (!DepressurizeFlag && PressurizeFlag) {
                    if (Vents[0].Depressurize || (OutsideVents.Count > 0 && OutsideVents[0].GetOxygenLevel() * 100 > AirLimit && Vents[0].Depressurize)) {
                        foreach (IMyDoor de in HangarDoors) de.CloseDoor();
                        foreach (IMySensorBlock de in HangarDoorSensors) de.Enabled = false;
                        foreach (IMyAirVent de in Vents) de.Depressurize = false;
                        HangarStatus = " Hangar bay pressurizing";
                        return true;
                    } else if (!Vents[0].Depressurize) {
                        foreach (IMyInteriorLight de in WarningLights) de.Enabled = false;
                        foreach (IMyInteriorLight de in ControlPressurize) de.Enabled = true;
                        HangarStatus = " Hangar bay pressurized";
                        return true;
                    } else return true;
                }
                foreach (IMyDoor de in HangarDoors) de.CloseDoor();
                foreach (IMySensorBlock de in HangarDoorSensors) de.Enabled = false;
                HangarStatus = " Hangar bay in unknown status, resetting";
                foreach (IMyInteriorLight de in ControlDepressurize) de.Enabled = true;
                foreach (IMyInteriorLight de in ControlPressurize) de.Enabled = true;

                return true;
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
