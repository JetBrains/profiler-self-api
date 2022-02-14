using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace JetBrains.Profiler.SelfApi.Impl.Unix
{
  // Note(ww898): See https://en.wikipedia.org/wiki/Executable_and_Linkable_Format for details.

  #region <elf.h> Declarations

  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal enum ElfIdentIndex
  {
    EI_MAG0       =  0, // File identification byte 0 index
    EI_MAG1       =  1, // File identification byte 1 index
    EI_MAG2       =  2, // File identification byte 2 index
    EI_MAG3       =  3, // File identification byte 3 index
    EI_CLASS      =  4, // File class byte index
    EI_DATA       =  5, // Data encoding byte index
    EI_VERSION    =  6, // File version byte index
    EI_OSABI      =  7, // OS ABI identification
    EI_ABIVERSION =  8, // ABI version
    EI_PAD        =  9, // Byte index of padding bytes
    EI_NIDENT     = 16
  }

  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal enum ElfMagic : byte
  {
    ELFMAG0 = 0x7F,
    ELFMAG1 = (byte)'E',
    ELFMAG2 = (byte)'L',
    ELFMAG3 = (byte)'F'
  }

  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal enum ElfClass : byte
  {
    ELFCLASSNONE = 0, /* Unknown class. */
    ELFCLASS32   = 1, /* 32-bit architecture. */
    ELFCLASS64   = 2  /* 64-bit architecture. */
  }

  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal enum ElfData : byte
  {
    ELFDATANONE = 0, /* Unknown data format. */
    ELFDATA2LSB = 1, /* 2's complement little-endian. */
    ELFDATA2MSB = 2  /* 2's complement big-endian. */
  }

  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal enum ElfVersion : byte
  {
    EV_NONE    = 0,
    EV_CURRENT = 1
  }

  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal enum ElfOsAbi : byte
  {
    ELFOSABI_NONE     =  0, /* UNIX System V ABI */
    ELFOSABI_HPUX     =  1, /* HP-UX operating system */
    ELFOSABI_NETBSD   =  2, /* NetBSD */
    ELFOSABI_LINUX    =  3, /* GNU/Linux */
    ELFOSABI_HURD     =  4, /* GNU/Hurd */
    ELFOSABI_86OPEN   =  5, /* 86Open common IA32 ABI */
    ELFOSABI_SOLARIS  =  6, /* Solaris */
    ELFOSABI_AIX      =  7, /* AIX */
    ELFOSABI_IRIX     =  8, /* IRIX */
    ELFOSABI_FREEBSD  =  9, /* FreeBSD */
    ELFOSABI_TRU64    = 10, /* TRU64 UNIX */
    ELFOSABI_MODESTO  = 11, /* Novell Modesto */
    ELFOSABI_OPENBSD  = 12, /* OpenBSD */
    ELFOSABI_OPENVMS  = 13, /* Open VMS */
    ELFOSABI_NSK      = 14, /* HP Non-Stop Kernel */
    ELFOSABI_AROS     = 15, /* Amiga Research OS */
    ELFOSABI_FENIXOS  = 16, /* FenixOS */
    ELFOSABI_CLOUDABI = 17, /* Nuxi CloudABI */
    ELFOSABI_OPENVOS  = 18  /* Stratus Technologies OpenVOS */
  }

  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal enum ElfType : ushort
  {
    ET_NONE   =      0, /* Unknown type. */
    ET_REL    =      1, /* Relocatable. */
    ET_EXEC   =      2, /* Executable. */
    ET_DYN    =      3, /* Shared object. */
    ET_CORE   =      4, /* Core file. */
    ET_LOOS   = 0xfe00, /* First operating system specific. */
    ET_HIOS   = 0xfeff, /* Last operating system-specific. */
    ET_LOPROC = 0xff00, /* First processor-specific. */
    ET_HIPROC = 0xffff  /* Last processor-specific. */
  }

  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal enum ElfMachine : ushort
  {
    EM_NONE        =   0, /* Unknown machine. */
    EM_M32         =   1, /* AT&T WE32100. */
    EM_SPARC       =   2, /* Sun SPARC. */
    EM_386         =   3, /* Intel i386. */
    EM_68K         =   4, /* Motorola 68000. */
    EM_88K         =   5, /* Motorola 88000. */
    EM_IAMCU       =   6, /* Intel MCU. */
    EM_860         =   7, /* Intel i860. */
    EM_MIPS        =   8, /* MIPS R3000 Big-Endian only. */
    EM_S370        =   9, /* IBM System/370. */
    EM_MIPS_RS3_LE =  10, /* MIPS R3000 Little-Endian. */
    EM_PARISC      =  15, /* HP PA-RISC. */
    EM_VPP500      =  17, /* Fujitsu VPP500. */
    EM_SPARC32PLUS =  18, /* SPARC v8plus. */
    EM_960         =  19, /* Intel 80960. */
    EM_PPC         =  20, /* PowerPC 32-bit. */
    EM_PPC64       =  21, /* PowerPC 64-bit. */
    EM_S390        =  22, /* IBM System/390. */
    EM_V800        =  36, /* NEC V800. */
    EM_FR20        =  37, /* Fujitsu FR20. */
    EM_RH32        =  38, /* TRW RH-32. */
    EM_RCE         =  39, /* Motorola RCE. */
    EM_ARM         =  40, /* ARM. */
    EM_SH          =  42, /* Hitachi SH. */
    EM_SPARCV9     =  43, /* SPARC v9 64-bit. */
    EM_TRICORE     =  44, /* Siemens TriCore embedded processor. */
    EM_ARC         =  45, /* Argonaut RISC Core. */
    EM_H8_300      =  46, /* Hitachi H8/300. */
    EM_H8_300H     =  47, /* Hitachi H8/300H. */
    EM_H8S         =  48, /* Hitachi H8S. */
    EM_H8_500      =  49, /* Hitachi H8/500. */
    EM_IA_64       =  50, /* Intel IA-64 Processor. */
    EM_MIPS_X      =  51, /* Stanford MIPS-X. */
    EM_COLDFIRE    =  52, /* Motorola ColdFire. */
    EM_68HC12      =  53, /* Motorola M68HC12. */
    EM_MMA         =  54, /* Fujitsu MMA. */
    EM_PCP         =  55, /* Siemens PCP. */
    EM_NCPU        =  56, /* Sony nCPU. */
    EM_NDR1        =  57, /* Denso NDR1 microprocessor. */
    EM_STARCORE    =  58, /* Motorola Star*Core processor. */
    EM_ME16        =  59, /* Toyota ME16 processor. */
    EM_ST100       =  60, /* STMicroelectronics ST100 processor. */
    EM_TINYJ       =  61, /* Advanced Logic Corp. TinyJ processor. */
    EM_X86_64      =  62, /* Advanced Micro Devices x86-64 */
    EM_PDSP        =  63, /* Sony DSP Processor. */
    EM_FX66        =  66, /* Siemens FX66 microcontroller. */
    EM_ST9PLUS     =  67, /* STMicroelectronics ST9+ 8/16 microcontroller. */
    EM_ST7         =  68, /* STmicroelectronics ST7 8-bit microcontroller. */
    EM_68HC16      =  69, /* Motorola MC68HC16 microcontroller. */
    EM_68HC11      =  70, /* Motorola MC68HC11 microcontroller. */
    EM_68HC08      =  71, /* Motorola MC68HC08 microcontroller. */
    EM_68HC05      =  72, /* Motorola MC68HC05 microcontroller. */
    EM_SVX         =  73, /* Silicon Graphics SVx. */
    EM_ST19        =  74, /* STMicroelectronics ST19 8-bit mc. */
    EM_VAX         =  75, /* Digital VAX. */
    EM_CRIS        =  76, /* Axis Communications 32-bit embedded processor. */
    EM_JAVELIN     =  77, /* Infineon Technologies 32-bit embedded processor. */
    EM_FIREPATH    =  78, /* Element 14 64-bit DSP Processor. */
    EM_ZSP         =  79, /* LSI Logic 16-bit DSP Processor. */
    EM_MMIX        =  80, /* Donald Knuth's educational 64-bit proc. */
    EM_HUANY       =  81, /* Harvard University machine-independent object files. */
    EM_PRISM       =  82, /* SiTera Prism. */
    EM_AVR         =  83, /* Atmel AVR 8-bit microcontroller. */
    EM_FR30        =  84, /* Fujitsu FR30. */
    EM_D10V        =  85, /* Mitsubishi D10V. */
    EM_D30V        =  86, /* Mitsubishi D30V. */
    EM_V850        =  87, /* NEC v850. */
    EM_M32R        =  88, /* Mitsubishi M32R. */
    EM_MN10300     =  89, /* Matsushita MN10300. */
    EM_MN10200     =  90, /* Matsushita MN10200. */
    EM_PJ          =  91, /* picoJava. */
    EM_OPENRISC    =  92, /* OpenRISC 32-bit embedded processor. */
    EM_ARC_A5      =  93, /* ARC Cores Tangent-A5. */
    EM_XTENSA      =  94, /* Tensilica Xtensa Architecture. */
    EM_VIDEOCORE   =  95, /* Alphamosaic VideoCore processor. */
    EM_TMM_GPP     =  96, /* Thompson Multimedia General Purpose Processor. */
    EM_NS32K       =  97, /* National Semiconductor 32000 series. */
    EM_TPC         =  98, /* Tenor Network TPC processor. */
    EM_SNP1K       =  99, /* Trebia SNP 1000 processor. */
    EM_ST200       = 100, /* STMicroelectronics ST200 microcontroller. */
    EM_IP2K        = 101, /* Ubicom IP2xxx microcontroller family. */
    EM_MAX         = 102, /* MAX Processor. */
    EM_CR          = 103, /* National Semiconductor CompactRISC microprocessor. */
    EM_F2MC16      = 104, /* Fujitsu F2MC16. */
    EM_MSP430      = 105, /* Texas Instruments embedded microcontrollermsp430. */
    EM_BLACKFIN    = 106, /* Analog Devices Blackfin (DSP) processor. */
    EM_SE_C33      = 107, /* S1C33 Family of Seiko Epson processors. */
    EM_SEP         = 108, /* Sharp embedded microprocessor. */
    EM_ARCA        = 109, /* Arca RISC Microprocessor. */
    EM_UNICORE     = 110, /* Microprocessor series from PKU-Unity Ltd. and MPRC of Peking University */
    EM_AARCH64     = 183, /* AArch64 (64-bit ARM) */
    EM_RISCV       = 243, /* RISC-V */

    /* Non-standard or deprecated. */
    EM_ALPHA_STD   =     41, /* Digital Alpha (standard value). */
    EM_ALPHA       = 0x9026  /* Alpha (written in the absence of an ABI) */
  }

  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  [Flags]
  internal enum ElfFlags : uint
  {
    EF_ARM_RELEXEC              = 0x00000001,
    EF_ARM_HASENTRY             = 0x00000002,
    EF_ARM_SYMSARESORTED        = 0x00000004,
    EF_ARM_DYNSYMSUSESEGIDX     = 0x00000008,
    EF_ARM_MAPSYMSFIRST         = 0x00000010,
    EF_ARM_LE8                  = 0x00400000,
    EF_ARM_BE8                  = 0x00800000,
    EF_ARM_EABIMASK             = 0xFF000000,
    EF_ARM_EABI_UNKNOWN         = 0x00000000,
    EF_ARM_EABI_VER1            = 0x01000000,
    EF_ARM_EABI_VER2            = 0x02000000,
    EF_ARM_EABI_VER3            = 0x03000000,
    EF_ARM_EABI_VER4            = 0x04000000,
    EF_ARM_EABI_VER5            = 0x05000000,
    EF_ARM_INTERWORK            = 0x00000004,
    EF_ARM_APCS_26              = 0x00000008,
    EF_ARM_APCS_FLOAT           = 0x00000010,
    EF_ARM_PIC                  = 0x00000020,
    EF_ARM_ALIGN8               = 0x00000040,
    EF_ARM_NEW_ABI              = 0x00000080,
    EF_ARM_OLD_ABI              = 0x00000100,
    EF_ARM_ABI_FLOAT_SOFT       = 0x00000200,
    EF_ARM_ABI_FLOAT_HARD       = 0x00000400,
    EF_ARM_MAVERICK_FLOAT       = 0x00000800,

    EF_MIPS_NOREORDER           = 0x00000001,
    EF_MIPS_PIC                 = 0x00000002, /* Contains PIC code */
    EF_MIPS_CPIC                = 0x00000004, /* STD PIC calling sequence */
    EF_MIPS_UCODE               = 0x00000010,
    EF_MIPS_ABI2                = 0x00000020, /* N32 */
    EF_MIPS_OPTIONS_FIRST       = 0x00000080,
    EF_MIPS_32BITMODE           = 0x00000100,
    EF_MIPS_ABI                 = 0x0000F000,
    EF_MIPS_ABI_O32             = 0x00001000,
    EF_MIPS_ABI_O64             = 0x00002000,
    EF_MIPS_ABI_EABI32          = 0x00003000,
    EF_MIPS_ABI_EABI64          = 0x00004000,
    EF_MIPS_ARCH_ASE            = 0x0F000000, /* Architectural extensions */
    EF_MIPS_ARCH_ASE_MDMX       = 0x08000000, /* MDMX multimedia extension */
    EF_MIPS_ARCH_ASE_M16        = 0x04000000, /* MIPS-16 ISA extensions */
    EF_MIPS_ARCH                = 0xF0000000, /* Architecture field */
    EF_MIPS_ARCH_1              = 0x00000000, /* -mips1 code */
    EF_MIPS_ARCH_2              = 0x10000000, /* -mips2 code */
    EF_MIPS_ARCH_3              = 0x20000000, /* -mips3 code */
    EF_MIPS_ARCH_4              = 0x30000000, /* -mips4 code */
    EF_MIPS_ARCH_5              = 0x40000000, /* -mips5 code */
    EF_MIPS_ARCH_32             = 0x50000000, /* -mips32 code */
    EF_MIPS_ARCH_64             = 0x60000000, /* -mips64 code */
    EF_MIPS_ARCH_32R2           = 0x70000000, /* -mips32r2 code */
    EF_MIPS_ARCH_64R2           = 0x80000000, /* -mips64r2 code */

    EF_PPC_EMB                  = 0x80000000,
    EF_PPC_RELOCATABLE          = 0x00010000,
    EF_PPC_RELOCATABLE_LIB      = 0x00008000,

    EF_PPC64_ABI_VER0           = 0x00000000, // 0 for unspecified or not using any features affected by the differences.
    EF_PPC64_ABI_VER1           = 0x00000001, // 1 for original ABI using function descriptors,
    EF_PPC64_ABI_VER2           = 0x00000002, // 2 for revised ABI without function descriptors,
    EF_PPC64_ABI                = 0x00000003,

    EF_RISCV_RVC                = 0x00000001,
    EF_RISCV_FLOAT_ABI_MASK     = 0x00000006,
    EF_RISCV_FLOAT_ABI_SOFT     = 0x00000000,
    EF_RISCV_FLOAT_ABI_SINGLE   = 0x00000002,
    EF_RISCV_FLOAT_ABI_DOUBLE   = 0x00000004,
    EF_RISCV_FLOAT_ABI_QUAD     = 0x00000006,
    EF_RISCV_RVE                = 0x00000008,
    EF_RISCV_TSO                = 0x00000010,

    EF_SPARC_EXT_MASK           = 0x00ffff00,
    EF_SPARC_32PLUS             = 0x00000100,
    EF_SPARC_SUN_US1            = 0x00000200,
    EF_SPARC_HAL_R1             = 0x00000400,
    EF_SPARC_SUN_US3            = 0x00000800,

    EF_SPARCV9_MM               = 0x00000003,
    EF_SPARCV9_TSO              = 0x00000000,
    EF_SPARCV9_PSO              = 0x00000001,
    EF_SPARCV9_RMO              = 0x00000002,

    EF_PARISC_TRAPNIL           = 0x00010000, /* trap on NULL derefs */
    EF_PARISC_EXT               = 0x00020000, /* program uses arch exts */
    EF_PARISC_LSB               = 0x00040000, /* program expects LSB mode */
    EF_PARISC_WIDE              = 0x00080000, /* program expects wide mode */
    EF_PARISC_NO_KABP           = 0x00100000, /* don't allow kernel assisted branch prediction */
    EF_PARISC_LAZYSWAP          = 0x00200000, /* allow lazy swap allocation for dynamically allocated program segments */
    EF_PARISC_ARCH              = 0x0000ffff, /* architecture version */
    EFA_PARISC_1_0              = 0x0000020B,
    EFA_PARISC_1_1              = 0x00000210,
    EFA_PARISC_2_0              = 0x00000214,

    EF_SH_MACH_MASK             = 0x0000001f,
    EF_SH_UNKNOWN               = 0x00000000,
    EF_SH_SH1                   = 0x00000001,
    EF_SH_SH2                   = 0x00000002,
    EF_SH_SH3                   = 0x00000003,
    EF_SH_DSP                   = 0x00000004,
    EF_SH_SH3_DSP               = 0x00000005,
    EF_SH_SH3E                  = 0x00000008,
    EF_SH_SH4                   = 0x00000009,
    EF_SH5                      = 0x0000000A,
    EF_SH2E                     = 0x0000000B,
    EF_SH4A                     = 0x0000000C,
    EF_SH2A                     = 0x0000000D,
    EF_SH4_NOFPU                = 0x00000010,
    EF_SH4A_NOFPU               = 0x00000011,
    EF_SH4_NOMMU_NOFPU          = 0x00000012,
    EF_SH2A_NOFPU               = 0x00000013,
    EF_SH3_NOMMU                = 0x00000014,
    EF_SH2A_SH4_NOFPU           = 0x00000015,
    EF_SH2A_SH3_NOFPU           = 0x00000016,
    EF_SH2A_SH4                 = 0x00000017,
    EF_SH2A_SH3E                = 0x00000018,

    EF_IA_64_MASKOS             = 0x0000000f,	/* OS-specific flags.  */
    EF_IA_64_ARCH               = 0xff000000,	/* Arch. version mask.  */
    EF_IA_64_ARCHVER_1          = 0x01000000, /* Arch. version level 1 compat.  */

    EF_IA_64_TRAPNIL            = 0x00000001, /* Trap NIL pointer dereferences.  */
    EF_IA_64_EXT                = 0x00000004, /* Program uses arch. extensions.  */
    EF_IA_64_BE                 = 0x00000008, /* PSR BE bit set (big-endian).  */
    EFA_IA_64_EAS2_3            = 0x23000000, /* IA64 EAS 2.3.  */

    EF_IA_64_ABI64              = 0x00000010, /* 64-bit ABI.  */
    EF_IA_64_REDUCEDFP          = 0x00000020, /* Only FP6-FP11 used.  */
    EF_IA_64_CONS_GP            = 0x00000040, /* gp as program wide constant.  */
    EF_IA_64_NOFUNCDESC_CONS_GP = 0x00000080, /* And no function descriptors.  */
    EF_IA_64_ABSOLUTE           = 0x00000100, /* Load at absolute addresses.  */
}

  [StructLayout(LayoutKind.Sequential)]
  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal unsafe struct Elf32_Ehdr
  {
    public fixed byte e_ident[(int)ElfIdentIndex.EI_NIDENT]; // Magic number and other info
    public ushort e_type;                                    // Object file type
    public ushort e_machine;                                 // Architecture
    public uint e_version;                                   // Object file version
    public uint e_entry;                                     // Entry point virtual address
    public uint e_phoff;                                     // Program header table file offset
    public uint e_shoff;                                     // Section header table file offset
    public uint e_flags;                                     // Processor-specific flags
    public ushort e_ehsize;                                  // ELF header size in bytes
    public ushort e_phentsize;                               // Program header table entry size
    public ushort e_shentsize;                               // Section header table entry size
    public ushort e_phnum;                                   // Program header table entry count
    public ushort e_shnum;                                   // Section header table entry count
    public ushort e_shstrndx;                                // Section header string table index
  }

  [StructLayout(LayoutKind.Sequential)]
  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal unsafe struct Elf64_Ehdr
  {
    public fixed byte e_ident[(int)ElfIdentIndex.EI_NIDENT]; // Magic number and other info
    public ushort e_type;                                    // Object file type
    public ushort e_machine;                                 // Architecture
    public uint e_version;                                   // Object file version
    public ulong e_entry;                                    // Entry point virtual address
    public ulong e_phoff;                                    // Program header table file offset
    public ulong e_shoff;                                    // Section header table file offset
    public uint e_flags;                                     // Processor-specific flags
    public ushort e_ehsize;                                  // ELF header size in bytes
    public ushort e_phentsize;                               // Program header table entry size
    public ushort e_shentsize;                               // Section header table entry size
    public ushort e_phnum;                                   // Program header table entry count
    public ushort e_shnum;                                   // Section header table entry count
    public ushort e_shstrndx;                                // Section header string table index
  }

  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal enum ElfSegmentType : uint
  {
    PT_NULL = 0,                  // Program header table entry unused
    PT_LOAD = 1,                  // Loadable program segment
    PT_DYNAMIC = 2,               // Dynamic linking information
    PT_INTERP = 3,                // Program interpreter
    PT_NOTE = 4,                  // Auxiliary information
    PT_SHLIB = 5,                 // Reserved
    PT_PHDR = 6,                  // Entry for header table itself
    PT_TLS = 7,                   // Thread-local storage segment
    PT_NUM = 8,                   // Number of defined types
    PT_LOOS = 0x60000000,         // Start of OS-specific
    PT_GNU_EH_FRAME = 0x6474e550, // GCC .eh_frame_hdr segment
    PT_GNU_STACK = 0x6474e551,    // Indicates stack executability
    PT_GNU_RELRO = 0x6474e552,    // Read-only after relocation
    PT_LOSUNW = 0x6ffffffa,
    PT_SUNWBSS = 0x6ffffffa,      // Sun Specific segment
    PT_SUNWSTACK = 0x6ffffffb,    // Stack segment
    PT_HISUNW = 0x6fffffff,
    PT_HIOS = 0x6fffffff,         // End of OS-specific
    PT_LOPROC = 0x70000000,       // Start of processor-specific
    PT_HIPROC = 0x7fffffff        // End of processor-specific
  }

  [StructLayout(LayoutKind.Sequential)]
  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal struct Elf32_Phdr
  {
    public uint p_type;   // Segment type
    public uint p_offset; // Segment file offset
    public uint p_vaddr;  // Segment virtual address
    public uint p_paddr;  // Segment physical address
    public uint p_filesz; // Segment size in file
    public uint p_memsz;  // Segment size in memory
    public uint p_flags;  // Segment flags
    public uint p_align;  // Segment alignment
  }

  [StructLayout(LayoutKind.Sequential)]
  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal struct Elf64_Phdr
  {
    public uint p_type;    // Segment type
    public uint p_flags;   // Segment flags
    public ulong p_offset; // Segment file offset
    public ulong p_vaddr;  // Segment virtual address
    public ulong p_paddr;  // Segment physical address
    public ulong p_filesz; // Segment size in file
    public ulong p_memsz;  // Segment size in memory
    public ulong p_align;  // Segment alignment
  }

  #endregion
}