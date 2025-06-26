using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.Audio;
using Content.Shared.Humanoid;
using Content.Shared.Whitelist;


namespace Content.Shared._PANEL.Prototypes;

[Prototype("interaction")]
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class InteractionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name;

    [DataField(required: true)]
    public string Description;

    /// <summary>
    /// Item to be held by the player.
    /// </summary>
    [DataField]
    public List<EntProtoId> InhandObject = new();

    /// <summary>
    /// The sound that will be appear when the interaction is played
    /// </summary>
    [DataField]
    public List<SoundSpecifier> Sounds = new();

    /// <summary>
    /// HashSet of emotes: Kick, hug and other.
    /// </summary>
    [DataField]
    public HashSet<string> Emotes = new();

    /// <summary>
    /// Can the action be used on user panel?
    /// </summary>
    [DataField]
    public bool SelfUse = false;

    /// <summary>
    /// Icon next to interaction.
    /// </summary>
    [DataField]
    public SpriteSpecifier Icon = SpriteSpecifier.Invalid;

    /// <summary>
    /// The category of interaction.
    /// </summary>
    [DataField]
    public string Category = "standart";

    /// <summary>
    /// List because we can add new sex
    /// </summary>
    [DataField]
    public List<Sex> UserSex = [];

    [DataField]
    public List<Sex> TargetSex = [];

    [DataField] 
    public HashSet<string> TargetTagWhitelist = new();
    [DataField] 
    public HashSet<string> TargetTagBlacklist = new();

    [DataField] 
    public HashSet<string> UserTagWhitelist = new();

    [DataField] 
    public HashSet<string> UserTagBlacklist = new();

    [DataField] 
    public int UserPercent = 0;

    [DataField] 
    public int TargetPercent = 0;


    /// <summary>
    /// If this is ERP interaction?
    /// </summary>
    [DataField]
    public bool Erp = true;

}
