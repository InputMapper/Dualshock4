using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ODIF;

namespace DualShock4
{
    public class HidDevice : IDisposable
    {
        public enum ReadStatus
        {
            Success = 0,
            WaitTimedOut = 1,
            WaitFail = 2,
            NoDataRead = 3,
            ReadError = 4,
            NotConnected = 5
        }

        private readonly string _description;
        private readonly string _devicePath;
        private readonly HidDeviceAttributes _deviceAttributes;

        private readonly HidDeviceCapabilities _deviceCapabilities;
        private bool _monitorDeviceEvents;
        private string serial = null;
        internal HidDevice(string devicePath, string description = null)
        {
            _devicePath = devicePath;
            _description = description;

            try
            {
                var hidHandle = OpenHandle(_devicePath, false);

                _deviceAttributes = GetDeviceAttributes(hidHandle);
                _deviceCapabilities = GetDeviceCapabilities(hidHandle);

                hidHandle.Close();
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.TraceError(exception.Message);
                throw new Exception(string.Format("Error querying HID device '{0}'.", devicePath), exception);

            }
        }

        public SafeFileHandle safeReadHandle { get; private set; }
        public FileStream fileStream { get; private set; }
        public bool IsOpen { get; private set; }
        public bool IsExclusive { get; private set; }
        public bool IsConnected { get { return HidDevices.IsConnected(_devicePath); } }
        public string Description { get { return _description; } }
        public HidDeviceCapabilities Capabilities { get { return _deviceCapabilities; } }
        public HidDeviceAttributes Attributes { get { return _deviceAttributes; } }
        public string DevicePath { get { return _devicePath; } }

        public override string ToString()
        {
            return string.Format("VendorID={0}, ProductID={1}, Version={2}, DevicePath={3}",
                                _deviceAttributes.VendorHexId,
                                _deviceAttributes.ProductHexId,
                                _deviceAttributes.Version,
                                _devicePath);
        }

        public void OpenDevice(bool isExclusive)
        {
            if (IsOpen) return;
            try
            {
                if (safeReadHandle == null || safeReadHandle.IsInvalid)
                    safeReadHandle = OpenHandle(_devicePath, isExclusive);
            }
            catch (Exception exception)
            {
                IsOpen = false;
                throw new Exception("Error opening HID device.", exception);
            }

            IsOpen = !safeReadHandle.IsInvalid;
            IsExclusive = isExclusive;
        }

        public void CloseDevice()
        {
            if (!IsOpen) return;
            closeFileStreamIO();

            IsOpen = false;
        }

        public void Dispose()
        {
            CancelIO();
            CloseDevice();
        }

        public void CancelIO()
        {
            if (IsOpen)
                NativeMethods.CancelIoEx(safeReadHandle.DangerousGetHandle(), IntPtr.Zero);
        }

        public bool ReadInputReport(byte[] data)
        {
            if (safeReadHandle == null)
                safeReadHandle = OpenHandle(_devicePath, true);
            return NativeMethods.HidD_GetInputReport(safeReadHandle, data, data.Length);
        }

        private static HidDeviceAttributes GetDeviceAttributes(SafeFileHandle hidHandle)
        {
            var deviceAttributes = default(NativeMethods.HIDD_ATTRIBUTES);
            deviceAttributes.Size = Marshal.SizeOf(deviceAttributes);
            NativeMethods.HidD_GetAttributes(hidHandle.DangerousGetHandle(), ref deviceAttributes);
            return new HidDeviceAttributes(deviceAttributes);
        }

        private static HidDeviceCapabilities GetDeviceCapabilities(SafeFileHandle hidHandle)
        {
            var capabilities = default(NativeMethods.HIDP_CAPS);
            var preparsedDataPointer = default(IntPtr);

            if (NativeMethods.HidD_GetPreparsedData(hidHandle.DangerousGetHandle(), ref preparsedDataPointer))
            {
                NativeMethods.HidP_GetCaps(preparsedDataPointer, ref capabilities);
                NativeMethods.HidD_FreePreparsedData(preparsedDataPointer);
            }
            return new HidDeviceCapabilities(capabilities);
        }

        private void closeFileStreamIO()
        {
            if (fileStream != null)
                fileStream.Close();
            fileStream = null;
            System.Diagnostics.Trace.WriteLine("Close device filestream");
            if (safeReadHandle != null && !safeReadHandle.IsInvalid)
            {
                safeReadHandle.Close();
                System.Diagnostics.Trace.WriteLine("Close device handle");

            }
            safeReadHandle = null;
            System.Diagnostics.Trace.WriteLine("IO closed");
        }

