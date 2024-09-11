// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/lust-station/blob/master/CLA.txt
using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
namespace Content.Shared._Sunrise.ERP.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SexComponent : Component
{
    [DataField, AutoNetworkedField] public Sex Sex;
}
