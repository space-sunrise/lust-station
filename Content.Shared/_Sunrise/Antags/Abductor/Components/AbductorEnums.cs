﻿using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Antags.Abductor;

[Serializable, NetSerializable]
public enum AbductorExperimentatorVisuals : byte
{
    Full
}
[Serializable, NetSerializable]
public enum AbductorOrganType : byte
{
    None,
    Health,
    Gravity,
    Egg,
    Spider,
    Vent,
    Pacified,
    Ephedrine,
    Liar,
    Owo,
    EMP
}
[Serializable, NetSerializable]
public enum AbductorArmorModeType : byte
{
    Combat,
    Stealth
}
[Serializable, NetSerializable]
public enum AbductorCameraConsoleUIKey
{
    Key
}

[Serializable, NetSerializable]
public enum AbductorConsoleUIKey
{
    Key
}
