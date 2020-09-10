﻿using System;
using System.Runtime.InteropServices;
using JetBrains.Profiler.SelfApi.Impl.Unix;

namespace JetBrains.Profiler.SelfApi.Impl
{
  internal static partial class Helper
  {
    private static readonly Lazy<PlatformId> ourPlatform = new Lazy<PlatformId>(DeducePlatformId);
    private static readonly Lazy<ArchitectureId> ourOsArchitecture = new Lazy<ArchitectureId>(DeduceOsArchitecture);

    public static PlatformId Platform => ourPlatform.Value;
    public static ArchitectureId OsArchitecture => ourOsArchitecture.Value;

    public static void ChModExecutable(string path)
    {
      if (ourPlatform.Value != PlatformId.Windows)
        UnixChMod(path, UnixFileModes.rwxr_xr_x);
    }

    public static void ChModNormal(string path)
    {
      if (ourPlatform.Value != PlatformId.Windows)
        UnixChMod(path, UnixFileModes.rw_r__r__);
    }

    private static PlatformId DeducePlatformId()
    {
#if NETSTANDARD1_0
#error No OS detection possible
#elif NET20 || NET35 || NET40 || NET45 || NET451 || NET452 || NET46 || NET461 || NET462 || NET47
      switch (Environment.OSVersion.Platform)
      {
      case PlatformID.Unix: return ourUnixConfig.Value.Item1;
      case PlatformID.Win32NT: return PlatformId.Windows;
      }
#else
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return PlatformId.Windows;
      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return PlatformId.MacOs;
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
      case PlatformID.Unix: return ourUnixConfig.Value.Item2;
      case PlatformID.Win32NT: return Environment.Is64BitOperatingSystem ? ArchitectureId.X64 : ArchitectureId.X86;
      default:
        throw new PlatformNotSupportedException();
      }
#else
      switch (RuntimeInformation.OSArchitecture)
      {
      case Architecture.Arm64: return ArchitectureId.Arm64;
      case Architecture.X64: return ArchitectureId.X64;
      case Architecture.X86: return ArchitectureId.X86;
      default: throw new ArgumentOutOfRangeException();
      }
#endif
    }

    public static string ToFolderName(this PlatformId platformId)
    {
      switch (platformId)
      {
      case PlatformId.Linux: return "linux";
      case PlatformId.MacOs: return "macos";
      case PlatformId.Windows: return "windows";
      default: throw new ArgumentOutOfRangeException(nameof(platformId), platformId, null);
      }
    }

    public static string ToFolderName(this ArchitectureId architectureId)
    {
      switch (architectureId)
      {
      case ArchitectureId.Arm64: return "arm64";
      case ArchitectureId.X64: return "x64";
      case ArchitectureId.X86: return "x86";
      default: throw new ArgumentOutOfRangeException(nameof(architectureId), architectureId, null);
      }
    }

    public static void CheckAttachCompatibility()
    {
      // Note: This condition will not work on .NET Core 1.x/2.x because Environment.Version is incorrect.
      // Note: We also exclude .NET Core 3.x on macOS because the attach feature is not implemented in it. 
      if (ourPlatform.Value == PlatformId.MacOs && Environment.Version.Major == 3)
        throw new Exception("The self-profiling API is supported only on .NET 5.0 or later");
    }

    public static void CheckSamplingCompatibility()
    {
      // Note: This condition will not work on .NET Core 1.x/2.x because Environment.Version is incorrect.
      // Note: We also exclude .NET Core 3.0 on Unix because the synchronous sampling is not implemented in it. 
      if ((ourPlatform.Value == PlatformId.Linux || ourPlatform.Value == PlatformId.MacOs) && Environment.Version.Major == 3 && Environment.Version.Minor == 0)
        throw new Exception("The self-profiling API is supported only on .NET Core 3.1 or later");
    }
  }
}