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

        public class WriteLCD {
            public WriteLCD(MyGridProgram script) {
                _script = script;
                lcd1 = new StringBuilder();
                lcds = new List<IMyTextPanel>();
            }
            public WriteLCD(MyGridProgram script, string lcdName) {
                _script = script;
                lcd1 = new StringBuilder();
                lcds = new List<IMyTextPanel>();
                LCDBuild(lcdName);
            }

            MyGridProgram _script;
            StringBuilder lcd1;
            List<IMyTextPanel> lcds;
            private const int MAX_NUMBER_CHARACTERS = 100000;

            public bool CleanLCD() {
                return WriteToLCD(lcds, "", Color.White, false);
            }

            public bool FlushToLCD() {
                if (!WriteToLCD(lcds, lcd1.ToString(), Color.White, true)) return false;
                lcd1.Clear();
                return true;
            }

            public bool FlushToLCD(Color color) {
                if (!WriteToLCD(lcds, lcd1.ToString(), color, true)) return false;
                lcd1.Clear();
                return true;
            }

            public void LCDBuild(string lcdName) {
                lcds.Clear();
                _script.GridTerminalSystem.SearchBlocksOfName(lcdName, null, b => {
                    if (b is IMyTextPanel) lcds.Add((IMyTextPanel)b);
                    return false;
                });
            }

            public void WriteToLCD(string textToWrite) {
                lcd1.Append(textToWrite);
            }

            public void WriteToLCD(string textToWrite, Color color, bool append = true) {
                WriteToLCD(lcds, textToWrite, color, append);
            }

            public bool WriteToLCD(List<IMyTextPanel> LCDs, string textToWrite, Color color, bool append = true) {
                if (textToWrite.Length >= MAX_NUMBER_CHARACTERS) textToWrite = " ERROR/n Attempted to write/n too many characters";
                for (int i = 0; i < LCDs.Count; ++i) {
                    if (!LCDs[i].IsFunctional) return false;
                    LCDs[i].WritePublicText(textToWrite, append);
                    LCDs[i].SetValue("FontColor", color);
                    LCDs[i].ShowTextureOnScreen();
                    LCDs[i].ShowPublicTextOnScreen();
                }//for
                return true;
            }

            public void WritetoLCDTitle(string textToWrite, Color color, bool append = true) {
                WriteToLCDTitle(lcds, textToWrite, color, append);
            }

            public bool WriteToLCDTitle(List<IMyTextPanel> LCDs, string textToWrite, Color color, bool append) {
                for (int i = 0; i < LCDs.Count; ++i) {
                    if (!LCDs[i].IsFunctional) return false;
                    LCDs[i].WritePublicTitle(textToWrite, append);
                    LCDs[i].SetValue("FontColor", color);
                    LCDs[i].ShowTextureOnScreen();
                    LCDs[i].ShowPublicTextOnScreen();
                }//for
                return true;
            }
        }

    }
}
