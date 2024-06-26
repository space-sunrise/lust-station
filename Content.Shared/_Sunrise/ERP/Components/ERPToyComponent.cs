// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/lust-station/blob/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;
namespace Content.Shared._Sunrise.ERP.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ERPToyComponent : Component
{
    [DataField] public List<string> GenitalTagList; // Tags: anus, vagina, penis
    [DataField] public int SelectedGenital = 0;
    [DataField] public int LoveAdding = 15;
}

[Serializable, NetSerializable]
public sealed partial class ERPToyDoAfterEvent : SimpleDoAfterEvent
{
}
