using System;
using System.IO;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace JetBrains.Profiler.SelfApi.Impl.Unix
{
  internal static class UnixHelper
  {
    internal static void UnixChMod([NotNull] string path, UnixFileModes mode)
    {
      if (!Path.IsPathRooted(path))
        throw new ArgumentException(nameof(path));
      var rc = LibC.chmod(path, mode);
      if (rc != 0)
        throw new Exception("chmod() was failed with errno " + Marshal.GetLastWin32Error());
    }
  }
}