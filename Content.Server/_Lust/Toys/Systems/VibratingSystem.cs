using Content.Server.Chat.Systems;
using Content.Server.Jittering;
using Content.Server._Lust.Toys.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared._Sunrise.ERP.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Lust.Toys.Systems;

public sealed class VibratingSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly JitteringSystem _jittering = default!;


    // Добавляет стоны и заполняет панель
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<VibratingComponent, InteractionComponent>();
        while (query.MoveNext(out var uid, out var comp, out var love))
        {
            if (!_mobStateSystem.IsAlive(uid))
                return;

            if (curTime < comp.NextMoanTime)
                return;

            if (curTime > love.LoveDelay)
            {
                love.ActualLove += (comp.AddedLove + _random.Next(-comp.AddedLove / 2, comp.AddedLove / 2)) / 100f;
                love.TimeFromLastErp = curTime;
            }
            comp.NextMoanTime = curTime + TimeSpan.FromSeconds(comp.MoanInterval);
            _chatSystem.TryEmoteWithChat(uid, "Moan", ignoreActionBlocker: true);
            _jittering.AddJitter(uid, comp.Amplitude, comp.Frequency);
            Dirty(uid, love);
        }
    }
}
