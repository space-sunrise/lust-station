using Robust.Shared.Enums;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared._Sunrise;

public sealed partial class BodyTypePrototype
{
    private static Sex NormalizeLustSex(Sex sex)
    {
        return sex == Sex.Futanari ? Sex.Female : sex;
    }
}
