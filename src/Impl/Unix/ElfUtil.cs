using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace JetBrains.Profiler.SelfApi.Impl.Unix
{
  internal static class ElfUtil
  {
    [NotNull]
    public static unsafe ElfInfo GetElfInfo([NotNull] string file)
    {
      using var mappedFile = MemoryMappedFile.CreateFromFile(file, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
      using var mappedView = mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
      var size = checked((ulong)mappedView.Capacity);
      byte* ptr = null;
      mappedView.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
      try
      {
        if ((ElfMagic)ptr[(int)ElfIdentIndex.EI_MAG0] != ElfMagic.ELFMAG0 ||
            (ElfMagic)ptr[(int)ElfIdentIndex.EI_MAG1] != ElfMagic.ELFMAG1 ||
            (ElfMagic)ptr[(int)ElfIdentIndex.EI_MAG2] != ElfMagic.ELFMAG2 ||
            (ElfMagic)ptr[(int)ElfIdentIndex.EI_MAG3] != ElfMagic.ELFMAG3)
          throw new FormatException("Invalid ELF magics");
        if ((ElfVersion)ptr[(int)ElfIdentIndex.EI_VERSION] != ElfVersion.EV_CURRENT)
          throw new FormatException("Inconsistent ELF version");

        var data = (ElfData)ptr[(int)ElfIdentIndex.EI_DATA];
        var needConvert = BitConverter.IsLittleEndian != data switch
          {
            ElfData.ELFDATA2LSB => true,
            ElfData.ELFDATA2MSB => false,
            _ => throw new FormatException("Inconsistent ELF data")
          };

        ushort GetUInt16(ushort value) => needConvert ? ConvertUInt16(value) : value;
        uint GetUInt32(uint value) => needConvert ? ConvertUInt32(value) : value;
        ulong GetUInt64(ulong value) => needConvert ? ConvertUInt64(value) : value;

        ElfType type;
        ElfMachine machine;
        ElfFlags flags;
        string interpreter = null;
        var @class = (ElfClass)ptr[(int)ElfIdentIndex.EI_CLASS];
        switch (@class)
        {
        case ElfClass.ELFCLASS32:
          if ((uint)sizeof(Elf32_Ehdr) > size)
            throw new FormatException("Too short ELF32 header");
          var elf32Ehdr = (Elf32_Ehdr*)ptr;
          type = (ElfType)GetUInt16(elf32Ehdr->e_type);
          machine = (ElfMachine)GetUInt16(elf32Ehdr->e_machine);
          flags = (ElfFlags)GetUInt32(elf32Ehdr->e_flags);
          if (GetUInt32(elf32Ehdr->e_version) != (uint)ElfVersion.EV_CURRENT)
            throw new FormatException("Invalid version of ELF32 program header");
          if (sizeof(Elf32_Phdr) != GetUInt16(elf32Ehdr->e_phentsize))
            throw new FormatException("Invalid size of ELF32 program header");
          var ePhOff32 = GetUInt32(elf32Ehdr->e_phoff);
          var ePhNum32 = GetUInt16(elf32Ehdr->e_phnum);
          if (ePhOff32 + ePhNum32 * (uint)sizeof(Elf32_Phdr) > size)
            throw new FormatException("Too short ELF32 program header table");
          var elf32Phdr = (Elf32_Phdr*)(ptr + ePhOff32);
          for (var phi = ePhNum32; phi-- > 0; ++elf32Phdr)
            if ((ElfSegmentType)GetUInt32(elf32Phdr->p_type) == ElfSegmentType.PT_INTERP)
            {
              var pFileSz32 = GetUInt32(elf32Phdr->p_filesz);
              var pOffset32 = GetUInt32(elf32Phdr->p_offset);
              if (pOffset32 + pFileSz32 > size)
                throw new FormatException("Too short ELF32 interpreter section");
              if (StrNLenZ(ptr + pOffset32, pFileSz32) != pFileSz32)
                throw new FormatException("Invalid size of ELF32 interpreter section");
              interpreter = Marshal.PtrToStringAnsi((IntPtr)(ptr + pOffset32), checked((int)(pFileSz32 - 1)));
              break;
            }

          break;
        case ElfClass.ELFCLASS64:
          if ((uint)sizeof(Elf64_Ehdr) > size)
            throw new FormatException("Too short ELF64 header");
          var elf64Ehdr = (Elf64_Ehdr*)ptr;
          type = (ElfType)GetUInt16(elf64Ehdr->e_type);
          machine = (ElfMachine)GetUInt16(elf64Ehdr->e_machine);
          flags = (ElfFlags)GetUInt32(elf64Ehdr->e_flags);
          if (GetUInt32(elf64Ehdr->e_version) != (uint)ElfVersion.EV_CURRENT)
            throw new FormatException("Invalid version of ELF64 program header");
          if (sizeof(Elf64_Phdr) != GetUInt16(elf64Ehdr->e_phentsize))
            throw new FormatException("Invalid size of ELF64 program header");
          var ePhOff64 = GetUInt64(elf64Ehdr->e_phoff);
          var ePhNum64 = GetUInt16(elf64Ehdr->e_phnum);
          if (ePhOff64 + ePhNum64 * (ulong)sizeof(Elf64_Phdr) > size)
            throw new FormatException("Too short ELF64 program header table");
          var elf64Phdr = (Elf64_Phdr*)(ptr + ePhOff64);
          for (var phi = ePhNum64; phi-- > 0; ++elf64Phdr)
            if ((ElfSegmentType)GetUInt32(elf64Phdr->p_type) == ElfSegmentType.PT_INTERP)
            {
              var pFileSz64 = GetUInt64(elf64Phdr->p_filesz);
              var pOffset64 = GetUInt64(elf64Phdr->p_offset);
              if (pOffset64 + pFileSz64 > size)
                throw new FormatException("Too short ELF64 interpreter section");
              if (StrNLenZ(ptr + pOffset64, pFileSz64) != pFileSz64)
                throw new FormatException("Invalid size of ELF64 interpreter section");
              interpreter = Marshal.PtrToStringAnsi((IntPtr)(ptr + pOffset64), checked((int)(pFileSz64 - 1)));
              break;
            }

          break;

        default:
          throw new FormatException("Unknown ELF class");
        }

        return new ElfInfo(@class, data, (ElfOsAbi)ptr[(int)ElfIdentIndex.EI_OSABI], ptr[(int)ElfIdentIndex.EI_ABIVERSION], type, machine, flags, interpreter);
      }
      finally
      {
        mappedView.SafeMemoryMappedViewHandle.ReleasePointer();
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort ConvertUInt16(ushort value) => unchecked((ushort)(
      ((byte)(value >> 8) << 0) |
      ((byte)(value >> 0) << 8)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ConvertUInt32(uint value) => unchecked(
      ((uint)(byte)(value >> 24) << 0) |
      ((uint)(byte)(value >> 16) << 8) |
      ((uint)(byte)(value >> 8) << 16) |
      ((uint)(byte)(value >> 0) << 24));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ConvertUInt64(ulong value) => unchecked(
      ((ulong)(byte)(value >> 56) << 0) |
      ((ulong)(byte)(value >> 48) << 8) |
      ((ulong)(byte)(value >> 40) << 16) |
      ((ulong)(byte)(value >> 32) << 24) |
      ((ulong)(byte)(value >> 24) << 32) |
      ((ulong)(byte)(value >> 16) << 40) |
      ((ulong)(byte)(value >> 8) << 48) |
      ((ulong)(byte)(value >> 0) << 56));

    private static unsafe uint StrNLenZ([NotNull] byte* str, uint maxLen)
    {
      uint len = 0;
      while (len < maxLen)
      {
        ++len;
        if (*str++ == 0)
          break;
      }

      return len;
    }

    private static unsafe ulong StrNLenZ([NotNull] byte* str, ulong maxLen)
    {
      ulong len = 0;
      while (len < maxLen)
      {
        ++len;
        if (*str++ == 0)
          break;
      }

      return len;
    }

    public sealed class ElfInfo
    {
      public readonly ElfClass Class;
      public readonly ElfData Data;
      public readonly ElfFlags Flags;

      [CanBeNull]
      public readonly string Interpreter;

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
}