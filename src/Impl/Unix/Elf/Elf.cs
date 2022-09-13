using System;
using System.Diagnostics.CodeAnalysis;

namespace JetBrains.Profiler.SelfApi.Impl.Unix.Elf
{
  // Note(ww898): See https://en.wikipedia.org/wiki/Executable_and_Linkable_Format for details.

  #region <elf.h> Declarations

  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal enum ElfClass : byte
  {
    ELFCLASSNONE = 0,
    ELFCLASS32   = 1, // 32-bit object file
    ELFCLASS64   = 2  // 64-bit object file
  }

  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal enum ElfData : byte
  {
    ELFDATANONE = 0, // Invalid data encoding.
    ELFDATA2LSB = 1, // Little-endian object file
    ELFDATA2MSB = 2  // Big-endian object file
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
    ELFOSABI_NONE         =   0, // UNIX System V ABI
    ELFOSABI_HPUX         =   1, // HP-UX operating system
    ELFOSABI_NETBSD       =   2, // NetBSD
    ELFOSABI_LINUX        =   3, // GNU/Linux
    ELFOSABI_HURD         =   4, // GNU/Hurd
    ELFOSABI_86OPEN       =   5, // 86Open common IA32 ABI
    ELFOSABI_SOLARIS      =   6, // Solaris
    ELFOSABI_AIX          =   7, // AIX
    ELFOSABI_IRIX         =   8, // IRIX
    ELFOSABI_FREEBSD      =   9, // FreeBSD
    ELFOSABI_TRU64        =  10, // TRU64 UNIX
    ELFOSABI_MODESTO      =  11, // Novell Modesto
    ELFOSABI_OPENBSD      =  12, // OpenBSD
    ELFOSABI_OPENVMS      =  13, // Open VMS
    ELFOSABI_NSK          =  14, // HP Non-Stop Kernel
    ELFOSABI_AROS         =  15, // Amiga Research OS
    ELFOSABI_FENIXOS      =  16, // FenixOS
    ELFOSABI_CLOUDABI     =  17, // Nuxi CloudABI
    ELFOSABI_OPENVOS      =  18, // Stratus Technologies OpenVOS
    ELFOSABI_C6000_ELFABI =  64, // Bare-metal TMS320C6000
    ELFOSABI_AMDGPU_HSA   =  64, // AMD HSA runtime
    ELFOSABI_C6000_LINUX  =  65, // Linux TMS320C6000
    ELFOSABI_ARM          =  97, // ARM
    ELFOSABI_STANDALONE   = 255  // Standalone (embedded) application
  }

  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal enum ElfType : ushort
  {
    ET_NONE   =      0, // Unknown type
    ET_REL    =      1, // Relocatable
    ET_EXEC   =      2, // Executable
    ET_DYN    =      3, // Shared object
    ET_CORE   =      4, // Core file
    ET_LOOS   = 0xfe00, // First operating system specific
    ET_HIOS   = 0xfeff, // Last operating system-specific
    ET_LOPROC = 0xff00, // First processor-specific
    ET_HIPROC = 0xffff  // Last processor-specific
  }

  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal enum ElfMachine : ushort
  {
    EM_NONE          =   0, // No machine
    EM_M32           =   1, // AT&T WE 32100
    EM_SPARC         =   2, // SPARC
    EM_386           =   3, // Intel 386
    EM_68K           =   4, // Motorola 68000
    EM_88K           =   5, // Motorola 88000
    EM_IAMCU         =   6, // Intel MCU
    EM_860           =   7, // Intel 80860
    EM_MIPS          =   8, // MIPS R3000
    EM_S370          =   9, // IBM System/370
    EM_MIPS_RS3_LE   =  10, // MIPS RS3000 Little-endian
    EM_PARISC        =  15, // Hewlett-Packard PA-RISC
    EM_VPP500        =  17, // Fujitsu VPP500
    EM_SPARC32PLUS   =  18, // Enhanced instruction set SPARC
    EM_960           =  19, // Intel 80960
    EM_PPC           =  20, // PowerPC
    EM_PPC64         =  21, // PowerPC64
    EM_S390          =  22, // IBM System/390
    EM_SPU           =  23, // IBM SPU/SPC
    EM_V800          =  36, // NEC V800
    EM_FR20          =  37, // Fujitsu FR20
    EM_RH32          =  38, // TRW RH-32
    EM_RCE           =  39, // Motorola RCE
    EM_ARM           =  40, // ARM
    EM_SH            =  42, // Hitachi SH
    EM_SPARCV9       =  43, // SPARC V9
    EM_TRICORE       =  44, // Siemens TriCore
    EM_ARC           =  45, // Argonaut RISC Core
    EM_H8_300        =  46, // Hitachi H8/300
    EM_H8_300H       =  47, // Hitachi H8/300H
    EM_H8S           =  48, // Hitachi H8S
    EM_H8_500        =  49, // Hitachi H8/500
    EM_IA_64         =  50, // Intel IA-64 processor architecture
    EM_MIPS_X        =  51, // Stanford MIPS-X
    EM_COLDFIRE      =  52, // Motorola ColdFire
    EM_68HC12        =  53, // Motorola M68HC12
    EM_MMA           =  54, // Fujitsu MMA Multimedia Accelerator
    EM_PCP           =  55, // Siemens PCP
    EM_NCPU          =  56, // Sony nCPU embedded RISC processor
    EM_NDR1          =  57, // Denso NDR1 microprocessor
    EM_STARCORE      =  58, // Motorola Star*Core processor
    EM_ME16          =  59, // Toyota ME16 processor
    EM_ST100         =  60, // STMicroelectronics ST100 processor
    EM_TINYJ         =  61, // Advanced Logic Corp. TinyJ embedded processor family
    EM_X86_64        =  62, // AMD x86-64 architecture
    EM_PDSP          =  63, // Sony DSP Processor
    EM_PDP10         =  64, // Digital Equipment Corp. PDP-10
    EM_PDP11         =  65, // Digital Equipment Corp. PDP-11
    EM_FX66          =  66, // Siemens FX66 microcontroller
    EM_ST9PLUS       =  67, // STMicroelectronics ST9+ 8/16 bit microcontroller
    EM_ST7           =  68, // STMicroelectronics ST7 8-bit microcontroller
    EM_68HC16        =  69, // Motorola MC68HC16 Microcontroller
    EM_68HC11        =  70, // Motorola MC68HC11 Microcontroller
    EM_68HC08        =  71, // Motorola MC68HC08 Microcontroller
    EM_68HC05        =  72, // Motorola MC68HC05 Microcontroller
    EM_SVX           =  73, // Silicon Graphics SVx
    EM_ST19          =  74, // STMicroelectronics ST19 8-bit microcontroller
    EM_VAX           =  75, // Digital VAX
    EM_CRIS          =  76, // Axis Communications 32-bit embedded processor
    EM_JAVELIN       =  77, // Infineon Technologies 32-bit embedded processor
    EM_FIREPATH      =  78, // Element 14 64-bit DSP Processor
    EM_ZSP           =  79, // LSI Logic 16-bit DSP Processor
    EM_MMIX          =  80, // Donald Knuth's educational 64-bit processor
    EM_HUANY         =  81, // Harvard University machine-independent object files
    EM_PRISM         =  82, // SiTera Prism
    EM_AVR           =  83, // Atmel AVR 8-bit microcontroller
    EM_FR30          =  84, // Fujitsu FR30
    EM_D10V          =  85, // Mitsubishi D10V
    EM_D30V          =  86, // Mitsubishi D30V
    EM_V850          =  87, // NEC v850
    EM_M32R          =  88, // Mitsubishi M32R
    EM_MN10300       =  89, // Matsushita MN10300
    EM_MN10200       =  90, // Matsushita MN10200
    EM_PJ            =  91, // picoJava
    EM_OPENRISC      =  92, // OpenRISC 32-bit embedded processor
    EM_ARC_COMPACT   =  93, // ARC International ARCompact processor (old
                            // spelling/synonym: EM_ARC_A5)
    EM_XTENSA        =  94, // Tensilica Xtensa Architecture
    EM_VIDEOCORE     =  95, // Alphamosaic VideoCore processor
    EM_TMM_GPP       =  96, // Thompson Multimedia General Purpose Processor
    EM_NS32K         =  97, // National Semiconductor 32000 series
    EM_TPC           =  98, // Tenor Network TPC processor
    EM_SNP1K         =  99, // Trebia SNP 1000 processor
    EM_ST200         = 100, // STMicroelectronics (www.st.com) ST200
    EM_IP2K          = 101, // Ubicom IP2xxx microcontroller family
    EM_MAX           = 102, // MAX Processor
    EM_CR            = 103, // National Semiconductor CompactRISC microprocessor
    EM_F2MC16        = 104, // Fujitsu F2MC16
    EM_MSP430        = 105, // Texas Instruments embedded microcontroller msp430
    EM_BLACKFIN      = 106, // Analog Devices Blackfin (DSP) processor
    EM_SE_C33        = 107, // S1C33 Family of Seiko Epson processors
    EM_SEP           = 108, // Sharp embedded microprocessor
    EM_ARCA          = 109, // Arca RISC Microprocessor
    EM_UNICORE       = 110, // Microprocessor series from PKU-Unity Ltd. and MPRC
                            // of Peking University
    EM_EXCESS        = 111, // eXcess: 16/32/64-bit configurable embedded CPU
    EM_DXP           = 112, // Icera Semiconductor Inc. Deep Execution Processor
    EM_ALTERA_NIOS2  = 113, // Altera Nios II soft-core processor
    EM_CRX           = 114, // National Semiconductor CompactRISC CRX
    EM_XGATE         = 115, // Motorola XGATE embedded processor
    EM_C166          = 116, // Infineon C16x/XC16x processor
    EM_M16C          = 117, // Renesas M16C series microprocessors
    EM_DSPIC30F      = 118, // Microchip Technology dsPIC30F Digital Signal
                            // Controller
    EM_CE            = 119, // Freescale Communication Engine RISC core
    EM_M32C          = 120, // Renesas M32C series microprocessors
    EM_TSK3000       = 131, // Altium TSK3000 core
    EM_RS08          = 132, // Freescale RS08 embedded processor
    EM_SHARC         = 133, // Analog Devices SHARC family of 32-bit DSP
                            // processors
    EM_ECOG2         = 134, // Cyan Technology eCOG2 microprocessor
    EM_SCORE7        = 135, // Sunplus S+core7 RISC processor
    EM_DSP24         = 136, // New Japan Radio (NJR) 24-bit DSP Processor
    EM_VIDEOCORE3    = 137, // Broadcom VideoCore III processor
    EM_LATTICEMICO32 = 138, // RISC processor for Lattice FPGA architecture
    EM_SE_C17        = 139, // Seiko Epson C17 family
    EM_TI_C6000      = 140, // The Texas Instruments TMS320C6000 DSP family
    EM_TI_C2000      = 141, // The Texas Instruments TMS320C2000 DSP family
    EM_TI_C5500      = 142, // The Texas Instruments TMS320C55x DSP family
    EM_MMDSP_PLUS    = 160, // STMicroelectronics 64bit VLIW Data Signal Processor
    EM_CYPRESS_M8C   = 161, // Cypress M8C microprocessor
    EM_R32C          = 162, // Renesas R32C series microprocessors
    EM_TRIMEDIA      = 163, // NXP Semiconductors TriMedia architecture family
    EM_HEXAGON       = 164, // Qualcomm Hexagon processor
    EM_8051          = 165, // Intel 8051 and variants
    EM_STXP7X        = 166, // STMicroelectronics STxP7x family of configurable
                            // and extensible RISC processors
    EM_NDS32         = 167, // Andes Technology compact code size embedded RISC
                            // processor family
    EM_ECOG1         = 168, // Cyan Technology eCOG1X family
    EM_ECOG1X        = 168, // Cyan Technology eCOG1X family
    EM_MAXQ30        = 169, // Dallas Semiconductor MAXQ30 Core Micro-controllers
    EM_XIMO16        = 170, // New Japan Radio (NJR) 16-bit DSP Processor
    EM_MANIK         = 171, // M2000 Reconfigurable RISC Microprocessor
    EM_CRAYNV2       = 172, // Cray Inc. NV2 vector architecture
    EM_RX            = 173, // Renesas RX family
    EM_METAG         = 174, // Imagination Technologies META processor
                            // architecture
    EM_MCST_ELBRUS   = 175, // MCST Elbrus general purpose hardware architecture
    EM_ECOG16        = 176, // Cyan Technology eCOG16 family
    EM_CR16          = 177, // National Semiconductor CompactRISC CR16 16-bit
                            // microprocessor
    EM_ETPU          = 178, // Freescale Extended Time Processing Unit
    EM_SLE9X         = 179, // Infineon Technologies SLE9X core
    EM_L10M          = 180, // Intel L10M
    EM_K10M          = 181, // Intel K10M
    EM_AARCH64       = 183, // ARM AArch64
    EM_AVR32         = 185, // Atmel Corporation 32-bit microprocessor family
    EM_STM8          = 186, // STMicroeletronics STM8 8-bit microcontroller
    EM_TILE64        = 187, // Tilera TILE64 multicore architecture family
    EM_TILEPRO       = 188, // Tilera TILEPro multicore architecture family
    EM_CUDA          = 190, // NVIDIA CUDA architecture
    EM_TILEGX        = 191, // Tilera TILE-Gx multicore architecture family
    EM_CLOUDSHIELD   = 192, // CloudShield architecture family
    EM_COREA_1ST     = 193, // KIPO-KAIST Core-A 1st generation processor family
    EM_COREA_2ND     = 194, // KIPO-KAIST Core-A 2nd generation processor family
    EM_ARC_COMPACT2  = 195, // Synopsys ARCompact V2
    EM_OPEN8         = 196, // Open8 8-bit RISC soft processor core
    EM_RL78          = 197, // Renesas RL78 family
    EM_VIDEOCORE5    = 198, // Broadcom VideoCore V processor
    EM_78KOR         = 199, // Renesas 78KOR family
    EM_56800EX       = 200, // Freescale 56800EX Digital Signal Controller (DSC)
    EM_BA1           = 201, // Beyond BA1 CPU architecture
    EM_BA2           = 202, // Beyond BA2 CPU architecture
    EM_XCORE         = 203, // XMOS xCORE processor family
    EM_MCHP_PIC      = 204, // Microchip 8-bit PIC(r) family
    EM_INTEL205      = 205, // Reserved by Intel
    EM_INTEL206      = 206, // Reserved by Intel
    EM_INTEL207      = 207, // Reserved by Intel
    EM_INTEL208      = 208, // Reserved by Intel
    EM_INTEL209      = 209, // Reserved by Intel
    EM_KM32          = 210, // KM211 KM32 32-bit processor
    EM_KMX32         = 211, // KM211 KMX32 32-bit processor
    EM_KMX16         = 212, // KM211 KMX16 16-bit processor
    EM_KMX8          = 213, // KM211 KMX8 8-bit processor
    EM_KVARC         = 214, // KM211 KVARC processor
    EM_CDP           = 215, // Paneve CDP architecture family
    EM_COGE          = 216, // Cognitive Smart Memory Processor
    EM_COOL          = 217, // iCelero CoolEngine
    EM_NORC          = 218, // Nanoradio Optimized RISC
    EM_CSR_KALIMBA   = 219, // CSR Kalimba architecture family
    EM_AMDGPU        = 224, // AMD GPU architecture
    EM_RISCV         = 243,
    EM_LOONGARCH     = 258, // LoongArch processor

    // A request has been made to the maintainer of the official registry for
    // such numbers for an official value for WebAssembly. As soon as one is
    // allocated, this enum will be updated to use it.
    EM_WEBASSEMBLY   = 0x4157, // WebAssembly architecture

    // Non-standard or deprecated
    EM_ALPHA_STD     =     41, // Digital Alpha (standard value)
    EM_ALPHA         = 0x9026  // Alpha (written in the absence of an ABI)
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
    EF_MIPS_PIC                 = 0x00000002, // Contains PIC code
    EF_MIPS_CPIC                = 0x00000004, // STD PIC calling sequence
    EF_MIPS_UCODE               = 0x00000010,
    EF_MIPS_ABI2                = 0x00000020, // N32
    EF_MIPS_OPTIONS_FIRST       = 0x00000080,
    EF_MIPS_32BITMODE           = 0x00000100,
    EF_MIPS_ABI                 = 0x0000F000,
    EF_MIPS_ABI_O32             = 0x00001000,
    EF_MIPS_ABI_O64             = 0x00002000,
    EF_MIPS_ABI_EABI32          = 0x00003000,
    EF_MIPS_ABI_EABI64          = 0x00004000,
    EF_MIPS_ARCH_ASE            = 0x0F000000, // Architectural extensions
    EF_MIPS_ARCH_ASE_MDMX       = 0x08000000, // MDMX multimedia extension
    EF_MIPS_ARCH_ASE_M16        = 0x04000000, // MIPS-16 ISA extensions
    EF_MIPS_ARCH                = 0xF0000000, // Architecture field
    EF_MIPS_ARCH_1              = 0x00000000, // -mips1 code
    EF_MIPS_ARCH_2              = 0x10000000, // -mips2 code
    EF_MIPS_ARCH_3              = 0x20000000, // -mips3 code
    EF_MIPS_ARCH_4              = 0x30000000, // -mips4 code
    EF_MIPS_ARCH_5              = 0x40000000, // -mips5 code
    EF_MIPS_ARCH_32             = 0x50000000, // -mips32 code
    EF_MIPS_ARCH_64             = 0x60000000, // -mips64 code
    EF_MIPS_ARCH_32R2           = 0x70000000, // -mips32r2 code
    EF_MIPS_ARCH_64R2           = 0x80000000, // -mips64r2 code

    EF_PPC_EMB                  = 0x80000000,
    EF_PPC_RELOCATABLE          = 0x00010000,
    EF_PPC_RELOCATABLE_LIB      = 0x00008000,

    EF_PPC64_ABI_VER0           = 0x00000000, // 0 for unspecified or not using any features affected by the differences
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

    EF_PARISC_TRAPNIL           = 0x00010000, // trap on NULL derefs
    EF_PARISC_EXT               = 0x00020000, // program uses arch exts
    EF_PARISC_LSB               = 0x00040000, // program expects LSB mode
    EF_PARISC_WIDE              = 0x00080000, // program expects wide mode
    EF_PARISC_NO_KABP           = 0x00100000, // don't allow kernel assisted branch prediction
    EF_PARISC_LAZYSWAP          = 0x00200000, // allow lazy swap allocation for dynamically allocated program segments
    EF_PARISC_ARCH              = 0x0000ffff, // architecture version
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

    EF_IA_64_MASKOS             = 0x0000000f,	// OS-specific flags
    EF_IA_64_ARCH               = 0xff000000,	// Arch version mask
    EF_IA_64_ARCHVER_1          = 0x01000000, // Arch version level 1 compat

    EF_IA_64_TRAPNIL            = 0x00000001, // Trap NIL pointer dereferences
    EF_IA_64_EXT                = 0x00000004, // Program uses arch extensions
    EF_IA_64_BE                 = 0x00000008, // PSR BE bit set (big-endian)
    EFA_IA_64_EAS2_3            = 0x23000000, // IA64 EAS 23

    EF_IA_64_ABI64              = 0x00000010, // 64-bit ABI
    EF_IA_64_REDUCEDFP          = 0x00000020, // Only FP6-FP11 used
    EF_IA_64_CONS_GP            = 0x00000040, // gp as program wide constant
    EF_IA_64_NOFUNCDESC_CONS_GP = 0x00000080, // And no function descriptors
    EF_IA_64_ABSOLUTE           = 0x00000100, // Load at absolute addresses
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

  #endregion
}