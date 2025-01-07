using System.Diagnostics;
using System.Text;

namespace IccProfileWatchdog.IccProfiles.Models;

/// <summary>
/// Represents a single tag in an ICC profile.
/// </summary>
[DebuggerDisplay("Profile Tag {TagName}")]
public class IccProfileTag
{
    /// <summary>
    /// Tag signature as listed in clause 10 of the ICC profile.
    /// </summary>
    public uint Tag { get; init; }

    /// <summary>
    /// Tag signature as a string.
    /// </summary>
    public string TagName => As4CC(Tag);

    /// <summary>
    /// Offset to the beginning of the tag data in the ICC profile stream.
    /// </summary>
    public uint Offset { get; init; }

    /// <summary>
    /// Length of the tag data in the ICC profile stream.
    /// </summary>
    public uint Size { get; init; }

    /// <summary>
    /// Parsed representation of the tag (if it is a recognized tag).
    /// </summary>
    public object? Data { get; set; }

    public IccProfileTag(uint tag, uint offset, uint size, object? data)
    {
        Tag = tag;
        Offset = offset;
        Size = size;
        Data = data;
    }

    private static string As4CC(uint source)
        => Encoding.UTF8.GetString(
        [
            (byte)(source >> 24),
            (byte)(source >> 16),
            (byte)(source >> 8),
            (byte)(source)
        ]);
}
