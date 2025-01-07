using IccProfileWatchdog.IccProfiles.Extensions;
using IccProfileWatchdog.IccProfiles.Models;
using IccProfileWatchdog.IccProfiles.Parser.ByteSource;
using System.Buffers;
using System.IO.Pipelines;
using static IccProfileWatchdog.IccProfiles.Models.IccProfile;

namespace IccProfileWatchdog.IccProfiles.Parser;

/// <summary>
/// Parses an ICC profile from a PipeReader.
/// </summary>
public readonly struct IccProfileParser
{
    private readonly IByteSource _source;

    /// <summary>
    /// Create a new parser from a ByteSource.
    /// </summary>
    /// <param name="source"></param>
    public IccProfileParser(PipeReader source)
    {
        _source = new ByteSource.ByteSource(source); 
    }

    /// <summary>
    /// Parse an ICC profile from the PipeReader passed in the constructor.
    /// </summary>
    /// <returns>ICC profile </returns>
    public async ValueTask<IccProfile> ParseAsync()
    {
        var profile = await ReadIccProfileDataAsync();
        foreach (var tag in profile.Tags)
        {
            await _source.AdvanceToLocalPositionAsync(tag.Offset);
            var buffer = await ReadAtLeastAsync((int)tag.Size);

            tag.Data = ReadTagData(buffer.Buffer.Slice(0, tag.Size));
        }

        return profile;
    }

    private async Task<IccProfile> ReadIccProfileDataAsync()
    {
        var readResult = await ReadAtLeastAsync(132);

        var header = ReadHeader(readResult.Buffer);
        var tags = new IccProfileTag[ReadTagCount(readResult.Buffer.Slice(128))];

        _source.AdvanceTo(readResult.Buffer.GetPosition(132));

        await ReadOffsetsAsync(tags);
        Array.Sort(tags, (x, y) => x.Offset.CompareTo(y.Offset));

        return new IccProfile(header, tags);
    }

    private async Task<ReadResult> ReadAtLeastAsync(int minSize)
    {
        var readResult = await _source.ReadAsync();

        while (readResult.Buffer.Length < minSize)
        {
            if (readResult.IsCompleted)
            {
                throw new InvalidDataException("Too short for an ICC profile.");
            }

            _source.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
            readResult = await _source.ReadAsync();
        }

        return readResult;
    }

    private static IccHeader ReadHeader(ReadOnlySequence<byte> buffer)
    {
        var reader = new SequenceReader<byte>(buffer);
        return new IccHeader(
            reader.ReadBigEndianUInt32(),
            reader.ReadBigEndianUInt32(),
            reader.ReadBigEndianUInt32(),
            reader.ReadBigEndianUInt32(),
            reader.ReadBigEndianUInt32(),
            reader.ReadBigEndianUInt32(),
            reader.ReadDateTimeNumber(),
            reader.ReadBigEndianUInt32(),
            reader.ReadBigEndianUInt32(),
            reader.ReadBigEndianUInt32(),
            reader.ReadBigEndianUInt32(),
            reader.ReadBigEndianUInt32(),
            reader.ReadBigEndianUInt64(),
            reader.ReadBigEndianUInt32(),
            reader.Reads15Fixed16(),
            reader.Reads15Fixed16(),
            reader.Reads15Fixed16(),
            reader.ReadBigEndianUInt32(),
            reader.ReadBigEndianUInt64(),
            reader.ReadBigEndianUInt64()
        );
    }

    private static uint ReadTagCount(ReadOnlySequence<byte> slice)
    {
        var reader = new SequenceReader<byte>(slice);
        return reader.ReadBigEndianUInt32();
    }

    private static void ReadOffsets(ReadOnlySequence<byte> buffer, IccProfileTag[] tags)
    {
        var reader = new SequenceReader<byte>(buffer);
        for (int i = 0; i < tags.Length; i++)
        {
            tags[i] = ParseSingleOffset(ref reader);
        }
    }

    private static IccProfileTag ParseSingleOffset(ref SequenceReader<byte> reader)
    {
        return new IccProfileTag(
            tag: reader.ReadBigEndianUInt32(),
            offset: reader.ReadBigEndianUInt32(),
            size: reader.ReadBigEndianUInt32(),
            null);
    }

    private async ValueTask ReadOffsetsAsync(IccProfileTag[] tags)
    {
        var result = await ReadAtLeastAsync(12 * tags.Length);
        ReadOffsets(result.Buffer, tags);
        _source.AdvanceTo(result.Buffer.GetPosition(12 * tags.Length));
    }

    private static VideoCardGammaTableTag? ReadTagData(ReadOnlySequence<byte> buffer)
    {
        var reader = new SequenceReader<byte>(buffer);
        var tagType = reader.ReadBigEndianUInt32();

        // we are only interested in 'vcgt' tag (0x76636774)
        if (tagType == 0x76636774)
        {
            return new VideoCardGammaTableTag(ref reader);
        }

        return null;
    }
}
