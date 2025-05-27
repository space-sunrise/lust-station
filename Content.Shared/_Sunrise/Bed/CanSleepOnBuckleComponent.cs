namespace Content.Shared._Sunrise.Bed
{
    [RegisterComponent]
    public sealed partial class CanSleepOnBuckleComponent : Component
    {
        public Dictionary<EntityUid, EntityUid> SleepAction = [];
    }
}
