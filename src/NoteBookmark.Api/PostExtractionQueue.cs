using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NoteBookmark.Api;

public record ExtractionTask(string PostId, string Url);

public class PostExtractionQueue
{
    private readonly Channel<ExtractionTask> _queue;

    public PostExtractionQueue()
    {
        // Unbounded channel is simple and suitable for this task queue.
        _queue = Channel.CreateUnbounded<ExtractionTask>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public void QueueBackgroundWorkItem(ExtractionTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _queue.Writer.TryWrite(task);
    }

    public async ValueTask<ExtractionTask> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
