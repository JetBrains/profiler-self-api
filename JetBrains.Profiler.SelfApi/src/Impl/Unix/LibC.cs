using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace JetBrains.Profiler.SelfApi.Impl.Unix
{
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  internal static class LibC
  {
    internal const string LibraryName = "libc"; // Note: No extension here, because CoreCLR support that case

    [DllImport(LibraryName, SetLastError = true, ExactSpelling = true)]
    internal static extern int chmod(string pathname, UnixFileModes mode);
  }
}