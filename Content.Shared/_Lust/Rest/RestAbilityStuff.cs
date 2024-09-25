using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Rest;

/// <summary>
/// Компонент, дающий возможность сидеть или вставать.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RestAbilityComponent : Component
{
    /// <summary>
    /// Находимся ли мы в положении сидя?
    /// </summary>
    [DataField, AutoNetworkedField] public bool IsResting;

    /// <summary>
    /// КД дуафтера для начала сидения/вставания
    /// </summary>
    [DataField] public TimeSpan Cooldown = TimeSpan.FromSeconds(1);

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

[Serializable, NetSerializable]
public sealed partial class RestChangeSpriteEvent : EntityEventArgs
{
    public NetEntity Entity;
}

public sealed partial class ActionLightToggledSunriseEvent : CancellableEntityEventArgs {}

#endregion
