#if XAMARIN
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Diagnostics.Eventing;
using System.Reactive.Concurrency;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Uno.Extensions;

namespace Uno.Services.Diagnostics.Eventing
{
	/// <summary>
	/// Defines a EventSink that target files.
	/// </summary>
	public partial class FileEventSink : IEventSink
	{
		private string _basePath;
		private readonly IScheduler _scheduler;
		private readonly FileStream _stream;
		private readonly BinaryWriter _writer;
		private readonly Stopwatch _watch;

		/// <summary>
		/// Creates a new FileEventSink. This class generates files that need to be 
		/// translated into ETL files using the Uno.EventSourceConverter tool.
		/// </summary>
		/// <param name="basePath">The base path for the trace files.</param>
		public FileEventSink(string basePath)
		{
			_basePath = basePath;
			_scheduler = new EventLoopScheduler();

			var fileName = "{0:yyyyMMdd-hhmmssfff}.trace".InvariantCultureFormat(DateTime.UtcNow);
			var filePath = Path.Combine(basePath, "traces", fileName);

			Directory.CreateDirectory(Path.GetDirectoryName(filePath));

			GenerateManifest(filePath);

			Console.WriteLine("Writing traces to {0}", filePath);

			_stream = File.OpenWrite(filePath);
			_writer = new BinaryWriter(_stream);
			_watch = Stopwatch.StartNew();

			// Magic
			_writer.Write(new byte[] { 0x42, 0x42, 0x42, 0x42 });

			// Version
			_writer.Write((byte)0x03);
		}

		private static void GenerateManifest(string manifestFile)
		{
			GenerateManifest(AppDomain.CurrentDomain.GetAssemblies(), manifestFile);
		}

		public void WriteEvent(Guid providerId, int threadId, EventDescriptor eventDescriptor, object[] data)
		{
			var elapsedTicks = _watch.Elapsed.Ticks;

			_scheduler.Schedule(() => Serialize(providerId, threadId, elapsedTicks, eventDescriptor, data));
		}

		private void Serialize(Guid providerId, int threadId, long elapsedTicks, EventDescriptor eventDescriptor, object[] data)
		{
			_writer.Write(providerId.ToByteArray());
			_writer.Write(elapsedTicks);
			_writer.Write(threadId);
			_writer.Write(eventDescriptor.EventId);
			_writer.Write((byte)eventDescriptor.Opcode);
			_writer.Write(eventDescriptor.Task);
			_writer.Write(eventDescriptor.Version);
			_writer.Write(eventDescriptor.Channel);
			_writer.Write(eventDescriptor.Keywords);
			_writer.Write(eventDescriptor.Level);
			_writer.Write(eventDescriptor.ActivityId);
			_writer.Write(eventDescriptor.RelatedActivityId);
			_writer.Write(data?.Length ?? 0);

			if (data != null)
			{
				foreach (var d in data)
				{
					if (d is string)
					{
						_writer.Write((byte)0);
						_writer.Write(d as string);
					}
					else if (d is int)
					{
						_writer.Write((byte)1);
						_writer.Write((int)d);
					}
					else if (d is long)
					{
						_writer.Write((byte)2);
						_writer.Write((long)d);
					}
					else
					{
						_writer.Write((byte)0);
						_writer.Write(d?.ToString() ?? "<null>");
					}

				}
			}

			_writer.Flush();
		}
	}
}
#endif
