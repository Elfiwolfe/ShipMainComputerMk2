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
        public class Orbital {

            MyGridProgram _script;

            //Orbital
            public OrbitalOperation OrbitalMode { get; set; } = OrbitalOperation.Off;
            public bool GravDriveOn { get; set; } = true;
            public string EmergencyThrusterName { get; set; } = "[EmergencyThrust]";
            public double GravityEmergency { get; set; } = 0.3;//Where to turn on emergency thrust
            public double GravityTarget { get; set; } = 0.20;//target orbit
            public double AltitudeTarget { get; set; } = -1000;//target sealevel
            public double HoverTarget { get; set; } = 20;//
            public double VelocityLimit { get; set; } = 90;//m/s
            public double HeightOffset { get; set; } = 100;//height adjuster for varrious ships. generally height of shipController
            public double AltitudeBuffer { get; set; } = 5;//buffer above and below Target
            public string Status {
                get { return StatusBuilder.ToString(); }
                set {
                    StatusBuilder.Clear();
                    StatusBuilder.Append(value);
                }
            }
            private StringBuilder StatusBuilder = new StringBuilder();
            public bool OrbitalListNeedsBuilding { get; set; } = true;

            //orbital helper
            public enum OrbitalOperation { Off, GravityMode, AltitudeMode, HoverMode }
            double normalGravity = 0;
            List<IMyThrust> thrusters = new List<IMyThrust>();
            List<IMyThrust> emergencyThrusters = new List<IMyThrust>();
            //List<IMyGravityGenerator> gravDrive = new List<IMyGravityGenerator>();//Do I need this?
            List<IMyGravityGenerator> gravDrivePos = new List<IMyGravityGenerator>();
            List<IMyGravityGenerator> gravDriveNeg = new List<IMyGravityGenerator>();
            List<IMyArtificialMassBlock> vMass = new List<IMyArtificialMassBlock>();
            List<IMyShipController> shipControllers = new List<IMyShipController>();
            List<IMyCameraBlock> downCameras = new List<IMyCameraBlock>();
            //bool orbitalListBuilt = false;
            Vector3D gravityVector = new Vector3D();
            Vector3D shipVector = new Vector3D();
            Vector3D shipToGravityVector = new Vector3D();
            int massOfShip = 0;
            double velocityTowardGravity = 0;
            float normalThrust = 0;
            float emergencyThrust = 0;
            float gravThrust = 0;
            float thrustNeeded = 0;
            float requestedGravity = 0;
            double seaLevelAltitude = 0;
            double surfaceAltitude = 0;
            double lidarAltitude = 0;
            double landingAltitude = 0;
            double tempDouble = 0;
            double targetVelocity = 0;
            double thrustApplied = 0;
            double effectiveGravDriveThrust = 0;
            double maxEffectiveThrust = 0;
            double availableThrust = 0;
            double tempAdjustmentGravity = 0;
            double requestedThrust = 0;
            float normalThrustPerThruster = 0;

            public Orbital(MyGridProgram script) {
                _script = script;
                Status = "";
            }

            public Orbital(MyGridProgram script, OrbitalOperation oMode) : this(script) {
                OrbitalMode = oMode;
            }
            /// <summary>
            /// Gravity Orbit default
            /// </summary>
            /// <param name="script"></param>
            /// <param name="gravityEmergency"></param>
            /// <param name="emergencyThrusterName"></param>
            /// <param name="gravityTarget"></param>
            /// <param name="velocityLimit"></param>
            public Orbital(MyGridProgram script, double gravityEmergency, string emergencyThrusterName, double gravityTarget,
                double velocityLimit) : this(script) {
                OrbitalMode = OrbitalOperation.GravityMode;
                GravityEmergency = gravityEmergency;
                EmergencyThrusterName = emergencyThrusterName;
                GravityTarget = gravityTarget;
                VelocityLimit = velocityLimit;
            }
 
            public void BuildOrbitList() {
                thrusters.Clear();
                emergencyThrusters.Clear();
                //gravDrive.Clear();
                gravDrivePos.Clear();
                gravDriveNeg.Clear();
                vMass.Clear();
                shipControllers.Clear();
                downCameras.Clear();
                _script.GridTerminalSystem.GetBlocksOfType(shipControllers, b => b.CubeGrid == _script.Me.CubeGrid);
                if (shipControllers.Count > 0) {
                    gravityVector = shipControllers[0].GetNaturalGravity();
                    normalGravity = gravityVector.Length() / 9.81;
                }
                if (shipControllers.Count == 0 || normalGravity == 0) {
                    normalGravity = 0;
                    //orbitalListBuilt = false;
                    return;
                }
                _script.GridTerminalSystem.GetBlocksOfType<IMyThrust>(null, b => {
                    if (b.CubeGrid == _script.Me.CubeGrid) {
                        switch (b.WorldMatrix.GetClosestDirection(gravityVector)) {
                            case Base6Directions.Direction.Forward:
                                if (b.CustomName.Contains(EmergencyThrusterName)) { emergencyThrusters.Add(b); } else { thrusters.Add(b); }
                                break;
                        }
                    }
                    return false;
                });
                _script.GridTerminalSystem.GetBlocksOfType(vMass, b => b.CubeGrid == _script.Me.CubeGrid);
                _script.GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator>(null, b => {
                    if (b.CubeGrid == _script.Me.CubeGrid) {
                        foreach (IMyVirtualMass de in vMass) {
                            if (b.IsPositionInRange(de.GetPosition())) {
                                switch (b.WorldMatrix.GetClosestDirection(gravityVector)) {
                                    case Base6Directions.Direction.Up:
                                        gravDrivePos.Add(b);
                                        break;
                                    case Base6Directions.Direction.Down:
                                        gravDriveNeg.Add(b);
                                        break;
                                }
                            }
                        }
                    }
                    return false;
                });
                _script.GridTerminalSystem.GetBlocksOfType(downCameras, b => {
                    if (b.CubeGrid == _script.Me.CubeGrid) {
                        switch (b.WorldMatrix.GetClosestDirection(gravityVector)) {
                            case Base6Directions.Direction.Forward:
                                return true;
                        }
                    }
                    return false;
                });
                OrbitalListNeedsBuilding = false;
            }

            public void CurrentAltitude() {
                OrbitalMode = OrbitalOperation.AltitudeMode;
                AltitudeTarget = FindAltitude();
            }

            public void CurrentGravity() {
                OrbitalMode = OrbitalOperation.GravityMode;
                GravityTarget = FindGravity();
            }

            public void CurrentHover() {
                if (shipControllers.Count < 1) return;
                OrbitalMode = OrbitalOperation.HoverMode;
                tempDouble = 0;
                surfaceAltitude = 0;
                lidarAltitude = 0;
                seaLevelAltitude = 0;
                foreach (IMyShipController de in shipControllers) {
                    de.TryGetPlanetElevation(MyPlanetElevation.Surface, out tempDouble);
                    if (tempDouble > surfaceAltitude) surfaceAltitude = tempDouble;
                }
                gravityVector = shipControllers[0].GetNaturalGravity();
                lidarAltitude = LidarRange(downCameras, 0, 0);
                if (lidarAltitude < surfaceAltitude) HoverTarget = lidarAltitude;
                else HoverTarget = surfaceAltitude;
            }

            string DisplayLargeNumber(float number) {
                string powerValue = " kMGTPEZY";
                float result = number;
                int ordinal = 0;
                while (ordinal < powerValue.Length && result >= 1000) {
                    result /= 1000;
                    ordinal++;
                }
                string resultString = Math.Round(result, 1, MidpointRounding.AwayFromZero).ToString();
                if (ordinal > 0) {
                    resultString += " " + powerValue[ordinal];
                }
                return resultString;
            }

            public void GoLand(double target = -1000) {
                OrbitalMode = OrbitalOperation.AltitudeMode;
                AltitudeTarget = target;
            }

            public void GoDeepSpace(double target = 0.01) {
                OrbitalMode = OrbitalOperation.GravityMode;
                GravityTarget = target;
            }

            public void GoHover(double target = 100) {
                OrbitalMode = OrbitalOperation.HoverMode;
                HoverTarget = target;
            }

            double FindAltitude() {
                if (shipControllers.Count < 1) { return 0; }
                seaLevelAltitude = 0;
                shipControllers[0].TryGetPlanetElevation(MyPlanetElevation.Sealevel, out seaLevelAltitude);
                return seaLevelAltitude;
            }
            double FindGravity() {
                if (shipControllers.Count < 1) { return 0; }
                normalGravity = shipControllers[0].GetNaturalGravity().Length() / 9.81;
                return normalGravity;
            }

            double LidarRange(List<IMyCameraBlock> cameras, float pitch, float yaw) {
                double range = 99999;
                double range2 = 99999;
                MyDetectedEntityInfo info;
                foreach (IMyCameraBlock de in cameras) {
                    if (de.EnableRaycast) {
                        info = de.Raycast(de.AvailableScanRange, pitch, yaw);
                        if (info.HitPosition.HasValue) {
                            range2 = Vector3D.Distance(de.GetPosition(), info.HitPosition.Value);
                        }
                        if (range2 < range) range = range2;
                    }
                }
                return range;
            }

            public void Rebuild() {
                foreach (IMyThrust de in thrusters) de?.SetValueFloat("Override", 0);
                foreach (IMyThrust de in emergencyThrusters) de?.SetValueFloat("Override", 0);
                foreach (IMyGravityGenerator de in gravDrivePos) de.GravityAcceleration = 0;//this way or
                foreach (IMyGravityGenerator de in gravDriveNeg) de?.SetValueFloat("Gravity", 0);//this way
                BuildOrbitList();
            }

            public void TurnOffOrbital() {
                foreach (IMyThrust de in thrusters) de?.SetValueFloat("Override", 0);
                foreach (IMyThrust de in emergencyThrusters) de?.SetValueFloat("Override", 0);
                foreach (IMyGravityGenerator de in gravDrivePos) de.GravityAcceleration = 0;//this way or
                foreach (IMyGravityGenerator de in gravDriveNeg) de?.SetValueFloat("Gravity", 0);//this way
                OrbitalMode = OrbitalOperation.Off;
            }

            public string Update() {
                massOfShip = 0;
                velocityTowardGravity = 0;
                emergencyThrust = 0;
                normalThrust = 0;
                gravThrust = 0;
                seaLevelAltitude = 0;
                surfaceAltitude = 0;
                lidarAltitude = 0;
                landingAltitude = 0;
                StatusBuilder.Clear();
                StatusBuilder.Append($"\n OrbitalMode:  {OrbitalMode}");
                if (OrbitalMode == OrbitalOperation.Off) return StatusBuilder.ToString();
                if (shipControllers.Any(de => de != de.CubeGrid.GetCubeBlock(de.Position)?.FatBlock)
                    || thrusters.Any(de => de != de.CubeGrid.GetCubeBlock(de.Position)?.FatBlock)
                    || emergencyThrusters.Any(de => de != de.CubeGrid.GetCubeBlock(de.Position)?.FatBlock)
                    || gravDriveNeg.Any(de => de != de.CubeGrid.GetCubeBlock(de.Position)?.FatBlock)
                    || gravDrivePos.Any(de => de != de.CubeGrid.GetCubeBlock(de.Position)?.FatBlock)
                    || vMass.Any(de => de != de.CubeGrid.GetCubeBlock(de.Position)?.FatBlock)
                    || downCameras.Any(de => de != de.CubeGrid.GetCubeBlock(de.Position)?.FatBlock))
                    OrbitalListNeedsBuilding = true;
                if (!OrbitalListNeedsBuilding && shipControllers.Count > 0) {
                    gravityVector = shipControllers[0].GetNaturalGravity();
                    normalGravity = gravityVector.Length() / 9.81;
                    if (thrusters.Count > 0 && thrusters[0].WorldMatrix.GetClosestDirection(gravityVector) == Base6Directions.Direction.Forward) {
                        OrbitalListNeedsBuilding = true;
                    }
                }
                if (shipControllers.Count == 0 || OrbitalListNeedsBuilding) {
                    foreach (IMyThrust de in thrusters) de?.SetValueFloat("Override", 0);
                    foreach (IMyThrust de in emergencyThrusters) de?.SetValueFloat("Override", 0);
                    foreach (IMyGravityGenerator de in gravDrivePos) de.GravityAcceleration = 0;
                    foreach (IMyGravityGenerator de in gravDriveNeg) de.GravityAcceleration = 0;
                    BuildOrbitList();
                }
                if (shipControllers.Count == 0) {
                    //cannot go on.
                    StatusBuilder.Append("\nOrbital: Error no shipController");
                    return StatusBuilder.ToString();
                }
                massOfShip = shipControllers[0].CalculateShipMass().PhysicalMass;
                shipVector = shipControllers[0].GetShipVelocities().LinearVelocity;
                shipToGravityVector = VectorProjection(shipVector, gravityVector);
                velocityTowardGravity = ((int)(-1 * Math.Sign(gravityVector.Dot(shipVector)))) * shipToGravityVector.Length();
                StatusBuilder.Append($"\n Mass : {DisplayLargeNumber(massOfShip)} kg");
                StatusBuilder.Append($"\n Rate of Change: {velocityTowardGravity.ToString("0.00")} m/s");
                shipControllers[0].TryGetPlanetElevation(MyPlanetElevation.Sealevel, out seaLevelAltitude);
                foreach (IMyShipController de in shipControllers) {
                    de.TryGetPlanetElevation(MyPlanetElevation.Surface, out tempDouble);
                    if (tempDouble > surfaceAltitude) surfaceAltitude = tempDouble;
                }
                //thrusters
                foreach (IMyThrust de in thrusters) {
                    if (de.IsWorking) normalThrust += de.MaxEffectiveThrust;
                }
                if (normalThrust == 0) normalThrust = 1;
                foreach (IMyThrust de in emergencyThrusters) emergencyThrust += de.MaxEffectiveThrust;
                if (GravDriveOn) gravThrust = 490500 * (gravDriveNeg.Count + gravDrivePos.Count);
                thrustNeeded = (float)(massOfShip * normalGravity * 9.81);
                StatusBuilder.Append($"\n Thrust needed : {DisplayLargeNumber(thrustNeeded)} N");
                //Decision point
                if (normalGravity == 0) {
                    foreach (IMyThrust de in thrusters) de.SetValueFloat("Override", 0);
                    foreach (IMyThrust de in emergencyThrusters) {
                        de.SetValueFloat("Override", 0);
                        de.Enabled = false;
                    }
                    foreach (IMyGravityGenerator de in gravDriveNeg) de.GravityAcceleration = 0;
                    foreach (IMyGravityGenerator de in gravDrivePos) de.GravityAcceleration = 0;
                    StatusBuilder.Append($"\n Not in gravity well");
                    return StatusBuilder.ToString();
                }
                effectiveGravDriveThrust = gravThrust * MathHelper.Clamp(1 - 2 * normalGravity, 0f, 1f);
                maxEffectiveThrust = effectiveGravDriveThrust + normalThrust;
                tempAdjustmentGravity = MathHelper.Clamp(normalGravity * 9.31, 0, VelocityLimit / 2);
                availableThrust = Math.Max(maxEffectiveThrust - thrustNeeded, 0);
                targetVelocity = 0;
                if (OrbitalMode == OrbitalOperation.AltitudeMode || OrbitalMode == OrbitalOperation.HoverMode) {
                    foreach (IMyCameraBlock de in downCameras) de.EnableRaycast = true;
                    lidarAltitude = LidarRange(downCameras, 0, 0);
                    if (lidarAltitude < surfaceAltitude) landingAltitude = lidarAltitude;
                    else landingAltitude = surfaceAltitude;
                    if (OrbitalMode == OrbitalOperation.HoverMode) tempDouble = HoverTarget + seaLevelAltitude - landingAltitude;
                    else tempDouble = AltitudeTarget;
                    if (seaLevelAltitude < tempDouble - AltitudeBuffer) {
                        targetVelocity = MathHelper.Clamp(Math.Sqrt(tempAdjustmentGravity * Math.Abs(tempDouble - seaLevelAltitude - velocityTowardGravity)), 0, VelocityLimit);
                    } else if (seaLevelAltitude > tempDouble + AltitudeBuffer) {
                        if (landingAltitude < (HeightOffset + 100 + velocityTowardGravity * velocityTowardGravity / tempAdjustmentGravity)) {
                            //landing
                            targetVelocity = -Math.Max(Math.Sqrt(Math.Abs(MathHelper.Clamp((landingAltitude - HeightOffset - 12.5), 0d, 100) * tempAdjustmentGravity)), 1d);
                        } else {
                            targetVelocity = -MathHelper.Clamp(Math.Sqrt(0.7 * tempAdjustmentGravity * Math.Abs(seaLevelAltitude - tempDouble + velocityTowardGravity)), 0, VelocityLimit);
                        }
                    }
                }
                //could just do else here
                if (OrbitalMode == OrbitalOperation.GravityMode) {
                    if (Math.Round(normalGravity, 2) > Math.Round(GravityTarget, 2)) {
                        if (VelocityLimit > tempAdjustmentGravity * 10) targetVelocity = 10 * tempAdjustmentGravity;
                        else targetVelocity = VelocityLimit;
                    } else if (Math.Round(normalGravity, 2) < Math.Round(GravityTarget, 2)) targetVelocity = -VelocityLimit;
                }
                StatusBuilder.Append($"\n request velocity {targetVelocity.ToString("0.00")} m/s");
                requestedThrust = (targetVelocity - velocityTowardGravity) * massOfShip + thrustNeeded;
                //gravDrive
                if (GravDriveOn) {
                    thrustApplied = Math.Sign(requestedThrust) * Math.Min(Math.Abs(requestedThrust), effectiveGravDriveThrust);
                    requestedThrust -= Math.Min(Math.Abs(requestedThrust), effectiveGravDriveThrust);
                }
                requestedGravity = (float)((thrustApplied / (MathHelper.Clamp(1 - 2 * normalGravity, 0f, 1f)) / 50000) / (gravDrivePos.Count + gravDriveNeg.Count));
                if (requestedGravity < 0 || float.IsNaN(requestedGravity)) requestedGravity = 0;
                foreach (IMyGravityGenerator de in gravDrivePos) de.GravityAcceleration = requestedGravity;
                foreach (IMyGravityGenerator de in gravDriveNeg) de.GravityAcceleration = -requestedGravity;
                //Normal Drive
                if (thrusters.Count > 0 && requestedThrust > 0) {
                    thrustApplied = Math.Min(requestedThrust, normalThrust);
                    requestedThrust -= thrustApplied;
                }
                normalThrustPerThruster = (float)(100 * thrustApplied / normalThrust);
                if (normalThrustPerThruster < 1.0001) { normalThrustPerThruster = 1.0001f; }
                foreach (IMyThrust de in thrusters) de.SetValueFloat("Override", normalThrustPerThruster);
                if (OrbitalMode == OrbitalOperation.GravityMode && normalGravity > GravityEmergency) {
                    // Emergency mode
                    StatusBuilder.Append("\n Emergency thrust engaged");
                    foreach (IMyThrust de in emergencyThrusters) {
                        de.Enabled = true;
                        de.SetValueFloat("Override", 100);
                    }
                } else if (OrbitalMode == OrbitalOperation.GravityMode && normalGravity < GravityEmergency) {
                    foreach (IMyThrust de in emergencyThrusters) {
                        de.Enabled = false;
                        de.SetValueFloat("Override", 0);
                    }
                }
                if (OrbitalMode == OrbitalOperation.AltitudeMode || OrbitalMode == OrbitalOperation.HoverMode) {
                    StatusBuilder.Append($"\n Altitude = {seaLevelAltitude.ToString("0")} m.");
                    if (OrbitalMode == OrbitalOperation.AltitudeMode) StatusBuilder.Append($"  Target = {AltitudeTarget.ToString("0")} m.");
                    else StatusBuilder.Append($"  Hover at = {HoverTarget.ToString("0")} m.");
                    StatusBuilder.Append($"\n Ground level = {surfaceAltitude.ToString("0")} m.");
                    StatusBuilder.Append($"\n Lidar Altitude = {lidarAltitude.ToString("0")} m.");
                }
                if (OrbitalMode == OrbitalOperation.GravityMode) {
                    StatusBuilder.Append($"\n Current Orbit = {normalGravity.ToString("0.000")} g.");
                    StatusBuilder.Append($"\n  Target Orbit = {GravityTarget.ToString("0.000")} g.");
                }
                StatusBuilder.Append($"\n Lift: Normal: {DisplayLargeNumber((float)(normalThrust / (normalGravity * 9.81)))} kg.");
                StatusBuilder.Append($"\n    GravDrive: {DisplayLargeNumber((float)(effectiveGravDriveThrust / (normalGravity * 9.81)))} kg.");
                StatusBuilder.Append($"\n    Emergency: {DisplayLargeNumber((float)(emergencyThrust / (normalGravity * 9.81)))} kg.");
                return StatusBuilder.ToString();
            }

            Vector3D VectorProjection(Vector3D a, Vector3D b) {//project a onto b 
                Vector3D projection = a.Dot(b) / b.LengthSquared() * b;
                return projection;
            }
        }
    }
}
