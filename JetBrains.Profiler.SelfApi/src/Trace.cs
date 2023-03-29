using System;
using System.Diagnostics;
using System.Threading;

namespace JetBrains.Profiler.SelfApi
{
  /// <summary>
  /// The entry point for all logging related to self-profiling.
  /// </summary>
  public static class Trace
  {
    /// <summary>
    /// The current instance of <see cref="TraceSource"/> used for all logging inside self-profiling API.
    /// </summary>
    /// <remarks>
    /// You can control trace output via App.config or via programatically added listeners, see examples below.
    /// <example>
    /// Redirect to console using App.config:
    /// <code>
    /// &lt;system.diagnostics&gt;
    ///   &lt;sources&gt;
    ///     &lt;source name="JetBrains.Profiler.SelfApi"
    ///                switchName="SourceSwitch"
    ///                switchType="System.Diagnostics.SourceSwitch" &gt;
    ///         &lt;listeners&gt;
    ///             &lt;add name="Console" /&gt;
    ///         &lt;/listeners&gt;
    ///     &lt;/source&gt;
    ///   &lt;/sources&gt;
    ///   &lt;switches&gt;
    ///     &lt;add name="SourceSwitch" value="Verbose" /&gt;
    ///   &lt;/switches&gt;
    ///   &lt;sharedListeners&gt;
    ///     &lt;add name="Console"
    ///          type="System.Diagnostics.ConsoleTraceListener"
    ///          initializeData="false"/&gt;
    ///   &lt;/sharedListeners&gt;
    ///   &lt;trace autoflush="true" indentsize="4"&gt;
    ///     &lt;listeners&gt;
    ///         &lt;add name="Console" /&gt;
    ///     &lt;/listeners&gt;
    ///   &lt;/trace&gt;
    /// &lt;/system.diagnostics&gt;
    /// </code>
    /// </example>
    /// <example>
    /// <br/>
    /// Redirect to console using programatically added listener:
    /// <code>
    /// Trace.Source.Switch = new SourceSwitch("SourceSwitch", "Verbose");
    /// Trace.Source.Listeners.Add(new ConsoleTraceListener());
    /// </code>
    /// </example>
    /// </remarks>
    public static readonly TraceSource Source = new TraceSource("JetBrains.Profiler.SelfApi");

    private static int _id;

    /// <summary>
    /// Writes message with VERBOSE level.
    /// </summary>
    public static void Verbose(string message)
    {
      Source.TraceEvent(TraceEventType.Verbose, Interlocked.Increment(ref _id), message);
    }

    /// <summary>
    /// Writes message with VERBOSE level.
    /// </summary>
    public static void Verbose(string format, params object[] arguments)
    {
      Source.TraceEvent(TraceEventType.Verbose, Interlocked.Increment(ref _id), format, arguments);
    }

    /// <summary>
    /// Writes message with INFO level.
    /// </summary>
    public static void Info(string message)
    {
      Source.TraceEvent(TraceEventType.Information, Interlocked.Increment(ref _id), message);
    }

    /// <summary>
    /// Writes message with INFO level.
    /// </summary>
    public static void Info(string format, params object[] arguments)
    {
      Source.TraceEvent(TraceEventType.Information, Interlocked.Increment(ref _id), format, arguments);
    }

    /// <summary>
    /// Writes message with ERROR level.
    /// </summary>
    public static void Error(string message)
    {
      Source.TraceEvent(TraceEventType.Error, Interlocked.Increment(ref _id), message);
    }

    /// <summary>
    /// Writes message with ERROR level.
    /// </summary>
    public static void Error(string message, Exception exception)
    {
      Source.TraceEvent(TraceEventType.Error, Interlocked.Increment(ref _id), message + Environment.NewLine + exception);
    }
  }
}