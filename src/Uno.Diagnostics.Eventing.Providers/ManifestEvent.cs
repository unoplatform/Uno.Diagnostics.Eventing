using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Services.Diagnostics.Eventing
{
	/// <summary>
	/// Defines a trace event in a manifest file
	/// </summary>
    public class ManifestEvent
    {
		public int EventId { get; set; }

		public string EventName { get; set; }
	}
}
