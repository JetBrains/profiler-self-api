using System;
using System.IO;
using System.Runtime.InteropServices;
using JetBrains.Profiler.SelfApi.Impl.Unix;

namespace JetBrains.Profiler.SelfApi.Impl
{
  internal static partial class Helper
  {
    private static readonly Lazy<Tuple<PlatformId, ArchitectureId>> UnixConfigLazy = new Lazy<Tuple<PlatformId, ArchitectureId>>(DeduceUnixConfig);

    private static PlatformId ToPlatformId(string sysname)
    {
      switch (sysname)
      {
      case "Darwin": return PlatformId.MacOs;
      case "Linux": return PlatformId.Linux;
      default: throw new ArgumentOutOfRangeException(nameof(sysname), sysname, null);
      }
    }

    private static int ToNameLen(this PlatformId platformId)
    {
      switch (platformId)
      {
      case PlatformId.Linux: return 65;
      case PlatformId.MacOs: return 256;
      default: throw new ArgumentOutOfRangeException(nameof(platformId), platformId, null);
      }
    }

    private static ArchitectureId ToArchitecture(string machine)
    {
      switch (machine)
      {
      case "aarch64": return ArchitectureId.Arm64;
      case "x86_64": return ArchitectureId.X64;
      default: throw new ArgumentOutOfRangeException(nameof(machine), machine, null);
      }
    }

    private static Tuple<PlatformId, ArchitectureId> DeduceUnixConfig()
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
          throw new Exception("uname() was failed with errno " + Marshal.GetLastWin32Error());

        var platform = ToPlatformId(Marshal.PtrToStringAnsi(buf));
        var nameLen = platform.ToNameLen();
        const int machineIndex = 4;
        return Tuple.Create(platform, ToArchitecture(Marshal.PtrToStringAnsi(buf + machineIndex * nameLen)));
      }
      finally
      {
        if (buf != IntPtr.Zero)
          Marshal.FreeHGlobal(buf);
      }
    }

    private static void UnixChMod(string path, UnixFileModes mode)
    {
      if (!Path.IsPathRooted(path))
        throw new ArgumentException(nameof(path));
      var rc = LibC.chmod(path, mode);
      if (rc != 0)
        throw new Exception("chmod() was failed with errno " + Marshal.GetLastWin32Error());
    }
  }
}