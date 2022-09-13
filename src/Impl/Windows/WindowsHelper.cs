using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace JetBrains.Profiler.SelfApi.Impl.Windows
{
  internal static class WindowsHelper
  {
    private static readonly Kernel32Dll.IsWow64Process2Delegate ourIsWow64Process2;
    private static readonly Lazy<ArchitectureId> ourOsArchitectureLazy = new(DeduceOsArchitecture);

    static unsafe WindowsHelper()
    {
      var hModule = Kernel32Dll.GetModuleHandleW(Kernel32Dll.LibraryName);
      if (hModule != null)
      {
        var pIsWow64Process2 = Kernel32Dll.GetProcAddress(hModule, nameof(Kernel32Dll.IsWow64Process2));
        if (pIsWow64Process2 != null)
          ourIsWow64Process2 = (Kernel32Dll.IsWow64Process2Delegate)Marshal.GetDelegateForFunctionPointer((IntPtr)pIsWow64Process2, typeof(Kernel32Dll.IsWow64Process2Delegate));
      }
    }

    public static ArchitectureId OsArchitecture => ourOsArchitectureLazy.Value;

    private static ArchitectureId ToArchitecture(MachineId machineId) => machineId switch
      {
        MachineId.IMAGE_FILE_MACHINE_I386 => ArchitectureId.X86,
        MachineId.IMAGE_FILE_MACHINE_AMD64 => ArchitectureId.X64,
        MachineId.IMAGE_FILE_MACHINE_ARMNT => ArchitectureId.Arm,
        MachineId.IMAGE_FILE_MACHINE_ARM64 => ArchitectureId.Arm64,
        _ => throw new ArgumentOutOfRangeException(nameof(machineId), machineId, null)
      };

    private static ArchitectureId ToArchitecture(ProcessorArchitecture processorArchitecture) => processorArchitecture switch
      {
        ProcessorArchitecture.PROCESSOR_ARCHITECTURE_INTEL => ArchitectureId.X86,
        ProcessorArchitecture.PROCESSOR_ARCHITECTURE_AMD64 => ArchitectureId.X64,
        ProcessorArchitecture.PROCESSOR_ARCHITECTURE_ARM => ArchitectureId.Arm,
        ProcessorArchitecture.PROCESSOR_ARCHITECTURE_ARM64 => ArchitectureId.Arm64,
        _ => throw new ArgumentOutOfRangeException(nameof(processorArchitecture), processorArchitecture, null)
      };

    private static unsafe ArchitectureId DeduceOsArchitecture()
    {
      // Note(ww898): GetNativeSystemInfo() on Windows ARM64 returns 9(PROCESSOR_ARCHITECTURE_AMD64) instead of 12(PROCESSOR_ARCHITECTURE_ARM64) for X64 processes!!! So, I should use Kernel32Dll.IsWow64Process2() to detect real OS architecture.
      if (ourIsWow64Process2 != null)
      {
        MachineId processMachine, nativeMachine;
        if (ourIsWow64Process2(Kernel32Dll.GetCurrentProcess(), &processMachine, &nativeMachine) == 0)
          throw new Win32Exception();
        return ToArchitecture(nativeMachine);
      }

      var systemInfo = new SYSTEM_INFO();
      Kernel32Dll.GetNativeSystemInfo(&systemInfo);
      return ToArchitecture(systemInfo.wProcessorArchitecture);
    }
  }
}