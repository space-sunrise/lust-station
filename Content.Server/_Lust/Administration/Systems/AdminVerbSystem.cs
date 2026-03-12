

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

        var fSignName = Loc.GetString("admin-smite-fuck-sign-name").ToLowerInvariant();
        Verb fSign = new()
        {
            Text = fSignName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("_Lust/Objects/Misc/fucksign.rsi"), "icon"),
            Act = () =>
            {
                EnsureComp<KillSignComponent>(args.Target, out var comp);
                comp.Sprite = new SpriteSpecifier.Rsi(new("_Lust/Objects/Misc/fucksign.rsi"), "sign");
                comp.HideFromOwner = false; // We set it to false anyway, in case the hidden smite was used beforehand.
                Dirty(args.Target, comp);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", fSignName, Loc.GetString("admin-smite-fuck-sign-description"))
        };
        args.Verbs.Add(fSign);
    }
}
