#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Diagnostics.Eventing;

namespace Uno.Diagnostics.Eventing.Providers.Helpers
{
    public partial class MemoryMonitoring
    {
		private Thread _thread;

		public MemoryMonitoring()
		{
			_thread = new Thread(RunMonitoring);
			_thread.Name = "Uno-MemoryMonitoring";
			_thread.Start();
		}

		private void RunMonitoring()
		{
			try
			{
				do
				{
					Thread.Sleep(3000);

					var runtime = Java.Lang.Runtime.GetRuntime();

					if (_trace.IsEnabled)
					{
						_trace.WriteEventActivity(TraceProvider.TotalMemory, EventOpcode.Send, new[] { runtime.TotalMemory().ToString() });
					}
				}
				while (true);
			}
			catch(Exception e)
			{
				Console.WriteLine($"Failed to read memory stats ({e})");
			}
		}
	}
}
#endif
