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
		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		public static class TraceProvider
		{	
			// {A4417074-FB89-4618-94CF-BD802FE5633E}
			public readonly static Guid Id = new Guid(0xa4417074, 0xfb89, 0x4618, 0x94, 0xcf, 0xbd, 0x80, 0x2f, 0xe5, 0x63, 0x3e);

			public const int User = 1;
			public const int System = 2;
			public const int IOWait = 3;
			public const int IRQ = 4;
			public const int Process = 5;
		}
	}
}
