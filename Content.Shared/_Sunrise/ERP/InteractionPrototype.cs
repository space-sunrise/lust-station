// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/lust-station/blob/master/CLA.txt
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Audio;
using Content.Shared.Humanoid;
namespace Content.Shared._Sunrise.ERP;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

[Prototype("interaction")]
public sealed partial class InteractionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = default!; //Обнять, Дать пять, что-либо ещё..

    [DataField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Texture(new("/Textures/_Sunrise/Interface/ERP/blankIcon.png")); //Иконка рядом с текстом

    [DataField("sounds")]
    public List<SoundSpecifier> Sounds = new();
    //public HashSet<SoundSpecifier> Sounds = new(); //Хлюп-хлюп...

    [DataField] public HashSet<string> Emotes = new();

    [DataField]
    public Sex UserSex = Sex.Unsexed; //Unsexed = любой

    [DataField]
    public Sex TargetSex = Sex.Unsexed;

    [DataField]
    public bool UserWithoutCloth = false; //Нужно ли, чтобы на энтити не было комбенизона / скафандра

    [DataField]
    public bool TargetWithoutCloth = false;

    [DataField] public bool Erp = false; // Это ЕРП-действие?

    [DataField] public int LovePercentUser = 0; // Сколько процентов добавлять к шкале "окончания"
    [DataField] public int LovePercentTarget = 0; // Сколько процентов добавлять к шкале "окончания"


    [DataField] public HashSet<string> UserTagWhitelist = new();
    [DataField] public HashSet<string> TargetTagWhitelist = new();
    [DataField] public HashSet<string> UserTagBlacklist = new();
    [DataField] public HashSet<string> TargetTagBlacklist = new();
    //[DataField("inhandObject", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    [DataField] public HashSet<string> InhandObject = new();
    [DataField] public bool UseSelf = false;
    [DataField] public string Category = "standart";
    [DataField] public int SortOrder = 0;
}
