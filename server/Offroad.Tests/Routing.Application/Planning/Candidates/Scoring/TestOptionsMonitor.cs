using Microsoft.Extensions.Options;

namespace Offroad.Tests.Routing.Application.Planning.Candidates.Scoring;

internal sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
{
    public T CurrentValue { get; }

    public TestOptionsMonitor(T value) => CurrentValue = value;

    public T Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
