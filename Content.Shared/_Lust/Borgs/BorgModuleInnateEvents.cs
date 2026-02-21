using Content.Shared.Actions;

namespace Content.Shared._Sunrise.Silicons.Borgs;

/// <summary>
/// Ивент на активацию встроенного предмета
/// </summary>
public sealed partial class ModuleInnateUseItem : InstantActionEvent
{
    public readonly EntityUid Item;

    public ModuleInnateUseItem(EntityUid item)
        : this()
    {
        Item = item;
    }
}

/// <summary>
/// Ивент на активацию встроенного предмета с взаимодействием с целью
/// </summary>
public sealed partial class ModuleInnateInteractionItem : EntityTargetActionEvent
{
    public readonly EntityUid Item;

    public ModuleInnateInteractionItem(EntityUid item)
        : this()
    {
        Item = item;
    }
}
