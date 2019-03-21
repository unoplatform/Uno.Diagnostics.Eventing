using System;

namespace Uno.Services.Contract.Diagnostics
{
	/// <summary>
	/// A performance counters service
	/// </summary>
	public interface IPerformanceCountersService
	{
		IDisposable Measure(string name);
		void Measure(string name, Action action);
		bool IsEnabled { get; set; }
		void Reset();

		long Increment(string name);
		long Decrement(string name);
		long SetCount(string name, long count);

		IObservable<long> ObserveCounter(string name);
		IObservable<(string counter, long count)> ObserveCounters();
		IObservable<TimeSpan> ObserveMeasure(string name);
		IObservable<(string counter, TimeSpan duration)> ObserveMeasures();

		CounterReport[] GenerateReport();
	}
}
