using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Audio;
using Content.Shared.Humanoid;
namespace Content.Shared._Sunrise.ERP;

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

    [DataField] public int LovePercent = 0; // Сколько процентов добавлять к шкале "окончания"
}
