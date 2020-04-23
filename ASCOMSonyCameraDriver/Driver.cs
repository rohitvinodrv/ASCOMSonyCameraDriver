//tabs=4
// --------------------------------------------------------------------------------
//
// ASCOM Camera driver for Sony Mirrorless Camera
//
// Description:	Implements ASCOM driver for Sony Mirrorless camera.
//				Communicates using USB connection.
//
// Implements:	ASCOM Camera interface version: 2
// Author:		(2019) Doug Henderson <dougforpres@hotmail.com>
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	-------------------------------------------------------
// 10-Dec-2019	XXX	6.0.0	Initial edit, created from ASCOM driver template
// --------------------------------------------------------------------------------
//
#define Camera

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;
using Microsoft.Win32;

namespace ASCOM.SonyMirrorless
{
    //
    // Your driver's DeviceID is ASCOM.SonyMirrorless.Camera
    //
    // The Guid attribute sets the CLSID for ASCOM.SonyMirrorless.Camera

    // TODO Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM Camera Driver for Sony Mirrorless Camera.
    /// </summary>
    [Guid("33a95fe9-6775-4291-a16c-d6a2f5e4812c")]
    [ClassInterface(ClassInterfaceType.None)]
    public class Camera : ICameraV2
    {
        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        internal static string driverID = "ASCOM.SonyMirrorless.Camera";

        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        private readonly static string driverDescription = "Sony Mirrorless Camera";

        internal static string traceStateProfileName = "Trace Level";
        internal static string traceStateDefault = "false";
        internal static string cameraProfileName = "Camera ID";
        internal static string cameraDefault = "";
        internal static string readoutModeDefaultProfileName = "Readout Mode";
        internal static string readoutModeDefault = "0";
        internal static string saveRawImageProfileName = "Save Raw Image";
        internal static string saveRawImageDefault = "false";
        internal static string useLiveviewProfileName = "Use Camera Liveview";
        internal static string useLiveviewDefault = "true";
        internal static string personalityProfileName = "Personality";
        internal static string personalityDefault = "1";

        internal static string deviceId; // Variables to hold the currrent device configuration

        /// <summary>
        /// Private variable to hold the camera object
        /// </summary>
        internal static SonyCamera camera = null;

        /// <summary>
        /// Private variable to hold an ASCOM Utilities object
        /// </summary>
        private Util utilities;

        /// <summary>
        /// Private variable to hold an ASCOM AstroUtilities object to provide the Range method
        /// </summary>
        private AstroUtils astroUtilities;

        /// <summary>
        /// Variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
        /// </summary>
        internal static TraceLogger tl;

        internal static short defaultReadoutMode = SonyCommon.OUTPUTFORMAT_RGB;
        internal static bool SaveRawImageData = false;
        internal static string SaveRawImageFolder = "";
        internal static bool UseLiveview = false;
        internal static int Personality = SonyCommon.PERSONALITY_APT;

        /// <summary>
        /// Initializes a new instance of the <see cref="a6400"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public Camera()
        {
            tl = new TraceLogger("", "SonyMirrorless");
            ReadProfile(); // Read device configuration from the ASCOM Profile store

            tl.LogMessage("Camera", "Starting initialisation");

            utilities = new Util(); //Initialise util object
            astroUtilities = new AstroUtils(); // Initialise astro utilities object

            tl.LogMessage("Camera", "Completed initialisation");
        }

        //
        // PUBLIC COM INTERFACE ICameraV2 IMPLEMENTATION
        //

        #region Common properties and methods.

