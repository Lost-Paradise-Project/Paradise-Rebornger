namespace Content.Shared._GoobStation.NTR;

[RegisterComponent]
public sealed partial class NtrBankAccountComponent : Component
{
    [DataField]
    public int Balance;

    [DataField]
    public int TotalEarned;
}
