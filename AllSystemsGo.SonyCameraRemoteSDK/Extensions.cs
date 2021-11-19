using AllSystemsGo.CRSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpGen.Runtime;

namespace AllSystemsGo.SonyCameraRemoteSDK;

public static class Extensions
{
	public static string GetSerialNumber(this ICrCameraObjectInfo icrcoi)
	{
		var ba = new byte[icrcoi.IdSize];
		MemoryHelpers.Read(icrcoi.Id, ba, 0, (int) icrcoi.IdSize);
		return Encoding.Unicode.GetString(ba, 0, ba.Length - 2);
	}

	public static CrDevicePropertyCode GetDevicePropertyCode(this CrDeviceProperty deviceProperty)
	{
		return (CrDevicePropertyCode) deviceProperty.Code;
	}
}