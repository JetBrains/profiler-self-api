namespace JetBrains.Profiler.SelfApi.Impl
{
  internal interface IResponseCommandProcessor
  {
    void ProcessCommand(string command, string args);
  }
}