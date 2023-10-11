---
uid: Uno.Diagnostics.Eventing.Using
---

**Uno.Diagnostics.Eventing** is a set of packages and tools that enable the use of the [Windows Performance Analyzer](https://docs.microsoft.com/en-us/windows-hardware/test/wpt/windows-performance-analyzer) tooling for Uno Platform apps using iOS, Android, Linux, Windows and WebAssembly.

## Event Tracing using Uno Platform

Uno Platform provides an abstraction for event tracing that allows for the generation of custom trace events to be visualized in the WPA tools from the Windows Platform SDK.

An trace event is a timestamped data structure that can be mapped on a timeline, correlated with other events, such as start/stop pairs and related events.

While Windows provides the registration and authoring of event sources natively, other platforms do not provide this, and Uno Platform provides this abstraction through the `IEventProvider` interface.

To create your own event provider, [follow this guide](xref:Uno.Diagnostics.Eventing.CreateProvider).

## Prepare your workstation for tracing

### PerfView

Ensure you have the latest version of **PerfView**. You can download it [from GitHub](https://github.com/Microsoft/perfview/releases). Note that it's an executable only, there's no installer.

> **PerfView** is the tool used to gather metrics while the application is running
> (or to translate from Android & iOS)

### WPA - Windows Performance Analyzer

You must have **Windows Performance Analyzer** (_WPA_) installed on your machine. This tool
is installed from the _Windows SDK_. To install it:

  1. Right-click on _Start_ on Windows
  2. Click `Apps & Features` (usually the first menu option)
  3. Locate the most recent version of `Windows Software Development Kit` installed
     on your machine (you should have more than one, use version number to locate the latest)
  4. Click `Modify`
  5. Click `Next` (operation should be `Change`)
  6. Check the `Windows Performance Toolkit` (usually the first option)

> **Windows Performance Analyzer** is the tool to analyze the result.

## Prepare your app for tracing

The tracing output will be composed of two files: the trace file and the trace manifest.

The trace file contains the raw data of the events, which will be transformed into an ETL
file in a later step. The manifest file contains the description of all the event source found
via reflection in all the public classes available in the current AppDomain.

First, add the following NuGet packages to your project:
- [`Uno.Diagnostics.Eventing.Providers`](https://www.nuget.org/packages/Uno.Diagnostics.Eventing.Providers)
- [`System.Reactive`](https://www.nuget.org/packages/System.Reactive)

Then, activate the tracing with the following:

# [**Android**](#tab/setup-android)

``` csharp
public static void Android_EnableTracing()
{
    Uno.Diagnostics.Eventing.Tracing.IsEnabled = true;

    var traceFolder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments);

    if (!traceFolder.Exists())
    {
        traceFolder.Mkdirs();
    }

    // You may need to add permission WRITE_EXTERNAL_STORAGE in manifest
    // to avoid the next line to produce a System.UnauthorizedAccessException
    Uno.Diagnostics.Eventing.Tracing.Factory =
        new Uno.Services.Diagnostics.Eventing.EventProviderFactory(
            new Uno.Services.Diagnostics.Eventing.FileEventSink(traceFolder.AbsolutePath)
        );
}
```

Then invoke the `Android_EnableTracing()` in the Android application constructor.

# [**iOS**](#tab/setup-ios)

``` csharp
public static void IOS_EnableTracing()
{
    Uno.Diagnostics.Eventing.Tracing.IsEnabled = true;

    var traceFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    var traceUnoFolder = Path.Combine(traceFolder, "UnoTrace");

    if (!Directory.Exists(traceUnoFolder))
    {
        Directory.CreateDirectory(traceUnoFolder);
    }

    Uno.Diagnostics.Eventing.Tracing.Factory =
        new Uno.Services.Diagnostics.Eventing.EventProviderFactory(
            new Uno.Services.Diagnostics.Eventing.FileEventSink(traceUnoFolder, new System.Reactive.Concurrency.EventLoopScheduler())
        );
}
```

Invoke the `IOS_EnableTracing()` in the app entry point, before the call to `UIApplication.Main`.

# [**Skia (WPF/Gtk/Framebuffer)**](#tab/setup-skia)

``` csharp
public static void EnableTracing()
{
    Uno.Diagnostics.Eventing.Tracing.IsEnabled = true;

    var traceFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    var traceUnoFolder = Path.Combine(traceFolder, "UnoTrace");

    if (!Directory.Exists(traceUnoFolder))
    {
        Directory.CreateDirectory(traceUnoFolder);
    }

    Uno.Diagnostics.Eventing.Tracing.Factory =
        new Uno.Services.Diagnostics.Eventing.EventProviderFactory(
            new Uno.Services.Diagnostics.Eventing.FileEventSink(traceUnoFolder, new System.Reactive.Concurrency.EventLoopScheduler())
        );
}
```

- For Skia+WPF heads, Invoke the `EnableTracing()` in the WPF `App.xaml` constructor, before the call to `new WpfHost()`.
- For the other Skia heads, Invoke the `EnableTracing()` before the `FrameBufferHost` or `GtkHost` invocation in the app's entry point.

# [**WebAssembly**](#tab/setup-wasm)

``` csharp
public static void EnableTracing()
{
    Uno.Diagnostics.Eventing.Tracing.IsEnabled = true;

    var traceFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    var traceUnoFolder = Path.Combine(traceFolder, "UnoTrace");

    if (!Directory.Exists(traceUnoFolder))
    {
        Directory.CreateDirectory(traceUnoFolder);
    }

    Uno.Diagnostics.Eventing.Tracing.Factory =
        new Uno.Services.Diagnostics.Eventing.EventProviderFactory(
            new Uno.Services.Diagnostics.Eventing.FileEventSink(traceUnoFolder, System.Reactive.Concurrency.ImmediateScheduler.Instance)
        );
}
```

Then invoke the `EnableTracing()` before the `Microsoft.UI.Xaml.Application.Start` invocation in the app's entry point.

You will also need to add the following to your application, so that you can download the generated trace file:

```csharp
private async void SaveProfile()
{
    var fileSavePicker = new FileSavePicker();
    fileSavePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
    fileSavePicker.SuggestedFileName = "trace.zip";
    fileSavePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".zip" });

#if !HAS_UNO
    // For Uno.WinUI-based apps
    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
    WinRT.Interop.InitializeWithWindow.Initialize(fileSavePicker, hwnd);
#endif

    StorageFile saveFile = await fileSavePicker.PickSaveFileAsync();
    if (saveFile != null)
    {
        CachedFileManager.DeferUpdates(saveFile);

        var traceFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "UnoTrace");
        var archive = Path.Combine(Windows.Storage.ApplicationData.Current.TemporaryFolder.Path, "trace.zip");
        Directory.CreateDirectory(Windows.Storage.ApplicationData.Current.TemporaryFolder.Path);

        ZipFile.CreateFromDirectory(traceFolder, archive);

        await FileIO.WriteBytesAsync(saveFile, File.ReadAllBytes(archive));

        await CachedFileManager.CompleteUpdatesAsync(saveFile);
    }
}
```

Invoke this method from a button click handler, or a timer.

# [**UWP**](#tab/uwp)

``` csharp
public static void EnableTracing()
{
    Uno.Diagnostics.Eventing.Tracing.IsEnabled = true;

    Uno.Diagnostics.Eventing.Tracing.Factory =
        new Uno.Services.Diagnostics.Eventing.EventProviderFactory();
}
```

Invoke the `EnableTracing()` in the `App` constructor.

***

## Capturing Traces

# [**Uno Platform**](#tab/capture-uno)

Run the application to fill the trace file, which will give as many trace points as necessary.

**Make sure that the tracing is enabled as soon as possible in your application**, otherwise, some components that are started early may not be traced (such as Uno's IoC container).

# [**UWP**](#tab/capture-uwp)

1. Run your app first using a debugger and look for the `Available providers:` providers line in the debug log
2. Download [PerfView](https://github.com/Microsoft/perfview/releases) from Microsoft.
3. Run perfview in command line using the following arguments:
    `perfview /onlyProviders=[content_of_the_providers_log_above** collect output01 -zip:false`
4. Run the application again
5. Stop the Perfview trace
6. Open the trace with `WPA` (see the section below)

> **IMPORTANT**: you should ensure no other software are consuming I/O or CPU during the capture:
> this can alter the result.

***

## Transforming the Uno trace file in a Windows ETL file (Uno Platform heads only)

The transform tool is a runtime replay tool because as the time of writing, the ETL file format is closed, and the available APIs don't allow the creation of custom events, and particularly don't allow for setting the timestamp or threadId of a trace event. This means that for a trace file that was taken over 55 seconds, the transform tool will run for 55 seconds, replaying the events one by one at the appropriate relative time.

1. Grab the trace files :

    * For Android :
        * Determine the path using the ADB logcat, one of the first logging lines will indicate the path to pull
        * Most of the time, running `adb pull /mnt/sdcard/Documents/traces` will be enough.
    * For iOS:
        * Get the container for the installed app using the XCode devices window, or using the file location given by the VS output window.
    * For Skia heads:
        * Get the file using the file location given by the VS output window or the app's console.
    * For WebAssembly:
        * Invoke the method defined (`SaveProfile`) ealier to save the file generated inside the browser's sandbox.

2. Transform the trace files using the **Uno.ETLTranslator tool**, in the Uno solution.

    > [!IMPORTANT] This section can only be run on a Windows machine

    1. Download [PerfView](http://www.microsoft.com/en-ca/download/details.aspx?id=28567) from Microsoft.
    1. Run `dotnet tool install -f dotnet tool install -g Uno.ETLTranslator`
    1. Run the converter `uno-etltranslator path\to\myfile.trace` file you just pulled from the app
    1. When asked, copy/paste and run the Perfview command line into another *Command Prompt* window
    1. Make converter continue and finish by pressing enter
    1. Press the stop collection button in PerfView
    1. Select the "Use empty symbols path"
    1. The output file is now called `PerfViewData.etl` in the `PerfView` folder

Note that enabling the .NET linker/Trimmer removes the trace hints, making the manifest file empty. In such a case, generate a manifest file using a debug build with the linking disabled, in a simulator (to build quickly).

> [!IMPORTANT] You should ensure no other software are consuming I/O or CPU during the translation,
> this cause noise in the result.

## Visualizing the trace using Windows Performance Analyzer (WPA)

WPA is a very powerful, yet very complex tool. The amount of information available may be confusing at first, but given a bit of time, some patterns start to emerge.

To visualize a WPA trace file :

1. Double click it to open, which should start WPA
   (alternatively you can type `start <file>.etl` from command line)
2. In the Pofiles menu, click **browse**, then select the file named
   [`Content\uno.ui.wpaProfile`](https://aka.platform.uno/event-tracing-profile) in the Uno.ETLTranslator project (Uno project).
3. Pre-configured windows will open to profile somehow relevant information.

In the analysis tab, individual the events are displayed. In the tabular view of the generic events window, most of the events will have payload information that detail the events such as the priority of an Background task schedule, or the URI of a Web request.

In the Regions of Interest window, the Start/Stop event are materialized more accurately, giving actual time periods for activities. This can provide duration information about asynchronous operations such as web requests, async methods, etc...

## How to interpret the traces

Uno and Uno components provide out-of-the-box trace providers, which can be analyzer to provide details of the application's behavior, down to the microsecond.

The regions of interest are sorted by the longest to lowest regions, which means that expanding a provider's node will give a good idea of what's costly. When clicking the a region, every region below in the blue highlighted zones will be a child of this region, meaning that this region's duration is including of its children. It may be interesting to check those children regions for performance issues.
