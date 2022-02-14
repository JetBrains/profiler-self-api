using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using JetBrains.Profiler.SelfApi.Impl.Unix;

namespace JetBrains.Profiler.SelfApi.Impl.Linux
{
  internal static class LinuxHelper
  {
    private static readonly Lazy<Tuple<LinuxLibCId, ArchitectureId>> ourLibCLazy = new(DeduceElfConfig);

    public static LinuxLibCId LibC => ourLibCLazy.Value.Item1;
    public static ArchitectureId ProcessArchitecture => ourLibCLazy.Value.Item2;

    private static LinuxLibCId GetInterpreterLibC([NotNull] string interpreter)
    {
      if (interpreter == null)
        throw new ArgumentNullException(nameof(interpreter));

      // Note(ww898): Removing interpreter directory is NixOS support because interpreter path contains hash:
      //                /nix/store/c1nqsqwl9allxbxhqx3iqfxk363qrnzv-glibc-2.32-54/lib/ld-linux-aarch64.so.1
      //                /nix/store/jsp3h3wpzc842j0rz61m5ly71ak6qgdn-glibc-2.32-54/lib/ld-linux-x86-64.so.2 (attention: lib64 was changed to lib)
      var interpreterFileName = interpreter.Substring(interpreter.LastIndexOf('/') + 1);
      if (interpreterFileName.StartsWith("ld-linux"))
        return LinuxLibCId.Glibc;
      if (interpreterFileName.StartsWith("ld-musl"))
        return LinuxLibCId.Musl;
      throw new PlatformNotSupportedException();
    }

    private static Tuple<LinuxLibCId, ArchitectureId> DeduceElfConfig()
    {
      var elfInfo = ElfUtil.GetElfInfo("/proc/self/exe");
      if (elfInfo.OsAbi is not (ElfOsAbi.ELFOSABI_NONE or ElfOsAbi.ELFOSABI_LINUX))
        throw new FormatException("Invalid Linux ELF OS ABI");
      if (elfInfo.Type is not (ElfType.ET_DYN or ElfType.ET_EXEC))
        throw new FormatException("Invalid Linux ELF e_type");
      return Tuple.Create(
        GetInterpreterLibC(elfInfo.Interpreter ?? throw new FormatException("Can't find ELF program interpreter")),
        elfInfo.Machine switch
          {
            ElfMachine.EM_X86_64 => ArchitectureId.X64,
            ElfMachine.EM_AARCH64 => ArchitectureId.Arm64,
            ElfMachine.EM_ARM => ArchitectureId.Arm,
            _ => throw new FormatException("Invalid Linux ELF e_machine")
          });
    }
  }
}