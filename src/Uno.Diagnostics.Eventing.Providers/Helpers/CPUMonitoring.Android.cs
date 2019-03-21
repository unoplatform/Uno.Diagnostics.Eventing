#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Diagnostics.Eventing;

namespace Uno.Diagnostics.Eventing.Providers.Helpers
{
    public partial class CPUMonitoring
    {
		private Thread _thread;

		public CPUMonitoring()
		{
			_thread = new Thread(MonitorCPU);
			_thread.Name = "Uno-CPUMonitoring";
			_thread.Start();
		}

		private void MonitorCPU()
		{
			try
			{
				do
				{
					Thread.Sleep(TimeSpan.FromSeconds(5));

					var value = PublishCPUStats();

					if (_trace.IsEnabled)
					{
						_trace.WriteEventActivity(TraceProvider.User, EventOpcode.Send, new[] { value.user.ToString() });
						_trace.WriteEventActivity(TraceProvider.System, EventOpcode.Send, new[] { value.system.ToString() });
						_trace.WriteEventActivity(TraceProvider.IOWait, EventOpcode.Send, new[] { value.ioWait.ToString() });
						_trace.WriteEventActivity(TraceProvider.IRQ, EventOpcode.Send, new[] { value.irq.ToString() });
						_trace.WriteEventActivity(TraceProvider.Process, EventOpcode.Send, new[] { value.process.ToString() });
					}
				}
				while (true);
			}
			catch(Exception e)
			{
				Console.WriteLine($"Failed to read CPU stats. ({e})");
			}
		}

		private static (int user, int system, int ioWait, int irq, int process) PublishCPUStats()
		{
			var pi = new ProcessStartInfo("/system/bin/top", "-n 1")
			{
				RedirectStandardOutput = true,
				UseShellExecute = false
			};

			var p = Process.Start(pi);
			p.WaitForExit();

			var processId = Process.GetCurrentProcess().Id.ToString();

			(int user, int system, int ioWait, int irq) system = (-1, -1, -1, -1);
			int processCPU = -1;

			string line = null;
			while((line = p.StandardOutput.ReadLine()) != null)
			{
				if(line.StartsWith("user ", StringComparison.OrdinalIgnoreCase) && system.user == -1)
				{
					var fields = line.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

					var values = fields.Select(f =>
						{
							string s = f.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)[1].TrimEnd(new[] { '%' });
							return int.Parse(s, CultureInfo.InvariantCulture);
						}
					);

					system = (values.ElementAtOrDefault(0), values.ElementAtOrDefault(1), values.ElementAtOrDefault(2), values.ElementAtOrDefault(3));
				}

				if(line.TrimStart(new[] { ' ' }).StartsWith(processId + " "))
				{
					var cpuField = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(2);
					processCPU = int.Parse(cpuField.TrimEnd(new[] { '%' }), CultureInfo.InvariantCulture);
				}
			}

			return (system.user, system.system, system.ioWait, system.irq, processCPU);
		}
	}
}
#endif
