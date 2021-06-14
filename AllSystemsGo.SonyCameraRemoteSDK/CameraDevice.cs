using System;
using System.Diagnostics;
using AllSystemsGo.CRSDK;
using SharpGen.Runtime;

namespace AllSystemsGo.SonyCameraRemoteSDK
{
	public class CameraDevice : CallbackBase, IDeviceCallback
	{
		public void OnConnected(DeviceConnectionVersion version)
		{
			Debug.WriteLine("Connected");
		}

		public void OnDisconnected(uint error)
		{
			Debug.WriteLine("Disconnected");
		}

		public void OnPropertyChanged()
		{
			Debug.WriteLine("PropertyChanged");
		}

		public void OnLvPropertyChanged()
		{
			Debug.WriteLine("LvPropertyChanged");
		}

		public void OnCompleteDownload(string filename)
		{
			Debug.WriteLine("CompleteDownload");
		}

		public void OnCompleteDownload(sbyte filename)
		{
			Debug.WriteLine("CompleteDownload");
		}

		public void OnWarning(uint warning)
		{
			Debug.WriteLine("Warning");
		}

		public void OnError(uint error)
		{
			Debug.WriteLine("Error");
		}

		public new void Dispose()
		{
			throw new NotImplementedException();
		}
	}
}