        public void flush_Queue()
        {
            if (safeReadHandle != null)
            {
                NativeMethods.HidD_FlushQueue(safeReadHandle);
            }
        }

        private ReadStatus ReadWithFileStreamTask(byte[] inputBuffer)
        {
            try
            {
                if (fileStream.Read(inputBuffer, 0, inputBuffer.Length) > 0)
                {
                    return ReadStatus.Success;
                }
                else
                {
                    return ReadStatus.NoDataRead;
                }
            }
            catch (Exception)
            {
                return ReadStatus.ReadError;
            }
        }
        public ReadStatus ReadFile(byte[] inputBuffer)
        {
            if (safeReadHandle == null)
                safeReadHandle = OpenHandle(_devicePath, true);
            try
            {
                uint bytesRead;
                if (NativeMethods.ReadFile(safeReadHandle.DangerousGetHandle(), inputBuffer, (uint)inputBuffer.Length, out bytesRead, IntPtr.Zero))
                {
                    return ReadStatus.Success;
                }
                else
                {
                    return ReadStatus.NoDataRead;
                }
            }
            catch (Exception)
            {
                return ReadStatus.ReadError;
            }
        }
        protected Byte m_IntIn = 0xFF;
        protected Byte m_IntOut = 0xFF;
        protected Byte m_BulkIn = 0xFF;
        protected Byte m_BulkOut = 0xFF;
        public virtual Boolean ReadIntPipe(Byte[] Buffer, Int32 Length, ref Int32 Transfered)
        {
            try { return NativeMethods.WinUsb_ReadPipe(safeReadHandle.DangerousGetHandle(), m_IntIn, Buffer, Length, ref Transfered, IntPtr.Zero); }
            catch { return false; }
        }

        public ReadStatus ReadWithFileStream(byte[] inputBuffer, int timeout)
        {
            try
            {
                if (safeReadHandle == null)
                    safeReadHandle = OpenHandle(_devicePath, true);
                if (fileStream == null && !safeReadHandle.IsInvalid)
                    fileStream = new FileStream(safeReadHandle, FileAccess.ReadWrite, inputBuffer.Length, false);
                if (!safeReadHandle.IsInvalid && fileStream.CanRead)
                {

                    Task<ReadStatus> readFileTask = new Task<ReadStatus>(() => ReadWithFileStreamTask(inputBuffer));
                    readFileTask.Start();
                    bool success = readFileTask.Wait(timeout);
                    if (success)
                    {
                        if (readFileTask.Result == ReadStatus.Success)
                        {
                            return ReadStatus.Success;
                        }
                        else if (readFileTask.Result == ReadStatus.ReadError)
                        {
                            return ReadStatus.ReadError;
                        }
                        else if (readFileTask.Result == ReadStatus.NoDataRead)
                        {
                            return ReadStatus.NoDataRead;
                        }
                    }
                    else
                        return ReadStatus.WaitTimedOut;
                }

            }
            catch (Exception e)
            {
                if (e is AggregateException)
                {
                    System.Diagnostics.Trace.TraceError(e.Message);
                    return ReadStatus.WaitFail;
                }
                else
                {
                    return ReadStatus.ReadError;
                }
            }




            return ReadStatus.ReadError;
        }

