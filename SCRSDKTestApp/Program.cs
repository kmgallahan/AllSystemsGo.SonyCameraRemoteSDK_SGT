using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using AllSystemsGo.CRSDK;
using AllSystemsGo.SonyCameraRemoteSDK;

namespace SCRSDKTestApp
{
	class Program
	{
		static void Main(string[] args)
		{
			var initResult = CR_Core.Init(0);

			var enumCameraObjectsResult = CR_Core.EnumCameraObjects(out var cameraEnum, 10);

			if (cameraEnum == null || cameraEnum.Count == 0)
			{
				Debug.WriteLine("CAMERA NOT CONNECTED!");
				CR_Core.Release();
				return;
			}

			var cameraObjInfo = cameraEnum.GetCameraObjectInfo(0);
			var cameraDevice = new CameraDevice();

			var connectResult = CR_Core.Connect(cameraObjInfo, cameraDevice, out var handle);

			// var GetDevicePropertiesResult = CR_Core.GetDeviceProperties(handle, out var props, out var numProps);
			//
			// var GetLiveViewPropertiesResult = CR_Core.GetLiveViewProperties(handle, out var liveViewProps, out var liveViewNumProps);

			CrImageInfo crImageInfo = new CrImageInfo();

			do
			{
				Debug.WriteLine("Trying to get liveview image...");
				CR_Core.GetLiveViewImageInfo(handle, out crImageInfo);
				Thread.Sleep(50);
			} while (crImageInfo.BufferSize == 0);
 
			// var GetLiveViewImageInfoResult = CR_Core.GetLiveViewImageInfo(handle, out var liveViewInfo);

			// 	if (crImageInfo.BufferSize == 0)
			// {
			// 	Debug.WriteLine("No Liveview imageinfo available!");
			// 	CR_Core.Release();
			// 	return;
			// }

			var buffersize = Convert.ToInt32(crImageInfo.BufferSize);

			var liveViewImage = new CrImageDataBlock
			{
				FrameNo = 0,
				Size = crImageInfo.BufferSize,
				DataPointer = Marshal.AllocHGlobal(buffersize)
			};

			var getLiveViewImageResult = CR_Core.GetLiveViewImage(handle, ref liveViewImage);

			if (liveViewImage.ImageSize == 0)
			{
				Debug.WriteLine("No Liveview image captured!");
				CR_Core.Release();
				return;
			}
			
			var ba = new byte[buffersize];
			Marshal.Copy(liveViewImage.DataPointer, ba, 0, buffersize);

			var SOI_Marker = StringToByteArray("FFD8");
			var EOI_Marker = StringToByteArray("FFD9");

			var imageStartPosition = IndexOfSequence(ba, SOI_Marker, 0).ElementAt(0);
			var imageEndPosition = IndexOfSequence(ba, EOI_Marker, 0).ElementAt(0);
			var imageSize = imageEndPosition - imageStartPosition;

			var imageMemoryStream = new MemoryStream(ba, imageStartPosition, imageSize, false);

			// var sendShutterDownResult = CR_Core.SendCommand(handle, Convert.ToUInt16(CrCommandId.CommandIdRelease), CrCommandParam.CommandParamDown);
			// Thread.Sleep(35);
			// var sendShutterUpResult = CR_Core.SendCommand(handle, Convert.ToUInt16(CrCommandId.CommandIdRelease), CrCommandParam.CommandParamUp);
			
			CR_Core.Release();
		}

		public static List<int> IndexOfSequence(byte[] buffer, byte[] pattern, int startIndex)
		{
			List<int> positions = new List<int>();
			int i = Array.IndexOf<byte>(buffer, pattern[0], startIndex);
			while (i >= 0 && i <= buffer.Length - pattern.Length)
			{
				byte[] segment = new byte[pattern.Length];
				Buffer.BlockCopy(buffer, i, segment, 0, pattern.Length);
				if (segment.SequenceEqual<byte>(pattern))
					positions.Add(i);
				i = Array.IndexOf<byte>(buffer, pattern[0], i + 1);
			}
			return positions;
		}

		public static byte[] StringToByteArray(string hex)
		{
			return Enumerable.Range(0, hex.Length)
				.Where(x => x % 2 == 0)
				.Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
				.ToArray();
		}
	}
}
