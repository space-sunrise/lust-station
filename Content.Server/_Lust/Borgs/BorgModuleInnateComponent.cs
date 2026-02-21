using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Silicons.Borgs;

/// <summary>
/// Компонент, позволяющий давать боргам действия (экшены) и компоненты через модуль
/// </summary>
[RegisterComponent]
public sealed partial class BorgModuleInnateComponent : Component
{
    /// <summary>
    /// Предметы, которые активируются прямо в руке
    /// </summary>
    [ViewVariables]
    public List<EntProtoId?> UseItems = new();

    /// <summary>
    /// Предметы, с помощью которых можно взаимодействовать с сущностями
    /// </summary>
    [ViewVariables]
    public List<EntProtoId?> InteractionItems = new();

    /// <summary>
    /// Компоненты, которые будут добавлены боргу при установке модуля
    /// Будут удалены после его изъятия!
    /// </summary>
    [ViewVariables]
    public ComponentRegistry InnateComponents = new();

    /// <summary>
    /// Айди добавленных предметов этим модулем
    /// Данный список нужен сугубо для корректной очистки
    /// </summary>
    [ViewVariables]
    public List<EntityUid> InnateItems = new();


    /// <summary>
    /// Экшены для борга, созданные данным модулем
    /// Данный список нужен сугубо для корректной очистки
    /// </summary>
    [ViewVariables]
    public List<EntityUid> Actions = new();
}
