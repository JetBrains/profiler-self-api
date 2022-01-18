﻿using System;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Profiler.SelfApi.Impl.Linux;
using JetBrains.Profiler.SelfApi.Impl.Unix;

namespace JetBrains.Profiler.SelfApi.Impl
{
  internal static class Helper
  {
    private static readonly Lazy<PlatformId> ourPlatformLazy = new(DeducePlatformId);
    private static readonly Lazy<ArchitectureId> ourOsArchitectureLazy = new(DeduceOsArchitecture);
    private static readonly Lazy<LinuxLibCId?> ourLinuxLibCLazy = new(DeduceLinuxLibC);

    public static PlatformId Platform => ourPlatformLazy.Value;
    public static ArchitectureId OsArchitecture => ourOsArchitectureLazy.Value;
    public static LinuxLibCId? LinuxLibC => ourLinuxLibCLazy.Value;

    public static void ChModExecutable(string path)
    {
      if (ourPlatformLazy.Value != PlatformId.Windows)
        UnixHelper.UnixChMod(path, UnixFileModes.rwxr_xr_x);
    }

    public static void ChModNormal(string path)
    {
      if (ourPlatformLazy.Value != PlatformId.Windows)
        UnixHelper.UnixChMod(path, UnixFileModes.rw_r__r__);
    }

    private static PlatformId DeducePlatformId()
    {
#if NETSTANDARD1_0
#error No OS detection possible
#elif NET20 || NET35 || NET40 || NET45 || NET451 || NET452 || NET46 || NET461 || NET462 || NET47
      switch (Environment.OSVersion.Platform)
      {
      case PlatformID.Unix: return UnixHelper.Platform;
      case PlatformID.Win32NT: return PlatformId.Windows;
      }
#else
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return PlatformId.Windows;
      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return PlatformId.MacOsX;
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return PlatformId.Linux;
#endif
      throw new PlatformNotSupportedException();
    }

    private static ArchitectureId DeduceOsArchitecture()
    {
#if NETSTANDARD1_0
#error No OS detection possible
#elif NET20 || NET35 || NET40 || NET45 || NET451 || NET452 || NET46 || NET461 || NET462 || NET47
      switch (Environment.OSVersion.Platform)
      {
      case PlatformID.Unix: return UnixHelper.OsArchitecture;
      case PlatformID.Win32NT: return Environment.Is64BitOperatingSystem ? ArchitectureId.X64 : ArchitectureId.X86;
      }
#else
      switch (RuntimeInformation.OSArchitecture)
      {
      case Architecture.Arm64: return ArchitectureId.Arm64;
      case Architecture.X64: return ArchitectureId.X64;
      case Architecture.X86: return ArchitectureId.X86;
      }
#endif
      throw new PlatformNotSupportedException();
    }

    private static LinuxLibCId? DeduceLinuxLibC() => Platform == PlatformId.Linux ? LinuxHelper.LibC : null;

    public static string MakeRid(PlatformId platform, ArchitectureId architecture, LinuxLibCId? linuxLibC = null)
    {
      if (platform == PlatformId.Linux && linuxLibC == null)
        throw new ArgumentNullException(nameof(linuxLibC));
      if (platform != PlatformId.Linux && linuxLibC != null)
        throw new ArgumentException(nameof(linuxLibC));
      var builder = new StringBuilder().Append(platform.GetRidName());
      if (linuxLibC != null && linuxLibC.Value.GetRidName() is var linuxLibCStr && !string.IsNullOrEmpty(linuxLibCStr))
        builder.Append('-').Append(linuxLibCStr);
      builder.Append('-').Append(architecture.GetRidName());
      return builder.ToString();
    }

    private static string GetRidName(this PlatformId platform) => platform switch
      {
        PlatformId.Linux => "linux",
        PlatformId.MacOsX => "macos",
        PlatformId.Windows => "windows",
        _ => throw new PlatformNotSupportedException()
      };

    private static string GetRidName(this LinuxLibCId linuxLibC) => linuxLibC switch
      {
        LinuxLibCId.Glibc => "",
        LinuxLibCId.Musl => "musl",
        _ => throw new PlatformNotSupportedException()
      };

    private static string GetRidName(this ArchitectureId architecture) => architecture switch
      {
        ArchitectureId.Arm64 => "arm64",
        ArchitectureId.X64 => "x64",
        ArchitectureId.X86 => "x86",
        _ => throw new PlatformNotSupportedException()
      };

    public static void CheckAttachCompatibility()
    {
      // Note: This condition will not work on .NET Core 1.x/2.x because Environment.Version is incorrect.
      // Note: We also exclude .NET Core 3.x on macOS because the attach feature is not implemented in it. 
      if (ourPlatformLazy.Value == PlatformId.MacOsX && Environment.Version.Major == 3)
        throw new Exception("The self-profiling API is supported only on .NET 5.0 or later");
    }

    public static void CheckSamplingCompatibility()
    {
      // Note: This condition will not work on .NET Core 1.x/2.x because Environment.Version is incorrect.
      // Note: We also exclude .NET Core 3.0 on Unix because the synchronous sampling is not implemented in it. 
      if ((ourPlatformLazy.Value == PlatformId.Linux || ourPlatformLazy.Value == PlatformId.MacOsX) && Environment.Version.Major == 3 && Environment.Version.Minor == 0)
        throw new Exception("The self-profiling API is supported only on .NET Core 3.1 or later");
    }
  }
}