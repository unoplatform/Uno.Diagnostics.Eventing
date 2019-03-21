using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Uno.Services.Diagnostics.Eventing
{
	/// <summary>
	/// Defines a manifest provider in a trace file
	/// </summary>
	public partial class ManifestProvider
	{
		public string ProviderId { get; set; }

		public string ProviderName { get; set; }

		public ManifestEvent[] Events { get; set; }
	}
}
