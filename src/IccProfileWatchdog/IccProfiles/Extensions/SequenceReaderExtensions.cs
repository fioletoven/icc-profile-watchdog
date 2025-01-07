using System.Buffers;

namespace IccProfileWatchdog.IccProfiles.Extensions;

internal static class SequenceReaderExtensions
{
    /// <summary>
    /// Attempts to read a big endian value of a given number of bytes into a ulong. 
    /// </summary>
    /// <param name="reader">The source of bytes for the data.</param>
    /// <param name="value">Out parameter for the result of the parsing operation</param>
    /// <param name="byteCount">Number of bytes to read</param>
    /// <returns>True if there are enough bytes to read.  False otherwise.</returns>
    public static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out ulong value, int byteCount)
    {
        value = 0;
        for (int i = 0; i < byteCount; i++)
        {
            value <<= 8;
            if (!reader.TryRead(out var oneByte))
            {
                return false;
            }

            value |= oneByte;
        }

        return true;
    }

    /// <summary>
    /// Try to read a big endian UInt8 value from the reader.
    /// </summary>
    /// <param name="reader">Source of data to parse from.</param>
    /// <returns>The number read from the reader.</returns>
    /// <exception cref="InvalidDataException">If there is not enough bytes in the reader.</exception>
    public static byte ReadBigEndianUInt8(this ref SequenceReader<byte> reader)
        => (byte)reader.ReadBigEndianUint(1);

    /// <summary>
    /// Try to read a big endian Int16 value from the reader.
    /// </summary>
    /// <param name="reader">Source of data to parse from.</param>
    /// <returns>The number read from the reader.</returns>
    /// <exception cref="InvalidDataException">If there is not enough bytes in the reader.</exception>
    public static short ReadBigEndianInt16(this ref SequenceReader<byte> reader)
        => (short)reader.ReadBigEndianUint(2);

    /// <summary>
    /// Try to read a big endian UInt16 value from the reader.
    /// </summary>
    /// <param name="reader">Source of data to parse from.</param>
    /// <returns>The number read from the reader.</returns>
    /// <exception cref="InvalidDataException">If there is not enough bytes in the reader.</exception>
    public static ushort ReadBigEndianUInt16(this ref SequenceReader<byte> reader)
        => (ushort)reader.ReadBigEndianUint(2);

    /// <summary>
    /// Try to read a big endian UInt32 value from the reader.
    /// </summary>
    /// <param name="reader">Source of data to parse from.</param>
    /// <returns>The number read from the reader.</returns>
    /// <exception cref="InvalidDataException">If there is not enough bytes in the reader.</exception>
    public static uint ReadBigEndianUInt32(this ref SequenceReader<byte> reader)
        => (uint)reader.ReadBigEndianUint(4);

    /// <summary>
    /// Try to read a big endian UInt64 value from the reader.
    /// </summary>
    /// <param name="reader">Source of data to parse from.</param>
    /// <returns>The number read from the reader.</returns>
    /// <exception cref="InvalidDataException">If there is not enough bytes in the reader.</exception>
    public static ulong ReadBigEndianUInt64(this ref SequenceReader<byte> reader)
        => (ulong)reader.ReadBigEndianUint(8);

    /// <summary>
    /// Try to read a big endian value from the reader.
    /// </summary>
    /// <param name="reader">Source of data to parse from.</param>
    /// <param name="bytes">number of bytes to read.</param>
    /// <returns>The number read from the reader.</returns>
    /// <exception cref="InvalidDataException">If there is not enough bytes in the reader.</exception>
    public static ulong ReadBigEndianUint(this ref SequenceReader<byte> reader, int bytes)
    {
        if (!reader.TryReadBigEndian(out var ret, bytes))
        {
            throw new InvalidDataException("Not enough input");
        }

        return ret;
    }

    /// <summary>
    /// Reads a float encoded using the fixed 15/16 bit format from the ICC spec.
    /// </summary>
    /// <param name="reader">Source to read from.</param>
    /// <returns>The parsed number.</returns>
    public static float Reads15Fixed16(this ref SequenceReader<byte> reader)
        => ((float)reader.ReadBigEndianInt16()) + (((float)reader.ReadBigEndianUInt16()) / ((1 << 16) - 1));

    /// <summary>
    /// Reads DateTime from the reader.
    /// </summary>
    /// <param name="reader">Source of data to parse from.</param>
    /// <returns>DateTime object.</returns>
    /// <exception cref="InvalidDataException">If there is not enough bytes in the reader.</exception>
    public static DateTime ReadDateTimeNumber(this ref SequenceReader<byte> reader)
        => new(
        reader.ReadBigEndianUInt16(), 
        reader.ReadBigEndianUInt16(), 
        reader.ReadBigEndianUInt16(), 
        reader.ReadBigEndianUInt16(), 
        reader.ReadBigEndianUInt16(), 
        reader.ReadBigEndianUInt16(), 
        0, DateTimeKind.Utc);
}
