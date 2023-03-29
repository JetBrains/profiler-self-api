using JetBrains.Annotations;

namespace JetBrains.Profiler.SelfApi.Impl.Unix.Elf
{
  internal sealed class ElfInfo
  {
    public readonly ElfClass Class;
    public readonly ElfData Data;
    public readonly ElfFlags Flags;

    [CanBeNull] public readonly string Interpreter;

    public readonly ElfMachine Machine;
    public readonly ElfOsAbi OsAbi;
    public readonly byte OsAbiVersion;
    public readonly ElfType Type;

    public ElfInfo(ElfClass @class, ElfData data, ElfOsAbi osAbi, byte osAbiVersion, ElfType type, ElfMachine machine, ElfFlags flags, [CanBeNull] string interpreter)
    {
      Class = @class;
      Data = data;
      OsAbi = osAbi;
      OsAbiVersion = osAbiVersion;
      Type = type;
      Machine = machine;
      Flags = flags;
      Interpreter = interpreter;
    }
  }
}