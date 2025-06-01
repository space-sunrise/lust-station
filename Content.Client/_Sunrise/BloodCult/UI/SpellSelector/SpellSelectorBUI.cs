﻿using Content.Client._Sunrise.UserInterface.Radial;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.BloodCult.Items;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.BloodCult.UI.SpellSelector;

public sealed class SpellSelectorBUI : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private RadialContainer? _menu;

    private bool _selected;

    public SpellSelectorBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        _menu = new RadialContainer();
        _menu.Closed += () =>
        {
            if (_selected)
                return;

            Close();
        };

        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        foreach (var action in BloodCultistComponent.CultistActions)
        {
            if (!protoMan.TryIndex(action, out var proto))
                continue;

            if (!proto.Components.TryGetComponent("ActionComponent", out var actionComp))
                continue;

            if (actionComp is not ActionComponent actionComponent)
                continue;

            var icon = actionComponent.Icon;

            if (icon == null)
                continue;

            var texture = icon.Frame0();
            var button = _menu.AddButton(proto.Name, texture);

            button.Controller.OnPressed += _ =>
            {
                _selected = true;
                SendMessage(new CultSpellProviderSelectedBuiMessage(action));
                _menu.Close();
                Close();
            };
        }

        _menu.OpenAttached(Owner);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _menu?.Close();
    }
}
