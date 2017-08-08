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
        public class SolarArray {
            const int updateConstant = 5;
            //const Single rpmToRadian = 0.10467f;
            public IMyMotorStator Rotor { get; set; }
            public List<IMySolarPanel> Panels { get; set; }
            public float SpeedSetting { get; set; }
            public int TimeCount { get; set; }
            /// <summary>
            /// Power output in MW
            /// </summary>
            public float Power { get; set; }
            public float PowerOld { get; set; }
            public float Direction { get; set; }

            public SolarArray(IMyMotorStator rotor, List<IMySolarPanel> panels, float speedSetting = 0.04f) {
                Rotor = rotor;
                Panels = new List<IMySolarPanel>(panels);
                SpeedSetting = speedSetting;
                TimeCount = 0;
                Power = 0f;
                PowerOld = 0f;
                Direction = 1f;
            }

            public bool Update() {
                if (TimeCount++ < updateConstant) return true;
                TimeCount = 0;
                if (Rotor != Rotor.CubeGrid.GetCubeBlock(Rotor.Position)?.FatBlock) return false;
                PowerOld = Power;
                Power = 0f;
                foreach (IMySolarPanel v in Panels) {
                    if (v != v.CubeGrid.GetCubeBlock(v.Position)?.FatBlock) return false;
                    Power += (v?.MaxOutput).GetValueOrDefault();
                }
                if (Power == PowerOld) {
                    if (!(Rotor.TargetVelocity == 0)) {
                        Direction = (Rotor.TargetVelocity < 0 ? -1 : 1);
                        Rotor.TargetVelocity = 0f;
                    }
                } else if (!(Rotor.TargetVelocity == 0) && Power < PowerOld) {
                    Rotor.TargetVelocity = - Rotor.TargetVelocity * MathHelper.RPMToRadiansPerSecond;
                } else { if (! (Power < PowerOld))Rotor.TargetVelocity = Direction * SpeedSetting * MathHelper.RPMToRadiansPerSecond; }
                return true;
            }

        }
    }
}
