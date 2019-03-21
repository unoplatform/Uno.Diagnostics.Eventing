using System;
using System.Collections.Generic;
using System.Text;
using Uno.Diagnostics.Eventing;

namespace Uno.Services.Diagnostics.Eventing
{
	/// <summary>
	/// Defines an interface for writing events
	/// </summary>
	public interface IEventSink
	{
		void WriteEvent(Guid providerId, int threadId, EventDescriptor eventDescriptor, object[] data);
	}
}
