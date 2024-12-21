using Robust.Shared.Prototypes;
using Content.Shared.DeviceLinking;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Toys.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VibratingToyComponent : Component
{
    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    [DataField]
    public ProtoId<SinkPortPrototype> OffPort = "Off";

    [DataField("enabled"), AutoNetworkedField]
    public bool Enabled = false;

    public bool IsEquipped = false;

    [DataField("requiredSlot"), AutoNetworkedField]
    public SlotFlags RequiredSlot = SlotFlags.PLUG;

    [DataField, AutoNetworkedField]
    public float? BaseWalkSpeed;

    [DataField, AutoNetworkedField]
    public float? BaseSprintSpeed;

    [DataField, AutoNetworkedField]
    public float? BaseAcceleration;

    [DataField, AutoNetworkedField]
    public float TargetWalkSpeed = 2f;

    [DataField, AutoNetworkedField]
    public float TargetSprintSpeed = 3f;

    [DataField, AutoNetworkedField]
    public float TargetAcceleration = 20f;

    [DataField, AutoNetworkedField]
    public SoundSpecifier ActiveSound = new SoundPathSpecifier("/Audio/_Lust/Ambience/ERP/vibration.ogg");

    [DataField, AutoNetworkedField]
    [ViewVariables]
    public float Volume = 0.3f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float MaxDistance = 1f;

    [DataField, AutoNetworkedField]
    public EntityUid? PlayingStream;
}
