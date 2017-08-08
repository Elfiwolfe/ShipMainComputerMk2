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
    partial class Program {        //OIS Commo system.
        public class OutgoingMessage {
            List<RadioMessage> highPriority = new List<RadioMessage>();
            List<RadioMessage> mediumPriority = new List<RadioMessage>();
            List<RadioMessage> lowPriority = new List<RadioMessage>();
            class RadioMessage {
                public IMyRadioAntenna Radio { get; set; }
                public string To { get; set; }
                public string From { get; set; }
                public string Options { get; set; }
                public string Message { get; set; }
                public MyTransmitTarget TransmitTarget { get; set; }
                public char Seperator { get; set; }
                public RadioMessage(IMyRadioAntenna radio, string to, string from, string options, string message, MyTransmitTarget transmitTarget = MyTransmitTarget.Default, char seperator = ';') {
                    Radio = radio;
                    To = to;
                    From = from;
                    Options = options;
                    Message = message;
                    TransmitTarget = transmitTarget;
                    Seperator = seperator;
                }
            }
            public void HighPriority(IMyRadioAntenna radio, string to, string from, string options, string message,
        MyTransmitTarget transmitTarget = MyTransmitTarget.Default, bool append = false, char seperator = ';') {
                List<IMyRadioAntenna> radios = new List<IMyRadioAntenna> { radio };
                OutGoingMessage(ref highPriority, radios, to, from, options, message, transmitTarget, append, seperator);
            }

            public void HighPriority(List<IMyRadioAntenna> radios, string to, string from, string options, string message,
                MyTransmitTarget transmitTarget = MyTransmitTarget.Default, bool append = false, char seperator = ';') {
                OutGoingMessage(ref highPriority, radios, to, from, options, message, transmitTarget, append, seperator);
            }

            public void MediumPriority(IMyRadioAntenna radio, string to, string from, string options, string message,
        MyTransmitTarget transmitTarget = MyTransmitTarget.Default, bool append = false, char seperator = ';') {
                List<IMyRadioAntenna> radios = new List<IMyRadioAntenna> { radio };
                OutGoingMessage(ref mediumPriority, radios, to, from, options, message, transmitTarget, append, seperator);
            }

            public void MediumPriority(List<IMyRadioAntenna> radios, string to, string from, string options, string message,
        MyTransmitTarget transmitTarget = MyTransmitTarget.Default, bool append = false, char seperator = ';') {
                OutGoingMessage(ref mediumPriority, radios, to, from, options, message, transmitTarget, append, seperator);
            }

            public void LowPriority(IMyRadioAntenna radio, string to, string from, string options, string message,
        MyTransmitTarget transmitTarget = MyTransmitTarget.Default, bool append = false, char seperator = ';') {
                List<IMyRadioAntenna> radios = new List<IMyRadioAntenna> { radio };
                OutGoingMessage(ref lowPriority, radios, to, from, options, message, transmitTarget, append, seperator);
            }

            public void LowPriority(List<IMyRadioAntenna> radios, string to, string from, string options, string message,
        MyTransmitTarget transmitTarget = MyTransmitTarget.Default, bool append = false, char seperator = ';') {
                OutGoingMessage(ref lowPriority, radios, to, from, options, message, transmitTarget, append, seperator);
            }

            void OutGoingMessage(ref List<RadioMessage> mlist, List<IMyRadioAntenna> radios, string to, string from, string options, string message,
        MyTransmitTarget transmitTarget = MyTransmitTarget.Default, bool append = false, char seperator = ';') {
                if (append) {
                    bool added = false;
                    for (int i = 0; i < mlist.Count; ++i) {
                        for (int j = 0; j < radios.Count; ++j) {
                            if (mlist[i].Radio == radios[j] && mlist[i].To == to &&
                                mlist[i].From == from && mlist[i].Options == options &&
                                mlist[i].TransmitTarget == transmitTarget && mlist[i].Seperator == seperator) {
                                mlist[i].Message += "\n" + message;
                                added = true;
                            }
                        }
                    }
                    if (!added) {
                        for (int i = 0; i < radios.Count; ++i) {
                            mlist.Add(new RadioMessage(radios[i], to, from, options, message, transmitTarget, seperator));
                        }
                    }
                } else {
                    for (int i = 0; i < radios.Count; ++i) {
                        mlist.Add(new RadioMessage(radios[i], to, from, options, message, transmitTarget, seperator));
                    }
                }
            }

            public void SendMessages() {
                bool sent = false;
                for (int i = 0; i < highPriority.Count; ++i) {
                    sent = highPriority[i].Radio.TransmitMessage($"MSG{highPriority[i].Seperator}{highPriority[i].To}{highPriority[i].Seperator}{highPriority[i].From}{highPriority[i].Seperator}{highPriority[i].Options}{highPriority[i].Seperator}{highPriority[i].Message}", highPriority[i].TransmitTarget);
                    if (sent) {
                        if (highPriority.Remove(highPriority[i])) --i;
                    }
                }
                for (int i = 0; i < mediumPriority.Count; ++i) {
                    sent = mediumPriority[i].Radio.TransmitMessage($"MSG{mediumPriority[i].Seperator}{mediumPriority[i].To}{mediumPriority[i].Seperator}{mediumPriority[i].From}{mediumPriority[i].Seperator}{mediumPriority[i].Options}{mediumPriority[i].Seperator}{mediumPriority[i].Message}", mediumPriority[i].TransmitTarget);
                    if (sent) {
                        if (mediumPriority.Remove(mediumPriority[i])) --i;
                    }
                }
                for (int i = 0; i < lowPriority.Count; ++i) {
                    sent = lowPriority[i].Radio.TransmitMessage($"MSG{lowPriority[i].Seperator}{lowPriority[i].To}{lowPriority[i].Seperator}{lowPriority[i].From}{lowPriority[i].Seperator}{lowPriority[i].Options}{lowPriority[i].Seperator}{lowPriority[i].Message}", lowPriority[i].TransmitTarget);
                    if (sent) {
                        if (lowPriority.Remove(lowPriority[i])) --i;
                    }
                }
            }
        }
    }
}
