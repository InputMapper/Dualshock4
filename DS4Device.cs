using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ODIF;
using ODIF.Extensions;

namespace DualShock4
{
    internal class DS4Device
    {
        public ODIF.JoyAxis LSx { get; set; }
        public ODIF.JoyAxis LSy { get; set; }
        public ODIF.JoyAxis RSx { get; set; }
        public ODIF.JoyAxis RSy { get; set; }

        public ODIF.Button L3 { get; set; }
        public ODIF.Button R3 { get; set; }

        public ODIF.JoyAxis L2 { get; set; }
        public ODIF.JoyAxis R2 { get; set; }
        public ODIF.Button L1 { get; set; }
        public ODIF.Button R1 { get; set; }

        public ODIF.Button DUp { get; set; }
        public ODIF.Button DDown { get; set; }
        public ODIF.Button DLeft { get; set; }
        public ODIF.Button DRight { get; set; }

        public ODIF.Button Cross { get; set; }
        public ODIF.Button Circle { get; set; }
        public ODIF.Button Square { get; set; }
        public ODIF.Button Triangle { get; set; }

        public ODIF.Button PS { get; set; }
        public ODIF.Button Share { get; set; }
        public ODIF.Button Options { get; set; }

        public ODIF.JoyAxis Battery { get; set; }
        public ODIF.Button Charging { get; set; }

        public ODIF.Button TouchpadButton { get; set; }
        public ODIF.Button TouchpadTouchOne { get; set; }
        public ODIF.Button TouchpadTouchTwo { get; set; }

        public ODIF.JoyAxis TouchpadTouchOneX { get; set; }
        public ODIF.JoyAxis TouchpadTouchOneY { get; set; }

        public ODIF.JoyAxis TouchpadTouchTwoX { get; set; }
        public ODIF.JoyAxis TouchpadTouchTwoY { get; set; }

        public ODIF.JoyAxis GyroX { get; set; }
        public ODIF.JoyAxis GyroY { get; set; }
        public ODIF.JoyAxis GyroZ { get; set; }

        public ODIF.JoyAxis AccelX { get; set; }
        public ODIF.JoyAxis AccelY { get; set; }
        public ODIF.JoyAxis AccelZ { get; set; }

        public ODIF.RumbleMotor BigRumble { get; set; }
        public ODIF.RumbleMotor SmallRumble { get; set; }
        public ODIF.RGBLED LightBar { get; set; }

        public DS4Device()
        {
            LSx = new ODIF.JoyAxis("Left Stick X", DataFlowDirection.Input, "", Properties.Resources.LSX.ToImageSource());
            LSy = new ODIF.JoyAxis("Left Stick Y", DataFlowDirection.Input, "", Properties.Resources.LSY.ToImageSource());
            RSx = new ODIF.JoyAxis("Right Stick X", DataFlowDirection.Input, "",Properties.Resources.RSX.ToImageSource());
            RSy = new ODIF.JoyAxis("Right Stick Y", DataFlowDirection.Input, "", Properties.Resources.RSY.ToImageSource());

            L3 = new ODIF.Button("L3", DataFlowDirection.Input, "Left stick", Properties.Resources.L3.ToImageSource());
            R3 = new ODIF.Button("R3", DataFlowDirection.Input, "Right Stick", Properties.Resources.R3.ToImageSource());

            L2 = new ODIF.JoyAxis("L2", DataFlowDirection.Input, "Left Trigger", Properties.Resources.L2.ToImageSource()) { min_Value = 0 };
            R2 = new ODIF.JoyAxis("R2", DataFlowDirection.Input, "Right Trigger", Properties.Resources.R2.ToImageSource()) { min_Value = 0 };
            L1 = new ODIF.Button("L1", DataFlowDirection.Input, "Left Bumper", Properties.Resources.L1.ToImageSource());
            R1 = new ODIF.Button("R1", DataFlowDirection.Input, "Right Bumper", Properties.Resources.R1.ToImageSource());

            DUp = new ODIF.Button("DPad Up", DataFlowDirection.Input, "", Properties.Resources.DUp.ToImageSource());
            DDown = new ODIF.Button("DPad Down", DataFlowDirection.Input, "", Properties.Resources.DDown.ToImageSource());
            DLeft = new ODIF.Button("DPad Left", DataFlowDirection.Input, "", Properties.Resources.DLeft.ToImageSource());
            DRight = new ODIF.Button("DPad Right", DataFlowDirection.Input, "", Properties.Resources.DRight.ToImageSource());

            Cross = new ODIF.Button("Cross", DataFlowDirection.Input, "", Properties.Resources.Cross.ToImageSource());
            Circle = new ODIF.Button("Circle", DataFlowDirection.Input, "", Properties.Resources.Circle.ToImageSource());
            Square = new ODIF.Button("Square", DataFlowDirection.Input, "", Properties.Resources.Square.ToImageSource());
            Triangle = new ODIF.Button("Triangle", DataFlowDirection.Input, "", Properties.Resources.Triangle.ToImageSource());

            PS = new ODIF.Button("PS", DataFlowDirection.Input, "");
            Share = new ODIF.Button("Share", DataFlowDirection.Input, "", Properties.Resources.Share.ToImageSource());
            Options = new ODIF.Button("Options", DataFlowDirection.Input, "", Properties.Resources.Options.ToImageSource());

            Battery = new ODIF.JoyAxis("Battery Level", DataFlowDirection.Input, "");
            Charging = new ODIF.Button("Charging", DataFlowDirection.Input, "");

            TouchpadButton = new ODIF.Button("Touchpad Button", DataFlowDirection.Input, "");
            TouchpadTouchOne = new ODIF.Button("Touchpad Touch One", DataFlowDirection.Input, "");
            TouchpadTouchTwo = new ODIF.Button("Touchpad Touch Two", DataFlowDirection.Input, "");

            TouchpadTouchOneX = new ODIF.JoyAxis("Touchpad Touch One X", DataFlowDirection.Input, "") { min_Value = 0 };
            TouchpadTouchOneY = new ODIF.JoyAxis("Touchpad Touch One Y", DataFlowDirection.Input, "") { min_Value = 0 };
            TouchpadTouchTwoX = new ODIF.JoyAxis("Touchpad Touch Two X", DataFlowDirection.Input, "") { min_Value = 0 };
            TouchpadTouchTwoY = new ODIF.JoyAxis("Touchpad Touch Two Y", DataFlowDirection.Input, "") { min_Value = 0 };

            GyroX = new ODIF.JoyAxis("Gyroscope X", DataFlowDirection.Input, "");
            GyroY = new ODIF.JoyAxis("Gyroscope Y", DataFlowDirection.Input, "");
            GyroZ = new ODIF.JoyAxis("Gyroscope Z", DataFlowDirection.Input, "");

            AccelX = new ODIF.JoyAxis("Accelerometer X", DataFlowDirection.Input, "");
            AccelY = new ODIF.JoyAxis("Accelerometer Y", DataFlowDirection.Input, "");
            AccelZ = new ODIF.JoyAxis("Accelerometer Z", DataFlowDirection.Input, "");

            BigRumble = new RumbleMotor("Big Rumble", DataFlowDirection.Output, "");
            SmallRumble = new RumbleMotor("Small Rumble", DataFlowDirection.Output, "");
            LightBar = new RGBLED("Light Bar", DataFlowDirection.Output, "");
        }
    }
}
