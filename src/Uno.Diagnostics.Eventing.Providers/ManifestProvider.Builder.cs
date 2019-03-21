using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Uno.Diagnostics.Eventing.Providers.Helpers;
using Uno.Extensions;

namespace Uno.Services.Diagnostics.Eventing
{
    public partial class ManifestProvider
    {
		private static ManifestProvider[] _providers;
		private static object _gate = new object();

		/// <summary>
		/// Gets a list of providers available for the current process
		/// </summary>
		public static ManifestProvider[] GetProviders()
		{
			lock (_gate)
			{
				if (_providers == null)
				{
					var q = from assembly in GetAllAssembies()
							from type in GetTypes(assembly)
							where type.Name == "TraceProvider"
							let actualType = GetActualType(type)
							let idField = actualType.GetField("Id")
							where idField != null
							let eventFields = from field in actualType.GetFields()
											  where field.FieldType == typeof(int)
											  select new ManifestEvent
											  {
												  EventName = field.Name,
												  EventId = (int)field.GetValue(null)
											  }
							select new ManifestProvider
							{
								ProviderId = idField.GetValue(null)?.ToString(),
								ProviderName = actualType.DeclaringType.Name,
								Events = eventFields.ToArray()
							};

					_providers = q.ToArray();

					// Display the providers that can be used when capturing with PerfView
					DebugHelper.WriteLine("Available providers: " + _providers.Select(p => "*Uno-" + p.ProviderName).JoinBy(","));
				}

				return _providers;
			}
		}

		private static Assembly[] GetAllAssembies()
		{
#if NETFX_CORE
			var assemblies = new List<Assembly>();

			var files = Windows.ApplicationModel.Package.Current.InstalledLocation.GetFilesAsync().AsTask().Result;
			if (files == null)
			{
				return assemblies.ToArray();
			}

			foreach (var file in files.Where(file => file.FileType == ".dll" || file.FileType == ".exe"))
			{
				try
				{
					assemblies.Add(Assembly.Load(new AssemblyName(file.DisplayName)));
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex.Message);
				}
			}

			return assemblies.ToArray();
#else
			return AppDomain.CurrentDomain.GetAssemblies();
#endif
		}

		private static Type GetActualType(Type type)
		{
			Console.WriteLine($"Getting actual type for {type}.");
			try
			{
				return type.GetTypeInfo().IsGenericType ?
					type.MakeGenericType(
						type
							.GetGenericArguments()
							.Select(t => t.GetTypeInfo().GetGenericParameterConstraints().FirstOrDefault() ?? typeof(object))
							.ToArray())
					: type;
			}
			catch (Exception e)
			{
				throw new Exception($"Failed to generate manifest for {type}", e);
			}
		}

		private static Type[] GetTypes(System.Reflection.Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (Exception e)
			{
				Console.WriteLine($"Unable to read types for {assembly.FullName}, {e}");
				return new Type[0];
			}
		}
	}
}
