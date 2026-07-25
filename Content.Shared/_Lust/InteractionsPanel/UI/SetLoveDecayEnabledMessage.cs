using Robust.Shared.Serialization;

// Lust edit - сетевое сообщение расширяет Sunrise-панель из папки форка.
#pragma warning disable IDE0130
namespace Content.Shared._Sunrise.InteractionsPanel.Data.UI;

[Serializable, NetSerializable]
public sealed class SetLoveDecayEnabledMessage(bool enabled) : BoundUserInterfaceMessage
{
    public bool Enabled { get; } = enabled;
}
