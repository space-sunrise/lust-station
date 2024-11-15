using Content.Server.Chat.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Timing;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Lust.Chat;

public sealed class EmoteOnUseSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmoteOnUseComponent, UseInHandEvent>(OnUseInHand);
    }

    public void OnUseInHand(EntityUid uid, EmoteOnUseComponent? component, UseInHandEvent args)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp<UseDelayComponent>(uid, out var useDelayComponent) || _useDelay.IsDelayed((uid, useDelayComponent)))
            return;

        var emote = Loc.GetString(_random.Pick(component.Values));
        _chat.TryEmoteWithChat(uid, emote);
        _useDelay.TryResetDelay((uid, useDelayComponent));
    }
}
