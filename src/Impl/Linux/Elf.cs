using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace JetBrains.Profiler.SelfApi.Impl.Linux
{
  internal static class Elf
  {
    #region <elf.h> Declarations

    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ElfIdentIndex
    {
      EI_MAG0 = 0, // File identification byte 0 index
      EI_MAG1 = 1, // File identification byte 1 index
      EI_MAG2 = 2, // File identification byte 2 index
      EI_MAG3 = 3, // File identification byte 3 index
      EI_CLASS = 4, // File class byte index
      EI_DATA = 5, // Data encoding byte index
      EI_VERSION = 6, // File version byte index
      EI_OSABI = 7, // OS ABI identification
      EI_ABIVERSION = 8, // ABI version
      EI_PAD = 9, // Byte index of padding bytes
      EI_NIDENT = 16
    }

    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public unsafe struct Elf64_Ehdr
    {
      public fixed byte e_ident[(int)ElfIdentIndex.EI_NIDENT]; // Magic number and other info
      public ushort e_type; // Object file type
      public ushort e_machine; // Architecture
      public uint e_version; // Object file version
      public ulong e_entry; // Entry point virtual address
      public ulong e_phoff; // Program header table file offset
      public ulong e_shoff; // Section header table file offset
      public uint e_flags; // Processor-specific flags
      public ushort e_ehsize; // ELF header size in bytes
      public ushort e_phentsize; // Program header table entry size
      public ushort e_shentsize; // Section header table entry size
      public ushort e_phnum; // Program header table entry count
      public ushort e_shnum; // Section header table entry count
      public ushort e_shstrndx; // Section header string table index
    }

    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ElfSegmentType : uint
    {
      PT_NULL = 0, // Program header table entry unused
      PT_LOAD = 1, // Loadable program segment
      PT_DYNAMIC = 2, // Dynamic linking information
      PT_INTERP = 3, // Program interpreter
      PT_NOTE = 4, // Auxiliary information
      PT_SHLIB = 5, // Reserved
      PT_PHDR = 6, // Entry for header table itself
      PT_TLS = 7, // Thread-local storage segment
      PT_NUM = 8, // Number of defined types
      PT_LOOS = 0x60000000, // Start of OS-specific
      PT_GNU_EH_FRAME = 0x6474e550, // GCC .eh_frame_hdr segment
      PT_GNU_STACK = 0x6474e551, // Indicates stack executability
      PT_GNU_RELRO = 0x6474e552, // Read-only after relocation
      PT_LOSUNW = 0x6ffffffa,
      PT_SUNWBSS = 0x6ffffffa, // Sun Specific segment
      PT_SUNWSTACK = 0x6ffffffb, // Stack segment
      PT_HISUNW = 0x6fffffff,
      PT_HIOS = 0x6fffffff, // End of OS-specific
      PT_LOPROC = 0x70000000, // Start of processor-specific
      PT_HIPROC = 0x7fffffff // End of processor-specific
    }

    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public struct Elf64_Phdr
    {
      public ElfSegmentType p_type; // Segment type
      public uint p_flags; // Segment flags
      public ulong p_offset; // Segment file offset
      public ulong p_vaddr; // Segment virtual address
      public ulong p_paddr; // Segment physical address
      public ulong p_filesz; // Segment size in file
      public ulong p_memsz; // Segment size in memory
      public ulong p_align; // Segment alignment
    }

    #endregion
  }
}