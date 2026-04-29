using Content.Shared.Dataset;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Humanoid
{
    // You need to update profile, profile editor, maybe voices and names if you want to expand this further.
    public enum Virginity : byte
    {
        Yes,
        No
    }

    public record struct VirginityChangedEvent(Virginity OldErp, Virginity NewErp);
}
