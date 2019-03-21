using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Uno.Services.Diagnostics.Eventing;
using System.Xml.Serialization;
using System.Collections.Concurrent;
using System.Threading;

namespace Uno.ETLTranslator
{
	class Program
	{
		private const int FileVersion = 3;
		private static string _manifest;
		private static string _trace;

		static void Main(string[] args)
		{
			if (!args.Any() || args[0] == "/?")
			{
				Console.WriteLine("Usage: Uno.ETLTranslator.exe {Trace_file} [Trace_manifest_file]");
				Console.WriteLine("Remark: A file named {Trace_file}.manifest must also be present in the same directory.");
				return;
			}

			if (args.Length > 1)
			{
				_manifest = args.FirstOrDefault(a => a.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase)) ?? $"{args[0]}.manifest";
				_trace = args.Except(new[] { _manifest }).First();
			}
			else
			{
				_trace = args[0];
				_manifest = $"{_trace}.manifest";
			}

			var manifest = ReadManifest(_manifest);

			var lastTimeStamp = GetLastTimeStamp();
			var threads = GetThreadIds();

			Console.WriteLine($"Total recording duration: {TimeSpan.FromTicks(lastTimeStamp)}");
			Console.WriteLine($"Recording threads: {threads.Length}");

			// PerfView /onlyProviders=*Uno-PriorityEventLoopSchedulerBeta,*Uno-ViewModelBase,*Uno-Binder,*Uno-EnumObservableSource,*Uno-Layouter,*Uno-DependencyObject collect

			var map = MapProviders(manifest);

			var arguments = $"/onlyProviders={string.Join(",", map.Select(p => "*" + p.Value.Item1.Name))} collect {Path.GetFileNameWithoutExtension(_trace)} -zip:false";

			var perfView = new FileInfo("PerfView.exe");
			if (!perfView.Exists)
			{
				perfView = new FileInfo(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "PerfView.exe"));
			}

			if (perfView.Exists)
			{
				Process.Start(perfView.FullName, arguments);
			}
			else
			{
				Console.WriteLine($@"In a command line, run:
PerfView {arguments}
(Tip: You can put PerfView.exe in current directory for automatic execution)");
			}

			Console.WriteLine("Then press enter when perfview is running...");
			Console.ReadLine();

			Console.WriteLine("Replaying events...");

			var activityMap = new ConcurrentDictionary<long, Guid>();

			var threadTasks = threads
				.Select(tid => Task.Run(() => GenerateEvents(map, activityMap, tid)))
				.ToArray();

			Task.WhenAll(threadTasks).Wait();

			Console.WriteLine("You can now click \"Stop collection\".");
			Thread.Sleep(5000);
		}

		private static Dictionary<Guid, (EventSource, ManifestProvider)> MapProviders(Manifest manifest)
		{
			var providers = new Dictionary<Guid, (EventSource, ManifestProvider)>();

			foreach (var provider in manifest.Providers)
			{
				var id = Guid.Parse(provider.ProviderId);
				var value = (new EventSource("Uno-" + provider.ProviderName.Replace("`1", "")), provider);

				if(!providers.ContainsKey(id))
				{
					providers.Add(id, value);
				}
				else
				{
					Console.WriteLine($"WARNING: Duplicate provider {provider.ProviderName}");
				}
			}

			return providers;
		}

		private static void GenerateEvents(
			Dictionary<Guid, (EventSource, ManifestProvider)> map,
			ConcurrentDictionary<long, Guid> activityMap,
			int threadId
		)
		{
			foreach (var eventData in EnumerateEvents(_trace, true, threadId))
			{
				if (map.TryGetValue(new Guid(eventData.Provider), out var source))
				{
					var options = new EventSourceOptions()
					{
						Keywords = EventKeywords.None,
						ActivityOptions = EventActivityOptions.None
					};

					var eventName = source.Item2.Events.First(e => e.EventId == eventData.EventId).EventName;

					var isStart = eventName.EndsWith("Start");
					var isStop = eventName.EndsWith("Stop");

					Guid activity = Guid.Empty;
					Guid relatedActivity = Guid.Empty;

					if (!isStart && !isStop)
					{
						options.Opcode = (EventOpcode)eventData.OpCode;
					}

					if (eventData.ActivityId != 0)
					{
						activity = MapActivityId(activityMap, eventData.ActivityId);
					}

					if (eventData.RelatedActivityId != 0)
					{
						relatedActivity = MapActivityId(activityMap, eventData.RelatedActivityId);
					}

					// The block below is duplicated because the Write<T> method checks if the 
					// type is an anonymous, and this can't be refactored outside this method.

					if (eventData.Payload.Count <= 4)
					{
						var data = new
						{
							Field0 = eventData.Payload.ElementAtOrDefault(0)?.ToString(),
							Field1 = eventData.Payload.ElementAtOrDefault(1)?.ToString(),
							Field2 = eventData.Payload.ElementAtOrDefault(2)?.ToString(),
							Field3 = eventData.Payload.ElementAtOrDefault(3)?.ToString(),
						};

						source.Item1.Write(
							eventName,
							ref options
							, activityId: ref activity
							, relatedActivityId: ref relatedActivity
							, data: ref data
						);
					}
					else
					{
						var data = new
						{
							Field0 = eventData.Payload.ElementAtOrDefault(0)?.ToString(),
							Field1 = eventData.Payload.ElementAtOrDefault(1)?.ToString(),
							Field2 = eventData.Payload.ElementAtOrDefault(2)?.ToString(),
							Field3 = eventData.Payload.ElementAtOrDefault(3)?.ToString(),
							Field4 = eventData.Payload.ElementAtOrDefault(4)?.ToString(),
							Field5 = eventData.Payload.ElementAtOrDefault(5)?.ToString(),
							Field6 = eventData.Payload.ElementAtOrDefault(6)?.ToString(),
							Field7 = eventData.Payload.ElementAtOrDefault(7)?.ToString(),
						};

						source.Item1.Write(
							eventName,
							ref options
							, activityId: ref activity
							, relatedActivityId: ref relatedActivity
							, data: ref data
						);
					}
				}
				else
				{
					Console.WriteLine($"Skipping {new Guid(eventData.Provider)}");
				}
			}
		}

