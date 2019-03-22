using System;
using System.Collections.Generic;
using System.Text;
using Uno.Diagnostics.Eventing;
using System.Reactive.Concurrency;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Uno.Extensions;
using System.Threading.Tasks;
using System.Threading;

namespace Uno.Services.Diagnostics.Eventing
{
	/// <summary>
	/// Defines a EventSink that target files.
	/// </summary>
	public partial class FileEventSink : IEventSink
	{
		private string _basePath;
		private readonly IScheduler _writeScheduler;
		private FileStream _stream;
		private BinaryWriter _writer;
		private readonly Stopwatch _watch;

		int _pendingWrite = 0;
		object _stopGate = new object();
		private TaskCompletionSource<bool> _stopTask;

		/// <summary>
		/// Creates a new FileEventSink. This class generates files that need to be 
		/// translated into ETL files using the Umbrella.EventSourceConverter tool.
		/// </summary>
		/// <param name="basePath">The base path for the trace files.</param>
		public FileEventSink(string basePath, IScheduler writeScheduler)
		{
			_basePath = basePath;

#if !NETSTANDARD2_0
			_writeScheduler = new EventLoopScheduler();
#else
			_writeScheduler = writeScheduler ?? throw new ArgumentNullException(nameof(writeScheduler));
#endif

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

		public Task Stop()
		{
			lock(_stopGate)
			{
				if(_stopTask == null)
				{
					_stopTask = new TaskCompletionSource<bool>();
				}
			}

			return _stopTask.Task;
		}

		private static void GenerateManifest(string manifestFile)
		{
			GenerateManifest(AppDomain.CurrentDomain.GetAssemblies(), manifestFile);
		}

		public void WriteEvent(Guid providerId, int threadId, EventDescriptor eventDescriptor, object[] data)
		{
			if(_writer != null)
			{
				var elapsedTicks = _watch.Elapsed.Ticks;

				Interlocked.Increment(ref _pendingWrite);

				_writeScheduler.Schedule(() => {
					Serialize(providerId, threadId, elapsedTicks, eventDescriptor, data);
					var newCount = Interlocked.Decrement(ref _pendingWrite);

					if(newCount == 0 && _stopTask != null)
					{
						_writer.Dispose();
						_stream.Dispose();
						_writer = null;
						_stream = null;
						_stopTask.TrySetResult(true);
					}
				});
			}
		}

		private void Serialize(Guid providerId, int threadId, long elapsedTicks, EventDescriptor eventDescriptor, object[] data)
		{
			if(_writer != null)
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

				if(data != null)
				{
					foreach(var d in data)
					{
						if(d is string)
						{
							_writer.Write((byte)0);
							_writer.Write(d as string);
						}
						else if(d is int)
						{
							_writer.Write((byte)1);
							_writer.Write((int)d);
						}
						else if(d is long)
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
}
