using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace JetBrains.Profiler.SelfApi.Impl.Unix
{
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  [SuppressMessage("ReSharper", "IdentifierTypo")]
  internal static class LibC
  {
    private const string LibraryName = "libc"; // Note: No extension here, because CoreCLR support that case

    [DllImport(LibraryName, SetLastError = true)]
    public static extern int uname(IntPtr buf);
    
    [DllImport(LibraryName, SetLastError = true)]
    public static extern int chmod(string pathname, UnixFileModes mode);
  }
}