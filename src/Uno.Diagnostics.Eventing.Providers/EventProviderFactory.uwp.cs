#if NETFX_CORE
using Uno.Diagnostics.Eventing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Services.Diagnostics.Eventing
{
	/// <summary>
	/// Defines a default event provider factor for the Xamarin platform, that simulates Windows ETW.
	/// </summary>
	public class EventProviderFactory : IEventProviderFactory
	{
		private object gate = new object();
		private readonly Dictionary<Guid, IEventProvider> _providers = new Dictionary<Guid, IEventProvider>();

		public EventProviderFactory()
		{
		}

		IEventProvider IEventProviderFactory.GetProvider(Guid providerId)
		{
			lock(gate)
			{
				IEventProvider provider;

				if (!_providers.TryGetValue(providerId, out provider))
				{
					provider = _providers[providerId] = new EventProvider(providerId);
				}

				return provider;
			}
        }
	}
}
#endif
