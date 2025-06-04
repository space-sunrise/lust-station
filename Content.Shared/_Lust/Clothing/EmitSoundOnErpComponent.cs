using Robust.Shared.Audio;

namespace Content.Shared._Lust.Clothing;

[RegisterComponent]
public sealed partial class EmitSoundOnErpComponent : Component
{
    [DataField]
    public TimeSpan PrevSound = TimeSpan.Zero;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(0.1);

    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("FootstepJester");
}
