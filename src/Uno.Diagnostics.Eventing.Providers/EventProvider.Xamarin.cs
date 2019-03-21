#if XAMARIN
using Uno.Diagnostics.Eventing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Uno.Services.Diagnostics.Eventing
{
	/// <summary>
	/// Defines a default event provider for the Xamarin platform, that simulates Windows ETW.
	/// </summary>
	public class EventProvider : IEventProvider
	{
		private readonly Guid _providerId;
		private readonly IEventSink _sink;

		public EventProvider(Guid providerId, IEventSink sink)
		{
			_sink = sink;
			_providerId = providerId;
        }

		public EventProvider()
		{
		}

		public bool IsEnabled { get; set; } = true;

		bool IEventProvider.WriteMessageEvent(string eventMessage)
		{
			if(IsEnabled)
			{
				_sink.WriteEvent(_providerId, Thread.CurrentThread.ManagedThreadId, new EventDescriptor(0), new object[] { eventMessage });

				return true;
			}
			else
			{
				return false;
			}
		}

		bool IEventProvider.WriteEvent(EventDescriptor eventDescriptor, params object[] data)
		{
			if (IsEnabled)
			{
				_sink.WriteEvent(_providerId, Thread.CurrentThread.ManagedThreadId, eventDescriptor, data);

				return true;
			}
			else
			{
				return false;
			}
		}

		bool IEventProvider.WriteEvent(EventDescriptor eventDescriptor, string data)
		{
			if (IsEnabled)
			{
				_sink.WriteEvent(_providerId, Thread.CurrentThread.ManagedThreadId, eventDescriptor, new object[] { data });

				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
#endif
