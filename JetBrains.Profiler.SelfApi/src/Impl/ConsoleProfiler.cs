using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.HabitatDetector;

namespace JetBrains.Profiler.SelfApi.Impl
{
  internal sealed class ConsoleProfiler
  {
    public const int InfiniteTimeout = -1;

    private readonly Process _process;
    private readonly string _prefix;
    private readonly string _presentableName;
    private readonly List<string> _outputLines = new List<string>();
    private readonly List<string> _errorLines = new List<string>();
    [CanBeNull]
    private readonly Func<bool> _isApiReady;
    private int _firstOutputLineToProcess;

    public ConsoleProfiler(string executable, string arguments, string messageServicePrefix, string presentableName, [CanBeNull] Func<bool> isApiReady, IResponseCommandProcessor commandProcessor = null)
    {
      _prefix = messageServicePrefix;
      _presentableName = presentableName;
      _isApiReady = isApiReady;

      var commandRegex = BuildCommandRegex("([a-zA-Z-]*)", "(.*)");
      var si = new ProcessStartInfo
        {
          FileName = executable,
          Arguments = arguments,
          CreateNoWindow = true,
          UseShellExecute = false,
          RedirectStandardInput = true,
          RedirectStandardOutput = true,
          RedirectStandardError = true
        };

      _process = new Process {StartInfo = si};
      _process.OutputDataReceived +=
        (_, args) =>
          {
            if (args.Data != null)
            {
              Trace.Verbose(args.Data);
              if (commandProcessor != null)
              {
                var match = commandRegex.Match(args.Data);
                if (match.Success)
                {
                  commandProcessor.ProcessCommand(match.Groups[1].Value.ToLower(), match.Groups[2].Value);
                }
              }

              lock (_outputLines)
                _outputLines.Add(args.Data);
            }
          };

      _process.ErrorDataReceived +=
        (_, args) =>
          {
            if (args.Data != null)
            {
              lock (_errorLines)
              {
                _errorLines.Add(args.Data);
                Trace.Verbose(args.Data);
              }
            }
          };

      if (!_process.Start())
        throw new InvalidOperationException($"Unable to start {_presentableName}: Something went wrong");

      _process.BeginOutputReadLine();
      _process.BeginErrorReadLine();
    }

    public bool IsApiUsed => _isApiReady != null;
    private Match WaitFor(Regex regex, int milliseconds)
    {
      var stopwatch = new Stopwatch();
      stopwatch.Start();
      var lineNum = _firstOutputLineToProcess;
      while (true)
      {
        lock (_outputLines)
        {
          while (lineNum < _outputLines.Count)
          {
            var line = _outputLines[lineNum++];
            var match = regex.Match(line);
            if (match.Success)
            {
              _firstOutputLineToProcess = lineNum;
              return match;
            }
          }
        }

        if (_process.HasExited)
          return null;

        if (!IsInfiniteTimeout(milliseconds) && stopwatch.ElapsedMilliseconds > milliseconds)
          return null;

        Thread.Sleep(40);
      }
    }

    private Regex BuildCommandRegex(string command, string argument)
    {
      return new Regex($@"{_prefix}\[\x22{command}\x22(?:,\s*\{{{argument}\}})?\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public void AwaitResponse(string command, int milliseconds)
    {
      var regex = BuildCommandRegex(command, ".*");
      if (WaitFor(regex, milliseconds) == null)
      {
        if (_process.HasExited)
          throw BuildException($"{_presentableName} has exited unexpectedly. See details below.");
        if(!IsInfiniteTimeout(milliseconds))
          throw BuildException($"The command {command} for {_presentableName} has not finished in the given time ({milliseconds} ms).");
      }
    }

    private static bool IsInfiniteTimeout(int milliseconds) => milliseconds <= InfiniteTimeout;

    public void Send(string command, params string[] args)
    {
      if (IsApiUsed)
        throw new InvalidOperationException("It is not possible to send commands if profiler API is used");

      var messageBuilder = new StringBuilder();
      messageBuilder.Append(_prefix).Append("[\"").Append(command).Append("\"");

      if (args != null && args.Length > 0)
      {
        messageBuilder.Append(",{");
        for (var i = 0; i < args.Length; i += 2)
        {
          messageBuilder.Append(args[i]).Append(":");

          if (args[i + 1] != null)
            messageBuilder.Append("\"").Append(args[i + 1].Replace('"', '`')).Append("\"");
          else
            messageBuilder.Append("null");

          if (i > 0)
            messageBuilder.Append(",");
        }

        messageBuilder.Append("}");
      }

      messageBuilder.Append("]");

      var message = messageBuilder.ToString();
      Trace.Verbose(message);
      _process.StandardInput.WriteLine(message);
    }

    private InvalidOperationException BuildException(string caption)
    {
      var message = new StringBuilder();
      message.AppendLine(caption);

      message.AppendLine("*** Standard Error ***");
      lock (_errorLines)
        message.AppendLine(string.Join(Environment.NewLine, _errorLines));

      message.AppendLine();
      message.AppendLine("*** Standard Output ***");
      lock (_outputLines)
        message.AppendLine(string.Join(Environment.NewLine, _outputLines));

      throw new InvalidOperationException(message.ToString());
    }

    public void AwaitFinished(int milliseconds)
    {
      if (!_process.WaitForExit(milliseconds))
        throw BuildException($"{_presentableName} has not finished in the given time ({milliseconds} ms). Try increasing the profiler response timeout using UseCustomResponseTimeout.");

      _process.WaitForExit();

      if (_process.ExitCode != 0)
        throw BuildException($"{_presentableName} has failed. See details below.");
    }

    public void AwaitConnected(int milliseconds)
    {
      if(_isApiReady == null)
        return;

      var stopwatch = new Stopwatch();
      stopwatch.Start();

      while (!_isApiReady())
      {
        if (_process.HasExited)
          throw BuildException($"{_presentableName} has exited unexpectedly. See details below.");

        if (!IsInfiniteTimeout(milliseconds) && stopwatch.ElapsedMilliseconds > milliseconds)
          throw BuildException($"Profiler.Api was not ready in given time ({milliseconds} ms). Try increasing the profiler response timeout using UseCustomResponseTimeout.");

        Thread.Sleep(40);
      }
    }
  }
}