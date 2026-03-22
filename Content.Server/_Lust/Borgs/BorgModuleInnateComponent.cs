using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server._Lust.Silicons.Borgs;

/// <summary>
/// Компонент, позволяющий давать боргам действия (экшены) и компоненты через модуль
/// </summary>
[RegisterComponent]
public sealed partial class BorgModuleInnateComponent : Component
{
    /// <summary>
    /// Предметы, которые активируются прямо в руке
    /// </summary>
    [DataField]
    public List<EntProtoId?> UseItems = new();

    /// <summary>
    /// Предметы, с помощью которых можно взаимодействовать с сущностями
    /// </summary>
    [DataField]
    public List<EntProtoId?> InteractionItems = new();

    /// <summary>
    /// Компоненты, которые будут добавлены боргу при установке модуля
    /// Будут удалены после его изъятия!
    /// </summary>
    [DataField]
    public ComponentRegistry InnateComponents = new();

    /// <summary>
    /// Айди добавленных предметов этим модулем
    /// Данный список нужен сугубо для корректной очистки
    /// </summary>
    [ViewVariables, Access(typeof(BorgModuleInnateSystem))]
    public List<EntityUid> InnateItems = new();

    /// <summary>
    /// Экшены для борга, созданные данным модулем
    /// Данный список нужен сугубо для корректной очистки
    /// </summary>
    [ViewVariables, Access(typeof(BorgModuleInnateSystem))]
    public List<EntityUid> Actions = new();
}
