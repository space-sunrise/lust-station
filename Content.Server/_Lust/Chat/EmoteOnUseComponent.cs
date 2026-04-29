namespace Content.Server._Lust.Chat;

[RegisterComponent]
public sealed partial class EmoteOnUseComponent : Component
{
    [DataField]
    public List<string> Values;
}
