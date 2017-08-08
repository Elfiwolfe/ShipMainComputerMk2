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
        #region Autogyro  
        // http://forums.keenswh.com/threads/aligning-ship-to-planet-gravity.7373513/#post-1286885461  

        double CTRL_COEFF = 0.5;
        int LIMIT_GYROS = 3; // max number of gyros to use to align craft. Leaving some available allows for player control to continue during auto-align  
        IMyShipController rc;
        List<IMyGyro> gyros = new List<IMyGyro>();
        float minAngleRad = 0.01f; // how tight to maintain horizontal Lower is tighter.
        StringBuilder autoLevelStatus = new StringBuilder();

        string GyroMain(string argument) {
            autoLevelStatus.Clear();
            //autoLevelStatus.Append("Auto level is turned on");
            if (rc == null) { gyrosetup(); }
            if (rc == null) {
                autoLevelStatus.Append("Auto-Level:No Cockpit or Remote Control.");
                return autoLevelStatus.ToString() ;
            }
            Matrix or;
            rc.Orientation.GetMatrix(out or);
            Vector3D down;
            if (argument.ToLower().Contains("rocket")) { down = or.Backward; } else { down = or.Down; }
            Vector3D grav = rc.GetNaturalGravity();
            grav.Normalize();
            autoLevelStatus.Append("Auto-Level:");
            for (int i = 0; i < gyros.Count; ++i) {
                var g = gyros[i];
                g.Orientation.GetMatrix(out or);
                var localDown = Vector3D.Transform(down, MatrixD.Transpose(or));
                var localGrav = Vector3D.Transform(grav, MatrixD.Transpose(g.WorldMatrix.GetOrientation()));
                //Since the gyro ui lies, we are not trying to control yaw,pitch,roll but rather we  
                //need a rotation vector (axis around which to rotate)  
                var rot = Vector3D.Cross(localDown, localGrav);
                double ang = rot.Length();
                ang = Math.Atan2(ang, Math.Sqrt(Math.Max(0.0, 1.0 - ang * ang)));
                if (double.IsNaN(ang)) { // not in gravity
                    g.GyroOverride = false;
                    autoLevelStatus.Append("Auto-Level:Not in gravity well");
                    return autoLevelStatus.ToString() ;
                }
                if (ang < minAngleRad) { // close enough  
                    g.GyroOverride = false;
                    autoLevelStatus.Append($" on level.");
                    continue;
                }
                autoLevelStatus.Append($" Off level: {(MathHelper.ToDegrees(ang)).ToString("0.000")} deg.");
                double ctrl_vel = g.GetMaximum<float>("Yaw") * (ang / Math.PI) * CTRL_COEFF;
                ctrl_vel = Math.Min(g.GetMaximum<float>("Yaw"), ctrl_vel);
                ctrl_vel = Math.Max(0.01, ctrl_vel);
                rot.Normalize();
                rot *= ctrl_vel;
                g.SetValueFloat("Pitch", (float)rot.GetDim(0));
                g.SetValueFloat("Yaw", -(float)rot.GetDim(1));
                g.SetValueFloat("Roll", -(float)rot.GetDim(2));
                g.SetValueFloat("Power", 1.0f);
                g.SetValueBool("Override", true);
            }
            return autoLevelStatus.ToString();
        }

        string gyrosetup() {
            var l = new List<IMyTerminalBlock>();
            if (rc == null) {
                GridTerminalSystem.GetBlocksOfType<IMyShipController>(l, (x => { return x.CubeGrid == Me.CubeGrid; }));
                if (l.Count < 1) return "No RC!";
                rc = (IMyShipController)l[0];
            }
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(l, (x => { return x.CubeGrid == Me.CubeGrid; }));
            if (gyros.Count > 0) gyros.Clear();
            foreach (var tb in l) {
                bool add = true;
                foreach (var name in reservedNamesStatic) {
                    if (tb.CustomName.Contains(name) && !(name == "")) { add = false; }
                }
                if (add) { gyros.Add((IMyGyro)tb); }
            }
            //	gyros = l.ConvertAll({ x =>  (IMyGyro)x; });  
            if (gyros.Count > LIMIT_GYROS) { gyros.RemoveRange(LIMIT_GYROS, gyros.Count - LIMIT_GYROS); }
            return "G" + gyros.Count.ToString("00");
        }
        void gyrosOff() {
            for (int i = 0; i < gyros.Count; ++i) { gyros[i].GyroOverride = false; }
        }
        #endregion
    }
}
