using System;
using System.IO;
using System.Text;
using JetBrains.Annotations;

namespace JetBrains.Profiler.SelfApi.Impl.Unix.Elf
{
  internal static class ElfUtil
  {
    [NotNull]
    public static ElfInfo GetElfInfo([NotNull] Stream stream)
    {
      using var reader = new BinaryReader(stream.Rewind(), Encoding.UTF8, true);

      try
      {
        if (reader.ReadUInt32Be() != 0X7F454C46)
          throw new FormatException("Invalid ELF magics");

        ElfClass ei_class;

        switch (reader.ReadByte()) // e_ident[EI_CLASS]
        {
        case 1:
          ei_class = ElfClass.ELFCLASS32;
          break;
        case 2:
          ei_class = ElfClass.ELFCLASS64;
          break;
        default: throw new FormatException("Inconsistent ELF class");
        }

        bool isBe;
        ElfData ei_data;

        switch (reader.ReadByte()) // e_ident[EI_DATA]
        {
        case 1:
          isBe = false;
          ei_data = ElfData.ELFDATA2LSB;
          break;
        case 2:
          isBe = true;
          ei_data = ElfData.ELFDATA2MSB;
          break;
        default: throw new FormatException("Inconsistent ELF data");
        }

        var version = (ElfVersion)reader.ReadByte();

        if (version != ElfVersion.EV_CURRENT)
          throw new FormatException("Inconsistent ELF version");

        var osabi = (ElfOsAbi)reader.ReadByte();
        var osAbiVersion = reader.ReadByte();

        ElfType type;
        ElfMachine machine;
        ElfFlags flags;
        string interpreter = null;

        stream.Seek(7, SeekOrigin.Current); // skip EI_PAD

        switch (ei_class)
        {
        case ElfClass.ELFCLASS32:
          type = (ElfType)reader.ReadUInt16(isBe);
          machine = (ElfMachine)reader.ReadUInt16(isBe);
          var e_version32 = reader.ReadUInt32(isBe);

          if (e_version32 != (uint)ElfVersion.EV_CURRENT)
            throw new FormatException("Invalid version of ELF32 program header");

          stream.Seek(4, SeekOrigin.Current); // skip e_entry
          var ePhOff32 = reader.ReadUInt32(isBe);
          stream.Seek(4, SeekOrigin.Current); // skip e_shoff
          flags = (ElfFlags)reader.ReadUInt32(isBe);
          stream.Seek(2, SeekOrigin.Current); // skip e_ehsize
          var e_phentsize32 = reader.ReadUInt16(isBe);
          var ePhNum32 = reader.ReadUInt16(isBe);

          stream.Seek(ePhOff32, SeekOrigin.Begin);

          for (var phi = ePhNum32; phi-- > 0;)
          {
            var p_type = (ElfSegmentType)reader.ReadUInt32(isBe);

            if (p_type == ElfSegmentType.PT_INTERP)
            {
              var pOffset32 = reader.ReadUInt32(isBe);
              stream.Seek(8, SeekOrigin.Current); //skip p_vaddr, p_paddr
              var pFileSz32 = reader.ReadUInt32(isBe);
              stream.Seek(pOffset32, SeekOrigin.Begin);
              interpreter = new string(reader.ReadChars((int)(pFileSz32 - 1)));
              break;
            }

            stream.Seek(e_phentsize32 - 4, SeekOrigin.Current);
          }
          break;
        case ElfClass.ELFCLASS64:
          type = (ElfType)reader.ReadUInt16(isBe);
          machine = (ElfMachine)reader.ReadUInt16(isBe);
          var e_version64 = reader.ReadUInt32(isBe);

          if (e_version64 != (uint)ElfVersion.EV_CURRENT)
            throw new FormatException("Invalid version of ELF64 program header");

          stream.Seek(8, SeekOrigin.Current); // skip e_entry
          var ePhOff64 = reader.ReadUInt64(isBe);
          stream.Seek(8, SeekOrigin.Current); // skip e_shoff
          flags = (ElfFlags)reader.ReadUInt32(isBe);
          stream.Seek(2, SeekOrigin.Current); // skip e_ehsize
          var e_phentsize64 = reader.ReadUInt16(isBe);
          var ePhNum64 = reader.ReadUInt16(isBe);

          stream.Seek((long)ePhOff64, SeekOrigin.Begin);

          for (var phi = ePhNum64; phi-- > 0;)
          {
            var p_type = (ElfSegmentType)reader.ReadUInt32(isBe);

            if (p_type == ElfSegmentType.PT_INTERP)
            {
              stream.Seek(4, SeekOrigin.Current); //skip p_flags
              var pOffset64 = reader.ReadUInt64(isBe);
              stream.Seek(16, SeekOrigin.Current); //skip p_vaddr, p_paddr
              var pFileSz64 = reader.ReadUInt64(isBe);
              stream.Seek((long)pOffset64, SeekOrigin.Begin);
              interpreter = new string(reader.ReadChars((int)(pFileSz64 - 1)));
              break;
            }

            stream.Seek(e_phentsize64 - 4, SeekOrigin.Current);
          }
          break;

        default:
          throw new FormatException("Unknown ELF class");
        }

        return new ElfInfo(ei_class, ei_data, osabi, osAbiVersion, type, machine, flags, interpreter);
      }
      catch (IOException)
      {
        throw new InvalidDataException("Unknown format");
      }
    }
  }
}