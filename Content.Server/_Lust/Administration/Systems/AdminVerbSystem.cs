

using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Administration.Components;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._Lust.Administration.Systems;

public sealed partial class AdminVerbSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(GetSmiteVerbs);
    }

    private void GetSmiteVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        // 1984.
        if (HasComp<MapComponent>(args.Target) || HasComp<MapGridComponent>(args.Target))
            return;

        AddSmiteSign(
            "admin-smite-fuck-sign-name",
            "admin-smite-fuck-sign-description",
            new SpriteSpecifier.Rsi(new("_Lust/Objects/Misc/fucksign.rsi"), "icon"),
            new SpriteSpecifier.Rsi(new("_Lust/Objects/Misc/fucksign.rsi"), "sign"),
            args
        );

        string[] defaultSmiteNames = ["bald", "cat", "dog", "furry", "nerd", "peak", "raider", "stinky"];
        foreach (var name in defaultSmiteNames)
        {
            AddSmiteSign(
                $"admin-smite-{name}-sign-name",
                $"admin-smite-{name}-sign-description",
                new SpriteSpecifier.Rsi(new($"/Textures/Objects/Misc/killsign.rsi"), name),
                new SpriteSpecifier.Rsi(new($"/Textures/Objects/Misc/killsign.rsi"), name),
                args
            );
        }
    }

    private void AddSmiteSign(string nameLoc, string descLoc, SpriteSpecifier icon, SpriteSpecifier sign, GetVerbsEvent<Verb> args)
    {
        var name = Loc.GetString(nameLoc).ToLowerInvariant();
        var description = Loc.GetString(descLoc);
        Verb signVerb = new()
        {
            Text = name,
            Category = VerbCategory.Smite,
            Icon = icon,
            Act = () =>
            {
                EnsureComp<KillSignComponent>(args.Target, out var comp);
                comp.Sprite = sign;
                comp.HideFromOwner = false; // We set it to false anyway, in case the hidden smite was used beforehand.
                Dirty(args.Target, comp);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", name, description)
        };
        args.Verbs.Add(signVerb);
    }
}
