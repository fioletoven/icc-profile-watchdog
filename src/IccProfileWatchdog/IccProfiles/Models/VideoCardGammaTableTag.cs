using IccProfileWatchdog.IccProfiles.Extensions;
using System.Buffers;

namespace IccProfileWatchdog.IccProfiles.Models;

public class VideoCardGammaTableTag
{
    public ushort[] Red { get; }

    public ushort[] Green { get; }

    public ushort[] Blue { get; }

    public VideoCardGammaTableTag(ref SequenceReader<byte> reader)
    {
        reader.Advance(14);

        Red = new ushort[256];
        for (int i = 0; i < 256; i++)
        {
            Red[i] = reader.ReadBigEndianUInt16();
        }

        Green = new ushort[256];
        for (int i = 0; i < 256; i++)
        {
            Green[i] = reader.ReadBigEndianUInt16();
        }

        Blue = new ushort[256];
        for (int i = 0; i < 256; i++)
        {
            Blue[i] = reader.ReadBigEndianUInt16();
        }
    }
}
