using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpGen.Runtime;

namespace AllSystemsGo.CRSDK
{
	/// <summary>
	/// Functions
	/// </summary>
	public static partial class CR_Core
	{
		public static int GetDevicePropertiesComplete(long deviceHandle, out CrDeviceProperty[] properties, out int numOfProperties)
		{
			var result = GetDeviceProperties(deviceHandle, out var ptr, out numOfProperties);
			properties = new CrDeviceProperty[numOfProperties];
			MemoryHelpers.Read(ptr, properties, 0, numOfProperties);
			return result;
		}
	}
}
