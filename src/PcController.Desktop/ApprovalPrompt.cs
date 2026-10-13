using PcController.Core;

namespace PcController.Desktop;

public sealed class ApprovalPrompt(PairingRequest request, string address, Func<DateTimeOffset>? clock = null)
{
    private readonly TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public PairingRequest Request { get; } = request;
    public string Address { get; } = address;
    public Task<bool> Decision => completion.Task;
    public bool Finished => completion.Task.IsCompleted;
    public void Decide(bool allow) => completion.TrySetResult(allow && Request.Expires > (clock?.Invoke() ?? DateTimeOffset.UtcNow));
    public void Expire() { if (Request.Expires <= (clock?.Invoke() ?? DateTimeOffset.UtcNow)) Decide(false); }
}
