using SharpGen.Runtime;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace AllSystemsGo.CRSDK;

public static partial class CR_Core
{
	private static SemaphoreSlim getDevicePropertiesLock = new(1);
	private static SemaphoreSlim getLiveViewPropertiesLock = new(1);

	public static unsafe CrError ConnectComplete(ICrCameraObjectInfo cameraObjectInfoRef,
		IDeviceCallback callback, ref long deviceHandle)
	{
		var connectResult = Connect(cameraObjectInfoRef, callback, ref deviceHandle);
		if (connectResult is not CrError.ErrorNone) return connectResult;

		var getDevicePropertiesResult = CrError.ErrorConnectTimeOut;

		var retries = 100;
		while (retries > 0)
		{
			Thread.Sleep(50);
			getDevicePropertiesResult = GetDeviceProperties(deviceHandle, out var deviceProperties, out var numProps);
			ReleaseDeviceProperties_(deviceHandle, deviceProperties.ToPointer());
			if (getDevicePropertiesResult is CrError.ErrorNone) break;
			retries--;
		}
		return getDevicePropertiesResult;
	}

	public static unsafe CrError GetDevicePropertiesComplete(long deviceHandle, out CrDeviceProperty[] properties, out CrMagSetting magSetting)
	{
		getDevicePropertiesLock.Wait(-1);
		var result = GetDeviceProperties(deviceHandle, out var ptr, out var numOfProperties);
		magSetting = default;
		if (result is not CrError.ErrorNone)
		{
			Debug.WriteLine("Error: Sony Camera SDK returned an error when asked for DeviceProperties");
			properties = Array.Empty<CrDeviceProperty>();
			ReleaseDeviceProperties_(deviceHandle, ptr.ToPointer());
			getDevicePropertiesLock.Release();
			return result;
		}

		properties = new CrDeviceProperty[numOfProperties];
		MemoryHelpers.Read(ptr, properties, 0, numOfProperties);

		var magnificationSetting = properties.First(x => x.Code is (uint)CrDevicePropertyCode.DevicePropertyFocusMagnifierSetting);
		magSetting.Magnification = Marshal.ReadInt16(magnificationSetting.CurrentValue, 4);

		ReleaseDeviceProperties_(deviceHandle, ptr.ToPointer());
		getDevicePropertiesLock.Release();
		return result;
	}

	public static unsafe CrError GetLiveViewPropertiesComplete(long deviceHandle, out CrFocusFrameInfo focusFrameInfo, out CrMagPosInfo magPosInfo)
	{
		getLiveViewPropertiesLock.Wait(-1);
		var result = GetLiveViewProperties(deviceHandle, out var ptr, out var numOfProperties);
		focusFrameInfo = default;
		magPosInfo = default;
		if (result is not CrError.ErrorNone)
		{
			Debug.WriteLine("Error: Sony Camera SDK returned an error when asked for LiveViewProperties");
			ReleaseLiveViewProperties_(deviceHandle, ptr.ToPointer());
			getLiveViewPropertiesLock.Release();
			return result;
		}
		var properties = new CrLiveViewProperty[numOfProperties];
		MemoryHelpers.Read(ptr, properties, 0, numOfProperties);
		var afAreaPositionProperty =
			properties.First(x => x.Code is (uint)CrLiveViewPropertyCode.LiveViewPropertyAfAreaPosition);
		focusFrameInfo.XPosition = (uint)Marshal.ReadInt16(afAreaPositionProperty.Value, 5); // x position
		focusFrameInfo.MaxWidth = (uint)Marshal.ReadInt16(afAreaPositionProperty.Value, 9); // width of setable area
		focusFrameInfo.YPosition = (uint)Marshal.ReadInt16(afAreaPositionProperty.Value, 13); // y position
		focusFrameInfo.MaxHeight = (uint)Marshal.ReadInt16(afAreaPositionProperty.Value, 17); // height of setable area
		focusFrameInfo.Width = (uint)Marshal.ReadInt16(afAreaPositionProperty.Value, 21);
		focusFrameInfo.Height = (uint)Marshal.ReadInt16(afAreaPositionProperty.Value, 25);
		var focusMagnifierPositionProperty =
			properties.First(x => x.Code is (uint)CrLiveViewPropertyCode.LiveViewPropertyFocusMagnifierPosition);
		MemoryHelpers.Read(focusMagnifierPositionProperty.Value, ref magPosInfo);

		ReleaseLiveViewProperties_(deviceHandle, ptr.ToPointer());
		getLiveViewPropertiesLock.Release();
		return result;
	}

	public partial struct CrMagSetting
	{
		public short Magnification;
	}
}

// public partial struct CrDeviceProperty
// {
// 	public ulong? OldValue { get; set; }
//
// 	public override string ToString()
// 	{
// 		var property = Enum.GetName(typeof(CrDevicePropertyCode), Code);
//
// 		var value = Code switch
// 		{
// 			(uint)CrDevicePropertyCode.DevicePropertyExposureProgramMode => Enum.GetName(typeof(CrExposureProgram), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyShutterSpeed => Enum.GetName(typeof(CrShutterSpeedSet), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyFileType => Enum.GetName(typeof(CrFileType), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyWhiteBalance => Enum.GetName(typeof(CrWhiteBalanceSetting), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyFocusMode => Enum.GetName(typeof(CrFocusMode), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyFlashMode => Enum.GetName(typeof(CrFlashMode), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyWirelessFlash => Enum.GetName(typeof(CrWirelessFlash), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyRedEyeReduction => Enum.GetName(typeof(CrRedEyeReduction), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyDriveMode => Enum.GetName(typeof(CrDriveMode), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyDro => Enum.GetName(typeof(CrDRangeOptimizer), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyImageSize => Enum.GetName(typeof(CrImageSize), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyAspectRatio => Enum.GetName(typeof(CrAspectRatioIndex), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyPictureEffect => Enum.GetName(typeof(CrPictureEffect), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyFocusArea => Enum.GetName(typeof(CrFocusArea), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyColortemp => Enum.GetName(typeof(CrColortemp), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyColorTuningAB => Enum.GetName(typeof(CrColorTuning), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyColorTuningGM => Enum.GetName(typeof(CrColorTuning), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyLiveViewDisplayEffect => Enum.GetName(typeof(CrLiveViewDisplayEffect), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyStillImageStoreDestination => Enum.GetName(typeof(CrStillImageStoreDestination), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyPriorityKeySettings => Enum.GetName(typeof(CrPriorityKeySettings), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyNearFar => Enum.GetName(typeof(CrNearFarEnableStatus), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyRawFileCompressionType => Enum.GetName(typeof(CrRAWFileCompressionType), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyIntervalRecMode => Enum.GetName(typeof(CrIntervalRecMode), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyStillImageTransSize => Enum.GetName(typeof(CrPropertyStillImageTransSize), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyRawJPcSaveImage => Enum.GetName(typeof(CrPropertyRAWJPCSaveImage), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyLiveViewImageQuality => Enum.GetName(typeof(CrPropertyLiveViewImageQuality), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyLiveViewStatus => Enum.GetName(typeof(CrLiveViewStatus), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyFocusIndication => Enum.GetName(typeof(CrFocusIndicator), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyCustomWBExecutionState => Enum.GetName(typeof(CrPropertyCustomWBExecutionState), CurrentValue),
// 			(uint)CrDevicePropertyCode.DevicePropertyCustomWBCaptureOperation => Enum.GetName(typeof(CrPropertyCustomWBOperation), CurrentValue),
// 			_ => CurrentValue.ToString()
// 		};
//
// 		return property + " = " + value + Environment.NewLine;
// 	}
// }