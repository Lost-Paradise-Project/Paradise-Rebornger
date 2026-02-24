using Content.Shared.Fax;
using Content.Shared.Fax.Components;

namespace Content.Shared._GoobStation.Fax;

/// <summary>
/// Raised on an entity when it's getting sent with a fax.
/// Set Handled to true to cancel normal fax behavior.
/// </summary>
[ByRefEvent]
public record struct GettingFaxedSentEvent(ref readonly Entity<FaxMachineComponent> Fax, ref readonly FaxSendMessage Args, bool Handled = false);
