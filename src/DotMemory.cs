using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace JetBrains.Profiler.SelfApi
{
  /// <summary>
  /// The prototype of dotMemory self profiling API to create self-sufficient NuGet package.
  /// Based on single-exe console runner which is supposed to be distributed along with SelfApi dll. 
  /// </summary>
  /// <remarks>
  /// Target use case:
  /// * install NuGet (just NuGet, no any other actions required)
  /// * put in code DotMemory.GetSnapshotOnce (or Attach/GetSnapshot*/Detach)
  /// * deploy to staging
  /// * reproduce issue
  /// * take over generated workspace for investigation 
  /// </remarks>
  public static class DotMemory
  {
    [Serializable]
    public sealed class DotMemoryException : Exception
    {
      public DotMemoryException(string message) : base(message)
      {
      }
      
      public DotMemoryException(SerializationInfo info, StreamingContext context) : base(info, context)
      {
      }
    }

    public sealed class Config
    {
      internal string WorkspaceFile;
      internal bool IsOpenDotMemory;

      public Config SaveToFile(string filePath)
      {
        WorkspaceFile = filePath ?? throw new ArgumentNullException(nameof(filePath));
        return this;
      }
      
      public Config OpenDotMemory()
      {
        IsOpenDotMemory = true;
        return this;
      }
    }


    private const int Timeout = 30000;
    private static readonly object ourMutex = new object();
    private static Session ourSession;

    public static string GetSnapshotOnce()
    {
      return GetSnapshotOnce(new Config());
    }

    public static string GetSnapshotOnce(Config config)
    {
      if (config == null) throw new ArgumentNullException(nameof(config));

      lock (ourMutex)
      {
        if (ourSession != null)
          throw new DotMemoryException("The profiling session is active already: SelfApi.Attach was called early.");

        return RunConsole("get-snapshot", config).AwaitFinished(-1).WorkspaceFile;
      }
    }

    public static void Attach()
    {
      Attach(new Config());
    }

    public static void Attach(Config config)
    {
      if (config == null) throw new ArgumentNullException(nameof(config));

      lock (ourMutex)
      {
        if (ourSession != null)
          throw new DotMemoryException("The profiling session is active still: forgot to call SelfApi.Detach?");

        // TODO: -A = use api ?
        ourSession = RunConsole("attach -s", config).AwaitConnected(Timeout);
      }
    }
    
    public static string Detach()
    {
      lock (ourMutex)
      {
        if (ourSession == null)
          throw new DotMemoryException("The profiling session isn't active: forgot to call SelfApi.Attach?");

        try
        {
          return ourSession.Detach().AwaitFinished(Timeout).WorkspaceFile;
        }
        finally
        {
          ourSession = null;
        }
      }
    }
    
    public static void GetSnapshot(string name = null)
    {
      lock (ourMutex)
      {
        if (ourSession == null)
          throw new DotMemoryException("The profiling session isn't active: forgot to call SelfApi.Attach?");

        ourSession.GetSnapshot(name);
      }
    }

    private static string GetSaveToFilePath()
    {
      return Path.Combine(Path.GetTempPath(), $"{Process.GetCurrentProcess().ProcessName}.{DateTime.Now:yyyy-MM-ddTHH-mm-ss.fff}.dmw");
    }

    private static Session RunConsole(string command, Config config)
    {
      if (config.WorkspaceFile == null)
        config.WorkspaceFile = GetSaveToFilePath();
      
      var commandLine = new StringBuilder();
      commandLine.Append($"{command} {Process.GetCurrentProcess().Id} \"-f={config.WorkspaceFile}\"");

      if (config.IsOpenDotMemory)
        commandLine.Append(" --open-dotmemory");
    
      var console = Process.Start(
        new ProcessStartInfo
        {
          FileName = GetConsoleExecutable(),
          Arguments = commandLine.ToString(),
          CreateNoWindow = true,
          UseShellExecute = false,
          RedirectStandardInput = true,
          RedirectStandardOutput = true,
          RedirectStandardError = true
        }
      );
    
      if (console == null)
        throw new DotMemoryException("Something went wrong: unable to start dotMemoory console profiler.");

      return new Session(console, config.WorkspaceFile);
    }

    private static string GetConsoleExecutable()
    {
      var basePath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().EscapedCodeBase).LocalPath);
      if (basePath == null || !Path.IsPathRooted(basePath))
        throw new DotMemoryException("Unable to deduce absolute path of dotMemory console profiler.");

      return Path.Combine(basePath, "dotMemory.exe");
    }

    private sealed class Session
    {
      [SuppressMessage("ReSharper", "InconsistentNaming")]
      private const string __dotMemory = "##dotMemory";
      
      private readonly List<string> myOutputLines = new List<string>();
      private readonly List<string> myErrorLines = new List<string>();
      private readonly Process myConsole;

      public Session(Process console, string workspaceFile)
      {
        myConsole = console;
        
        console.OutputDataReceived +=
          (sender, args) =>
            {
              if (args.Data != null)
              {
                lock (myOutputLines)
                  myOutputLines.Add(args.Data);
              }
            };

        console.ErrorDataReceived +=
          (sender, args) =>
            {
              if (args.Data != null)
              {
                lock (myErrorLines)
                  myErrorLines.Add(args.Data);
              }
            };

        console.BeginOutputReadLine();
        console.BeginErrorReadLine();
        
        WorkspaceFile = workspaceFile;
      }

      public string WorkspaceFile { get; }

      [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
      public Session Detach()
      {
        Send("disconnect");
        return this;
      }
      
      [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
      public Session GetSnapshot(string name)
      {
        Send("get-snapshot", "name", name);
        return this;
      }
      
      public Session AwaitConnected(int milliseconds)
      {
        var regex = new Regex(__dotMemory + @"\[\x22connected\x22,\s*\{.*\}\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        if (TryAwaitFor(regex, milliseconds) == null)
          throw DotMemoryConsoleException("The dotMemory console profiler was not connected. See details below.");

        return this;
      }
      
      public Session AwaitFinished(int milliseconds)
      {
        if (!myConsole.WaitForExit(milliseconds))
          throw DotMemoryConsoleException("The dotMemory console profiler has not finished in given time. See details below.");

        if (myConsole.ExitCode != 0)
          throw DotMemoryConsoleException("The dotMemory console profiler has failed. See details below.");

        return this;
      }
      
      private Match TryAwaitFor(Regex regex, int milliseconds)
      {
        var startTime = DateTime.UtcNow;
        var lineNum = 0;
        while (true)
        {
          lock (myOutputLines)
          {
            while (lineNum < myOutputLines.Count)
            {
              var line = myOutputLines[lineNum++];
              var match = regex.Match(line);
              if (match.Success)
                return match;
            }
          }

          if (myConsole.HasExited)
            return null;

          if (milliseconds >= 0 && (DateTime.UtcNow - startTime).TotalMilliseconds > milliseconds)
            return null;
        }
      }
      
      private void Send(string command, params string[] args)
      {
        var messageBuilder = new StringBuilder();
        messageBuilder.Append(__dotMemory).Append("[\"").Append(command).Append("\"");

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

        myConsole.StandardInput.WriteLine(messageBuilder.ToString());
      }

      private DotMemoryException DotMemoryConsoleException(string caption)
      {
        var message = new StringBuilder();
        message.AppendLine(caption);
        
        message.AppendLine("*** Standard Error ***");
        lock (myErrorLines)
          message.AppendLine(string.Join(Environment.NewLine, myErrorLines));

        message.AppendLine();
        message.AppendLine("*** Standard Output ***");
        lock (myOutputLines)
          message.AppendLine(string.Join(Environment.NewLine, myOutputLines));
        
        throw new DotMemoryException(message.ToString());
      }
    }
  }
}