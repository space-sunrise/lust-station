using Content.Shared._Lust.Lactation;
using Robust.Shared.Console;

namespace Content.Server._Lust.Lactation;

/// <inheritdoc/>
public sealed class LactationSystem : SharedLactationSystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

    }
}
