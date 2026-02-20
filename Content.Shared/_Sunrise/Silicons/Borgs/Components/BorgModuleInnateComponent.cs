using Robust.Shared.Prototypes;
using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;

namespace Content.Server._Sunrise.Silicons.Borgs.Components;

/// <summary>
/// Компонент, позволяющий давать боргам действия (экшены) и компоненты через модуль
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BorgModuleInnateComponent : Component
{
    /// <summary>
    /// Предметы, которые активируются прямо в руке
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId?> UseItems = new();

    /// TODO: implement adding those
    /// <summary>
    /// Предметы, с помощью которых можно взаимодействовать с сущностями
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId?> InteractionItems = new();

    /// <summary>
    /// Компоненты, которые будут добавлены боргу при установке модуля
    /// Будут удалены после его изъятия!
    /// </summary>
    [DataField]
    public ComponentRegistry InnateComponents = new();

    [DataField, AutoNetworkedField]
    public List<EntityUid> Actions = new();
}
