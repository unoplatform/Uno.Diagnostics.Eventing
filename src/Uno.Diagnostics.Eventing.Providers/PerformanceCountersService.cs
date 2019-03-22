using System;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Threading;
using Uno;
using System.Diagnostics;
using Uno.Services.Contract.Diagnostics;
using System.Reactive.Subjects;
using Uno.Collections;
using Uno.Extensions;
using Uno.Logging;
using System.Reactive.Concurrency;

namespace Uno.Services.Diagnostics
{
	/// <summary>
	/// How the PerformanceCountersService Report behave with the cleaned-up entries.
	/// </summary>
	public enum PerformanceCountersServiceReportRollupBehavior
	{
		/// <summary>
		/// The report will consider stats of currently tracked entries, so the report will be consistent with the actual "Entries" property. All rolluped entires are ignored.
		/// </summary>
		TrackedAliveEntriesOnly,

		// <summary>
		// NOT IMPLEMENTED: 
		// The report will consider stats of no longer tracked entries, eg.: the Minimum will be the minimum since the start of the tracking, ever if no longer available in the .Entries property.
		// </summary>
		// TrackedAllEntries,
	}

	public class PerformanceCountersService : IPerformanceCountersService
	{
		private readonly Action<MeasureEntry> _callback;
		private LinkedList<MeasureEntry> _entries = new LinkedList<MeasureEntry>();
		private SynchronizedDictionary<string, long> _counters = new SynchronizedDictionary<string, long>();

		private Subject<(string counter, long count)> _countersChanged = new Subject<(string counter, long count)>();
		private Subject<(string counter, TimeSpan duration)> _measuresChanged = new Subject<(string counter, TimeSpan duration)>();
		private readonly int _maxEntries;
		private readonly int _keepLastEntriesCount;
		private readonly PerformanceCountersServiceReportRollupBehavior _rollupBehavior;

		/// <param name="maxEntries">
		/// The maximum number of entries to keep track and that will be available using the Entries property. Lower the value to reduce the maximum memory usage of the PerformanceCountersService.
		/// </param>
		/// <param name="keepLastEntriesCount">
		/// The minimum number of entries to keep track of. When the maximum number of entires has been reach, a number lastest entries == keepLastEntriesCount will still be available in the.Entries property.
		/// Try to avoid keepLastEntriesCount too close to maxEntries to avoid performance overhead.
		/// </param>
		/// <param name="rollupBehavior">The behavior of the report when some MeasureEntires has been rollup</param>
		public PerformanceCountersService(Action<MeasureEntry> callback = null, int? maxEntries = null, int? keepLastEntriesCount = null, PerformanceCountersServiceReportRollupBehavior rollupBehavior = PerformanceCountersServiceReportRollupBehavior.TrackedAliveEntriesOnly)
		{
			_callback = callback;
			_maxEntries = maxEntries.GetValueOrDefault(int.MaxValue);
			_keepLastEntriesCount = keepLastEntriesCount.GetValueOrDefault((int)(_maxEntries * .1));
			_rollupBehavior = rollupBehavior;
#if DEBUG
			IsEnabled = true;
#endif

#if !METRO
			MainThreadId = Thread.CurrentThread.ManagedThreadId;
#else
			MainThreadId = System.Environment.CurrentManagedThreadId;
#endif
		}

		public int MainThreadId { get; private set; }

		public bool IsEnabled { get; set; }

		public void Reset()
		{
			lock (_entries)
			{
				_entries.Clear();
			}
		}

		public IEnumerable<MeasureEntry> Entries
		{
			get { return _entries.AsEnumerable(); }
		}

		public IDisposable Measure(string name)
		{
			if (!IsEnabled)
			{
				// The using construct supports null disposable
				return null;
			}

			var e = new MeasureEntry { Name = name };

			AddEntry(e);

			e.StartTime = DateTime.UtcNow;

#if !METRO
			e.ThreadId = Thread.CurrentThread.ManagedThreadId;
#else
			e.ThreadId = System.Environment.CurrentManagedThreadId;
#endif

			var w = Stopwatch.StartNew();
			return Actions.ToDisposable(
				() =>
				{
					e.Duration = w.Elapsed;
					Log(e);
				});
		}

		private void AddEntry(MeasureEntry e)
		{
			lock (_entries)
			{
				_entries.AddLast(e);
				if (_entries.Count > _maxEntries)
				{
					RollupEntries();
				}
			}
		}

		private void RollupEntries()
		{
			switch (_rollupBehavior)
			{
				case PerformanceCountersServiceReportRollupBehavior.TrackedAliveEntriesOnly:
					{
						CleanupOldEntries();
					}
					break;

				// NOTE : PerformanceCountersServiceReportRollupBehavior.TrackedAllEntries, we could precalculate the report summary, so we don't leak memory with MeasureEntries, but keep track of data for the repord.
				default:
					{
						this.Log().Warn("PerformanceCountersService : PerformanceCountersServiceRollupBehavior not implemented");
					}
					break;
			}
		}

