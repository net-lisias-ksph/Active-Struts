using System;
using UnityEngine;

namespace DockingStrut
{
    public class ModuleDockingStrutTarget : ModuleDockingStrutBase
    {
        private const string ErrorNone = "none";
        [KSPField(isPersistant = true, guiActive = true)] public string Error = ErrorNone;
        [KSPField(isPersistant = true)] public bool IsDuo = false;
        [KSPField] public string StrutTargetName = "strut";
        [KSPField(isPersistant = true, guiActive = true)] public string TargeterId = Guid.Empty.ToString();
        private Guid _oldTargeter;
        private Transform _strut;

        protected override bool Linked
        {
            get { return this.Mode == DSMode.Linked && this.Targeter != null && this.Targeter.ID != Guid.Empty; }
        }

        public Vector3 StrutTarget
        {
            get { return this._strut.position; }
        }

        public ModuleDockingStrutTargeter Targeter { get; set; }

        public Guid TargeterID
        {
            get { return new Guid(this.TargeterId); }
            set { this.TargeterId = value.ToString(); }
        }

        public void BackupOldTargeter()
        {
            this._oldTargeter = this.TargeterID;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!this.IsDuo)
            {
                this.Strut.localScale = Vector3.zero;
            }
            if (state == StartState.Editor)
            {
                return;
            }
            this._strut = this.part.FindModelTransform(this.StrutTargetName);
            if (this.ID == Guid.Empty)
            {
                this.ID = Guid.NewGuid();
            }
            if (this.TargeterID != Guid.Empty && this.Mode == DSMode.Linked)
            {
                this.SetTargeterAtLoad();
            }
            else
            {
                this.Mode = DSMode.Unlinked;
            }
            this.Initialized = true;
            this.Events["Unlink"].guiName = "Unlink Targeter";
        }

        private void ResetTargeter()
        {
            this.TargeterID = this._oldTargeter;
        }

        [KSPEvent(name = "SetAsTarget", active = false, guiName = "Set as Target", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void SetAsTarget()
        {
            this.Targeter.SetTarget(this);
            this.Mode = DSMode.Linked;
            this.TargeterID = this.Targeter.ID;
            foreach (var moduleDockingStrut in DSUtil.GetAllDockingStrutModules(this.vessel))
            {
                if (moduleDockingStrut.part != this.part && moduleDockingStrut is ModuleDockingStrutTarget && moduleDockingStrut.Mode != DSMode.Linked)
                {
                    (moduleDockingStrut as ModuleDockingStrutTarget).ResetTargeter();
                }
                moduleDockingStrut.RevertGui();
                moduleDockingStrut.UpdateGui();
            }
            this.Events["SetAsTarget"].active = this.Events["SetAsTarget"].guiActive = false;
        }

        public void SetErrorMessage(string errMsg)
        {
            this.Mode = DSMode.Invalid;
            foreach (var e in this.Events)
            {
                e.active = e.guiActive = false;
            }
            this.Error = errMsg;
            this.Fields["Error"].guiActive = true;
        }

        public void SetTargetedBy(ModuleDockingStrutTargeter targeter)
        {
            this.Mode = DSMode.Target;
            this.Targeter = targeter;
            this.TargeterId = targeter.ID.ToString();
            this.part.SetHighlightColor(Color.green);
            this.part.SetHighlight(true);
        }

        private void SetTargeterAtLoad()
        {
            Debug.Log("setting targeter at load with ID " + this.TargeterID);
            var searchResult = this.vessel.GetDockingStrut(this.TargeterID);
            if (!searchResult.Item1 || !(searchResult.Item3 is ModuleDockingStrutTargeter))
            {
                foreach (var e in this.Events)
                {
                    e.active = e.guiActive = false;
                }
                this.Mode = DSMode.Unlinked;
                return;
            }
            this.Targeter = searchResult.Item3 as ModuleDockingStrutTargeter;
            Debug.Log("targeter with ID " + this.TargeterID + " set");
        }

        public override void UnlinkPartner(bool secondary = false)
        {
            if (secondary)
            {
                this.UnlinkSelf();
                return;
            }
            this.Targeter.UnlinkPartner(true);
        }

        protected override void UnlinkSelf()
        {
            this.Mode = DSMode.Unlinked;
            this.Events["Unlink"].active = this.Events["Unlink"].guiActive = false;
            this.Targeter = null;
            this.TargeterID = Guid.Empty;
            this.UpdateGui();
        }

        internal override void UpdateGui()
        {
            switch (this.Mode)
            {
                case DSMode.Linked:
                {
                    this.Events["Unlink"].active = this.Events["Unlink"].guiActive = true;
                }
                    break;
                case DSMode.Target:
                {
                    this.Events["SetAsTarget"].active = this.Events["SetAsTarget"].guiActive = true;
                    this.part.SetHighlightColor(Color.green);
                    this.part.SetHighlight(true);
                }
                    break;
            }
            if (this.Mode == DSMode.Invalid)
            {
                return;
            }
            if (this.Mode != DSMode.Target)
            {
                this.part.SetHighlight(false);
            }
            this.Error = ErrorNone;
            this.Fields["Error"].guiActive = false;
        }

        protected override void UpdateLink()
        {
            if (this.Mode != DSMode.Linked)
            {
                return;
            }
            if (this.Targeter.vessel != this.vessel)
            {
                this.Mode = DSMode.Unlinked;
                this.Events["Unlink"].guiActive = false;
            }
        }
    }
}