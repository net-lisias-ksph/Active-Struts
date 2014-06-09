using System;
using System.Linq;
using UnityEngine;

namespace ActiveStruts.Modules
{
    public class ModuleActiveStrutFreeAttachTarget : PartModule
    {
        [KSPField(isPersistant = true)] public string Id = Guid.Empty.ToString();

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