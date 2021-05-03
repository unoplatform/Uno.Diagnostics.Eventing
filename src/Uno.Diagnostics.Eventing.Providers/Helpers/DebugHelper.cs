using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uno;

namespace Uno.Diagnostics.Eventing.Providers.Helpers
{
    class DebugHelper
    {
		private static readonly Action<string> _writeLine;

		static DebugHelper()
		{
#if !METRO

#if __IOS__ || __MACOS || __ANDROID__ || NETSTANDARD2_0 || NET5_0 || NET6_0_OR_GREATER
			// Bypass the conditional attribute
			// Xamarin uses Console.WriteLine because Debug.WriteLine writes each entry twice
			// in VS's output window, and use particularly slow.
			var mi = typeof(System.Console).GetMethod("WriteLine", new[] { typeof(string) });
#else
			// Bypass the conditional attribute
			var mi = typeof(System.Diagnostics.Debug).GetMethod("WriteLine", new[] { typeof(string) });
#endif

			if (mi != null)
			{
				_writeLine = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), mi);
			}
			else
			{
				_writeLine = Actions<string>.Null;
			}
#else
			var mi = typeof(System.Diagnostics.Debug)
							.GetTypeInfo()
							.GetDeclaredMethods("WriteLine")
							.First(m => m.GetParameters().Count(p => p.ParameterType == typeof(string)) == 1);

			_writeLine = (Action<string>)mi.CreateDelegate(typeof(Action<string>));
#endif
		}

		/// <summary>
		/// Writes a mesage to the debugger output
		/// </summary>
		/// <param name="message"></param>
		public static void WriteLine(string message) 
			=> _writeLine(message);
	}
}