        /// <summary>
        /// Displays the Setup Dialog form.
        /// If the user clicks the OK button to dismiss the form, then
        /// the new settings are saved, otherwise the old values are reloaded.
        /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
        /// </summary>
        public void SetupDialog()
        {
            // consider only showing the setup dialog if not connected
            // or call a different dialog if connected
            if (IsConnected)
                System.Windows.Forms.MessageBox.Show("Camera is currently connected.  Some options are only available when not connected, these will be disabled.");

            using (SetupDialogForm F = new SetupDialogForm())
            {
                var result = F.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    WriteProfile(); // Persist device configuration values to the ASCOM Profile store
                }
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                tl.LogMessage("SupportedActions Get", "Returning empty arraylist");
                return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            LogMessage("", "Action {0}, parameters {1} not implemented", actionName, actionParameters);
            throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            // Call CommandString and return as soon as it finishes
            this.CommandString(command, raw);
            // or
            throw new ASCOM.MethodNotImplementedException("CommandBlind");
            // DO NOT have both these sections!  One or the other
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            string ret = CommandString(command, raw);
            // TODO decode the return string and return true or false
            // or
            throw new ASCOM.MethodNotImplementedException("CommandBool");
            // DO NOT have both these sections!  One or the other
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            // it's a good idea to put all the low level communication with the device here,
            // then all communication calls this function
            // you need something to ensure that only one command is in progress at a time

            throw new ASCOM.MethodNotImplementedException("CommandString");
        }

        public void Dispose()
        {
            // Clean up the tracelogger and util objects
            if (camera != null)
            {
                Connected = false;
                camera = null;
            }

            tl.Enabled = false;
            tl.Dispose();
            tl = null;
            utilities.Dispose();
            utilities = null;
            astroUtilities.Dispose();
            astroUtilities = null;
        }

        public bool Connected
        {
            get
            {
                if (camera != null)
                {
                    LogMessage("Connected", "Get {0}", camera.Connected);

                    return camera.Connected;
                }
                else
                {
                    LogMessage("Camera not yet specified/created", "");

                    return false;
                }
            }
            set
            {
                if (value && camera == null)
                {
                    LogMessage("connected", "Camera is NULL", "");
                    LogMessage("connected", "deviceId = '{0}'", deviceId);

                    // See if we can create a camera using deviceId
                    if (deviceId == "")
                    {
                        SetupDialog();
                    }

                    if (deviceId != "")
                    {
                        SonyCameraEnumerator enumerator = new SonyCameraEnumerator();

                        foreach (SonyCamera candidate in enumerator.Cameras)
                        {
                            if (camera == null && candidate.DisplayName == deviceId)
                            {
                                LogMessage("connected", "Found info... creating camera obj", "");
                                camera = candidate;
                                cameraNumX = (int)camera.Info.ImageWidthPixels;
                                cameraNumY = (int)camera.Info.ImageHeightPixels;

                                switch (defaultReadoutMode)
                                {
                                    case 0:
                                        camera.OutputMode = SonyCamera.ImageMode.RGB;
                                        break;

                                    case 1:
                                        camera.OutputMode = SonyCamera.ImageMode.RGGB;
                                        break;
                                }
                            }
                        }
                    }
                }

                if (camera != null)
                {
                    tl.LogMessage("Connected", "Set {0}", value);

                    if (value == camera.Connected)
                        return;

                    if (value)
                    {
                        LogMessage("Connected Set", "Connecting to camera {0}", deviceId);
                        camera.Connected = true;
                    }
                    else
                    {
                        LogMessage("Connected Set", "Disconnecting from camera {0}", deviceId);
                        camera.Connected = false;

                        // Trash the camera in the event the driver id changes
                        camera = null;
                    }
                }
                else
                {
                    tl.LogMessage("Connected Set", "Camera not yet specified");
                }
            }
        }

        public string Description
        {
            // TODO customise this device description
            get
            {
                tl.LogMessage("Description Get", driverDescription);
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                // TODO customise this driver description
                string driverInfo = "Sony Camera Driver. For support, email <retrodotkiwi@gmail.com>. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                LogMessage("InterfaceVersion Get", "2");
                return Convert.ToInt16("2");
            }
        }

