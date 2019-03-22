using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace Uno.Services.Diagnostics.Eventing
{
	partial class FileEventSink
	{
		/// <summary>
		/// Generates the manifest associated with the generated trace.
		/// </summary>
		/// <remarks>The implicit convention is that a traceable class will 
		/// include a TraceProvider inner class, an Id field of type Guid, and fields
		/// of type Int32 to declare events.
		/// </remarks>
		/// <param name="manifestFile"></param>
		public static void GenerateManifest(Assembly[] assemblies, string manifestFile)
		{
			var q = ManifestProvider.GetProviders();

			var serializer = new XmlSerializer(typeof(Manifest), new[] { typeof(ManifestProvider), typeof(ManifestEvent) });

			var manifestFilePath = manifestFile + ".manifest";

			using (var s = File.OpenWrite(manifestFilePath))
			{
				serializer.Serialize(s, new Manifest() { Providers = q.ToArray() });
			}

			Console.WriteLine($"Generated trace manifest file {manifestFilePath}");
		}

	}
}
