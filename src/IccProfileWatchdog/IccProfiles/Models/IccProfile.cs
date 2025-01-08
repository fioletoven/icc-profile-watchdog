// Portions of this file are derived from Melville.PDF by John Melville Licensed under the MIT License.

namespace IccProfileWatchdog.IccProfiles.Models;

/// <summary>
/// Represents a parsed ICC profile.
/// </summary>
public class IccProfile
{
    /// <summary>
    /// Header structure that contains information about this profile.
    /// </summary>
    public IccHeader Header { get; }

    /// <summary>
    /// A list of tags that define the profile operations or provide additional information.
    /// </summary>
    public IReadOnlyList<IccProfileTag> Tags { get; }

    public IccProfile(IccHeader header, IReadOnlyList<IccProfileTag> tags)
    {
        Header = header;
        Tags = tags;
    }

    /// <summary>
    /// ICC Header data structure.
    /// </summary>
    public record struct IccHeader(
        uint Size,
        uint CmmType,
        uint Version,
        uint ProfileClass,
        uint DeviceColorSpace,
        uint ProfileConnectionColorSpace,
        DateTime CreatedDate,
        uint Signature,
        uint Platform,
        uint ProfileFlags,
        uint Manufacturer,
        uint Device,
        ulong DeviceAttributes,
        uint RenderIntent,
        float IlluminantX,
        float IlluminantY,
        float IlluminantZ,
        uint Creator,
        ulong ProfileIdHigh,
        ulong ProfileIdLow
    );
}
