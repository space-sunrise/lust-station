using Robust.Shared.GameStates;
using Content.Shared.Inventory;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Lust.Toys.Components;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class VibratingComponent : Component
{
    public List<string> Emotes = new()
    {
        "Moan"
    };

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("moanInterval")]
    public float MoanInterval = 17;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("addedLove")]
    public int AddedLove = 8;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("amplitude")]
    public float Amplitude = 0.5f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("frequency")]
    public float Frequency = 0.2f;

    [DataField("nextMoanTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextMoanTime = TimeSpan.FromSeconds(0);

    [DataField("toy")]
    public EntityUid Toy;
}
