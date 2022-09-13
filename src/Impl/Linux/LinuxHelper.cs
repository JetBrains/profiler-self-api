using System;
using System.IO;
using JetBrains.Annotations;
using JetBrains.Profiler.SelfApi.Impl.Unix.Elf;

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

      // Note(ww898,k15tfu): Removing interpreter directory is NixOS/snap support (see https://youtrack.jetbrains.com/issue/RIDER-55371):
      //   /nix/store/c1nqsqwl9allxbxhqx3iqfxk363qrnzv-glibc-2.32-54/lib/ld-linux-aarch64.so.1
      //   /nix/store/jsp3h3wpzc842j0rz61m5ly71ak6qgdn-glibc-2.32-54/lib/ld-linux-x86-64.so.2
      //   /snap/core18/current/lib64/ld-linux-x86-64.so.2
      //   /snap/core20/current/lib/ld-linux-aarch64.so.1
      //   /snap/core20/current/lib/ld-linux-armhf.so.3
      //   /snap/core18/current/lib/ld-linux.so.2
      //   /lib/ld-linux-aarch64.so.1
      //   /lib/ld-linux-armhf.so.3
      //   /lib/ld-linux.so.2
      //   /lib/ld-musl-aarch64.so.1
      //   /lib/ld-musl-armhf.so.1
      //   /lib/ld-musl-i386.so.1
      //   /lib/ld-musl-x86_64.so.1
      var n = interpreter.LastIndexOf('/');
      var interpreterFileName = interpreter.Substring(n + 1);
      if (interpreterFileName.StartsWith("ld-linux-") ||
          interpreterFileName.StartsWith("ld-linux."))
        return LinuxLibCId.Glibc;
      if (interpreterFileName.StartsWith("ld-musl-") ||
          interpreterFileName.StartsWith("ld-musl."))
        return LinuxLibCId.Musl;

      // Note(ww898,k15tfu): Something special for dotnet-sdk installed via new snap (see https://youtrack.jetbrains.com/issue/RIDER-80530):
      //   /snap/dotnet-sdk/current/lib/x86_64-linux-gnu/ld-2.27.so
      //   /lib/x86_64-linux-gnu/ld-2.27.so
      if (interpreterFileName.StartsWith("ld-") ||
          interpreterFileName.StartsWith("ld."))
      {
        // Note(ww898,k15tfu): See https://llvm.org/doxygen/Triple_8h_source.html / https://llvm.org/doxygen/Triple_8cpp_source.html
        //   x86_64-linux-gnu
        //   aarch64-linux-gnu 
        //   arm-linux-gnueabihf
        //   aarch64-linux-musl
        //   arm-linux-musleabihf
        var k = interpreter.LastIndexOf('/', n - 1, n);
        var triple = interpreter.Substring(k + 1, n - k - 1);
        if (triple.Contains("-linux-gnu"))
          return LinuxLibCId.Glibc;
        if (triple.Contains("-linux-musl"))
          return LinuxLibCId.Musl;
      }

      // Bug(ww898,k15tfu): Please don't call external processes like ldd to detect GLibC/Musl in OS because it's incorrect for the current process (see NixOS/snap/...)!!! 
      throw new PlatformNotSupportedException($"Unknown ELF interpreter {interpreter}");
    }

    private static Tuple<LinuxLibCId, ArchitectureId> DeduceElfConfig()
    {
      using var fs = File.OpenRead("/proc/self/exe");
      var elfInfo = ElfUtil.GetElfInfo(fs);
      if (elfInfo.OsAbi is not (ElfOsAbi.ELFOSABI_NONE or ElfOsAbi.ELFOSABI_LINUX))
        throw new FormatException($"Invalid Linux ELF e_ident[EI_OSABI] {elfInfo.OsAbi}");
      if (elfInfo.Type is not (ElfType.ET_DYN or ElfType.ET_EXEC))
        throw new FormatException($"Invalid Linux ELF e_type {elfInfo.Type}");
      return Tuple.Create(
        GetInterpreterLibC(elfInfo.Interpreter ?? throw new FormatException("Can't find ELF program interpreter")),
        elfInfo.Class switch
          {
            ElfClass.ELFCLASS32 => elfInfo.Machine switch
              {
                ElfMachine.EM_ARM => ArchitectureId.Arm,
                _ => throw new FormatException($"Invalid Linux ELF e_ident[EI_CLASS] {elfInfo.Class} and e_machine {elfInfo.Machine}")
              },
            ElfClass.ELFCLASS64 => elfInfo.Machine switch
              {
                ElfMachine.EM_X86_64 => ArchitectureId.X64,
                ElfMachine.EM_AARCH64 => ArchitectureId.Arm64,
                _ => throw new FormatException($"Invalid Linux ELF e_ident[EI_CLASS] {elfInfo.Class} and e_machine {elfInfo.Machine}")
              },
            _ => throw new FormatException($"Invalid Linux ELF e_ident[EI_CLASS] {elfInfo.Class}")
          });
    }
  }
}