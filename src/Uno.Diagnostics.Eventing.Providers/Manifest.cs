using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Services.Diagnostics.Eventing
{
	/// <summary>
	/// A trace manifest file definition
	/// </summary>
    public class Manifest
    {
		public ManifestProvider[] Providers { get; set; }
	}
}
