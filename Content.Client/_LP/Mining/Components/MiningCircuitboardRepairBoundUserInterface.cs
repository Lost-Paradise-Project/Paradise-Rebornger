using System;
using Content.Shared._Wega.Mining.Components;
using Content.Shared._LP.Mining.Components;
using Content.Shared._Wega.Mining;
using Robust.Shared.Serialization;
using Robust.Shared.IoC;
using Robust.Client.UserInterface;

namespace Content.Client._LP.Mining.Components
{
    public sealed class MiningCircuitboardRepairBoundUserInterface : BoundUserInterface
    {
        private MiningCircuitboardRepairControl? _control;

        public MiningCircuitboardRepairBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _control = this.CreateWindow<MiningCircuitboardRepairControl>();
            _control.OnScan += OnScanPressed;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _control?.Dispose();
                _control = null;
            }
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is MiningCircuitboardRepairBoundInterfaceState repairState)
            {
                _control?.UpdateState(repairState);
            }
        }

        private void OnScanPressed()
        {
            SendMessage(new MiningCircuitboardRepairScanMessage());
        }
    }
}
