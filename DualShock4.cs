using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;
using ODIF.Localization;
using ODIF;
using ODIF.Extensions;
using System.Globalization;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace DualShock4
{
    public enum ThreadStatus { None, Starting, Running, RequestStop, Stoped, Error };
    [PluginInfo(
        PluginName = "Dualshock 4 Input",
        PluginDescription = "Allows for BT and USB DS4 connectivity in InputMapper.",
        PluginID = 11,
        PluginAuthorName = "InputMapper",
        PluginAuthorEmail = "jhebbel@gmail.com",
        PluginAuthorURL = "http://inputmapper.com",
        PluginIconPath = @"pack://application:,,,/DualShock4;component/Resources/BT3.ico"
    )]
    public class iDualShock4_Plugin : InputDevicePlugin, pluginSettings
    {
        private Timer BackupDeviceListener;
        public bool PluginActive { get { return true; } set { } }
        //public AsyncObservableCollection<InputDevice> Devices { get; set; }
        public SettingGroup settings { get; }

        public iDualShock4_Plugin()
        {
            //Devices = new AsyncObservableCollection<InputDevice>();
            
            settings = new SettingGroup("General Settings", "");
            Setting lowLatencyMode = new Setting("Low latency mode", "Extremely low latency and low system overhead, but does not support rumble, trackpad or lightbar.", SettingControl.Checkbox, SettingType.Bool, false);
            lowLatencyMode.descriptionVisibility = DescriptionVisibility.SubText;
            settings.settings.Add(lowLatencyMode);

            Setting connectExclusively = new Setting("Connect Exclusively", "", SettingControl.Checkbox, SettingType.Bool, true);
            connectExclusively.descriptionVisibility = DescriptionVisibility.SubText;
            settings.settings.Add(connectExclusively);

            settings.loadSettings();

            Global.HardwareChangeDetected += Global_HardwareChangeDetected;

            BackupDeviceListener = new Timer(CheckForDevices, null, 10, 3000);
            CheckForDevices();
        }


        private void Global_HardwareChangeDetected(object sender, EventArgs e)
        {
            CheckForDevices();
        }

        protected override void Dispose(bool disposing)
        {
            settings.saveSettings();
            Global.HardwareChangeDetected -= Global_HardwareChangeDetected;
            BackupDeviceListener.Dispose();
            foreach (InputDevice Device in Devices.ToList())
            {
                Device.Dispose();
                Devices.Remove(Device);
            }

        }

        private byte[] GenerateTestReport(ConnectionTypes ConnectionType)
        {
            byte[] TestReport = new byte[0];
            if (ConnectionType == ConnectionTypes.BT)
            {
                TestReport = new byte[78];
                TestReport[0] = 0x11;
                TestReport[1] = 0x80;
                TestReport[3] = 0xff;
                TestReport[10] = 0xff;
            }
            if (ConnectionType == ConnectionTypes.USB)
            {
                TestReport = new byte[78];
                TestReport[0] = 0x05;
                TestReport[1] = 0xff;
                TestReport[8] = 0xff;

            }
            return TestReport;
        }
        private bool isDeviceResponding(HidDevice hDevice)
        {
            return (hDevice.WriteOutputReportViaControl(GenerateTestReport(hDevice.HidConnectionType())));
        }
        public void CheckForDevices(object callback = null)
        {
            lock (Devices)
            {
                IEnumerable<HidDevice> foundDevices = HidDevices.Enumerate(0x054C, 0x05C4);

                foreach (HidDevice device in foundDevices)
                    if (Devices.Where(d => (d as DualShock4_Device).hDevice.DevicePath == device.DevicePath).Count() == 0)
                    {
                        if (device.IsConnected)
                        {
                            if (settings.getSetting("Connect Exclusively"))
                            {
                                device.OpenDevice(true);
                                if (!device.IsOpen)
                                {
                                    ErrorHandling.LogWarning(this, new Warning("Could not connect to DS4 exclusively. Another application may be open and accessing the controller."));
                                    device.OpenDevice(false);
                                }
                            } else
                            {
                                device.OpenDevice(false);
                            }

                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            while ((!device.IsOpen || !isDeviceResponding(device)) && sw.Elapsed.Seconds <= 5)
                            {
                                Thread.Sleep(500);
                            }
                            sw.Stop();

                            if (device.IsOpen)
                            {
                                if (isDeviceResponding(device))
                                {
                                    DualShock4_Device Device = new DualShock4_Device(device, settings);
                                    Devices.Add(Device);
                                }
                            } else
                            {
                                ErrorHandling.LogWarning(this, new Warning("Could not connect to DS4."));
                            }
                        }
                    }
                foreach (DualShock4_Device Device in Devices.ToList())
                    if (Device.lastReport.ElapsedMilliseconds >= 1000)
                    {
                        Device.Dispose();
                        Devices.Remove(Device);
                    }
                    //if (foundDevices.Where(d => d.DevicePath == Device.hDevice.DevicePath).Count() == 0)
                    //{
                    //    //Device.Dispose();
                    //    //Devices.Remove(Device);
                    //}

            }
        }
    }

    public class DualShock4_Device : InputDevice
    {

        public HidDevice hDevice { get; set; }

        public ConnectionTypes DeviceConnectionType { get; private set; }

        internal DS4Device deviceClass { get; }
        internal SettingGroup settings { get; set; }
        internal byte[] outputReportBuffer, outputReport, inputReport, btInputReport;
        internal Thread InputThread;
        internal System.Diagnostics.Stopwatch lastReport = new System.Diagnostics.Stopwatch();

        public DualShock4_Device(HidDevice Device,SettingGroup settings)
        {
            this.settings = settings;

            hDevice = Device;
            this.StatusIcon = hDevice.HidConnectionType() == ConnectionTypes.BT ? new BitmapImage(new Uri("pack://application:,,,/DualShock4;component/Resources/BT3.ico")) : new BitmapImage(new Uri("pack://application:,,,/DualShock4;component/Resources/USB.ico"));
            this.DeviceName = "Dualshock 4 " + iDualShock4_Extensions.IDfromConnectionString(hDevice.DevicePath);
            Console.WriteLine(hDevice.DevicePath);
            DeviceConnectionType = hDevice.HidConnectionType();
            deviceClass = new DS4Device();
            

            if (DeviceConnectionType == ConnectionTypes.USB)
            {
                inputReport = new byte[64];
                outputReport = new byte[hDevice.Capabilities.OutputReportByteLength];

                outputReport[0] = 0x05;
                outputReport[1] = 0xff;
                outputReport[8] = 0xff;
            }
            else
            {
                btInputReport = new byte[547];
                inputReport = new byte[545];
                outputReport = new byte[78];

                outputReport[0] = 0x11;
                outputReport[1] = 0x80;
                outputReport[3] = 0xff;
                outputReport[10] = 0xff;
            }


            InputChannels.Add(deviceClass.Circle);
            InputChannels.Add(deviceClass.Cross);
            InputChannels.Add(deviceClass.Square);
            InputChannels.Add(deviceClass.Triangle);

            InputChannels.Add(deviceClass.LSx);
            InputChannels.Add(deviceClass.LSy);
            InputChannels.Add(deviceClass.RSx);
            InputChannels.Add(deviceClass.RSy);

            InputChannels.Add(deviceClass.L1);
            InputChannels.Add(deviceClass.L2);
            InputChannels.Add(deviceClass.L3);
            InputChannels.Add(deviceClass.R1);
            InputChannels.Add(deviceClass.R2);
            InputChannels.Add(deviceClass.R3);

            InputChannels.Add(deviceClass.PS);
            InputChannels.Add(deviceClass.Share);
            InputChannels.Add(deviceClass.Options);

            InputChannels.Add(deviceClass.DUp);
            InputChannels.Add(deviceClass.DDown);
            InputChannels.Add(deviceClass.DLeft);
            InputChannels.Add(deviceClass.DRight);


            OutputChannels.Add(deviceClass.BigRumble);
            OutputChannels.Add(deviceClass.SmallRumble);
            OutputChannels.Add(deviceClass.LightBar);

            InputChannels.Add(deviceClass.Battery);
            InputChannels.Add(deviceClass.Charging);

            InputChannels.Add(deviceClass.GyroX);
            InputChannels.Add(deviceClass.GyroY);
            InputChannels.Add(deviceClass.GyroZ);

            InputChannels.Add(deviceClass.AccelX);
            InputChannels.Add(deviceClass.AccelY);
            InputChannels.Add(deviceClass.AccelZ);

            InputChannels.Add(deviceClass.TouchpadButton);
            InputChannels.Add(deviceClass.TouchpadTouchOne);
            InputChannels.Add(deviceClass.TouchpadTouchTwo);

            InputChannels.Add(deviceClass.TouchpadTouchOneX);
            InputChannels.Add(deviceClass.TouchpadTouchOneY);
            InputChannels.Add(deviceClass.TouchpadTouchTwoX);
            InputChannels.Add(deviceClass.TouchpadTouchTwoY);

            deviceClass.BigRumble.PropertyChanged += RumbleChanged;
            deviceClass.SmallRumble.PropertyChanged += RumbleChanged;
            deviceClass.LightBar.PropertyChanged += LightbarChanged;

            InputThread = new Thread(InputListener);
            InputThread.Start();
        }

        private void LightbarChanged(object sender, PropertyChangedEventArgs e)
        {
            sendHaptic();
        }

        private void RumbleChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            sendHaptic();
        }

        protected override void Dispose(bool disposing)
        {
            InputThread.Abort();
            hDevice.CancelIO();
            hDevice.CloseDevice();
            hDevice.Dispose();
        }

        private int SafeStickValue(int value)
        {
            value = value > 127 ? 127 : value;
            value = value < -127 ? -127 : value;
            return value;
        }
        private bool isDeviceResponding(HidDevice hDevice)
        {
            //return (hDevice.WriteOutputReportViaControl(GenerateTestReport(hDevice.HidConnectionType())));
            return true;
        }
        const int TOUCHPAD_DATA_OFFSET = 35;
        private byte TouchPacketCounter;
        private bool Touch1;
        private byte Touch1Identifier;
        private bool Touch2;
        private byte Touch2Identifier;

        private void InputListener()
        {

            //if (settings.getSetting("Low latency mode"))
            //    inputReport = new byte[10];

            byte[] accel = new byte[6];
            byte[] gyro = new byte[6];

            Thread.Sleep(500);
            sendHaptic();
            bool isResponding = true;
            lastReport.Start();
            while (isResponding)
            {
                if (!isDeviceResponding(hDevice))
                {
                    Dispose();
                }

                if (DeviceConnectionType == ConnectionTypes.BT)
                {
                    HidDevice.ReadStatus readStatus = hDevice.ReadFile(btInputReport);
                    if (readStatus == HidDevice.ReadStatus.Success)
                    {
                        Array.Copy(btInputReport, 2, inputReport, 0, inputReport.Length);
                    }
                    else
                    {
                        isResponding = false;
                    }
                }
                else if (DeviceConnectionType == ConnectionTypes.USB)
                {
                    HidDevice.ReadStatus readStatus = hDevice.ReadFile(inputReport);
                    if (readStatus != HidDevice.ReadStatus.Success)
                    {
                        isResponding = false;
                    }
                }
                if (isResponding)
                {
                    lastReport.Restart();
                }

                deviceClass.LSx.Value = SafeStickValue(inputReport[1] - 127) / 127f;
                deviceClass.LSy.Value = SafeStickValue(inputReport[2] - 127) / 127f;
                deviceClass.RSx.Value = SafeStickValue(inputReport[3] - 127) / 127f;
                deviceClass.RSy.Value= SafeStickValue(inputReport[4] - 127) / 127f;
                deviceClass.L2.Value = inputReport[8]/255f;
                deviceClass.R2.Value = inputReport[9]/255f;

                deviceClass.Triangle.Value = ((byte)inputReport[5] & (1 << 7)) != 0;
                deviceClass.Circle.Value = ((byte)inputReport[5] & (1 << 6)) != 0;
                deviceClass.Cross.Value = ((byte)inputReport[5] & (1 << 5)) != 0;
                deviceClass.Square.Value = ((byte)inputReport[5] & (1 << 4)) != 0;
                bool RDpadUp = ((byte)inputReport[5] & (1 << 3)) != 0;
                bool RDpadDown = ((byte)inputReport[5] & (1 << 2)) != 0;
                bool RDpadLeft = ((byte)inputReport[5] & (1 << 1)) != 0;
                bool RDpadRight = ((byte)inputReport[5] & (1 << 0)) != 0;

                byte dpad_state = 0;

                dpad_state = (byte)(
                ((RDpadRight ? 1 : 0) << 0) |
                ((RDpadLeft ? 1 : 0) << 1) |
                ((RDpadDown ? 1 : 0) << 2) |
                ((RDpadUp ? 1 : 0) << 3));

                switch (dpad_state)
                {
                    case 0: deviceClass.DUp.Value = true; deviceClass.DDown.Value = false; deviceClass.DLeft.Value = false; deviceClass.DRight.Value = false; break;
                    case 1: deviceClass.DUp.Value = true; deviceClass.DDown.Value = false; deviceClass.DLeft.Value = false; deviceClass.DRight.Value = true; break;
                    case 2: deviceClass.DUp.Value = false; deviceClass.DDown.Value = false; deviceClass.DLeft.Value = false; deviceClass.DRight.Value = true; break;
                    case 3: deviceClass.DUp.Value = false; deviceClass.DDown.Value = true; deviceClass.DLeft.Value = false; deviceClass.DRight.Value = true; break;
                    case 4: deviceClass.DUp.Value = false; deviceClass.DDown.Value = true; deviceClass.DLeft.Value = false; deviceClass.DRight.Value = false; break;
                    case 5: deviceClass.DUp.Value = false; deviceClass.DDown.Value = true; deviceClass.DLeft.Value = true; deviceClass.DRight.Value = false; break;
                    case 6: deviceClass.DUp.Value = false; deviceClass.DDown.Value = false; deviceClass.DLeft.Value = true; deviceClass.DRight.Value = false; break;
                    case 7: deviceClass.DUp.Value = true; deviceClass.DDown.Value = false; deviceClass.DLeft.Value = true; deviceClass.DRight.Value = false; break;
                    case 8: deviceClass.DUp.Value = false; deviceClass.DDown.Value = false; deviceClass.DLeft.Value = false; deviceClass.DRight.Value = false; break;
                }

                deviceClass.R3.Value = ((byte)inputReport[6] & (1 << 7)) != 0;
                deviceClass.L3.Value = ((byte)inputReport[6] & (1 << 6)) != 0;
                deviceClass.Options.Value = ((byte)inputReport[6] & (1 << 5)) != 0;
                deviceClass.Share.Value = ((byte)inputReport[6] & (1 << 4)) != 0;
                deviceClass.R1.Value = ((byte)inputReport[6] & (1 << 1)) != 0;
                deviceClass.L1.Value = ((byte)inputReport[6] & (1 << 0)) != 0;

                deviceClass.PS.Value = ((byte)inputReport[7] & (1 << 0)) != 0;
                deviceClass.TouchpadButton.Value = (inputReport[7] & (1 << 2 - 1)) != 0;

                Array.Copy(inputReport, 14, accel, 0, 6);
                Array.Copy(inputReport, 20, gyro, 0, 6);

                bool charging = (inputReport[30] & 0x10) != 0;
                float battery = (inputReport[30] & 0x0f) / 10;
                if (!charging)
                    battery += 10;
                battery = Math.Max(battery,0f);
                battery = Math.Min(battery, 1f);
                deviceClass.Charging.Value = charging;
                deviceClass.Battery.Value = battery;

                deviceClass.AccelX.Value = (Int16)((UInt16)(accel[0] << 8) | accel[1]) / 65535f;
                deviceClass.AccelY.Value = (Int16)((UInt16)(accel[2] << 8) | accel[3]) / 65535f;
                deviceClass.AccelZ.Value = (Int16)((UInt16)(accel[4] << 8) | accel[5]) / 65535f;

                deviceClass.GyroX.Value = (Int16)((UInt16)(gyro[0] << 8) | gyro[1]) / 65535f;
                deviceClass.GyroY.Value = (Int16)((UInt16)(gyro[2] << 8) | gyro[3]) / 65535f;
                deviceClass.GyroZ.Value = (Int16)((UInt16)(gyro[4] << 8) | gyro[5]) / 65535f;

                for (int touches = inputReport[-1 + TOUCHPAD_DATA_OFFSET - 1], touchOffset = 0; touches > 0; touches--, touchOffset += 9)
                {
                    //TouchPacketCounter = inputReport[-1 + TOUCHPAD_DATA_OFFSET + touchOffset];
                    deviceClass.TouchpadTouchOne.Value = (inputReport[0 + TOUCHPAD_DATA_OFFSET + touchOffset] >> 7) != 0 ? false : true; // >= 1 touch detected

                    //Touch1Identifier = (byte)(inputReport[0 + TOUCHPAD_DATA_OFFSET + touchOffset] & 0x7f);
                    deviceClass.TouchpadTouchTwo.Value = (inputReport[4 + TOUCHPAD_DATA_OFFSET + touchOffset] >> 7) != 0 ? false : true; // 2 touches detected
                    //Touch2Identifier = (byte)(inputReport[4 + TOUCHPAD_DATA_OFFSET + touchOffset] & 0x7f);

                    byte touchID1 = (byte)(inputReport[0 + TOUCHPAD_DATA_OFFSET + touchOffset] & 0x7F);
                    byte touchID2 = (byte)(inputReport[4 + TOUCHPAD_DATA_OFFSET + touchOffset] & 0x7F);
                    deviceClass.TouchpadTouchOneX.Value = (inputReport[1 + TOUCHPAD_DATA_OFFSET + touchOffset] + ((inputReport[2 + TOUCHPAD_DATA_OFFSET + touchOffset] & 0xF) * 255))/ 1920f;
                    deviceClass.TouchpadTouchOneY.Value = (((inputReport[2 + TOUCHPAD_DATA_OFFSET + touchOffset] & 0xF0) >> 4) + (inputReport[3 + TOUCHPAD_DATA_OFFSET + touchOffset] * 16))/ 950f;
                    deviceClass.TouchpadTouchTwoX.Value = (inputReport[5 + TOUCHPAD_DATA_OFFSET + touchOffset] + ((inputReport[6 + TOUCHPAD_DATA_OFFSET + touchOffset] & 0xF) * 255))/ 1920f;
                    deviceClass.TouchpadTouchTwoY.Value = (((inputReport[6 + TOUCHPAD_DATA_OFFSET + touchOffset] & 0xF0) >> 4) + (inputReport[7 + TOUCHPAD_DATA_OFFSET + touchOffset] * 16))/ 950f;

                    //if (cState.Touch1)
                    //{
                    //    cState.TouchLeft = (inputReport[1 + TOUCHPAD_DATA_OFFSET + touchOffset] + ((inputReport[2 + TOUCHPAD_DATA_OFFSET + touchOffset] & 0xF) * 255) >= 1920 * 2 / 5) ? false : true;
                    //    cState.TouchRight = (inputReport[1 + TOUCHPAD_DATA_OFFSET + touchOffset] + ((inputReport[2 + TOUCHPAD_DATA_OFFSET + touchOffset] & 0xF) * 255) < 1920 * 2 / 5) ? false : true;
                    //}
                    // Even when idling there is still a touch packet indicating no touch 1 or 2
                    //touchpad.handleTouchpad(inputReport, cState, touchOffset);
                }



                //Console.WriteLine((((byte)255 << 8) | (byte)255));
            }
            Dispose();
        }
        public void sendHaptic()
        {
            if (DeviceConnectionType == ConnectionTypes.BT)
            {
                outputReport[6] = (byte)Math.Round(deviceClass.SmallRumble.Value * 255f);
                outputReport[7] = (byte)Math.Round(deviceClass.BigRumble.Value * 255f);
                outputReport[8] = (byte)Math.Round((deviceClass.LightBar.Value as ColorRGBA).R * 255f); //red
                outputReport[9] = (byte)Math.Round((deviceClass.LightBar.Value as ColorRGBA).G * 255f); //green
                outputReport[10] = (byte)Math.Round((deviceClass.LightBar.Value as ColorRGBA).B * 255f); //blue
            }
            else if (DeviceConnectionType == ConnectionTypes.USB)
            {
                outputReport[4] = (byte)Math.Round(deviceClass.SmallRumble.Value * 255f);
                outputReport[5] = (byte)Math.Round(deviceClass.BigRumble.Value * 255f);
                outputReport[6] = (byte)Math.Round((deviceClass.LightBar.Value as ColorRGBA).R * 255f); //red
                outputReport[7] = (byte)Math.Round((deviceClass.LightBar.Value as ColorRGBA).G * 255f); //green
                outputReport[8] = (byte)Math.Round((deviceClass.LightBar.Value as ColorRGBA).B * 255f); //blue
            }

            hDevice.WriteOutputReportViaControl(outputReport);
        }
    }
    
    public static class iDualShock4_Extensions
    {
        public static string IDfromConnectionString(string connectionString)
        {
            return System.Text.RegularExpressions.Regex.Match(connectionString, @".+05c4#..([a-f0-9]+)&[0-9]&0000#", System.Text.RegularExpressions.RegexOptions.None).Groups[1].Value;
        }
    }
}
