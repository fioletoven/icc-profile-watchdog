using IccProfileWatchdog.Displays;
using IccProfileWatchdog.IccProfiles.Models;
using IccProfileWatchdog.IccProfiles.Parser;
using System.IO.Pipelines;

namespace IccProfileWatchdog.IccProfiles;

/// <summary>
/// Helper class for ICC profiles.
/// </summary>
public static class IccProfileHelper
{
    /// <summary>
    /// Reads ICC profile from a file path.
    /// </summary>
    /// <param name="fileName">Full path to the ICC profile.</param>
    /// <returns>Parsed profile.</returns>
    public static async Task<IccProfile?> LoadIccProfileAsync(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        using var stream = new FileStream(fileName, FileMode.Open);
        var reader = PipeReader.Create(stream, new StreamPipeReaderOptions(bufferSize: 4096 * 2));
        var parser = new IccProfileParser(reader);

        return await parser.ParseAsync();
    }

    /// <summary>
    /// Returns VideoCardGammaTable gamma ramp from the ICC profile.
    /// </summary>
    /// <param name="profile">ICC profile.</param>
    /// <returns>GammaRamp or null if no 'vcgt' tag found.</returns>
    public static GammaRamp? GetVcgtGammaRampFromProfile(IccProfile? profile)
    {
        if (profile == null)
        {
            return null;
        }

        var vcgtTag = profile.Tags.FirstOrDefault(t => t.TagName == "vcgt");
        if (vcgtTag == null || vcgtTag.Data == null)
        {
            return null;
        }

        var vcgt = (VideoCardGammaTableTag)vcgtTag.Data;

        return new GammaRamp
        {
            Red = vcgt.Red,
            Green = vcgt.Green,
            Blue = vcgt.Blue
        };
    }
}
