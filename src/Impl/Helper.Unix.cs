using System;
using System.Runtime.InteropServices;
using JetBrains.Profiler.SelfApi.Impl.Unix;

namespace JetBrains.Profiler.SelfApi.Impl
{
  internal static partial class Helper
  {
    private static readonly Lazy<UnixConfig> ourUnixConfig = new Lazy<UnixConfig>(DeduceUnixConfig);

    private static PlatformId ToPlatformId(string sysname)
    {
      switch (sysname)
      {
      case "Darwin": return PlatformId.MacOsX;
      case "Linux": return PlatformId.Linux;
      default: throw new ArgumentOutOfRangeException(nameof(sysname), sysname, null);
      }
    }

    private static int ToNameLen(this PlatformId platformId)
    {
      switch (platformId)
      {
      case PlatformId.Linux: return 65;
      case PlatformId.MacOsX: return 256;
      default: throw new ArgumentOutOfRangeException(nameof(platformId), platformId, null);
      }
    }

    private static ArchitectureId ToArchitecture(this string machine)
    {
      switch (machine)
      {
      case "aarch64": return ArchitectureId.Arm64;
      case "x86_64": return ArchitectureId.X64;
      default: throw new ArgumentOutOfRangeException(nameof(machine), machine, null);
      }
    }

    private static UnixConfig DeduceUnixConfig()
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

        var platformId = ToPlatformId(Marshal.PtrToStringAnsi(buf));
        var nameLen = platformId.ToNameLen();
        const int machineIndex = 4;
        return new UnixConfig
          {
            PlatformId = platformId,
            ArchitectureId = ToArchitecture(Marshal.PtrToStringAnsi(buf + machineIndex * nameLen))
          };
      }
      finally
      {
        if (buf != IntPtr.Zero)
          Marshal.FreeHGlobal(buf);
      }
    }

    #region Nested type: UnixConfig

    private struct UnixConfig
    {
      public PlatformId PlatformId;
      public ArchitectureId ArchitectureId;
    }

    #endregion
  }
}