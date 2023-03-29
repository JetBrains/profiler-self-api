using System.IO;

namespace JetBrains.Profiler.SelfApi.Impl.Unix.Elf
{
  internal static class ReadUtils
  {
    /// <summary>
    /// Set the stream position to start
    /// </summary>
    /// <param name="stream">Stream</param>
    /// <returns>Fluent</returns>
    internal static Stream Rewind(this Stream stream)
    {
      stream.Seek(0, SeekOrigin.Begin);
      return stream;
    }

    internal static void Skip(this BinaryReader reader, int len)
    {
      reader.BaseStream.Seek(len, SeekOrigin.Current);
    }

    internal static void Jump(this BinaryReader reader, uint len)
    {
      reader.BaseStream.Seek(len, SeekOrigin.Begin);
    }

    /// <summary>
    /// Read all data from the stream
    /// </summary>
    /// <param name="stream">Stream</param>
    internal static byte[] ReadAll(this Stream stream)
    {
      stream.Rewind();
      using var ms = new MemoryStream();
      stream.CopyTo(ms);
      return ms.ToArray();
    }

    /// <summary>
    /// Read all data from the current position of the stream
    /// </summary>
    /// <param name="stream">Stream</param>
    internal static byte[] ReadToEnd(this Stream stream)
    {
      using var ms = new MemoryStream();
      stream.CopyTo(ms);
      return ms.ToArray();
    }

    internal static uint ReadUInt32Be(this BinaryReader reader)
    {
      return SwapBytes(reader.ReadUInt32());
    }

    internal static ushort ReadUInt16(this BinaryReader reader, bool isBe)
    {
      var value = reader.ReadUInt16();
      return isBe
        ? SwapBytes(value)
        : value;
    }

    internal static uint ReadUInt32(this BinaryReader reader, bool isBe)
    {
      var value = reader.ReadUInt32();
      return isBe
        ? SwapBytes(value)
        : value;
    }

    internal static ulong ReadUInt64(this BinaryReader reader, bool isBe)
    {
      var value = reader.ReadUInt64();
      return isBe
        ? SwapBytes(value)
        : value;
    }

    internal static ushort ReadUInt16Le(BinaryReader reader, bool isBe)
    {
      var value = reader.ReadUInt16();
      return isBe
        ? SwapBytes(value)
        : value;
    }

    internal static uint ReadUInt32Le(BinaryReader reader, bool isBe)
    {
      var value = reader.ReadUInt32();
      return isBe
        ? SwapBytes(value)
        : value;
    }

    internal static ulong ReadUInt64Le(BinaryReader reader, bool isBe)
    {
      var value = reader.ReadUInt64();
      return isBe
        ? SwapBytes(value)
        : value;
    }

    private static ushort SwapBytes(ushort val)
    {
      return (ushort)((val << 8) | (val >> 8));
    }

    private static uint SwapBytes(uint val)
    {
      return (uint)SwapBytes((ushort)val) << 16 |
             (uint)SwapBytes((ushort)(val >> 16));
    }

    private static ulong SwapBytes(ulong val)
    {
      return (ulong)SwapBytes((uint)val) << 32 |
             (ulong)SwapBytes((uint)(val >> 32));
    }
  }
}