        public string Name
        {
            get
            {
                // IMPORTANT
                // This string cannot change, the APT software recognizes this name specifically and enables fast-readout
                // for preview mode.
                // "Sony Mirrorless Camera"
                string name = "Sony Mirrorless Camera";
                tl.LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region ICamera Implementation

        private const int default_ccdWidth = 6024; // Constants to define the ccd pixel dimenstions
        private const int default_ccdHeight = 4024;

        private int cameraNumX = default_ccdWidth; // Initialise variables to hold values required for functionality tested by Conform
        private int cameraNumY = default_ccdHeight;
        private int cameraStartX = 0;
        private int cameraStartY = 0;

        public void AbortExposure()
        {
            tl.LogMessage("AbortExposure", "Attempting to cancel any in-progress capture");

            camera.StopCapture();
        }

        public short BayerOffsetX
        {
            get
            {
                tl.LogMessage("BayerOffsetX Get Get", "");

                return (short)camera.Info.BayerXOffset;
            }
        }

        public short BayerOffsetY
        {
            get
            {
                tl.LogMessage("BayerOffsetY Get Get", "");

                return (short)camera.Info.BayerYOffset;
            }
        }

        public short BinX
        {
            get
            {
                tl.LogMessage("BinX Get", "1");
                return 1;
            }
            set
            {
                tl.LogMessage("BinX Set", value.ToString());
                if (value != 1) throw new ASCOM.InvalidValueException("BinX", value.ToString(), "1"); // Only 1 is valid in this simple template
            }
        }

        public short BinY
        {
            get
            {
                tl.LogMessage("BinY Get", "1");
                return 1;
            }
            set
            {
                tl.LogMessage("BinY Set", value.ToString());
                if (value != 1) throw new ASCOM.InvalidValueException("BinY", value.ToString(), "1"); // Only 1 is valid in this simple template
            }
        }

        public double CCDTemperature
        {
            get
            {
                tl.LogMessage("CCDTemperature Get Get", "Not implemented");
                throw new ASCOM.InvalidValueException("CCDTemperature Not Available");// PropertyNotImplementedException("CCDTemperature", false);
            }
        }

        public CameraStates CameraState
        {
            get
            {
                tl.LogMessage("CameraState Get", camera.State.ToString());

                return camera.State;
            }
        }

        public int CameraXSize
        {
            get
            {
                CheckConnected("Camera must be connected to read CameraXSize");

                tl.LogMessage("CameraXSize Get", camera.Info.ImageWidthPixels.ToString());
                return (int)camera.Info.ImageWidthPixels;
            }
        }

        public int CameraYSize
        {
            get
            {
                CheckConnected("Camera must be connected to read CameraYSize");

                tl.LogMessage("CameraYSize Get", camera.Info.ImageHeightPixels.ToString());
                return (int)camera.Info.ImageHeightPixels;
            }
        }

        public bool CanAbortExposure
        {
            get
            {
                tl.LogMessage("CanAbortExposure Get", true.ToString());
                return true;
            }
        }

        public bool CanAsymmetricBin
        {
            get
            {
                tl.LogMessage("CanAsymmetricBin Get", false.ToString());
                return false;
            }
        }

        public bool CanFastReadout
        {
            get
            {
                bool result = Personality != SonyCommon.PERSONALITY_NINA;

                tl.LogMessage("CanFastReadout Get", result.ToString());
                return result;
            }
        }

        public bool CanGetCoolerPower
        {
            get
            {
                tl.LogMessage("CanGetCoolerPower Get", false.ToString());
                return false;
            }
        }

        public bool CanPulseGuide
        {
            get
            {
                tl.LogMessage("CanPulseGuide Get", false.ToString());
                return false;
            }
        }

        public bool CanSetCCDTemperature
        {
            get
            {
                tl.LogMessage("CanSetCCDTemperature Get", false.ToString());
                return false;
            }
        }

        public bool CanStopExposure
        {
            get
            {
                tl.LogMessage("CanStopExposure Get", true.ToString());
                return true;
            }
        }

        public bool CoolerOn
        {
            get
            {
                tl.LogMessage("CoolerOn Get Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("CoolerOn", false);
            }
            set
            {
                tl.LogMessage("CoolerOn Set Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("CoolerOn", true);
            }
        }

        public double CoolerPower
        {
            get
            {
                tl.LogMessage("CoolerPower Get Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("CoolerPower", false);
            }
        }

        public double ElectronsPerADU
        {
            get
            {
                tl.LogMessage("ElectronsPerADU Get Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("ElectronsPerADU", false);
            }
        }

        public double ExposureMax
        {
            get
            {
                CheckConnected("Camera must be connected to read ExposureMax");

                tl.LogMessage("ExposureMax Get Get", camera.Info.ExposureTimeMax.ToString());
                return camera.Info.ExposureTimeMax;
            }
        }

        public double ExposureMin
        {
            get
            {
                CheckConnected("Camera must be connected to read ExposureMin");

                tl.LogMessage("ExposureMin Get", camera.Info.ExposureTimeMin.ToString());
                return camera.Info.ExposureTimeMin;
            }
        }

        public double ExposureResolution
        {
            get
            {
                CheckConnected("Camera must be connected to read ExposureResolution");

                tl.LogMessage("ExposureResolution Get", camera.Info.ExposureTimeStep.ToString());
                return camera.Info.ExposureTimeStep;
            }
        }

        public bool FastReadout
        {
            get
            {
                CheckConnected("Camera must be connected to read FastReadout");

                tl.LogMessage("FastReadout Get", camera.Mode.Preview.ToString());
                return camera.PreviewMode;
            }
            set
            {
                CheckConnected("Camera must be connected to set FastReadout");

                value = value && UseLiveview;

                tl.LogMessage("FastReadout Set", value.ToString());
                camera.PreviewMode = value;
            }
        }

        public double FullWellCapacity
        {
            get
            {
                tl.LogMessage("FullWellCapacity Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("FullWellCapacity", false);
            }
        }

        public short Gain
        {
            get
            {
                tl.LogMessage("Gain Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("Gain", false);
            }
            set
            {
                tl.LogMessage("Gain Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("Gain", true);
            }
        }

        public short GainMax
        {
            get
            {
                tl.LogMessage("GainMax Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GainMax", false);
            }
        }

        public short GainMin
        {
            get
            {
                tl.LogMessage("GainMin Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GainMin", true);
            }
        }

        public ArrayList Gains
        {
            get
            {
                tl.LogMessage("Gains Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("Gains", true);
            }
        }

        public bool HasShutter
        {
            get
            {
                tl.LogMessage("HasShutter Get", true.ToString());
                return true;
            }
        }

        public double HeatSinkTemperature
        {
            get
            {
                tl.LogMessage("HeatSinkTemperature Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("HeatSinkTemperature", false);
            }
        }

        public object ImageArray
        {
            get
            {
                CheckConnected("Camera must be connected to read image");

                object result = null;

                if (camera.LastImage.Status != SonyImage.ImageStatus.Ready)
                {
                    tl.LogMessage("ImageArray Get", "Throwing InvalidOperationException because of a call to ImageArray before the first image has been taken!");
                    throw new ASCOM.InvalidOperationException("Call to ImageArray before the first image has been taken!");
                }

                SonyImage image = camera.LastImage;

                int requestedWidth = NumX;
                int requestedHeight = NumY;
                LogMessage("ImageArray", "requestedWidth = {0}, cameraNumX = {1}, camera.Mode.ImageWidth = {2}", (int)Math.Min(cameraNumX, camera.Mode.ImageWidthPixels), cameraNumX, camera.Mode.ImageWidthPixels);

                tl.LogMessage("ImageArray Get", String.Format("(numX = {0}, numY = {1}, image.Width = {2}, image.Height = {3})", requestedWidth, requestedHeight, image.Width, image.Height));

                switch (image.m_info.ImageMode)
                {
                    case 1:
                        tl.LogMessage("BAYER info", String.Format("Dimensions = {0}, {1} x {2}", SonyImage.BAYER.Rank, SonyImage.BAYER.GetLength(0), SonyImage.BAYER.GetLength(1)));

                        result = Resize(SonyImage.BAYER, SonyImage.BAYER.Rank, StartX, StartY, requestedWidth, requestedHeight);
                        break;

                    case 2:
                        if (Personality != SonyCommon.PERSONALITY_NINA)
                        {
                            tl.LogMessage("RGB info", String.Format("Dimensions = {0}, {1} x {2} x {3}", SonyImage.RGB.Rank, SonyImage.RGB.GetLength(0), SonyImage.RGB.GetLength(1), SonyImage.RGB.GetLength(2)));

                            result = Resize(SonyImage.RGB, SonyImage.RGB.Rank, StartX, StartY, requestedWidth, requestedHeight);
                        }
                        else
                        {
                            tl.LogMessage("RGB info as MONO", String.Format("Dimensions = {0}, {1} x {2}", SonyImage.BAYER.Rank, SonyImage.BAYER.GetLength(0), SonyImage.BAYER.GetLength(1)));

                            result = Resize(SonyImage.BAYER, SonyImage.BAYER.Rank, StartX, StartY, requestedWidth, requestedHeight);
                        }
                        break;

                    default:
                        tl.LogMessage("Unknown info", String.Format("{0} - Throwing", image.m_info.ImageMode));

                        throw new ASCOM.InvalidOperationException("Call to ImageArray resulted in invalid image type!");
                }

                return result;
            }
        }

        public object ImageArrayVariant
        {
            get
            {
                CheckConnected("Camera must be connected to read image");

                if (camera.LastImage.Status != SonyImage.ImageStatus.Ready)
                {
                    tl.LogMessage("ImageArrayVariant Get", "Throwing InvalidOperationException because of a call to ImageArrayVariant before the first image has been taken!");
                    throw new ASCOM.InvalidOperationException("Call to ImageArrayVariant before the first image has been taken!");
                }
                object[,,] cameraImageArrayVariant = new object[cameraNumX, cameraNumY, 3];

                return cameraImageArrayVariant;
            }
        }

        public bool ImageReady
        {
            get
            {
                CheckConnected("Camera must be connected to check image status");

                tl.LogMessage("ImageReady Get", camera.LastImage.Status.ToString());

                return camera.ImageReady;
            }
        }

        public bool IsPulseGuiding
        {
            get
            {
                tl.LogMessage("IsPulseGuiding Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("IsPulseGuiding", false);
            }
        }

        public double LastExposureDuration
        {
            get
            {
                CheckConnected("Camera must be connected to read last exposure duration");

                if (camera.LastImage.Status != SonyImage.ImageStatus.Ready)
                {
                    tl.LogMessage("LastExposureDuration Get", "Throwing InvalidOperationException because of a call to LastExposureDuration before the first image has been taken!");
                    throw new ASCOM.InvalidOperationException("Call to LastExposureDuration before the first image has been taken!");
                }

                double result = camera.LastImage.Duration;
                tl.LogMessage("LastExposureDuration Get", result.ToString());

                return result;
            }
        }

        public string LastExposureStartTime
        {
            get
            {
                CheckConnected("Camera must be connected to read last exposure time");

                if (camera.LastImage.Status != SonyImage.ImageStatus.Ready)
                {
                    tl.LogMessage("LastExposureStartTime Get", "Throwing InvalidOperationException because of a call to LastExposureStartTime before the first image has been taken!");
                    throw new ASCOM.InvalidOperationException("Call to LastExposureStartTime before the first image has been taken!");
                }

                string exposureStartString = camera.LastImage.StartTime.ToString("yyyy-MM-ddTHH:mm:ss");
                tl.LogMessage("LastExposureStartTime Get", exposureStartString.ToString());
                return exposureStartString;
            }
        }

        public int MaxADU
        {
            get
            {
                tl.LogMessage("MaxADU Get", "20000");
                return 20000;
            }
        }

        public short MaxBinX
        {
            get
            {
                tl.LogMessage("MaxBinX Get", "1");
                return 1;
            }
        }

        public short MaxBinY
        {
            get
            {
                tl.LogMessage("MaxBinY Get", "1");
                return 1;
            }
        }

        public int NumX
        {
            get
            {
                CheckConnected("Camera must be connected to get NumX");

                tl.LogMessage("NumX Get", camera.Mode.ImageWidthPixels.ToString());
                return (int)Math.Min(cameraNumX, camera.Mode.ImageWidthPixels);
            }
            set
            {
                CheckConnected("Camera must be connected to set NumX");

                cameraNumX = value;// (int)Math.Min(value, camera.Mode.ImageWidthPixels);
                tl.LogMessage("NumX set", value.ToString());
            }
        }

        public int NumY
        {
            get
            {
                CheckConnected("Camera must be connected to get NumY");

                tl.LogMessage("NumY Get", camera.Mode.ImageHeightPixels.ToString());
                return (int)Math.Min(cameraNumY, camera.Mode.ImageHeightPixels);
            }
            set
            {
                CheckConnected("Camera must be connected to set NumY");

                // NINA sets this before it changes readout mode
                cameraNumY = value;// (int)Math.Min(value, camera.Mode.ImageHeightPixels);
                tl.LogMessage("NumY set", value.ToString());
            }
        }

        public short PercentCompleted
        {
            get
            {
                tl.LogMessage("PercentCompleted Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("PercentCompleted", false);
            }
        }

        public double PixelSizeX
        {
            get
            {
                CheckConnected("Camera must be connected to get pixelsizex");

                tl.LogMessage("PixelSizeX Get", camera.Info.PixelWidth.ToString());
                return camera.Info.PixelWidth;
            }
        }

        public double PixelSizeY
        {
            get
            {
                CheckConnected("Camera must be connected to get pixelsizey");

                tl.LogMessage("PixelSizeY Get", camera.Info.PixelHeight.ToString());
                return camera.Info.PixelHeight;
            }
        }

        public void PulseGuide(GuideDirections Direction, int Duration)
        {
            tl.LogMessage("PulseGuide", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("PulseGuide");
        }

        public short ReadoutMode
        {
            get
            {
                CheckConnected("Camera must be connected to get readout mode");

                tl.LogMessage("ReadoutMode Get", camera.OutputMode.ToString());
                return (short)(camera.OutputMode - 1);
            }
            set
            {
                CheckConnected("Camera must be connected to set readout mode");

                if (ReadoutModes.Count > value)
                {
                    tl.LogMessage("ReadoutMode Set", value.ToString());

                    switch (value)
                    {
                        case 0:
                            camera.PreviewMode = false;
                            break;

                        case 1:
                            camera.PreviewMode = true;
                            break;
                    }
                }
                else
                {
                    throw new ASCOM.InvalidValueException("ReadoutMode not in allowable values");
                }
            }
        }

        public ArrayList ReadoutModes
        {
            get
            {
                ArrayList modes = new ArrayList();

                modes.Add(String.Format("Full Resolution ({0} x {1})", camera.Resolutions.ImageWidthPixels, camera.Resolutions.ImageHeightPixels));

                if (camera.HasLiveView)
                {
                    if (Personality == SonyCommon.PERSONALITY_NINA)
                    {
                        modes.Add(String.Format("LiveView ({0} x {1}) [Mono]", camera.Resolutions.PreviewWidthPixels, camera.Resolutions.PreviewHeightPixels));
                    }
                    else
                    {
                        modes.Add(String.Format("LiveView ({0} x {1})", camera.Resolutions.PreviewWidthPixels, camera.Resolutions.PreviewHeightPixels));
                    }
                };

                return modes;
            }
        }

        public string SensorName
        {
            get
            {
                CheckConnected("Camera must be connected to get SensorName");

                tl.LogMessage("SensorName Get", camera.Info.SensorName);
                return camera.Info.SensorName;
            }
        }

        public SensorType SensorType
        {
            get
            {
                CheckConnected("Camera must be connected to get SensorType");

                SensorType type;

                if (Personality == SonyCommon.PERSONALITY_NINA)
                {
                    type = camera.PreviewMode ? SensorType.Monochrome : SensorType.RGGB;
                }
                else
                {
                    type = camera.OutputMode == SonyCamera.ImageMode.RGB ? SensorType.Color : SensorType.RGGB;
                }
                tl.LogMessage("SensorType Get", type.ToString());
                return type;
            }
        }

        public double SetCCDTemperature
        {
            get
            {
                tl.LogMessage("SetCCDTemperature Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SetCCDTemperature", false);
            }
            set
            {
                tl.LogMessage("SetCCDTemperature Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SetCCDTemperature", true);
            }
        }

        public void StartExposure(double Duration, bool Light)
        {
            CheckConnected("Camera must be connected to start exposure");

            if (Duration < 0.0) throw new InvalidValueException("StartExposure", Duration.ToString(), "0.0 upwards");
            if (StartX + NumX > camera.Mode.ImageWidthPixels) throw new InvalidValueException("StartExposure", cameraNumX.ToString(), camera.Info.ImageWidthPixels.ToString());
            if (StartY + NumY > camera.Mode.ImageHeightPixels) throw new InvalidValueException("StartExposure", cameraNumY.ToString(), camera.Info.ImageHeightPixels.ToString());

            tl.LogMessage("StartExposure", Duration.ToString() + " " + Light.ToString());

            camera.StartCapture(Duration, Personality, defaultReadoutMode);
        }

        public int StartX
        {
            get
            {
                tl.LogMessage("StartX Get", cameraStartX.ToString());
                return cameraStartX;
            }
            set
            {
                cameraStartX = value;
                tl.LogMessage("StartX Set", value.ToString());
            }
        }

        public int StartY
        {
            get
            {
                tl.LogMessage("StartY Get", cameraStartY.ToString());
                return cameraStartY;
            }
            set
            {
                cameraStartY = value;
                tl.LogMessage("StartY set", value.ToString());
            }
        }

        public void StopExposure()
        {
            CheckConnected("Camera must be connected to stop exposure");

            tl.LogMessage("StopExposure", "Attempting to stop exposure");
            camera.StopCapture();
        }

        #endregion

        #region Private properties and methods
        // here are some useful properties and methods that can be used as required
        // to help with driver development

        #region ASCOM Registration

        // Register or unregister driver for ASCOM. This is harmless if already
        // registered or unregistered. 
        //
        /// <summary>
        /// Register or unregister the driver with the ASCOM Platform.
        /// This is harmless if the driver is already registered/unregistered.
        /// </summary>
        /// <param name="bRegister">If <c>true</c>, registers the driver, otherwise unregisters it.</param>
        private static void RegUnregASCOM(bool bRegister)
        {
            using (var P = new ASCOM.Utilities.Profile())
            {
                P.DeviceType = "Camera";
                if (bRegister)
                {
                    P.Register(driverID, driverDescription);
                }
                else
                {
                    P.Unregister(driverID);
                }
            }
        }

        /// <summary>
        /// This function registers the driver with the ASCOM Chooser and
        /// is called automatically whenever this class is registered for COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is successfully built.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During setup, when the installer registers the assembly for COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually register a driver with ASCOM.
        /// </remarks>
        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            RegUnregASCOM(true);
        }

        /// <summary>
        /// This function unregisters the driver from the ASCOM Chooser and
        /// is called automatically whenever this class is unregistered from COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is cleaned or prior to rebuilding.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During uninstall, when the installer unregisters the assembly from COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually unregister a driver from ASCOM.
        /// </remarks>
        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            RegUnregASCOM(false);
        }

        #endregion

        /// <summary>
        /// Returns true if there is a valid connection to the driver hardware
        /// </summary>
        private bool IsConnected
        {
            get
            {
                // TODO check that the driver hardware connection exists and is connected to the hardware
                return camera != null && camera.Connected;
            }
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!IsConnected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Camera";
                tl.Enabled = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, string.Empty, traceStateDefault));
                deviceId = driverProfile.GetValue(driverID, cameraProfileName, string.Empty, cameraDefault);
                defaultReadoutMode = Convert.ToInt16(driverProfile.GetValue(driverID, readoutModeDefaultProfileName, string.Empty, readoutModeDefault));
                SaveRawImageData = Convert.ToBoolean(driverProfile.GetValue(driverID, saveRawImageProfileName, string.Empty, saveRawImageDefault));
                UseLiveview = Convert.ToBoolean(driverProfile.GetValue(driverID, useLiveviewProfileName, string.Empty, useLiveviewDefault));
                Personality = Convert.ToInt16(driverProfile.GetValue(driverID, personalityProfileName, string.Empty, personalityDefault));

                if (defaultReadoutMode == 0)
                {
                    defaultReadoutMode = SonyCommon.OUTPUTFORMAT_RGGB;
                }

                // This needs to actually save to registry
                SaveRawImageFolder = (string)Registry.GetValue("HKEY_CURRENT_USER\\Software\\retro.kiwi\\SonyMTPCamera.dll", "File Save Path", "");
                LogMessage("ReadProfile", "DeviceID:             {0}", deviceId);
                LogMessage("ReadProfile", "Default Readout Mode: {0}", defaultReadoutMode.ToString());
                LogMessage("ReadProfile", "Save Raw files:       {0}", SaveRawImageData.ToString());
                LogMessage("ReadProfile", "Use Liveview:         {0}", UseLiveview.ToString());
                LogMessage("ReadProfile", "Personality:          {0}", Personality.ToString());
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Camera";
                driverProfile.WriteValue(driverID, traceStateProfileName, tl.Enabled.ToString());
                driverProfile.WriteValue(driverID, readoutModeDefaultProfileName, defaultReadoutMode.ToString());
                driverProfile.WriteValue(driverID, saveRawImageProfileName, SaveRawImageData.ToString());
                driverProfile.WriteValue(driverID, useLiveviewProfileName, UseLiveview.ToString());
                driverProfile.WriteValue(driverID, personalityProfileName, Personality.ToString());

                Registry.SetValue("HKEY_CURRENT_USER\\Software\\retro.kiwi\\SonyMTPCamera.dll", "File Save Path", SaveRawImageFolder);

                if (deviceId != null)
                {
                    driverProfile.WriteValue(driverID, cameraProfileName, deviceId.ToString());
                }
            }
        }

        /// <summary>
        /// Log helper function that takes formatted strings and arguments
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        internal static void LogMessage(string identifier, string message, params object[] args)
        {
            var msg = string.Format(message, args);
            tl.LogMessage(identifier, msg);
        }
        #endregion

        internal static object Resize(object array, int rank, int startX, int startY, int width, int height)
        {
            LogMessage("Resize", "rank={0}, startX={1}, startY={2}, width={3}, height={4}", rank, startX, startY, width, height);

            if (rank == 2)
            {
                int[,] input = (int[,])array;

                if (startX == 0 && startY == 0 && width == input.GetLength(0) && height == input.GetLength(1))
                {
                    return input;
                }

                int[,] output = new int[width, height];

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        output[x, y] = input[x + startX, y + startY];
                    }
                }

                return output;
            }
            else if (rank == 3)
            {
                int[,,] input = (int[,,])array;

                if (startX == 0 && startY == 0 && width == input.GetLength(0) && height == input.GetLength(1))
                {
                    return input;
                }

                int zLen = input.GetLength(2);
                int[,,] output = new int[width, height, zLen];

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int z = 0; z < zLen; z++)
                        {
                            output[x, y, z] = input[x + startX, y + startY, z];
                        }
                    }
                }

                return output;
            }
            else
            {
                // Ummm
                throw new ASCOM.InvalidValueException();
            }
        }
    }
}