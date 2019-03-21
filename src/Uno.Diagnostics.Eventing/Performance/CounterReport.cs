using System;

namespace Uno.Services.Contract.Diagnostics
{
	public class CounterReport
	{
		public string Name { get; set; }

		public long Count { get; set; }
		public TimeSpan Min { get; set; }
		public TimeSpan Max { get; set; }
		public TimeSpan Avg { get; set; }
		public double StdDev { get; set; }
		public TimeSpan Total { get; set; }

		public double MainThreadRatio { get; set; }

		public string Details
		{
			get
			{
				if(Count <= 1)
				{
					return
						FormatTime(Total) + (MainThreadRatio > 0 ? " -UI thread-" : string.Empty);
				}
				return $"{Count} times, " +
					$"{FormatTime(Min)}/" +
					$"{FormatTime(Max)}/" +
					$"{FormatTime(Avg)}/" +
					$"{StdDev:.000}/" +
					$"{FormatTime(Total)}/" +
					$"{MainThreadRatio}";
			}
		}

		private static string FormatTime(TimeSpan? timespan)
		{
			if(timespan == null)
			{
				return "na";
			}

			if(timespan.Value < TimeSpan.FromSeconds(10))
			{
				return $"{timespan.Value.TotalMilliseconds:0.0}ms";
			}

			return $"{timespan.Value.TotalSeconds:0.00}s";
		}
	}
}
