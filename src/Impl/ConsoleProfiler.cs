using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace JetBrains.Profiler.SelfApi.Impl
{
  internal interface IResponseCommandProcessor
  {
    void ProcessCommand(string command, string args);
  }

  internal class ConsoleProfiler
  {
    private readonly Process _process;
    private readonly string _prefix;
    private readonly string _presentableName;
    private readonly List<string> _outputLines = new List<string>();
    private readonly List<string> _errorLines = new List<string>();
    private readonly Func<bool> _isReady;
    private readonly IResponseCommandProcessor _commandProcessor;
    private readonly Regex _commandRegex;
    private int _firstOutputLineToProcess;

    public ConsoleProfiler(string executable, string arguments, string messageServicePrefix, string presentableName, Func<bool> isReady, IResponseCommandProcessor commandProcessor = null)
    {
      _prefix = messageServicePrefix;
      _presentableName = presentableName;
      _isReady = isReady;
      _commandProcessor = commandProcessor;
      _commandRegex = BuildCommandRegex("([a-zA-Z-]*)", "(.*)");

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
        (sender, args) =>
          {
            if (args.Data != null)
            {
              Trace.Verbose(args.Data);
              if (_commandProcessor != null)
              {
                var match = _commandRegex.Match(args.Data);
                if (match.Success)
                {
                  _commandProcessor.ProcessCommand(match.Groups[1].Value.ToLower(), match.Groups[2].Value);
                }
              }

              lock (_outputLines)
                _outputLines.Add(args.Data);
            }
          };

      _process.ErrorDataReceived +=
        (sender, args) =>
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

    private Match WaitFor(Regex regex, int milliseconds)
    {
      var startTime = DateTime.UtcNow;
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

        if (milliseconds >= 0 && (DateTime.UtcNow - startTime).TotalMilliseconds > milliseconds)
          return null;

        Thread.Sleep(40);
      }
    }

    private Regex BuildCommandRegex(string command, string argument)
    {
      return new Regex($@"{_prefix}\[\x22{command}\x22(?:,\s*\{{{argument}\}})?\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public bool AwaitResponse(string command, int milliseconds)
    {
      var regex = BuildCommandRegex(command, ".*");
      return WaitFor(regex, milliseconds) != null;
    }

    public void Send(string command, params string[] args)
    {
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
        throw BuildException($"{_presentableName} has not finished in the given time. See details below.");

      if (_process.ExitCode != 0)
        throw BuildException($"{_presentableName} has failed. See details below.");
    }

    public void AwaitConnected(int milliseconds)
    {
      if (!AwaitResponse("connected", milliseconds))
        throw BuildException($"{_presentableName} was not connected. See details below.");

      if (_isReady != null)
      {
        var startTime = DateTime.UtcNow;
        while (!_isReady())
        {
          if (_process.HasExited)
            throw BuildException($"{_presentableName} has exited unexpectedly. See details below.");

          if (milliseconds >= 0 && (DateTime.UtcNow - startTime).TotalMilliseconds > milliseconds)
            throw BuildException("Profiler.Api was not ready in given time. See details below.");

          Thread.Sleep(40);
        }
      }
    }
  }
}