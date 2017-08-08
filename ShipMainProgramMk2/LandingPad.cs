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
        public class LandingPad {

            public string ZoneName { get; set; }
            public List<IMyInteriorLight> Lights { get; set; }
            public List<IMyLandingGear> LandingLockPads { get; set; }
            public List<IMyShipConnector> Connectors { get; set; }
            public string Status {
                get { return StatusBuilder.ToString(); }
                set {
                    StatusBuilder.Clear();
                    StatusBuilder.Append(value);
                }
            }
            private StringBuilder StatusBuilder = new StringBuilder();
            public bool IsConnected { get; private set; }
            public bool IsConnectable { get; private set; }
            public bool IsLocked { get; private set; }
            public string PadStatus { get; private set; }

            public LandingPad(string zoneName, List<IMyLandingGear> landingGear, List<IMyInteriorLight> lights, List<IMyShipConnector> connectors) {
                ZoneName = zoneName;
                Lights = new List<IMyInteriorLight>(lights);
                LandingLockPads = new List<IMyLandingGear>(landingGear);
                Connectors = new List<IMyShipConnector>(connectors);
                Status = "";
            }

            public LandingPad(string zoneName, List<IMyLandingGear> landingGear, List<IMyInteriorLight> lights) :
                this(zoneName, landingGear, lights, new List<IMyShipConnector>()) { }

            public LandingPad(string zoneName, List<IMyLandingGear> landingGear, List<IMyShipConnector> connectors) :
                this(zoneName, landingGear, new List<IMyInteriorLight>(), connectors) { }

            public LandingPad(string zoneName, List<IMyShipConnector> connectors, List<IMyInteriorLight> lights) :
                this(zoneName, new List<IMyLandingGear>(), lights, connectors) { }

            public bool Update() {
                //if (LandingLockPads.Count == 0 && (Lights.Count == 0 || Connectors.Count == 0)) return false;
                IsConnected = false;
                IsConnectable = false;
                IsLocked = false;
                foreach (IMyShipConnector de in Connectors) {
                    if (de.Status == MyShipConnectorStatus.Connected) {
                        IsConnected = true;
                    }
                    if (de.Status == MyShipConnectorStatus.Connectable) {
                        IsConnectable = true;
                    }
                }
                if (IsConnected) {
                    foreach (IMyLandingGear de in LandingLockPads) {
                        //if (de.LockMode == LandingGearMode.ReadyToLock)
                        de.Lock();
                    }
                } else {
                    foreach (IMyLandingGear de in LandingLockPads) {
                        de.Unlock();
                    }
                }
                foreach (IMyLandingGear de in LandingLockPads) {
                    if (de.IsLocked) {
                        IsLocked = true;
                    }
                }
                if (IsLocked) {
                    foreach (IMyInteriorLight de in Lights) de.Color = Color.Green;
                } else if (IsConnectable) {
                    foreach (IMyInteriorLight de in Lights) de.Color = Color.Yellow;
                } else {
                    foreach (IMyInteriorLight de in Lights) de.Color = Color.White;
                }
                return true;
            }
        }
    }
}
