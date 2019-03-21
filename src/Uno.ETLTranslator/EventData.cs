using System.Collections.Generic;

namespace Uno.ETLTranslator
{
	internal class EventData
	{
		public EventData()
		{
		}

		public byte Channel { get; internal set; }
		public int DataLength { get; internal set; }
		public long ElapsedTicks { get; internal set; }
		public int EventId { get; internal set; }
		public long Keywords { get; internal set; }
		public byte Level { get; internal set; }
		public byte OpCode { get; internal set; }
		public List<object> Payload { get; internal set; }
		public byte[] Provider { get; internal set; }
		public int Task { get; internal set; }
		public int ThreadId { get; internal set; }
		public byte Version { get; internal set; }
		public long ActivityId { get; internal set; }
		public long RelatedActivityId { get; internal set; }
	}
}
