using System;
using JetBrains.Annotations;
using JetBrains.HabitatDetector;
using JetBrains.Profiler.SelfApi.Impl.Unix;

namespace JetBrains.Profiler.SelfApi.Impl
{
  internal static class Helper
  {
    public static void ChModExecutable([NotNull] string path)
    {
      if (HabitatInfo.Platform != JetPlatform.Windows)
        UnixHelper.UnixChMod(path, UnixFileModes.rwxr_xr_x);
    }

    public static void CheckAttachCompatibility()
    {
      // Note: This condition will not work on .NET Core 1.x/2.x because Environment.Version is incorrect.
      // Note: We also exclude .NET Core 3.x on macOS because the attach feature is not implemented in it.
      if (HabitatInfo.Platform == JetPlatform.MacOsX && Environment.Version.Major == 3)
        throw new Exception("The self-profiling API is supported only on .NET 5.0 or later");
    }

    public static void CheckSamplingCompatibility()
    {
      // Note: This condition will not work on .NET Core 1.x/2.x because Environment.Version is incorrect.
      // Note: We also exclude .NET Core 3.0 on Unix because the synchronous sampling is not implemented in it.
      if (HabitatInfo.Platform is JetPlatform.Linux or JetPlatform.MacOsX && Environment.Version.Major == 3 && Environment.Version.Minor == 0)
        throw new Exception("The self-profiling API is supported only on .NET Core 3.1 or later");
    }
  }
}