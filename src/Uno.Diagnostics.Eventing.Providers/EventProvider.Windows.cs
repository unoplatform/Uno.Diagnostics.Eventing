#if !XAMARIN
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Diagnostics.Eventing;

namespace Uno.Services.Diagnostics.Eventing
{
	public class EventProvider : IEventProvider
	{
		private readonly Guid _providerId;
		private readonly EventSource _innerSource;
		private readonly ManifestProvider _manifest;

		public EventProvider(Guid providerId)
		{
			_providerId = providerId;
			_manifest = ManifestProvider.GetProviders().FirstOrDefault(p => p.ProviderId == providerId.ToString());

			_innerSource = new EventSource("Uno-" + _manifest.ProviderName.Replace("`1", ""));
		}

		public bool IsEnabled => true;

		public bool WriteEvent(EventDescriptor eventDescriptor, string data) => WriteEvent(eventDescriptor, new { Field0 = data });

		public bool WriteEvent(EventDescriptor eventDescriptor, params object[] data)
		{
			// The block below is duplicated because the Write<T> method checks if the 
			// type is an anonymous, and this can't be refactored outside this method.

			if (data == null || data.Length <= 4)
			{
				return WriteEvent(
					eventDescriptor, new
					{
						Field0 = data?.ElementAtOrDefault(0)?.ToString(),
						Field1 = data?.ElementAtOrDefault(1)?.ToString(),
						Field2 = data?.ElementAtOrDefault(2)?.ToString(),
						Field3 = data?.ElementAtOrDefault(3)?.ToString(),
					}
				);
			}
			else
			{
				return WriteEvent(
					eventDescriptor, new
					{
						Field0 = data?.ElementAtOrDefault(0)?.ToString(),
						Field1 = data?.ElementAtOrDefault(1)?.ToString(),
						Field2 = data?.ElementAtOrDefault(2)?.ToString(),
						Field3 = data?.ElementAtOrDefault(3)?.ToString(),
						Field4 = data?.ElementAtOrDefault(4)?.ToString(),
						Field5 = data?.ElementAtOrDefault(5)?.ToString(),
						Field6 = data?.ElementAtOrDefault(6)?.ToString(),
						Field7 = data?.ElementAtOrDefault(7)?.ToString(),
					}
				);
			}
		}

		public bool WriteMessageEvent(string eventMessage) => 
			WriteEvent(
				new EventDescriptor(0), new[] {
					eventMessage
				}
			);

		private bool WriteEvent<T>(EventDescriptor eventDescriptor, T eventData)
		{
			var eventInfo = _manifest.Events.FirstOrDefault(e => e.EventId == eventDescriptor.EventId);

			if (eventInfo != null)
			{
				var options = new EventSourceOptions()
				{
					Keywords = EventKeywords.None,
					ActivityOptions = EventActivityOptions.None
				};

				var isStart = eventInfo.EventName.EndsWith("Start");
				var isStop = eventInfo.EventName.EndsWith("Stop");

				Guid activity = Guid.Empty;
				Guid relatedActivity = Guid.Empty;

				if (!isStart && !isStop)
				{
					options.Opcode = (System.Diagnostics.Tracing.EventOpcode)eventDescriptor.Opcode;
				}


				_innerSource.Write(
						eventInfo.EventName,
						ref options
						, activityId: ref activity
						, relatedActivityId: ref relatedActivity
						, data: ref eventData
					);

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
