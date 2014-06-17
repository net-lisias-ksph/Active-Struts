using System;
using System.Linq;
using ActiveStruts.Addons;
using UnityEngine;

namespace ActiveStruts.Modules
{
    public class ModuleActiveStrutFreeAttachTarget : PartModule, IDResetable
    {
        [KSPField(isPersistant = true)] public string Id = Guid.Empty.ToString();
        [KSPField(isPersistant = true)] public bool IdResetDone = false;

        public Guid ID
        {
            get
            {
                var guid = new Guid(this.Id);
                if (guid != Guid.Empty)
                {
                    return guid;
                }
                guid = Guid.NewGuid();
                this.Id = guid.ToString();
                return guid;
            }
            set { this.Id = value.ToString(); }
        }

        public Transform PartOrigin
        {
            get { return this.part.transform; }
        }

        public Rigidbody PartRigidbody
        {
            get { return this.part.rigidbody; }
        }

        public void ResetId()
        {
            var oldId = this.Id;
            this.Id = Guid.NewGuid().ToString();
            foreach (var moduleActiveStrut in Util.Util.GetAllActiveStruts().Where(m => m.FreeAttachTargetId != null))
            {
                if (moduleActiveStrut.FreeAttachTargetId == oldId)
                {
                    moduleActiveStrut.FreeAttachTargetId = this.Id;
                }
            }
            this.IdResetDone = true;
        }

        public override void OnStart(StartState state)
        {
            if (this.Id == Guid.Empty.ToString())
            {
                this.Id = Guid.NewGuid().ToString();
            }
            if (HighLogic.LoadedSceneIsEditor)
            {
                this.part.OnEditorAttach += this._processEditorAttach;
            }
            if (HighLogic.LoadedSceneIsFlight && !this.IdResetDone)
            {
                ActiveStrutsAddon.Enqueue(this);
            }
        }

        private void _processEditorAttach()
        {
            var allTargets = Util.Util.GetAllFreeAttachTargets();
            if (allTargets == null)
            {
                return;
            }
            if (allTargets.Any(t => t.ID == this.ID))
            {
                this.ID = Guid.NewGuid();
            }
        }
    }
}