        public bool WriteOutputReportViaControl(byte[] outputBuffer)
        {
            try
            {
                //if (safeReadHandle == null)
                //{
                //    safeReadHandle = OpenHandle(_devicePath, true);
                //}

                if (NativeMethods.HidD_SetOutputReport(safeReadHandle, outputBuffer, outputBuffer.Length))
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        private bool WriteOutputReportViaInterruptTask(byte[] outputBuffer)
        {
            try
            {
                fileStream.Write(outputBuffer, 0, outputBuffer.Length);
                return true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError(e.Message);
                return false;
            }
        }

        public bool WriteOutputReportViaInterrupt(byte[] outputBuffer, int timeout)
        {
            try
            {
                if (safeReadHandle == null)
                {
                    safeReadHandle = OpenHandle(_devicePath, true);
                }
                if (fileStream == null && !safeReadHandle.IsInvalid)
                {
                    fileStream = new FileStream(safeReadHandle, FileAccess.ReadWrite, outputBuffer.Length, false);
                }
                if (fileStream != null && fileStream.CanWrite && !safeReadHandle.IsInvalid)
                {
                    fileStream.Write(outputBuffer, 0, outputBuffer.Length);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

        }

        private SafeFileHandle OpenHandle(String devicePathName, Boolean isExclusive)
        {
            SafeFileHandle hidHandle;

            try
            {
                if (isExclusive)
                {
                    hidHandle = NativeMethods.CreateFile(devicePathName, NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE, 0, IntPtr.Zero, NativeMethods.OpenExisting, 0, 0);
                }
                else
                {
                    hidHandle = NativeMethods.CreateFile(devicePathName, NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE, NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE, IntPtr.Zero, NativeMethods.OpenExisting, 0, 0);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return hidHandle;
        }

        public bool readFeatureData(byte[] inputBuffer)
        {
            return NativeMethods.HidD_GetFeature(safeReadHandle.DangerousGetHandle(), inputBuffer, inputBuffer.Length);
        }

        public string readSerial()
        {

            try
            {
                if (serial != null)
                    return serial;

                if (Capabilities.InputReportByteLength == 64)
                {
                    byte[] buffer = new byte[16];
                    buffer[0] = 18;
                    readFeatureData(buffer);
                    serial = String.Format("{0:X02}:{1:X02}:{2:X02}:{3:X02}:{4:X02}:{5:X02}", buffer[6], buffer[5], buffer[4], buffer[3], buffer[2], buffer[1]);
                    return serial;

                }
                else
                {

                    byte[] buffer = new byte[126];
                    if (NativeMethods.HidD_GetSerialNumberString(safeReadHandle.DangerousGetHandle(), buffer, (uint)buffer.Length))
                    {
                        string MACAddr = System.Text.Encoding.Unicode.GetString(buffer).Replace("\0", string.Empty).ToUpper();
                        MACAddr = String.Format("{0}{1}:{2}{3}:{4}{5}:{6}{7}:{8}{9}:{10}{11}",
                            MACAddr[0], MACAddr[1], MACAddr[2], MACAddr[3], MACAddr[4],
                            MACAddr[5], MACAddr[6], MACAddr[7], MACAddr[8],
                            MACAddr[9], MACAddr[10], MACAddr[11]);
                        serial = MACAddr;
                        return serial;
                    }
                    else
                    {
                        System.Diagnostics.Trace.TraceWarning("The bluetooth adapter or bluetooth drivers being used may not be compatible with this software.");

                        string FakeMAC;
                        using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                        {
                            FakeMAC = BitConverter.ToString(
                              md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(DevicePath))
                            ).Replace("-", String.Empty);
                        }
                        FakeMAC = String.Format("99:{0}{1}:{2}{3}:{4}{5}:{6}{7}:{8}{9}",
                            FakeMAC[0], FakeMAC[1], FakeMAC[2], FakeMAC[3], FakeMAC[4],
                            FakeMAC[5], FakeMAC[6], FakeMAC[7], FakeMAC[8], FakeMAC[9]);
                        return FakeMAC;
                    }
                }
            }
            catch (Exception err)
            {
                if (err.GetType() == typeof(IndexOutOfRangeException))
                {
                    System.Diagnostics.Trace.TraceWarning("The bluetooth adapter or bluetooth drivers being used may not be compatible with this software.");

                    string FakeMAC;
                    using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                    {
                        FakeMAC = BitConverter.ToString(
                          md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(DevicePath))
                        ).Replace("-", String.Empty);
                    }
                    FakeMAC = String.Format("99:{0}{1}:{2}{3}:{4}{5}:{6}{7}:{8}{9}",
                        FakeMAC[0], FakeMAC[1], FakeMAC[2], FakeMAC[3], FakeMAC[4],
                        FakeMAC[5], FakeMAC[6], FakeMAC[7], FakeMAC[8], FakeMAC[9]);
                    return FakeMAC;

                }
            }
            System.Diagnostics.Trace.TraceWarning("The bluetooth adapter being used may not be compatible with this software.");
            return serial;


        }
    }
    public class HidDevices
    {
        private static Guid _hidClassGuid = Guid.Empty;

        public static bool IsConnected(string devicePath)
        {
            return EnumerateDevices().Any(x => x.Path == devicePath);
        }

        public static HidDevice GetDevice(string devicePath)
        {
            return Enumerate(devicePath).FirstOrDefault();
        }

        public static IEnumerable<HidDevice> Enumerate()
        {
            return EnumerateDevices().Select(x => new HidDevice(x.Path, x.Description));
        }

        public static IEnumerable<HidDevice> Enumerate(string devicePath)
        {
            return EnumerateDevices().Where(x => x.Path == devicePath).Select(x => new HidDevice(x.Path, x.Description));
        }

        public static IEnumerable<HidDevice> Enumerate(Guid deviceGUID)
        {
            return EnumerateDevices().Where(x => x.Path.Contains(deviceGUID.ToString())).Select(x => new HidDevice(x.Path, x.Description));
        }

        public static IEnumerable<HidDevice> Enumerate(int vendorId, params int[] productIds)
        {
            return EnumerateDevices().Select(x => new HidDevice(x.Path, x.Description)).Where(x => x.Attributes.VendorId == vendorId &&
                                                                                  productIds.Contains(x.Attributes.ProductId) );
            
        }

        public static IEnumerable<HidDevice> Enumerate(int vendorId)
        {
            return EnumerateDevices().Select(x => new HidDevice(x.Path, x.Description)).Where(x => x.Attributes.VendorId == vendorId);
        }

        private class DeviceInfo { public string Path { get; set; } public string Description { get; set; } }

        private static IEnumerable<DeviceInfo> EnumerateDevices()
        {
            var devices = new List<DeviceInfo>();
            var hidClass = HidClassGuid;
            var deviceInfoSet = NativeMethods.SetupDiGetClassDevs(ref hidClass, null, 0, NativeMethods.DIGCF_PRESENT | NativeMethods.DIGCF_DEVICEINTERFACE);

            if (deviceInfoSet.ToInt64() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                var deviceInfoData = CreateDeviceInfoData();
                var deviceIndex = 0;

                while (NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex, ref deviceInfoData))
                {
                    deviceIndex += 1;

                    var deviceInterfaceData = new NativeMethods.SP_DEVICE_INTERFACE_DATA();
                    deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);
                    var deviceInterfaceIndex = 0;

                    while (NativeMethods.SetupDiEnumDeviceInterfaces(deviceInfoSet, ref deviceInfoData, ref hidClass, deviceInterfaceIndex, ref deviceInterfaceData))
                    {
                        deviceInterfaceIndex++;
                        var devicePath = GetDevicePath(deviceInfoSet, deviceInterfaceData);
                        var description = GetBusReportedDeviceDescription(deviceInfoSet, ref deviceInfoData) ??
                                          GetDeviceDescription(deviceInfoSet, ref deviceInfoData);
                        devices.Add(new DeviceInfo { Path = devicePath, Description = description });
                    }
                }
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
            return devices;
        }

        private static NativeMethods.SP_DEVINFO_DATA CreateDeviceInfoData()
        {
            var deviceInfoData = new NativeMethods.SP_DEVINFO_DATA();

            deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);
            deviceInfoData.DevInst = 0;
            deviceInfoData.ClassGuid = Guid.Empty;
            deviceInfoData.Reserved = IntPtr.Zero;

            return deviceInfoData;
        }

        private static string GetDevicePath(IntPtr deviceInfoSet, NativeMethods.SP_DEVICE_INTERFACE_DATA deviceInterfaceData)
        {
            var bufferSize = 0;
            var interfaceDetail = new NativeMethods.SP_DEVICE_INTERFACE_DETAIL_DATA { Size = IntPtr.Size == 4 ? 4 + Marshal.SystemDefaultCharSize : 8 };

            NativeMethods.SetupDiGetDeviceInterfaceDetailBuffer(deviceInfoSet, ref deviceInterfaceData, IntPtr.Zero, 0, ref bufferSize, IntPtr.Zero);

            return NativeMethods.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, ref interfaceDetail, bufferSize, ref bufferSize, IntPtr.Zero) ?
                interfaceDetail.DevicePath : null;
        }

        private static Guid HidClassGuid
        {
            get
            {
                if (_hidClassGuid.Equals(Guid.Empty)) NativeMethods.HidD_GetHidGuid(ref _hidClassGuid);
                return _hidClassGuid;
            }
        }

        private static string GetDeviceDescription(IntPtr deviceInfoSet, ref NativeMethods.SP_DEVINFO_DATA devinfoData)
        {
            var descriptionBuffer = new byte[1024];

            var requiredSize = 0;
            var type = 0;

            NativeMethods.SetupDiGetDeviceRegistryProperty(deviceInfoSet,
                                                            ref devinfoData,
                                                            NativeMethods.SPDRP_DEVICEDESC,
                                                            ref type,
                                                            descriptionBuffer,
                                                            descriptionBuffer.Length,
                                                            ref requiredSize);

            return descriptionBuffer.ToUTF8String();
        }

        private static string GetBusReportedDeviceDescription(IntPtr deviceInfoSet, ref NativeMethods.SP_DEVINFO_DATA devinfoData)
        {
            var descriptionBuffer = new byte[1024];

            if (Environment.OSVersion.Version.Major > 5)
            {
                ulong propertyType = 0;
                var requiredSize = 0;

                var _continue = NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet,
                                                                        ref devinfoData,
                                                                        ref NativeMethods.DEVPKEY_Device_BusReportedDeviceDesc,
                                                                        ref propertyType,
                                                                        descriptionBuffer,
                                                                        descriptionBuffer.Length,
                                                                        ref requiredSize,
                                                                        0);

                if (_continue) return descriptionBuffer.ToUTF16String();
            }
            return null;
        }
    }
    public class HidDeviceAttributes
    {
        internal HidDeviceAttributes(NativeMethods.HIDD_ATTRIBUTES attributes)
        {
            VendorId = attributes.VendorID;
            ProductId = attributes.ProductID;
            Version = attributes.VersionNumber;

            VendorHexId = "0x" + attributes.VendorID.ToString("X4");
            ProductHexId = "0x" + attributes.ProductID.ToString("X4");
        }

