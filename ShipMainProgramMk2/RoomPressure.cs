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
        public class RoomPressure {

            public string ZoneName { get; set; }
            public List<IMyInteriorLight> Lights { get; set; }
            public List<IMyDoor> Doors { get; set; }
            public List<IMyAirVent> Vents { get; set; }
            public string Status {
                get { return StatusBuilder.ToString(); }
                set {
                    StatusBuilder.Clear();
                    StatusBuilder.Append(value);
                }
            }
            private StringBuilder StatusBuilder = new StringBuilder();
            public int AirLimit { get; set; }
            public float RoomVentPressure { get; private set; } = 0;

            public RoomPressure(string zoneName, List<IMyAirVent> vents, List<IMyInteriorLight> lights, List<IMyDoor> doors, int airLimit = 5) {
                ZoneName = zoneName;
                Vents = new List<IMyAirVent>(vents);
                Lights = new List<IMyInteriorLight>(lights);
                Doors = new List<IMyDoor>(doors);
                Status = "";
                AirLimit = airLimit;
            }
            public RoomPressure(string zoneName, List<IMyAirVent> vents, int airLimit = 5)
                : this(zoneName, vents, new List<IMyInteriorLight>(), new List<IMyDoor>(), airLimit) { }

            public RoomPressure(string zoneName, List<IMyAirVent> vents, List<IMyInteriorLight> lights, int airLimit = 5)
                : this(zoneName, vents, lights, new List<IMyDoor>(), airLimit) { }
            
            public RoomPressure(string zoneName, List<IMyAirVent> vents, List<IMyDoor> doors, int airlimit = 5)
                : this(zoneName, vents, new List<IMyInteriorLight>(), doors, airlimit) { }

            public bool Update(bool closeDoors = true) {
                if (Vents.Count == 0) return false;
                StatusBuilder.Clear();
                StatusBuilder.Append(ZoneName);
                RoomVentPressure = Vents[0].GetOxygenLevel() * 100;
                StatusBuilder.Append($" O2 {RoomVentPressure}%");
                if (!Vents[0].CanPressurize) StatusBuilder.Append(" Open to outside");
                if (RoomVentPressure > AirLimit) {
                    foreach (IMyInteriorLight de in Lights) de.Color = Color.White;
                } else {
                    foreach (IMyInteriorLight de in Lights) de.Color = Color.Red;
                    if (closeDoors && !Vents[0].Depressurize) {
                        foreach (IMyDoor de in Doors) de.CloseDoor();
                    }
                }

                return true;
            }

        }
    }
}
