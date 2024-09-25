using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Rest;

[RegisterComponent, NetworkedComponent]
public sealed partial class RestAbilityComponent : Component
{
    [DataField] public bool IsResting;

    [DataField] public TimeSpan Cooldown = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Бекап скорости передвижения, так как она сбрасывается до нуля при сидении
    /// </summary>
    public float PreviousWalkSpeed;

    /// <summary>
    /// Бекап скорости передвижения, так как она сбрасывается до нуля при сидении
    /// </summary>
    public float PreviousSprintSpeed;

    /// <summary>
    /// Список слоев, которые будут выключаться вместе с основным спрайтом. В строках.
    /// </summary>
    [DataField]
    public HashSet<string> DisableableStringLayers= new ();

    /// <summary>
    /// Список слоев, которые будут выключаться вместе с основным спрайтом. В енумах
    /// </summary>
    [DataField]
    public HashSet<Enum> DisableableEnumLayers= new ();
}

[Serializable, NetSerializable]
public enum RestVisuals : byte
{
    Base,
    Resting,
}

#region Events

public sealed partial class RestActionEvent : InstantActionEvent {}

[Serializable, NetSerializable]
public sealed partial class RestDoAfterEvent : SimpleDoAfterEvent {}

public sealed partial class RestChangeSpriteEvent : EntityEventArgs
{
    public EntityUid Entity;
}

public sealed partial class ActionLightToggledSunriseEvent : CancellableEntityEventArgs {}

#endregion