        public int VendorId { get; private set; }
        public int ProductId { get; private set; }
        public int Version { get; private set; }
        public string VendorHexId { get; set; }
        public string ProductHexId { get; set; }
    }
    public class HidDeviceCapabilities
    {
        internal HidDeviceCapabilities(NativeMethods.HIDP_CAPS capabilities)
        {
            Usage = capabilities.Usage;
            UsagePage = capabilities.UsagePage;
            InputReportByteLength = capabilities.InputReportByteLength;
            OutputReportByteLength = capabilities.OutputReportByteLength;
            FeatureReportByteLength = capabilities.FeatureReportByteLength;
            Reserved = capabilities.Reserved;
            NumberLinkCollectionNodes = capabilities.NumberLinkCollectionNodes;
            NumberInputButtonCaps = capabilities.NumberInputButtonCaps;
            NumberInputValueCaps = capabilities.NumberInputValueCaps;
            NumberInputDataIndices = capabilities.NumberInputDataIndices;
            NumberOutputButtonCaps = capabilities.NumberOutputButtonCaps;
            NumberOutputValueCaps = capabilities.NumberOutputValueCaps;
            NumberOutputDataIndices = capabilities.NumberOutputDataIndices;
            NumberFeatureButtonCaps = capabilities.NumberFeatureButtonCaps;
            NumberFeatureValueCaps = capabilities.NumberFeatureValueCaps;
            NumberFeatureDataIndices = capabilities.NumberFeatureDataIndices;

        }

