using System;
using UnityEngine;

namespace ActiveStruts
{
    public class ModuleActiveStrutTarget : ModuleActiveStrutBase
    {
        private const string ErrorNone = "none";
        [KSPField(isPersistant = true, guiActive = true)] public string Error = ErrorNone;
        private ModuleActiveStrutTargeter _targeter;

        protected override bool Linked
        {
            get { return this.Mode == ASMode.Linked; }
        }

        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor)
            {
                return;
            }
            if (this.HasPartner)
            {
                this.Partner = this.part.Modules[TargeterModuleName] as ModuleActiveStrutBase;
                this.Fields["State"].guiActive = false;
            }
            if (this.ID == Guid.Empty)
            {
                this.ID = Guid.NewGuid();
            }
            this.Mode = ASMode.Unlinked;
            this.Initialized = true;
        }

        private void ResetTargeter()
        {
            this._targeter = null;
            this.Mode = this.Mode == ASMode.Linked ? ASMode.Linked : ASMode.Unlinked;
        }

        [KSPEvent(name = "SetAsTarget", active = false, guiName = "Set as Target", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void SetAsTarget()
        {
            ConnectorManager.Deactivate();
            this._targeter.SetTarget(this);
            this.Mode = ASMode.Linked;
            foreach (var moduleDockingStrut in ASUtil.GetAllDockingStrutModules(this.vessel))
            {
                if (moduleDockingStrut is ModuleActiveStrutTarget)
                {
                    (moduleDockingStrut as ModuleActiveStrutTarget).ResetTargeter();
                }
                moduleDockingStrut.RevertGui();
                moduleDockingStrut.UpdateGui();
            }
            this.Events["SetAsTarget"].active = this.Events["SetAsTarget"].guiActive = false;
        }

        public void SetErrorMessage(string errMsg)
        {
            this.Mode = ASMode.Invalid;
            foreach (var e in this.Events)
            {
                e.active = e.guiActive = false;
            }
            this.Error = errMsg;
            this.Fields["Error"].guiActive = true;
        }

        public void SetTargetedBy(ModuleActiveStrutTargeter targeter)
        {
            this.Mode = ASMode.Target;
            this._targeter = targeter;
            this.part.SetHighlightColor(Color.green);
            this.part.SetHighlight(true);
        }

        internal override void UpdateGui()
        {
            switch (this.Mode)
            {
                case ASMode.Target:
                {
                    this.Events["SetAsTarget"].active = this.Events["SetAsTarget"].guiActive = true;
                    this.part.SetHighlightColor(Color.green);
                    this.part.SetHighlight(true);
                }
                    break;
            }
            if (this.Mode == ASMode.Invalid)
            {
                return;
            }
            if (this.Mode != ASMode.Target)
            {
                this.part.SetHighlight(false);
            }
            this.Error = ErrorNone;
            this.Fields["Error"].guiActive = false;
            if (this.HasPartner)
            {
                var moduleActiveStrutTargeter = this.Partner as ModuleActiveStrutTargeter;
                if (moduleActiveStrutTargeter != null)
                {
                    moduleActiveStrutTargeter.ShareState(this.Mode);
                }
                return;
            }
            this.State = this.Mode.ToString();
        }
    }
}