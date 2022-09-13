using System.Diagnostics.CodeAnalysis;

namespace JetBrains.Profiler.SelfApi.Impl.Windows
{
  /// <summary>
  ///   Machine ID in the COFF header
  /// </summary>
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal enum MachineId : ushort
  {
    // @formatter:off
    IMAGE_FILE_MACHINE_UNKNOWN     = 0x0000,
    IMAGE_FILE_MACHINE_TARGET_HOST = 0x0001,  // Useful for indicating we want to interact with the host and not a WoW guest.
    IMAGE_FILE_MACHINE_I386        = 0x014c,  // Intel 386.
    IMAGE_FILE_MACHINE_R3000       = 0x0162,  // MIPS little-endian, 0x160 big-endian
    IMAGE_FILE_MACHINE_R4000       = 0x0166,  // MIPS little-endian
    IMAGE_FILE_MACHINE_R10000      = 0x0168,  // MIPS little-endian
    IMAGE_FILE_MACHINE_WCEMIPSV2   = 0x0169,  // MIPS little-endian WCE v2
    IMAGE_FILE_MACHINE_ALPHA       = 0x0184,  // Alpha_AXP
    IMAGE_FILE_MACHINE_SH3         = 0x01a2,  // Hitachi SH3 little-endian
    IMAGE_FILE_MACHINE_SH3DSP      = 0x01a3,  // Hitachi SH3 DSP
    IMAGE_FILE_MACHINE_SH3E        = 0x01a4,  // Hitachi SH3E little-endian
    IMAGE_FILE_MACHINE_SH4         = 0x01a6,  // Hitachi SH4 little-endian
    IMAGE_FILE_MACHINE_SH5         = 0x01a8,  // SH5
    IMAGE_FILE_MACHINE_ARM         = 0x01c0,  // ARM Little-Endian
    IMAGE_FILE_MACHINE_THUMB       = 0x01c2,  // ARM Thumb/Thumb-2 Little-Endian (ARM 10 Thumb family CPU)
    IMAGE_FILE_MACHINE_ARMNT       = 0x01c4,  // ARM Thumb-2 Little-Endian (Windows ARMv7)
    IMAGE_FILE_MACHINE_AM33        = 0x01d3,  // Matsushita AM33
    IMAGE_FILE_MACHINE_POWERPC     = 0x01F0,  // IBM PowerPC Little-Endian
    IMAGE_FILE_MACHINE_POWERPCFP   = 0x01f1,  // IBM PowerPC FP
    IMAGE_FILE_MACHINE_IA64        = 0x0200,  // Intel 64
    IMAGE_FILE_MACHINE_MIPS16      = 0x0266,  // MIPS
    IMAGE_FILE_MACHINE_ALPHA64     = 0x0284,  // ALPHA64
    IMAGE_FILE_MACHINE_MIPSFPU     = 0x0366,  // MIPS
    IMAGE_FILE_MACHINE_MIPSFPU16   = 0x0466,  // MIPS
    IMAGE_FILE_MACHINE_TRICORE     = 0x0520,  // Infineon
    IMAGE_FILE_MACHINE_CEF         = 0x0CEF,  // Common Executable Format (Windows CE).
    IMAGE_FILE_MACHINE_EBC         = 0x0EBC,  // EFI Byte Code
    IMAGE_FILE_MACHINE_AMD64       = 0x8664,  // AMD64 (K8)
    IMAGE_FILE_MACHINE_M32R        = 0x9041,  // M32R little-endian
    IMAGE_FILE_MACHINE_ARM64       = 0xAA64,  // ARM64 Little-Endian (Windows ARM64v8)
    IMAGE_FILE_MACHINE_CEE         = 0xC0EE,
    IMAGE_FILE_MACHINE_RISCV32     = 0x5032,  // 32bit RISC-V ISA
    IMAGE_FILE_MACHINE_RISCV64     = 0x5064,  // 64bit RISC-V ISA
    IMAGE_FILE_MACHINE_RISCV128    = 0x5128,  // 128bit RISC-V ISA
    IMAGE_FILE_MACHINE_LOONGARCH32 = 0x6232,  // LOONGARCH32
    IMAGE_FILE_MACHINE_LOONGARCH64 = 0x6264,  // LOONGARCH64
    
    // Obsolete
    
    IMAGE_FILE_MACHINE_I486        = 0x014d, // Intel 486.
    IMAGE_FILE_MACHINE_PENTIUM     = 0x014e, // Intel Pentium.
    IMAGE_FILE_MACHINE_R3000_BE    = 0x0160, // MIPS 3K big-endian
    IMAGE_FILE_MACHINE_POWERPC_BE  = 0x01F2, // Xbox 360 (Xenon) (IBM PowerPC big-endian)
    IMAGE_FILE_MACHINE_SPARC       = 0x2000,
    // @formatter:on
  }
}