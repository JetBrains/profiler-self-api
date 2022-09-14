using System;
using System.IO;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace JetBrains.Profiler.SelfApi.Impl.Unix
{
  internal static class UnixHelper
  {
    private static readonly Lazy<Tuple<PlatformId, ArchitectureId>> ourUnameLazy = new(DeduceUname);

    public static PlatformId Platform => ourUnameLazy.Value.Item1;
    public static ArchitectureId KernelArchitecture => ourUnameLazy.Value.Item2;

    private static PlatformId ToPlatformId([NotNull] string sysname) => sysname switch
      {
        "Darwin" => PlatformId.MacOsX,
        "Linux" => PlatformId.Linux,
        _ => throw new PlatformNotSupportedException()
      };

    private static int ToNameLen(this PlatformId platformId) => platformId switch
      {
        PlatformId.Linux => 65,
        PlatformId.MacOsX => 256,
        _ => throw new PlatformNotSupportedException()
      };

    private static ArchitectureId ToArchitecture(PlatformId platform, [CanBeNull] string machine) => platform switch
      {
        PlatformId.Linux => machine switch
          {
            "aarch64" => ArchitectureId.Arm64,
            "x86_64" => ArchitectureId.X64,
            "armv7l" or "armv8l" => ArchitectureId.Arm,
            _ => throw new ArgumentOutOfRangeException(nameof(machine), machine, null)
          },
        PlatformId.MacOsX => machine switch
          {
            "arm64" => ArchitectureId.Arm64,
            "x86_64" => ArchitectureId.X64,
            _ => throw new ArgumentOutOfRangeException(nameof(machine), machine, null)
          },
        _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
      };

    [NotNull]
    private static Tuple<PlatformId, ArchitectureId> DeduceUname()
    {
      var buf = IntPtr.Zero;
      try
      {
        // from sys/utsname.h:
        // struct utsname
        // {
        //   char sysname[NAMELEN];
        //   char nodename[NAMELEN];
        //   char release[NAMELEN];
        //   char version[NAMELEN];
        //   char machine[NAMELEN];
        // };

        buf = Marshal.AllocHGlobal(8192);
        var rc = LibC.uname(buf);
        if (rc != 0)
          throw new Exception("uname() from libc returned " + rc);

        var platform = ToPlatformId(Marshal.PtrToStringAnsi(buf));
        var nameLen = platform.ToNameLen();
        const int machineIndex = 4;
        return Tuple.Create(platform, ToArchitecture(platform, Marshal.PtrToStringAnsi(buf + machineIndex * nameLen)));
      }
      finally
      {
        if (buf != IntPtr.Zero)
          Marshal.FreeHGlobal(buf);
      }
    }

    public static void UnixChMod([NotNull] string path, UnixFileModes mode)
    {
      if (!Path.IsPathRooted(path))
        throw new ArgumentException(nameof(path));
      var rc = LibC.chmod(path, mode);
      if (rc != 0)
        throw new Exception("chmod() was failed with errno " + Marshal.GetLastWin32Error());
    }
  }
}