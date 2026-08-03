using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid;

[Serializable, NetSerializable]
public enum BreastSize : byte
{
    AA,
    A,
    B,
    C,
    D,
    DD,
    E,
    F,
}

[Serializable, NetSerializable]
public enum ButtSize : byte
{
    Athletic,
    Standard,
    Large,
}