		private static int[] GetThreadIds()
		{
			return EnumerateEvents(_trace, false).Select(e => e.ThreadId).Distinct().ToArray();
		}

		private static long GetLastTimeStamp()
		{
			return EnumerateEvents(_trace, false).Last().ElapsedTicks;
		}

		private static Guid MapActivityId(ConcurrentDictionary<long, Guid> _activityMap, long activityId)
		{
			Guid activity;

			if (!_activityMap.TryGetValue(activityId, out activity))
			{
				_activityMap[activityId] = activity = Guid.NewGuid();
			}

			return activity;
		}

		private static IEnumerable<EventData> EnumerateEvents(string inputFile, bool replayRealTime, int? threadId = null)
		{
			Stopwatch w = Stopwatch.StartNew();
			var startTime = DateTime.Now;

			using (var s = File.OpenRead(inputFile))
			{
				using (var reader = new BinaryReader(s))
				{
					var baseTime = DateTime.Now;

					var magic = reader.ReadBytes(4);

					if (!magic.SequenceEqual(new byte[] { 0x42, 0x42, 0x42, 0x42 }))
					{
						throw new InvalidDataException("The file format is not recognized");
					}

					var version = reader.ReadByte();

					if(version != FileVersion)
					{
						throw new InvalidDataException($"Invalid version {version}, expected {FileVersion}");
					}

					while (true)
					{
						var eventData = ReadEvent(reader);

						if (eventData == null)
						{
							yield break;
						}

						if(threadId != null && eventData.ThreadId != threadId)
						{
							continue;
						}

						if (replayRealTime)
						{
							var waitingTime = TimeSpan.FromTicks(eventData.ElapsedTicks - w.Elapsed.Ticks);
                            if (waitingTime > TimeSpan.FromSeconds(1))
							{
								Console.WriteLine($"{DateTime.Now - startTime} / {TimeSpan.FromTicks(w.Elapsed.Ticks)} / {w.Elapsed}: tid({threadId}) Waiting for {waitingTime}");
							}

							while (w.Elapsed.Ticks < eventData.ElapsedTicks)
							{
								// Do some waiting...
							}
						}

						yield return eventData;
					}
				}
			}
		}

		private static EventData ReadEvent(BinaryReader reader)
		{
			try {
				var eventData = new EventData();

				eventData.Provider = reader.ReadBytes(16);
				eventData.ElapsedTicks = reader.ReadInt64();
				eventData.ThreadId = reader.ReadInt32();
				eventData.EventId = reader.ReadInt32();
				eventData.OpCode = reader.ReadByte();
				eventData.Task = reader.ReadInt32();
				eventData.Version = reader.ReadByte();
				eventData.Channel = reader.ReadByte();
				eventData.Keywords = reader.ReadInt64();
				eventData.Level = reader.ReadByte();
				eventData.ActivityId = reader.ReadInt64();
				eventData.RelatedActivityId = reader.ReadInt64();
				eventData.DataLength = reader.ReadInt32();
				eventData.Payload = new List<object>();

				for (int i = 0; i < eventData.DataLength; i++)
				{
					var dataType = reader.ReadByte();

					switch (dataType)
					{
						case 0:
							eventData.Payload.Add(reader.ReadString());
							break;
						case 1:
							eventData.Payload.Add(reader.ReadInt32());
							break;
						case 2:
							eventData.Payload.Add(reader.ReadInt64());
							break;
					}
				}

				return eventData;
			}
			catch (IOException e)
			{
				return null;
			}
		}

		private static Manifest ReadManifest(string manifestFilePath)
		{
			var serializer = new XmlSerializer(typeof(Manifest), new[] { typeof(ManifestProvider), typeof(ManifestEvent) });

			using (var s = File.OpenRead(manifestFilePath))
			{
				return serializer.Deserialize(s) as Manifest;
			}
		}
	}
}
