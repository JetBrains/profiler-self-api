using System;
using System.IO;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace JetBrains.Profiler.SelfApi.Impl.Unix
{
  internal static class UnixHelper
  {
    private static readonly Lazy<Tuple<PlatformId, ArchitectureId>> ourUnixConfigLazy = new(DeduceUnixConfig);

    public static PlatformId Platform => ourUnixConfigLazy.Value.Item1;
    public static ArchitectureId OsArchitecture => ourUnixConfigLazy.Value.Item2;

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

    private static ArchitectureId ToArchitecture([NotNull] string machine) => machine switch
      {
        "arm64" or "aarch64" => ArchitectureId.Arm64,
        "x86_64" => ArchitectureId.X64,
        _ => throw new PlatformNotSupportedException()
      };

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