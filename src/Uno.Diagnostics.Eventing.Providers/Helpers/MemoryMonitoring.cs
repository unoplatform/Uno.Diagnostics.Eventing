using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Diagnostics.Eventing;

namespace Uno.Diagnostics.Eventing.Providers.Helpers
{
	public partial class MemoryMonitoring
    {
		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		public static class TraceProvider
		{
			// {EC310FD4-E386-4EA7-97E3-AF3A8463BE23}
			public readonly static Guid Id = new Guid(0xec310fd4, 0xe386, 0x4ea7, 0x97, 0xe3, 0xaf, 0x3a, 0x84, 0x63, 0xbe, 0x23);

			public const int TotalMemory = 1;
			public const int MaxHeap = 2;
		}

	}
}
