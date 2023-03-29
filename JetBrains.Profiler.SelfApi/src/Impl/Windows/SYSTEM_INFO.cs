using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace JetBrains.Profiler.SelfApi.Impl.Windows
{
  [StructLayout(LayoutKind.Sequential)]
  [NoReorder]
  internal struct SYSTEM_INFO
  {
    /// <seealso cref="ProcessorArchitecture"/>
    public ProcessorArchitecture wProcessorArchitecture;
    public UInt16 wReserved;
    public UInt32 dwPageSize;
    public IntPtr lpMinimumApplicationAddress;
    public IntPtr lpMaximumApplicationAddress;
    public UIntPtr dwActiveProcessorMask;
    public UInt32 dwNumberOfProcessors;
    public UInt32 dwProcessorType;
    public UInt32 dwAllocationGranularity;
    public UInt16 wProcessorLevel;
    public UInt16 wProcessorRevision;
  }
}