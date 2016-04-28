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
        public InputChannelTypes.JoyAxis LSx { get; set; }
        public InputChannelTypes.JoyAxis LSy { get; set; }
        public InputChannelTypes.JoyAxis RSx { get; set; }
        public InputChannelTypes.JoyAxis RSy { get; set; }

        public InputChannelTypes.Button L3 { get; set; }
        public InputChannelTypes.Button R3 { get; set; }

        public InputChannelTypes.JoyAxis L2 { get; set; }
        public InputChannelTypes.JoyAxis R2 { get; set; }
        public InputChannelTypes.Button L1 { get; set; }
        public InputChannelTypes.Button R1 { get; set; }

        public InputChannelTypes.Button DUp { get; set; }
        public InputChannelTypes.Button DDown { get; set; }
        public InputChannelTypes.Button DLeft { get; set; }
        public InputChannelTypes.Button DRight { get; set; }

        public InputChannelTypes.Button Cross { get; set; }
        public InputChannelTypes.Button Circle { get; set; }
        public InputChannelTypes.Button Square { get; set; }
        public InputChannelTypes.Button Triangle { get; set; }

        public InputChannelTypes.Button PS { get; set; }
        public InputChannelTypes.Button Share { get; set; }
        public InputChannelTypes.Button Options { get; set; }

        public InputChannelTypes.JoyAxis Battery { get; set; }
        public InputChannelTypes.Button Charging { get; set; }

        public InputChannelTypes.Button TouchpadButton { get; set; }
        public InputChannelTypes.Button TouchpadTouchOne { get; set; }
        public InputChannelTypes.Button TouchpadTouchTwo { get; set; }

        public InputChannelTypes.JoyAxis TouchpadTouchOneX { get; set; }
        public InputChannelTypes.JoyAxis TouchpadTouchOneY { get; set; }

        public InputChannelTypes.JoyAxis TouchpadTouchTwoX { get; set; }
        public InputChannelTypes.JoyAxis TouchpadTouchTwoY { get; set; }

        public InputChannelTypes.JoyAxis GyroX { get; set; }
        public InputChannelTypes.JoyAxis GyroY { get; set; }
        public InputChannelTypes.JoyAxis GyroZ { get; set; }

        public InputChannelTypes.JoyAxis AccelX { get; set; }
        public InputChannelTypes.JoyAxis AccelY { get; set; }
        public InputChannelTypes.JoyAxis AccelZ { get; set; }

        public OutputChannelTypes.RumbleMotor BigRumble { get; set; }
        public OutputChannelTypes.RumbleMotor SmallRumble { get; set; }
        public OutputChannelTypes.RGBLED LightBar { get; set; }

        public DS4Device()
        {
            LSx = new InputChannelTypes.JoyAxis("Left Stick X", "", Properties.Resources.LSX.ToImageSource());
            LSy = new InputChannelTypes.JoyAxis("Left Stick Y", "", Properties.Resources.LSY.ToImageSource());
            RSx = new InputChannelTypes.JoyAxis("Right Stick X", "",Properties.Resources.RSX.ToImageSource());
            RSy = new InputChannelTypes.JoyAxis("Right Stick Y", "", Properties.Resources.RSY.ToImageSource());

            L3 = new InputChannelTypes.Button("L3", "Left stick", Properties.Resources.L3.ToImageSource());
            R3 = new InputChannelTypes.Button("R3", "Right Stick", Properties.Resources.R3.ToImageSource());

            L2 = new InputChannelTypes.JoyAxis("L2", "Left Trigger", Properties.Resources.L2.ToImageSource()) { min_Value = 0 };
            R2 = new InputChannelTypes.JoyAxis("R2", "Right Trigger", Properties.Resources.R2.ToImageSource()) { min_Value = 0 };
            L1 = new InputChannelTypes.Button("L1", "Left Bumper", Properties.Resources.L1.ToImageSource());
            R1 = new InputChannelTypes.Button("R1", "Right Bumper", Properties.Resources.R1.ToImageSource());

            DUp = new InputChannelTypes.Button("DPad Up", "", Properties.Resources.DUp.ToImageSource());
            DDown = new InputChannelTypes.Button("DPad Down", "", Properties.Resources.DDown.ToImageSource());
            DLeft = new InputChannelTypes.Button("DPad Left", "", Properties.Resources.DLeft.ToImageSource());
            DRight = new InputChannelTypes.Button("DPad Right", "", Properties.Resources.DRight.ToImageSource());

            Cross = new InputChannelTypes.Button("Cross", "", Properties.Resources.Cross.ToImageSource());
            Circle = new InputChannelTypes.Button("Circle", "", Properties.Resources.Circle.ToImageSource());
            Square = new InputChannelTypes.Button("Square", "", Properties.Resources.Square.ToImageSource());
            Triangle = new InputChannelTypes.Button("Triangle", "", Properties.Resources.Triangle.ToImageSource());

            PS = new InputChannelTypes.Button("PS", "");
            Share = new InputChannelTypes.Button("Share", "", Properties.Resources.Share.ToImageSource());
            Options = new InputChannelTypes.Button("Options", "", Properties.Resources.Options.ToImageSource());

            Battery = new InputChannelTypes.JoyAxis("Battery Level", "");
            Charging = new InputChannelTypes.Button("Charging", "");

            TouchpadButton = new InputChannelTypes.Button("Touchpad Button","");
            TouchpadTouchOne = new InputChannelTypes.Button("Touchpad Touch One", "");
            TouchpadTouchTwo = new InputChannelTypes.Button("Touchpad Touch Two", "");

            TouchpadTouchOneX = new InputChannelTypes.JoyAxis("Touchpad Touch One X", "") { min_Value = 0 };
            TouchpadTouchOneY = new InputChannelTypes.JoyAxis("Touchpad Touch One Y", "") { min_Value = 0 };
            TouchpadTouchTwoX = new InputChannelTypes.JoyAxis("Touchpad Touch Two X", "") { min_Value = 0 };
            TouchpadTouchTwoY = new InputChannelTypes.JoyAxis("Touchpad Touch Two Y", "") { min_Value = 0 };

            GyroX = new InputChannelTypes.JoyAxis("Gyroscope X", "");
            GyroY = new InputChannelTypes.JoyAxis("Gyroscope Y", "");
            GyroZ = new InputChannelTypes.JoyAxis("Gyroscope Z", "");

            AccelX = new InputChannelTypes.JoyAxis("Accelerometer X", "");
            AccelY = new InputChannelTypes.JoyAxis("Accelerometer Y", "");
            AccelZ = new InputChannelTypes.JoyAxis("Accelerometer Z", "");

            BigRumble = new OutputChannelTypes.RumbleMotor("Big Rumble","");
            SmallRumble = new OutputChannelTypes.RumbleMotor("Small Rumble", "");
            LightBar = new OutputChannelTypes.RGBLED("Light Bar", "");
        }
    }
}
