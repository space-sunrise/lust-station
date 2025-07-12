// using Robust.Shared.Audio.Systems;
// using Robust.Shared.Timing;

// namespace Content.Shared._Lust.Clothing;

// public sealed class EmitSoundOnErpSystem : EntitySystem
// {
//     [Dependency] private readonly IGameTiming _timing = default!;
//     [Dependency] private readonly SharedAudioSystem _audio = default!;

//     /// <inheritdoc/>
//     public override void Initialize()
//     {
//         SubscribeLocalEvent<_Lust.Clothing.EmitSoundOnErpComponent, ClothingErpOccuredEvent>(OnErp);
//     }

//     private void OnErp(EntityUid uid, _Lust.Clothing.EmitSoundOnErpComponent component, ClothingErpOccuredEvent args)
//     {
//         if (_timing.CurTime - component.PrevSound < component.Cooldown)
//             return;
//         component.PrevSound = _timing.CurTime;

//         _audio.PlayPvs(component.Sound, uid);
//     }
// }