		private void CleanupOldEntries()
		{
			this.Log().Info("PerformanceCountersService : Rollup - Cleaning old entries");

			var nbToRemove = _entries.Count - _keepLastEntriesCount;
			for (var i = 0; i < nbToRemove; i++)
			{
				_entries.RemoveFirst();
			}
		}

		public void Measure(string name, Action action)
		{
			if (!IsEnabled)
			{
				action();
			}
			else
			{
				var e = new MeasureEntry() { Name = name };

				AddEntry(e);

				e.StartTime = DateTime.UtcNow;

#if !METRO
				e.ThreadId = Thread.CurrentThread.ManagedThreadId;
#else
				e.ThreadId = System.Environment.CurrentManagedThreadId;
#endif
				var w = Stopwatch.StartNew();

				action();

				e.Duration = w.Elapsed;
				Log(e);
			}
		}

		private void Log(MeasureEntry entry)
		{
			if (_callback != null)
			{
				_callback(entry);
			}
		}

		public CounterReport[] GenerateReport()
		{
			if (!IsEnabled)
			{
				return new CounterReport[0];
			}

			lock (_entries)
			{
				var q = from entry in _entries
						group entry by entry.Name into counters
						let values = counters.ToArray()
						orderby values.Average(c => c.Duration.SelectOrDefault(_ => _.Value.Ticks)) descending
						select FormatEntry(counters.Key, values);

				var array = q.ToArray();

				this.Log().DebugFormat("Performance report: {0}\n{1}", ReportHeader, string.Join("\n", array.Select(e => e.Name + ": " + e.Details).ToArray()));

				return array;
			}
		}

		public long Increment(string name)
		{
			var value = _counters.Lock.Write(c =>

				c[name] = c.UnoGetValueOrDefault(name, 0) + 1
			);

			_countersChanged.OnNext((name, value));

			return value;
		}

		public long Decrement(string name)
		{
			var value = _counters.Lock.Write(c => c[name] = c.UnoGetValueOrDefault(name, 1) - 1);

			_countersChanged.OnNext((name, value));

			return value;
		}

		public long SetCount(string name, long count)
		{
			var value = _counters.Lock.Write(c => c[name] = count);

			_countersChanged.OnNext((name, value));

			return value;
		}

		public IObservable<long> ObserveCounter(string name)
		{
			return _countersChanged
				.Where(e => e.counter == name)
				.Select(e => e.count)
				.StartWith(Scheduler.Default, _counters.UnoGetValueOrDefault(name));
		}

		public IObservable<(string counter, long count)> ObserveCounters()
		{
			return _countersChanged
				.StartWith(
					Scheduler.Default,
					_counters.Select(p => (p.Key, p.Value)).ToArray()
				);
		}

		public IObservable<TimeSpan> ObserveMeasure(string name)
		{
			return _measuresChanged
				.Where(e => e.counter == name)
				.Select(e => e.duration);
		}

		public IObservable<(string counter, TimeSpan duration)> ObserveMeasures()
		{
			return _measuresChanged;
		}

		public static string ReportHeader
		{
			get { return "(Min/Max/Average/Stdev/Sum/UIratio)"; }
		}

		private CounterReport FormatEntry(string counterName, IEnumerable<MeasureEntry> measures)
		{
			var validMeasures = measures.Where(m => m.Duration != null).ToArray();

			var count = validMeasures.Count();
			var mainThreadCount = validMeasures.Count(x => x.ThreadId == MainThreadId);
			var mainThreadRatio = Math.Round(mainThreadCount > 0 ? Math.Round((double) mainThreadCount/count, 2) : 0.0, 2);

			if (count > 1)
			{
				var minTime = validMeasures.Min(c => c.Duration.Value);
				var maxTime = validMeasures.Max(c => c.Duration.Value);
				var avgTime = TimeSpan.FromMilliseconds(validMeasures.Average(c => c.Duration.Value.TotalMilliseconds));
				var stdDevTime = validMeasures.Select(c => c.Duration.Value.TotalMilliseconds).StdDev();
				var totalTime = TimeSpan.FromMilliseconds(validMeasures.Sum(c => c.Duration.Value.TotalMilliseconds));
				return new CounterReport()
					{
						Name = counterName,
						Count = count,
						Min = minTime,
						Max = maxTime,
						Avg = avgTime,
						StdDev = stdDevTime,
						Total = totalTime,
						MainThreadRatio = mainThreadRatio
					};
			}

			var duration = count > 0 ? validMeasures[0].Duration.Value : TimeSpan.Zero;
			return new CounterReport()
				{
					Name = counterName,
					Count = count,
					Min = duration,
					Max = duration,
					Avg = duration,
					StdDev = 0,
					Total = duration,
					MainThreadRatio = mainThreadRatio
				};
		}
	}

	public class MeasureEntry
	{
		public string Name { get; set; }

		public DateTime StartTime { get; set; }

		public TimeSpan? Duration { get; set; }

		public int ThreadId { get; set; }
	}
}
