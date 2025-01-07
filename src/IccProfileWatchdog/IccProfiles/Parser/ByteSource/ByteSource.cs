using System.Diagnostics;
using System.IO.Pipelines;

namespace IccProfileWatchdog.IccProfiles.Parser.ByteSource;

/// <summary>
/// Implementation of IByteSource that reads from a pipe.
/// </summary>
public class ByteSource : IByteSource
{
    private readonly PipeReader _inner;
    private ReadResult? _currentBuffer;

    /// <summary>
    /// Position within the source stream.
    /// </summary>
    public long Position { get; private set; }

    /// <summary>
    /// Creates a ByteSource from a pipereader.
    /// </summary>
    /// <param name="inner">The pipereader to get data from.</param>
    public ByteSource(PipeReader inner)
    {
        _inner = inner;
    }

    /// <inheritdoc />
    public bool TryRead(out ReadResult result)
    {
        if (_inner.TryRead(out result))
        {
            _currentBuffer = result;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        var result = await _inner.ReadAsync(cancellationToken);
        _currentBuffer = result;

        return result;
    }

    /// <summary>
    /// Mark the entire buffered sequence as examined.
    /// </summary>
    public void MarkSequenceAsExamined()
    {
        Debug.Assert(_currentBuffer is not null);
        if (_currentBuffer.HasValue)
        {
            AdvanceTo(_currentBuffer.Value.Buffer.Start, _currentBuffer.Value.Buffer.End);
        }
    }

    /// <summary>
    /// Consume bytes to a given position.
    /// </summary>
    /// <param name="consumed">Position of the next byte to be read.</param>
    public void AdvanceTo(SequencePosition consumed)
    {
        IncrementPosition(consumed);
        _inner.AdvanceTo(consumed);
    }

    /// <summary>
    /// Consume bytes and mark other bytes as examined.
    /// </summary>
    /// <param name="consumed">The position of the next byte to consume.</param>
    /// <param name="examined">The position of the first unexamined byte.</param>
    public void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
        IncrementPosition(consumed);
        _inner.AdvanceTo(consumed, examined);
    }

    private void IncrementPosition(SequencePosition consumed)
    {
        if (!_currentBuffer.HasValue)
        {
            throw new InvalidOperationException("No buffer to advance within");
        }

        Position += _currentBuffer.Value.Buffer.Slice(0, consumed).Length;
    }
}
