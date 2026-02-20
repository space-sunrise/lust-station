using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Silicons.Borgs;

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
