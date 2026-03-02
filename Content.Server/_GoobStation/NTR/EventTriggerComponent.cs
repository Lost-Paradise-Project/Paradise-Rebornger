using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._GoobStation.NTR
{
    [RegisterComponent]
    public sealed partial class EventTriggerComponent : Component
    {
        [DataField("eventId", required: true)]
        public string EventId = string.Empty;
    }
}
