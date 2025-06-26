using Content.Shared.Mind;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Audio;
using Content.Shared.Humanoid;
using Content.Shared.Whitelist;

namespace Content.Shared._PANEL;

[Serializable, NetSerializable]
public sealed record InteractionInfo(
    string Name,
    string Description,
    string Id,
    bool SelfUse,
    bool Erp,
    List<EntProtoId> InhandObject,
    HashSet<string> Emotes,
    SpriteSpecifier Icon,
    List<Sex> UserSex,
    List<Sex> TargetSex,
    List<SoundSpecifier> Sounds,
    HashSet<string> ProtoUserBlacklist,
    HashSet<string> ProtoUserWhitelist,
    HashSet<string> ProtoTargetBlacklist,
    HashSet<string> ProtoTargetWhitelist,
    HashSet<string> UserTags,
    HashSet<string> TargetTags,
    int UserPercent,
    int TargetPercent)
{
    public bool IsPinned { get; set; }
}

[NetSerializable, Serializable]
public sealed class InteractionInfoChangedEvent : EntityEventArgs
{
    public InteractionInfo? InteractionInfo;
}