        public short Usage { get; private set; }
        public short UsagePage { get; private set; }
        public short InputReportByteLength { get; private set; }
        public short OutputReportByteLength { get; private set; }
        public short FeatureReportByteLength { get; private set; }
        public short[] Reserved { get; private set; }
        public short NumberLinkCollectionNodes { get; private set; }
        public short NumberInputButtonCaps { get; private set; }
        public short NumberInputValueCaps { get; private set; }
        public short NumberInputDataIndices { get; private set; }
        public short NumberOutputButtonCaps { get; private set; }
        public short NumberOutputValueCaps { get; private set; }
        public short NumberOutputDataIndices { get; private set; }
        public short NumberFeatureButtonCaps { get; private set; }
        public short NumberFeatureValueCaps { get; private set; }
        public short NumberFeatureDataIndices { get; private set; }
    }
    static class Extensions
    {
        public static string ToUTF8String(this byte[] buffer)
        {
            var value = Encoding.UTF8.GetString(buffer);
            return value.Remove(value.IndexOf((char)0));
        }

        public static string ToUTF16String(this byte[] buffer)
        {
            var value = Encoding.Unicode.GetString(buffer);
            return value.Remove(value.IndexOf((char)0));
        }

        public static ConnectionTypes HidConnectionType(this HidDevice hidDevice)
        {
            return hidDevice.Capabilities.InputReportByteLength == 64 ? ConnectionTypes.USB : ConnectionTypes.BT;
        }
    }
}
