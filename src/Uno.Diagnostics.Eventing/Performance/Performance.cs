using System;
using Uno.Services.Contract.Diagnostics;
using Uno.Diagnostics.Eventing;
using Uno;
using Uno.Diagnostics.Eventing.Helpers;

namespace Uno.Services.Diagnostics
{
	/// <summary>
	/// A performance measuring class.
	/// </summary>
	/// <remarks>If the <see cref="Performance.IsEnabled"/> is false, then the the measurments are forwarded to <see cref="Uno.Diagnostics.Eventing.Tracing"/>.</remarks>
	public static class Performance
    {
        private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{325E3F8F-1DB7-4A2F-BAA8-F9B3D4967CBD}");

			public const int Performance_MeasureStart = 1;
			public const int Performance_MeasureStop = 2;
		}

		/// <summary>
		/// The performance counters service instance.
		/// </summary>
        public static IPerformanceCountersService Service {
            get;
            set;
        }


		/// <summary>
		/// Measures the time between the call to measure, and the call to dispose of the returned instance.
		/// </summary>
		/// <param name="name">The name of the measured scope</param>
		/// <returns>An IDisposable instance that will stop the measure of the scope</returns>
		public static IDisposable Measure(string name)
		{
			if (Service != null && Service.IsEnabled)
			{
				return Service.Measure(name);
			}
			else
			{
                if (_trace.IsEnabled)
                {
                    return _trace.WriteEventActivity(
                        TraceProvider.Performance_MeasureStart,
                        TraceProvider.Performance_MeasureStop,
                        new[] { name }
                    );
                }
                else
                {
                    // Null is acceptable as a disposable value, the using construct supports it.
                    return null;
                }
			}
		}

		/// <summary>
		/// Measures the time between the call to measure, and the call to dispose of the returned instance.
		/// </summary>
		/// <param name="name">A func that provides the name of the measured scope</param>
		/// <returns>An IDisposable instance that will stop the measure of the scope</returns>
		public static IDisposable Measure(Func<string> name)
		{
			if (Service != null && Service.IsEnabled)
			{
				return Service.Measure(name());
			}
			else
            {
                if (_trace.IsEnabled)
                {
                    return _trace.WriteEventActivity(
                        TraceProvider.Performance_MeasureStart,
                        TraceProvider.Performance_MeasureStop,
                        new[] { name() }
                    );
                }
                else
                {
                    // Null is acceptable as a disposable value, the using construct supports it.
                    return null;
                }
			}
		}

		/// <summary>
		/// Measures the time spent in the specified action.
		/// </summary>
		/// <param name="name">The logical name of the action</param>
		/// <param name="action">The action to measure</param>
		public static void Measure(string name, Action action)
		{
			if (Service != null && Service.IsEnabled)
			{
				Service.Measure(name, action);
			}
			else
            {
                if (_trace.IsEnabled)
                {
                    using (_trace.WriteEventActivity(
                        TraceProvider.Performance_MeasureStart,
                        TraceProvider.Performance_MeasureStop,
                        new[] { name }
                    ))
                    {
                        action();
                    }
                }
                else
                {
                    action();
                }
			}
		}

		/// <summary>
		/// Measures the time spent in the specified action.
		/// </summary>
		/// <param name="name">A func that provides logical name of the action</param>
		/// <param name="action">The action to measure</param>
		public static void Measure(Func<string> name, Action action)
		{
			if (Service != null && Service.IsEnabled)
			{
				Service.Measure(name(), action);
			}
			else
			{
				if (_trace.IsEnabled)
				{
					using (_trace.WriteEventActivity(
						TraceProvider.Performance_MeasureStart,
						TraceProvider.Performance_MeasureStop,
						new[] { name() }
					))
					{
						action();
					}
				}
				else
				{
					action();
				}
			}
		}

		/// <summary>
		/// Measures the time spent in the specified func, and returns its value.
		/// </summary>
		/// <param name="name">A logical name of the func</param>
		/// <param name="action">The func to measure</param>
		public static T Measure<T>(string name, Func<T> action)
		{
			if (Service != null && Service.IsEnabled)
			{
				T result = default(T);

				Service.Measure(name, () => result = action());

				return result;
			}
			else
            {
                if (_trace.IsEnabled)
                {
                    using (_trace.WriteEventActivity(
                        TraceProvider.Performance_MeasureStart,
                        TraceProvider.Performance_MeasureStop,
                        new[] { name }
                    ))
                    {
                        return action();
                    }
                }
                else
                {
                    return action();
                }
			}
		}

		/// <summary>
		/// Measures the time spent in the specified func, and returns its value.
		/// </summary>
		/// <param name="name">A func that provides logical name of the func</param>
		/// <param name="action">The func to measure</param>
		public static T Measure<T>(Func<string> name, Func<T> action)
		{
			if (Service != null && Service.IsEnabled)
			{
				T result = default(T);

				Service.Measure(name(), () => result = action());

				return result;
			}
			else
			{
				if (_trace.IsEnabled)
				{
					using (_trace.WriteEventActivity(
						TraceProvider.Performance_MeasureStart,
						TraceProvider.Performance_MeasureStop,
						new[] { name() }
					))
					{
						return action();
					}
				}
				else
				{
					return action();
				}
			}
		}

		/// <summary>
		/// Increases the specified counter name for the duration of the disposable scope. Once the scope is disposed, the counter is decreased.
		/// </summary>
		/// <param name="name">The name of the counter</param>
		/// <returns></returns>
		public static IDisposable Count(string name)
		{
			if (Service != null && Service.IsEnabled)
			{
				Service.Increment(name);

				return new ActionDisposable(() => Service.Decrement(name));
			}

			return null;
		}

		/// <summary>
		/// Increments the specified counter name.
		/// </summary>
		/// <param name="name">The name of the counter</param>
		/// <returns>The previous value of the counter, if any.</returns>
		public static long? Increment(string name)
		{
			if (Service != null && Service.IsEnabled)
			{
				return Service.Increment(name);
			}

			return null;
		}

		/// <summary>
		/// Decrements the specified counter name.
		/// </summary>
		/// <param name="name">The name of the counter</param>
		/// <returns>The previous value of the counter, if any.</returns>
		public static long? Decrement(string name)
		{
			if (Service != null && Service.IsEnabled)
			{
				return Service.Decrement(name);
			}

			return null;
		}

		/// <summary>
		/// Sets the count of the specified counter name.
		/// </summary>
		/// <param name="name">The name of the counter</param>
		/// <param name="count">The count</param>
		/// <returns>The previous value of the counter, if any.</returns>
		public static long? SetCount(string name, long count)
		{
			if (Service != null && Service.IsEnabled)
			{
				return Service.SetCount(name, count);
			}

			return null;
		}

		/// <summary>
		/// Determines if the performance subsystem is enabled.
		/// </summary>
		public static bool IsEnabled
        {
            get { return Service != null && Service.IsEnabled; }
            set
            {
				if(Service == null)
				{
					throw new InvalidOperationException("Service is null, cannot be set to Enabled.");
				}
            	Service.IsEnabled = value;
            }
        }
    }
}
