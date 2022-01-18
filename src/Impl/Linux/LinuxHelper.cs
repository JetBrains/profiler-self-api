using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace JetBrains.Profiler.SelfApi.Impl.Linux
{
  internal static class LinuxHelper
  {
    private static readonly Lazy<LinuxLibCId> ourLibCLazy = new(DeduceLibCBasedOnRuntimeLinker);

    public static LinuxLibCId LibC => ourLibCLazy.Value;

    private static unsafe string GetProcSelfExeProgramInterpreter()
    {
      using var mappedFile = MemoryMappedFile.CreateFromFile("/proc/self/exe", FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
      using var mappedView = mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

      byte* pointer = null;
      mappedView.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
      try
      {
        var ehdr = (Elf.Elf64_Ehdr*)pointer;
        if (sizeof(Elf.Elf64_Ehdr) > mappedView.Capacity)
          throw new Exception("Can't parse ELF header");

        var phdr = (Elf.Elf64_Phdr*)(pointer + ehdr->e_phoff);
        if ((long)ehdr->e_phoff + ehdr->e_phnum * sizeof(Elf.Elf64_Phdr) > mappedView.Capacity ||
            sizeof(Elf.Elf64_Phdr) != ehdr->e_phentsize)
          throw new Exception("Can't parse ELF program header");
        for (var phi = 0; phi < ehdr->e_phnum; ++phi, ++phdr)
          if (phdr->p_type == Elf.ElfSegmentType.PT_INTERP)
          {
            if ((long)(phdr->p_offset + phdr->p_filesz) > mappedView.Capacity ||
                Unix.LibC.strnlen((IntPtr)(pointer + phdr->p_offset), phdr->p_filesz) != phdr->p_filesz - 1)
              throw new Exception("Can't parse ELF program interpreter");
            return Marshal.PtrToStringAnsi((IntPtr)(pointer + phdr->p_offset), (int)phdr->p_filesz - 1);
          }
      }
      finally
      {
        mappedView.SafeMemoryMappedViewHandle.ReleasePointer();
      }

      throw new Exception("Can't find ELF program interpreter");
    }

    private static LinuxLibCId DeduceLibCBasedOnRuntimeLinker()
    {
      // Note(k15tfu): Checks the runtime linker specified by PT_INTERP program header entry.
      var interpreter = GetProcSelfExeProgramInterpreter();
      if (interpreter.EndsWith("/ld-linux-x86-64.so.2") || interpreter.EndsWith("/ld-linux-aarch64.so.1"))
        return LinuxLibCId.Glibc; // Most of Linux distros
      if (interpreter.EndsWith("/ld-musl-x86_64.so.1") || interpreter.EndsWith("/ld-musl-aarch64.so.1"))
        return LinuxLibCId.Musl; // Alpine
      throw new PlatformNotSupportedException();
    }
  }
}