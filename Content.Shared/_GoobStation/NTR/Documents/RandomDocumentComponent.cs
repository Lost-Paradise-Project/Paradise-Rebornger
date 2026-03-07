using Robust.Shared.Prototypes;

namespace Content.Shared._GoobStation.NTR.Documents
{
    [RegisterComponent]
    public sealed partial class RandomDocumentComponent : Component
    {
        [DataField(required: true)]
        public ProtoId<DocumentTypePrototype> DocumentType = default!;

        [DataField]
        public List<ProtoId<NtrTaskPrototype>> Tasks = new();
    }
}
