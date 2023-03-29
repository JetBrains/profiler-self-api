using System.Diagnostics.CodeAnalysis;

namespace JetBrains.Profiler.SelfApi.Impl.Unix
{
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  [SuppressMessage("ReSharper", "IdentifierTypo")]
  internal enum UnixFileModes : uint
  {
    /// <summary>
    /// Set user ID on execution
    /// </summary>
    S_ISUID = 0x0800,

    /// <summary>
    /// Set group ID on execution
    /// </summary>
    S_ISGID = 0x0400,

    /// <summary>
    /// Save swapped text after use (sticky).
    /// </summary>
    S_ISVTX = 0x0200,

    /// <summary>
    /// Read by owner
    /// </summary>
    S_IRUSR = 0x0100,

    /// <summary>
    /// Write by owner
    /// </summary>
    S_IWUSR = 0x0080,

    /// <summary>
    /// Execute by owner
    /// </summary>
    S_IXUSR = 0x0040,

    /// <summary>
    /// Read by group
    /// </summary>
    S_IRGRP = 0x0020,

    /// <summary>
    /// Write by group
    /// </summary>
    S_IWGRP = 0x0010,

    /// <summary>
    /// Execute by group
    /// </summary>
    S_IXGRP = 0x0008,

    /// <summary>
    /// Read by other
    /// </summary>
    S_IROTH = 0x0004,

    /// <summary>
    /// Write by other
    /// </summary>
    S_IWOTH = 0x0002,

    /// <summary>
    /// Execute by other
    /// </summary>
    S_IXOTH = 0x0001,

    /// <summary>
    /// Read, write, execute by group
    /// </summary>
    S_IRWXG = S_IRGRP | S_IWGRP | S_IXGRP,

    /// <summary>
    /// Read, write, execute by user
    /// </summary>
    S_IRWXU = S_IRUSR | S_IWUSR | S_IXUSR,

    /// <summary>
    /// Read, write, execute by other
    /// </summary>
    S_IRWXO = S_IROTH | S_IWOTH | S_IXOTH,

    /// <summary>
    /// 0777
    /// </summary>
    ACCESSPERMS = S_IRWXU | S_IRWXG | S_IRWXO,

    /// <summary>
    /// 07777
    /// </summary>
    ALLPERMS = S_ISUID | S_ISGID | S_ISVTX | S_IRWXU | S_IRWXG | S_IRWXO,

    /// <summary>
    /// 0666
    /// </summary>
    DEFFILEMODE = S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH,

    /// <summary>
    /// Bits which determine file type
    /// </summary>
    S_IFMT = 0xF000,

    /// <summary>
    /// Directory
    /// </summary>
    S_IFDIR = 0x4000,

    /// <summary>
    /// Character device
    /// </summary>
    S_IFCHR = 0x2000,

    /// <summary>
    /// Block device
    /// </summary>
    S_IFBLK = 0x6000,

    /// <summary>
    /// Regular file
    /// </summary>
    S_IFREG = 0x8000,

    /// <summary>
    /// FIFO
    /// </summary>
    S_IFIFO = 0x1000,

    /// <summary>
    /// Symbolic link
    /// </summary>
    S_IFLNK = 0xA000,

    /// <summary>
    /// Socket
    /// </summary>
    S_IFSOCK = 0xC000,

    /// <summary>
    /// Alias for 0644
    /// </summary>
    rw_r__r__ = S_IRUSR | S_IWUSR | S_IRGRP | S_IROTH,

    /// <summary>
    /// Alias for 0755
    /// </summary>
    rwxr_xr_x = S_IRWXU | S_IRGRP | S_IXGRP | S_IROTH | S_IXOTH
  }
}