
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.DirectionalEmote;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DirectionalEmoteComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool CanSendEmotes = true;

    [DataField, AutoNetworkedField]
    public bool CanReceiveEmotes = true;

    [DataField, AutoNetworkedField]
    public bool CanHideName = false;

    [AutoNetworkedField]
    public string LastEmote = string.Empty;

    public TimeSpan LastSendAt = TimeSpan.Zero;
    public TimeSpan Cooldown = TimeSpan.FromSeconds(2);
}

[Serializable, NetSerializable]
public sealed partial class DirectionalEmoteAttemptEvent : EntityEventArgs
{
    public NetEntity Target;
    public string Text;
    public bool HideName;

    public DirectionalEmoteAttemptEvent(NetEntity target, string text, bool hideName = false)
    {
        Target = target;
        Text = text;
        HideName = hideName;
    }
}
