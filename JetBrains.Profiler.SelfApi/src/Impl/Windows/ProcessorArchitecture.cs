namespace JetBrains.Profiler.SelfApi.Impl.Windows
{
  internal enum ProcessorArchitecture : ushort
  {
    /// <summary>
    ///   x86
    /// </summary>
    PROCESSOR_ARCHITECTURE_INTEL = 0,

    PROCESSOR_ARCHITECTURE_MIPS = 1,
    PROCESSOR_ARCHITECTURE_ALPHA = 2,
    PROCESSOR_ARCHITECTURE_PPC = 3,
    PROCESSOR_ARCHITECTURE_SHX = 4,

    /// <summary>
    ///   armv7*
    /// </summary>
    PROCESSOR_ARCHITECTURE_ARM = 5,

    /// <summary>
    ///   Intel Itanium-based
    /// </summary>
    PROCESSOR_ARCHITECTURE_IA64 = 6,

    PROCESSOR_ARCHITECTURE_ALPHA64 = 7,
    PROCESSOR_ARCHITECTURE_MSIL = 8,

    /// <summary>
    ///   x86_64, amd64, x64
    /// </summary>
    PROCESSOR_ARCHITECTURE_AMD64 = 9,

    PROCESSOR_ARCHITECTURE_IA32_ON_WIN64 = 10,
    PROCESSOR_ARCHITECTURE_NEUTRAL = 11,

    /// <summary>
    ///   arm64v8, aarch64
    /// </summary>
    PROCESSOR_ARCHITECTURE_ARM64 = 12,

    /// <summary>
    ///   Unknown architecture.
    /// </summary>
    PROCESSOR_ARCHITECTURE_UNKNOWN = 0xFFFF
  }
}