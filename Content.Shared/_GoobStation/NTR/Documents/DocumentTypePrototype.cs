using Robust.Shared.Prototypes;

namespace Content.Shared._GoobStation.NTR.Documents
{
    [Prototype("documentType")]
    public sealed partial class DocumentTypePrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; set; } = default!;

        [DataField]
        public string StartingText { get; set; } = string.Empty;

        [DataField]
        public string Template { get; set; } = string.Empty;

        [DataField]
        public string[] TextKeys { get; set; } = Array.Empty<string>();

        [DataField]
        public int[] TextCounts { get; set; } = Array.Empty<int>();
    }
}
