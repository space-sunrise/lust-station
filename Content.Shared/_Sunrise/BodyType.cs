using Content.Shared.Humanoid;
using Content.Shared.DisplacementMap;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise;

[Prototype]
public sealed partial class BodyTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = default!;

    [DataField(required: true)]
    public Dictionary<HumanoidVisualLayers, string> Sprites = new();

    [DataField]
    public List<string> SexRestrictions = new();

    // Lust added start - независимая кастомизация фигуры человека
    [DataField]
    public bool SupportsBodyCustomization;

    [DataField]
    public Dictionary<ButtSize, string> ButtSprites = new();

    [DataField]
    public DisplacementData? ShapeDisplacement;
    // Lust added end
}
