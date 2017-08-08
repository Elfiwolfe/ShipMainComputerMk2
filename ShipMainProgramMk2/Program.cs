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
    partial class Program : MyGridProgram {
        // O.I.S. Ship Main Computer Mk 2
        //
        //

        //Run options
        bool stopShipIfNotPiloted = true;
        bool runAutoLevel = false;//runs auto leveling in natural Gravity
        bool rocketGyro = false;//false if RC bottom towards gravity, true if RC back toward gravity
        /*
        Orbital.OrbitalOperation orbitalMode = Orbital.OrbitalOperation.Off;
        bool gravDriveOn = true;
        */

        //Customized Options
        string shipStatusLCDName = "[ShipStatus]";
        string orbitalStatusLCDName = "[ShipStatus]";
        string timerName = "Timer Block MainComputer";//timer to start
        int o2TankLow = 40;
        int o2TankHigh = 60;
        int h2TankLow = 90;
        int h2TankHigh = 100;
        bool rebuildneeded = false;
        int rebuildCounter = 0;
        int rebuildCounterLimit = 10;
        float solarAlign = 0.04f;
        string sunChaserName = "[SunChaser]";
        bool runSunChaser = false;

        //air system
        string controlLightName = "Control";//airlock control light
        string outsideDoorName = "Outer";//default outside airlock door
        string insideDoorName = "Inner";//default inside airlock door
        string outsideVentName = "outsideVent";//vents outside ship
        int airPressureWarning = 5;//O2 %
        //Hangar
        string hangarPressurizeName = "Pressurize";// hangar control light
        string hangarDepressurizeName = "Depressurize";// hangar control light
        /*
        //Orbital
        string emergencyThrusterName = "[EmergencyThrust]";
        double gravityEmergency = 0.3;//Where to turn on emergency thrust
        double gravityTarget = 0.20;//target orbit
        double altitudeTarget = -1000;//target sealevel
        double hoverTarget = 20;//
        double velocityLimit = 90;//m/s
        double heightOffset = 100;//height adjuster for varrious ships. generally height of shipController
        double altitudeBuffer = 5;//buffer above and below Target
        */

        //helpers
        List<string> reservedNamesStatic = new List<string> { "[LCD]", "[ShipStatus]", "[EmergencyThrust]", "[SunChaser]",
            "[GravDrive]", "[OrbitThruster]", "[GPSCommo]", "[Messages]", "[EnemyGPS]",  "" };//Names that are [name] but not airzones 
        List<Airlock> airlocks = new List<Airlock>();
        List<RoomPressure> roomPressures = new List<RoomPressure>();
        List<Hangar> hangars = new List<Hangar>();
        List<LandingPad> landingPads = new List<LandingPad>();
        List<IMyAirVent> outsideVents = new List<IMyAirVent>();
        List<IMyGasTank> gasTanks = new List<IMyGasTank>();
        List<IMyGasGenerator> gasGenerators = new List<IMyGasGenerator>();
        List<IMyOxygenFarm> o2Farms = new List<IMyOxygenFarm>();
        OutgoingMessage messageOut = new OutgoingMessage();
        List<IMyTimerBlock> timers = new List<IMyTimerBlock>();
        List<SolarArray> solarArrays = new List<SolarArray>();
        List<IMyRadioAntenna> sendingRadio = new List<IMyRadioAntenna>();
        List<IMyRadioAntenna> outRadios = new List<IMyRadioAntenna>();
        WriteLCD shipStatusLCD;
        WriteLCD orbitalStatusLCD;
        Orbital orbitalComputer;
        int statusTime = 0;

        //Method helpers
        string tempName = "";
        string tempString = "";
        bool controlExists = false;
        bool hangerPressurizeExists = false;
        bool hangerDepressurizeExists = false;
        bool insideDoorExist = false;
        bool outsideDoorExist = false;
        List<string> tempZoneNames = new List<string>();
        List<IMyInteriorLight> lights = new List<IMyInteriorLight>();
        List<IMyInteriorLight> lightsA = new List<IMyInteriorLight>();
        List<IMyInteriorLight> lightsB = new List<IMyInteriorLight>();
        List<IMyAirVent> vents = new List<IMyAirVent>();
        List<IMyAirVent> ventsA = new List<IMyAirVent>();
        List<IMyAirVent> ventsB = new List<IMyAirVent>();
        List<IMyDoor> doors = new List<IMyDoor>();
        List<IMyDoor> doorsA = new List<IMyDoor>();
        List<IMyDoor> doorsB = new List<IMyDoor>();
        List<IMyShipConnector> connectors = new List<IMyShipConnector>();
        List<IMyLandingGear> landingGears = new List<IMyLandingGear>();
        List<IMyGasTank> localGasTanks = new List<IMyGasTank>();
        List<IMySoundBlock> sounds = new List<IMySoundBlock>();
        List<IMySensorBlock> sensors = new List<IMySensorBlock>();
        float o2TanksFill = 0f;
        float h2TanksFill = 0f;
        int o2TanksCount = 0;
        int h2TanksCount = 0;
        MyResourceSinkComponent sink;
        MyResourceSourceComponent source;
        ListReader<MyDefinitionId> list;
        bool hasO2;
        string[] argMessages = new string[10];
        string[] pieces = new string[2];
        int tempInt = 0;
        double tempDouble = 0;
        bool test = false;
        float currentPower = 0f;
        float maxPower = 0f;
        float percentPower = 0f;
        List<IMyMotorStator> rotors = new List<IMyMotorStator>();
        List<IMySolarPanel> panels = new List<IMySolarPanel>();
        List<IMySolarPanel> sunChaserPanels = new List<IMySolarPanel>();
        List<IMyGyro> sunChaserGyro = new List<IMyGyro>();
        List<IMyThrust> sunChaserForeThrust = new List<IMyThrust>();
        List<IMyThrust> sunChaserAftThrust = new List<IMyThrust>();
        bool sunMoveStarted = false;
        bool sunTurnStarted = false;

        public Program() {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            orbitalComputer = new Orbital(this);
            ArgumentParser(Me.CustomData);
            if (Storage.Length > 0) {
                ArgumentParser(Storage);
            }
            shipStatusLCD = new WriteLCD(this, shipStatusLCDName);
            orbitalStatusLCD = new WriteLCD(this, orbitalStatusLCDName);
            GridTerminalSystem.GetBlocksOfType(outRadios, b => b.CubeGrid == Me.CubeGrid);
            ZoneConstruction();
            StartTimer(timers);
        }

        public void Save() {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
            StringBuilder writeToStorage = new StringBuilder();
            if (orbitalComputer.OrbitalMode == Orbital.OrbitalOperation.Off) writeToStorage.Append("orbitaloff");
            if (orbitalComputer.OrbitalMode == Orbital.OrbitalOperation.AltitudeMode) {
                writeToStorage.Append($"altitude\ntargetaltitude {orbitalComputer.AltitudeTarget}\n");
            }
            if (orbitalComputer.OrbitalMode == Orbital.OrbitalOperation.GravityMode) {
                writeToStorage.Append($"gravity\ntargetgravity {orbitalComputer.GravityTarget}\n");
            }
            if (orbitalComputer.OrbitalMode == Orbital.OrbitalOperation.HoverMode) {
                writeToStorage.Append($"hover\ntargethover {orbitalComputer.HoverTarget}\n");
            }
            Storage = writeToStorage.ToString();
        }

        public void Main(string argument = "") {
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked.
            // 
            // The method itself is required, but the argument above
            // can be removed if not needed.
            Echo(argument);
            if (argument == "rebuild") {
                ZoneConstruction();
                return;
            }
            if (argument.Length > 0 && (!(argument == "realtime") || !(argument == "hardcoded"))) {
                ArgumentParser(argument);
                return;
            }
            if (rebuildneeded) {
                if (rebuildCounter++ > rebuildCounterLimit) {
                    rebuildneeded = false;
                    rebuildCounter = 0;
                    ZoneConstruction();
                    return;
                }
            }
            if (!shipStatusLCD.CleanLCD()) { shipStatusLCD.LCDBuild(shipStatusLCDName); }
            if (!orbitalStatusLCD.CleanLCD()) { orbitalStatusLCD.LCDBuild(orbitalStatusLCDName); }
            shipStatusLCD.WriteToLCD(Me.CubeGrid.DisplayName);
            shipStatusLCD.WriteToLCD($"\n {GetPowerOutputString()}");
            if (stopShipIfNotPiloted) shipStatusLCD.WriteToLCD($"\n Ship Piloted : {!StopShipIfNotPiloted()}");
            if (runAutoLevel) {
                if (rocketGyro) {
                    shipStatusLCD.WriteToLCD($"\n{GyroMain("Rocket")}");
                } else {
                    shipStatusLCD.WriteToLCD($"\n{GyroMain("")}");
                }
            }
            //Orbital();
            orbitalComputer.Update();
            orbitalStatusLCD.WriteToLCD(orbitalComputer.Status);
            O2Tank(o2TankLow, o2TankHigh);
            H2Tank(h2TankLow, h2TankHigh);
            shipStatusLCD.WriteToLCD($"\n O2 Tanks {o2TanksFill.ToString("0.00")}%, H2 Tanks {h2TanksFill.ToString("0.00")}%");
            foreach (SolarArray de in solarArrays) {
                if (!de.Update()) {
                    rebuildneeded = true;
                    rebuildCounter = rebuildCounterLimit + 1;
                }
                shipStatusLCD.WriteToLCD($"\n Array Power {DisplayLargeNumber(de.Power * 1000000)}W");
            }
            foreach (Hangar de in hangars) {
                if (!de.Update()) {
                    rebuildneeded = true;
                    rebuildCounter = rebuildCounterLimit + 1;
                }
                shipStatusLCD.WriteToLCD($"\n {de.Status}");
            }
            foreach (Airlock de in airlocks) {
                if (!de.Update()) {
                    rebuildneeded = true;
                    rebuildCounter = rebuildCounterLimit + 1;
                }
                shipStatusLCD.WriteToLCD($"\n {de.Status}");
            }
            foreach (RoomPressure de in roomPressures) {
                if (!de.Update()) {
                    rebuildneeded = true;
                    rebuildCounter = rebuildCounterLimit + 1;
                }
                shipStatusLCD.WriteToLCD($"\n {de.Status}");
            }
            foreach (LandingPad de in landingPads) {
                if (!de.Update()) {
                    rebuildneeded = true;
                    rebuildCounter = rebuildCounterLimit + 1;
                }
            }
            if (!shipStatusLCD.FlushToLCD()) { shipStatusLCD.LCDBuild(shipStatusLCDName); }
            if (!orbitalStatusLCD.FlushToLCD()) { orbitalStatusLCD.LCDBuild(orbitalStatusLCDName); }
            if (++statusTime > 300) {
                statusTime = 0;
                for (int i = 0; i < outRadios.Count; ++i) {
                    if (!outRadios[i].IsFunctional) {
                        GridTerminalSystem.GetBlocksOfType(outRadios, b => b.CubeGrid == Me.CubeGrid);
                        break;
                    }
                }
                messageOut.LowPriority(outRadios, "anyone", Me.CubeGrid.DisplayName, "",
                    $"Status:{GetPowerOutputString()}\nStatus: O2 {o2TanksFill.ToString("0.00")}%, H2 {h2TanksFill.ToString("0.00")}%",
                    MyTransmitTarget.Owned, true, ';');
            }
            messageOut.SendMessages();
            StartTimer(timers);
        }

        void ArgumentParser(string argument) {
            argMessages = argument.Split('\n');
            foreach (string de in argMessages) {
                pieces = de.Split(' ');
                if (pieces.Count() < 1) continue;
                //at least 1 pieces below this
                if (pieces[0] == "autolevel") runAutoLevel = !runAutoLevel;
                if (pieces[0] == "autolevelon") runAutoLevel = true;
                if (pieces[0] == "goland") orbitalComputer.GoLand();
                if (pieces[0] == "godeepspace") orbitalComputer.GoDeepSpace();
                if (pieces[0] == "gohover") orbitalComputer.GoHover();
                if (pieces[0] == "orbitaloff") orbitalComputer.TurnOffOrbital();
                if (pieces[0] == "orbitalrebuild") orbitalComputer.OrbitalListNeedsBuilding = true;
                if (pieces[0] == "gravity") orbitalComputer.OrbitalMode = Orbital.OrbitalOperation.GravityMode;
                if (pieces[0] == "altitude") orbitalComputer.OrbitalMode = Orbital.OrbitalOperation.AltitudeMode;
                if (pieces[0] == "hover") orbitalComputer.OrbitalMode = Orbital.OrbitalOperation.HoverMode;
                if (pieces[0] == "currentaltitude") orbitalComputer.CurrentAltitude();
                if (pieces[0] == "currentgravity") orbitalComputer.CurrentGravity();
                if (pieces[0] == "currenthover") orbitalComputer.CurrentHover();
                if (pieces[0] == "gravdrive") {
                    orbitalComputer.GravDriveOn = !orbitalComputer.GravDriveOn;
                }
                if (pieces[0] == "gravdriveon") {
                    orbitalComputer.GravDriveOn = true;
                }
                if (pieces[0] == "sunchaser") runSunChaser = !runSunChaser;
                if (pieces[0] == "sunchaseron") runSunChaser = true;
                if (pieces[0] == "rebuildlcd") {
                    shipStatusLCD.LCDBuild(shipStatusLCDName);
                    orbitalStatusLCD.LCDBuild(orbitalStatusLCDName);
                }
                if (pieces.Count() < 2) continue;
                //at least 2 pieces below this
                if (pieces[0] == "timerName") {
                    tempString = "";
                    for (int i=1; i<pieces.Length; ++i) {
                        tempString += pieces[i];
                    }
                    timerName = tempString;
                }
                if (pieces[0] == "shipStatusLCDName") {
                    tempString = "";
                    for (int i = 1; i < pieces.Length; ++i) {
                        tempString += pieces[i];
                    }
                    shipStatusLCDName = tempString;
                }
                if (pieces[0] == "orbitalStatusLCDName") {
                    tempString = "";
                    for (int i = 1; i < pieces.Length; ++i) {
                        tempString += pieces[i];
                    }
                    orbitalStatusLCDName = tempString;
                }
                if (pieces[0] == "o2TankLow") {
                    if (int.TryParse(pieces[1], out tempInt)) o2TankLow = tempInt;
                }
                if (pieces[0] == "o2TankHigh") {
                    if (int.TryParse(pieces[1], out tempInt)) o2TankHigh = tempInt;
                }
                if (pieces[0] == "h2TankLow") {
                    if (int.TryParse(pieces[1], out tempInt)) h2TankLow = tempInt;
                }
                if (pieces[0] == "h2TankHigh") {
                    if (int.TryParse(pieces[1], out tempInt)) h2TankHigh = tempInt;
                }
                if (pieces[0] == "rebuild") {
                    if (int.TryParse(pieces[1], out tempInt)) {
                        rebuildCounterLimit = tempInt;
                        rebuildneeded = true;
                        rebuildCounter = 0;
                    }
                }
                if (pieces[0] == "stopShipIfNotPiloted") {
                    if (bool.TryParse(pieces[1], out test)) {
                        stopShipIfNotPiloted = test;
                    }
                }
                if (pieces[0] == "rocketGyro") {
                    if (bool.TryParse(pieces[1], out test)) {
                        rocketGyro = test;
                    }
                }
                if (pieces[0] == "targetaltitude") {
                    if (double.TryParse(pieces[1], out tempDouble)) {
                        orbitalComputer.AltitudeTarget = tempDouble;
                    }
                }
                if (pieces[0] == "targethover") {
                    if (double.TryParse(pieces[1], out tempDouble)) {
                        orbitalComputer.HoverTarget = tempDouble;
                    }
                }
                if (pieces[0] == "targetgravity") {
                    if (double.TryParse(pieces[1], out tempDouble)) {
                        orbitalComputer.GravityTarget = tempDouble;
                    }
                }
                if (pieces[0] == "velocitylimit") {
                    if (double.TryParse(pieces[1], out tempDouble)) {
                        orbitalComputer.VelocityLimit = tempDouble;
                    }
                }
                if (pieces[0] == "changealtitude") {
                    if (double.TryParse(pieces[1], out tempDouble)) {
                        orbitalComputer.AltitudeTarget = orbitalComputer.AltitudeTarget + tempDouble;
                    }
                }
                if (pieces[0] == "changehover") {
                    if (double.TryParse(pieces[1], out tempDouble)) {
                        orbitalComputer.HoverTarget = orbitalComputer.HoverTarget + tempDouble;
                    }
                }
                if (pieces[0] == "changegravity") {
                    if (double.TryParse(pieces[1], out tempDouble)) {
                        orbitalComputer.GravityTarget = orbitalComputer.GravityTarget + tempDouble;
                    }
                }
                if (pieces[0] == "heightoffset") {
                    if (double.TryParse(pieces[1], out tempDouble)) {
                        orbitalComputer.HeightOffset = tempDouble;
                    }
                }
                if (pieces[0] == "EmergencyThrusterName") {
                    tempString = "";
                    for (int i = 1; i < pieces.Length; ++i) {
                        tempString += pieces[i];
                    }
                    orbitalComputer.EmergencyThrusterName = tempString;
                }
            }
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
        string GetPowerOutputString() {
            GetPowerOutput();
            return $"Power: {percentPower.ToString("0.00")}%, current {DisplayLargeNumber(currentPower)}, max {DisplayLargeNumber(maxPower)}";
        }

        void GetPowerOutput() {
            currentPower = 0;
            maxPower = 0;
            percentPower = 0;
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(null, b => {
                if (!b.IsCharging) {
                    currentPower += b.CurrentOutput;
                    source = b.Components.Get<MyResourceSourceComponent>();
                    if (source != null) {
                        maxPower += source.MaxOutputByType(new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity"));
                    }
                }
                return false;
            });
            GridTerminalSystem.GetBlocksOfType<IMyReactor>(null, b => {
                if (b.Enabled) {
                    currentPower += b.CurrentOutput;
                    maxPower += b.MaxOutput;
                }
                return false;
            });
            GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(null, b => {
                currentPower += b.CurrentOutput;
                maxPower += b.MaxOutput;
                return false;
            });
            //all in MW,m need to change to watts
            maxPower *= 1000000;
            currentPower *= 1000000;
            if (maxPower > 0) { percentPower = (currentPower / maxPower) * 100; } else { percentPower = 100; }
        }

        float GetH2TankFill(List<IMyGasTank> airTanks) {
            h2TanksCount = 0;
            //get percent in tanks 
            h2TanksFill = 0;
            foreach (IMyGasTank de in airTanks) {
                de.Components.TryGet<MyResourceSinkComponent>(out sink);
                list = sink.AcceptedResources;
                hasO2 = false;
                for (int j = 0; j < list.Count; ++j) {
                    hasO2 = list[j].SubtypeId.ToString() == "Hydrogen";
                    if (hasO2) { break; }
                }
                if (hasO2) {
                    h2TanksFill += de.FilledRatio * 100;
                    ++h2TanksCount;
                }
            }
            if (h2TanksCount == 0) return -1;
            h2TanksFill /= h2TanksCount;
            return h2TanksFill;
        }

        float GetO2TankFill(List<IMyGasTank> airTanks) {
            o2TanksCount = 0;
            //get percent in tanks 
            o2TanksFill = 0;
            foreach (IMyGasTank de in airTanks) {
                de.Components.TryGet<MyResourceSinkComponent>(out sink);
                list = sink.AcceptedResources;
                hasO2 = false;
                for (int j = 0; j < list.Count; ++j) {
                    hasO2 = list[j].SubtypeId.ToString() == "Oxygen";
                    if (hasO2) { break; }
                }
                if (hasO2) {
                    o2TanksFill += de.FilledRatio * 100;
                    ++o2TanksCount;
                }
            }
            if (o2TanksCount == 0) return -1;
            o2TanksFill /= o2TanksCount;
            return o2TanksFill;
        }

        List<string> GetZoneNames(List<string> reservedNames) {
            var zoneList = new List<string>();
            GridTerminalSystem.SearchBlocksOfName("[", null, b => {
                if (b.CustomName.Contains(']')) {
                    tempName = b.CustomName.Substring(b.CustomName.IndexOf('['), b.CustomName.IndexOf(']') - b.CustomName.IndexOf('[') + 1);
                } else { tempName = ""; }
                if (!reservedNames.Contains(tempName) && !zoneList.Contains(tempName)) {
                    zoneList.Add(tempName);
                }
                return false;
            });
            return zoneList;
        }

        void GyroPitch(List<IMyGyro> gyro, float sunTurn) {
            foreach(IMyGyro de in gyro) {
                de.GyroOverride = true;
                de.Yaw = 0;
                de.Pitch = sunTurn;
                de.Roll = 0;
            }
        }

        void GyroYaw(List<IMyGyro> gyro, float sunTurn) {
            foreach (IMyGyro de in gyro) {
                de.GyroOverride = true;
                de.Yaw = sunTurn;
                de.Pitch = 0;
                de.Roll = 0;
            }
        }

        void H2Tank(int turnOn, int turnOff) {
            var percentFull = GetH2TankFill(gasTanks);
            if (percentFull == -1) return;
            if (percentFull >= turnOff) {
                foreach (IMyGasGenerator de in gasGenerators) de.Enabled = false;
                foreach (IMyOxygenFarm de in o2Farms) de.ApplyAction("OnOff_Off");
            }
            if (percentFull <= turnOn) {
                foreach (IMyGasGenerator de in gasGenerators) de.Enabled = true;
                foreach (IMyOxygenFarm de in o2Farms) de.ApplyAction("OnOff_On");
            }
        }

        double LidarRange(List<IMyCameraBlock> cameras, float pitch, float yaw) {
            double range = 99999;
            double range2 = 99999;
            MyDetectedEntityInfo info;
            foreach (IMyCameraBlock de in cameras) {
                info = de.Raycast(de.AvailableScanRange, pitch, yaw);
                if (info.HitPosition.HasValue && info.EntityId != Me.EntityId) {
                    range2 = Vector3D.Distance(de.GetPosition(), info.HitPosition.Value);
                }
                if (range2 < range) range = range2;
            }
            return range;
        }

        void O2Tank(int turnOn, int turnOff) {
            var percentFull = GetO2TankFill(gasTanks);
            if (percentFull == -1) return;
            if (percentFull >= turnOff) {
                foreach (IMyGasGenerator de in gasGenerators) de.Enabled = false;
                foreach (IMyOxygenFarm de in o2Farms) de.ApplyAction("OnOff_Off");
            }
            if (percentFull <= turnOn) {
                foreach (IMyGasGenerator de in gasGenerators) de.Enabled = true;
                foreach (IMyOxygenFarm de in o2Farms) de.ApplyAction("OnOff_On");
            }
        }

        bool ShipIsPiloted() {
            bool piloted = false;
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(null, b => {
                if (b.IsUnderControl) {
                    piloted = true; ;
                }
                return false;
            });
            return piloted;
        }

        void StartTimer(List<IMyTimerBlock> timers) {
            foreach (IMyTimerBlock de in timers) de.StartCountdown();
        }

        bool StopShipIfNotPiloted() {
            if (!ShipIsPiloted()) {
                GridTerminalSystem.GetBlocksOfType<IMyShipController>(null, b => {
                    b.DampenersOverride = true;
                    return false;
                });
                return true;
            }
            return false;
        }

        void SunChaser(string sunChaserName, float sunTurn, float sunOverride) {
            sunMoveStarted = false;
            sunTurnStarted = false;
            foreach(IMySolarPanel de in sunChaserPanels) {
                if (de.CustomName.Contains("Port") && de.MaxOutput > 0) {
                    sunTurnStarted = true;
                    GyroYaw(sunChaserGyro, -sunTurn);
                }else if((de.CustomName.Contains("Starboard") || de.CustomName.Contains("Aft") )&& de.MaxOutput > 0) {
                    sunTurnStarted = true;
                    GyroYaw(sunChaserGyro, sunTurn);
                }else if(de.CustomName.Contains("Top") && de.MaxOutput > 0) {
                    sunTurnStarted = true;
                    GyroPitch(sunChaserGyro, sunTurn);
                }else if(de.CustomName.Contains("Below") && de.MaxOutput > 0) {
                    sunTurnStarted = true;
                    GyroPitch(sunChaserGyro, -sunTurn);
                }
                if (de.CustomName.Contains("Fore") && de.MaxOutput > 0) {
                    sunMoveStarted = true;
                    foreach (IMyThrust fr in sunChaserForeThrust) fr.Enabled = false;
                    foreach (IMyThrust fr in sunChaserAftThrust) fr.SetValueFloat("Override", sunOverride);
                }
            }
            if (!sunMoveStarted) {
                foreach (IMyThrust fr in sunChaserForeThrust) fr.Enabled = true;
                foreach (IMyThrust fr in sunChaserAftThrust) fr.SetValueFloat("Override", 0);
            }
            if (!sunTurnStarted) {
                foreach (IMyGyro de in sunChaserGyro) de.GyroOverride = false;
            }
        }

        Vector3D VectorProjection(Vector3D a, Vector3D b) {//project a onto b 
            Vector3D projection = a.Dot(b) / b.LengthSquared() * b;
            return projection;
        }

        void ZoneConstruction() {
            ZoneConstruction(GetZoneNames(reservedNamesStatic));
        }
        void ZoneConstruction(List<string> zoneList) {
            //clear
            airlocks.Clear();
            roomPressures.Clear();
            hangars.Clear();
            landingPads.Clear();
            outsideVents.Clear();
            gasTanks.Clear();
            gasGenerators.Clear();
            o2Farms.Clear();
            timers.Clear();
            solarArrays.Clear();
            rotors.Clear();
            panels.Clear();
            sunChaserPanels.Clear();
            sunChaserGyro.Clear();
            sunChaserForeThrust.Clear();
            sunChaserAftThrust.Clear();
            GridTerminalSystem.GetBlocksOfType(outsideVents, b => {
                if (b.CubeGrid == Me.CubeGrid && b.CustomName.Contains(outsideVentName)) return true;
                return false;
            });
            GridTerminalSystem.GetBlocksOfType(gasTanks, b => {
                if (b.CubeGrid == Me.CubeGrid) return true;
                return false;
            });
            GridTerminalSystem.GetBlocksOfType(gasGenerators, b => {
                if (b.CubeGrid == Me.CubeGrid) return true;
                return false;
            });
            GridTerminalSystem.GetBlocksOfType(o2Farms);
            GridTerminalSystem.GetBlocksOfType(timers, b => {
                if (b.CubeGrid == Me.CubeGrid && b.CustomName.Contains(timerName)) return true;
                return false;
            });
            GridTerminalSystem.GetBlocksOfType(rotors);
            foreach (IMyMotorStator v in rotors) {
                GridTerminalSystem.GetBlocksOfType(panels, b => b.CubeGrid == v.TopGrid);
                if (panels.Count > 0) {
                    solarArrays.Add(new SolarArray(v, new List<IMySolarPanel>(panels), solarAlign));
                }
            }
            GridTerminalSystem.SearchBlocksOfName(sunChaserName, null ,b => {
                if (b.CubeGrid == Me.CubeGrid) {
                    if (b is IMySolarPanel) sunChaserPanels.Add((IMySolarPanel)b);
                    else if (b is IMyGyro) sunChaserGyro.Add((IMyGyro)b);
                    else if (b is IMyThrust && b.CustomName.Contains("Fore")) sunChaserForeThrust.Add((IMyThrust)b);
                    else if (b is IMyThrust && b.CustomName.Contains("Aft")) sunChaserAftThrust.Add((IMyThrust)b);
                }
                return false;
            });
            //walk through each possible zone
            foreach (string de in zoneList) {
                controlExists = false;
                hangerPressurizeExists = false;
                hangerDepressurizeExists = false;
                lights.Clear();
                lightsA.Clear();
                lightsB.Clear();
                doors.Clear();
                vents.Clear();
                connectors.Clear();
                landingGears.Clear();
                localGasTanks.Clear();
                sounds.Clear();
                sensors.Clear();
                outsideDoorExist = false;
                insideDoorExist = false;
                GridTerminalSystem.SearchBlocksOfName(de, null, b => {
                    if (!(b.CubeGrid == Me.CubeGrid)) return false;
                    if (b is IMyInteriorLight) {
                        lights.Add((IMyInteriorLight)b);
                        if (b.CustomName.Contains(controlLightName)) controlExists = true;
                        else if (b.CustomName.Contains(hangarPressurizeName)) {
                            hangerPressurizeExists = true;
                            lightsA.Add((IMyInteriorLight)b);
                        } else if (b.CustomName.Contains(hangarDepressurizeName)) {
                            hangerDepressurizeExists = true;
                            lightsB.Add((IMyInteriorLight)b);
                        }
                    } else if (b is IMyDoor) {
                        doors.Add((IMyDoor)b);
                        if (b.CustomName.Contains(outsideDoorName)) outsideDoorExist = true;
                        if (b.CustomName.Contains(insideDoorName)) insideDoorExist = true;
                    } else if (b is IMyAirVent) {
                        vents.Add((IMyAirVent)b);
                    } else if (b is IMyShipConnector) {
                        connectors.Add((IMyShipConnector)b);
                    } else if (b is IMyLandingGear) {
                        landingGears.Add((IMyLandingGear)b);
                    } else if (b is IMyGasTank) {
                        localGasTanks.Add((IMyGasTank)b);
                    } else if (b is IMySoundBlock) {
                        sounds.Add((IMySoundBlock)b);
                    } else if (b is IMySensorBlock) {
                        sensors.Add((IMySensorBlock)b);
                    }
                    return false;
                });
                //airlock?
                if (controlExists && vents.Count > 0 && doors.Count > 0) {
                    doorsA.Clear();
                    doorsB.Clear();
                    ventsA.Clear();
                    ventsB.Clear();
                    if (localGasTanks.Count == 0) localGasTanks = new List<IMyGasTank>(gasTanks);
                    //standard airlock
                    if (outsideDoorExist && insideDoorExist) {
                        foreach (IMyDoor fr in doors) {
                            if (fr.CustomName.Contains(outsideDoorName)) {
                                doorsA.Add(fr);
                            } else if (fr.CustomName.Contains(insideDoorName)) {
                                doorsB.Add(fr);
                            }
                        }
                        if (outsideVents.Count > 0) {
                            airlocks.Add(new Airlock(de, vents, lights, doorsA, doorsB, localGasTanks, airPressureWarning, outsideVents, new List<IMyAirVent>()));
                            continue;
                        } else {
                            airlocks.Add(new Airlock(de, vents, lights, doorsA, doorsB, localGasTanks, airPressureWarning));
                            continue;
                        }
                    }
                    //? ? airlock, try to figure out [zonename]
                    tempZoneNames.Clear();
                    foreach (IMyDoor fr in doors) {
                        int startIndex = fr.CustomName.IndexOf(de);
                        tempString = fr.CustomName.Remove(startIndex, de.Length);
                        tempName = "";
                        if (tempString.Contains('[') && tempString.Contains(']') && (tempString.IndexOf('[') < tempString.IndexOf(']'))) {
                            //second zone name found
                            tempName = tempString.Substring(tempString.IndexOf('['), tempString.IndexOf(']') - tempString.IndexOf('[') + 1);
                            if (!tempZoneNames.Contains(tempName) && !reservedNamesStatic.Contains(tempName)) {
                                tempZoneNames.Add(tempName);
                            }
                        }
                    }
                    //BackWards compatiblity with older ships
                    if (tempZoneNames.Count == 1 && outsideDoorExist) {
                        tempZoneNames.Add(outsideDoorName);
                    }
                    if (tempZoneNames.Count == 2) {
                        //valid airlock
                        foreach (IMyDoor fr in doors) {
                            if (fr.CustomName.Contains(tempZoneNames[0])) {
                                doorsA.Add(fr);
                            } else if (fr.CustomName.Contains(tempZoneNames[1])) {
                                doorsB.Add(fr);
                            }
                        }
                        GridTerminalSystem.GetBlocksOfType<IMyAirVent>(null, b => {
                            if (b.CustomName.Contains(tempZoneNames[0])) {
                                ventsA.Add(b);
                            } else if (b.CustomName.Contains(tempZoneNames[1])) {
                                ventsB.Add(b);
                            } else if (b.CustomName.Contains(outsideVentName)) {
                                ventsB.Add(b);
                            }
                            return false;
                        });
                        if (ventsA.Count > 0 && ventsB.Count > 0 && doorsA.Count > 0 && doorsB.Count > 0) {
                            airlocks.Add(new Airlock(de, vents, lights, doorsA, doorsB, localGasTanks, airPressureWarning, ventsA, ventsB));
                            continue;
                        } else if (ventsA.Count > 0 && tempZoneNames.Contains(outsideDoorName) && doorsA.Count > 0 && doorsB.Count > 0) {
                            airlocks.Add(new Airlock(de, vents, lights, doorsB, doorsA, localGasTanks, airPressureWarning, new List<IMyAirVent>(), ventsA));
                            continue;
                        }
                    }
                }
                //No Airlock found
                //Hanger
                if (hangerPressurizeExists && hangerDepressurizeExists && vents.Count > 0) {
                    foreach (IMyInteriorLight fe in lightsA) lights.Remove(fe);
                    foreach (IMyInteriorLight fe in lightsB) lights.Remove(fe);
                    if (localGasTanks.Count == 0) localGasTanks = new List<IMyGasTank>(gasTanks);
                    if (vents.Count > 0 && lights.Count > 0 && sounds.Count > 0 && doors.Count > 0 && sensors.Count > 0 && outsideVents.Count > 0 && localGasTanks.Count > 0) {
                        hangars.Add(new Hangar(de, lightsA, lightsB, vents, lights, sounds, doors, sensors, outsideVents, localGasTanks, airPressureWarning));
                        continue;
                    }
                    if (vents.Count > 0 && lights.Count > 0 && sounds.Count > 0 && doors.Count > 0 && sensors.Count > 0 && localGasTanks.Count > 0) {
                        hangars.Add(new Hangar(de, lightsA, lightsB, vents, doors, sensors, sounds, localGasTanks, airPressureWarning));
                        continue;
                    }
                    if (vents.Count > 0 && doors.Count > 0 && outsideVents.Count > 0 && localGasTanks.Count > 0) {
                        hangars.Add(new Hangar(de, lightsA, lightsB, vents, doors, outsideVents, localGasTanks, airPressureWarning));
                        continue;
                    }
                    if (vents.Count > 0 && doors.Count > 0 && sensors.Count > 0 && localGasTanks.Count > 0) {
                        hangars.Add(new Hangar(de, lightsA, lightsB, vents, doors, sensors, localGasTanks, airPressureWarning));
                        continue;
                    }
                    if (vents.Count > 0 && doors.Count > 0 && localGasTanks.Count > 0) {
                        hangars.Add(new Hangar(de, lightsA, lightsB, vents, doors, localGasTanks, airPressureWarning));
                        continue;
                    }

                }
                //RoomPressure
                if (vents.Count > 0) {
                    if (doors.Count > 0 && lights.Count > 0) {
                        roomPressures.Add(new RoomPressure(de, vents, lights, doors, airPressureWarning));
                    } else if (lights.Count > 0) {
                        roomPressures.Add(new RoomPressure(de, vents, lights, airPressureWarning));
                    } else if (doors.Count > 0) {
                        roomPressures.Add(new RoomPressure(de, vents, doors, airPressureWarning));
                    }
                }//RoomPressure
                //Landing Pad
                if ((landingGears.Count > 0 && (connectors.Count > 0 || lights.Count > 0)) || (connectors.Count > 0 &&(landingGears.Count > 0 || lights.Count > 0))) {
                    if (connectors.Count > 0 && lights.Count > 0) {
                        landingPads.Add(new LandingPad(de, landingGears, lights, connectors));
                    } else if (connectors.Count > 0 && landingGears.Count > 0) {
                        landingPads.Add(new LandingPad(de, landingGears, connectors));
                    } else if (landingPads.Count >0 && lights.Count >0){
                        landingPads.Add(new LandingPad(de, landingGears, lights));
                    } else {
                        landingPads.Add(new LandingPad(de, connectors, lights));
                    }
                }
            }
            //not built
        }
    }
}