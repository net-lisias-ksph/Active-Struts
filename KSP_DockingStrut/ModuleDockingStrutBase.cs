/*
The MIT License (MIT)
Copyright (c) 2014 marce

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using UnityEngine;

namespace ActiveStruts
{
    public abstract class ModuleDockingStrutBase : PartModule
    {
        protected const float MaxDistance = 15;
        public const string TargeterModuleName = "ModuleDockingStrutTargeter";
        public const string TargetModuleName = "ModuleDockingStrutTarget";
        [KSPField(isPersistant = true, guiActive = true)] protected string Id = Guid.Empty.ToString();
        protected bool Initialized;
        [KSPField(isPersistant = true)] protected bool IsLinked = false;
        [KSPField(guiActive = true)] public string State = "n.a.";

        protected Transform Strut;
        [KSPField] public string StrutName = "strut";
        protected float StrutX, StrutY;
        private DSMode _mode = DSMode.Undefined;

        public Guid ID
        {
            get { return new Guid(this.Id); }
            set { this.Id = value.ToString(); }
        }

        protected abstract bool Linked { get; }

        public DSMode Mode
        {
            get
            {
                //ReSharper don't invert if
                if (this._mode == DSMode.Undefined)
                {
                    this._mode = this.IsLinked ? DSMode.Linked : DSMode.Unlinked;
                    this.State = this._mode.ToString();
                }
                return this._mode;
            }
            set
            {
                this._mode = value;
                if (value == DSMode.Linked)
                {
                    this.IsLinked = true;
                }
                this.State = value.ToString();
            }
        }

        public override void OnStart(StartState state)
        {
            this.Strut = this.part.FindModelTransform(this.StrutName);
            this.StrutX = this.Strut.localScale.x;
            this.StrutY = this.Strut.localScale.y;
        }

        public override void OnUpdate()
        {
            if (!this.Initialized)
            {
                return;
            }
            this.UpdateGui();
            this.UpdateLink();
        }

        public void RevertGui()
        {
            this.Mode = this.Linked ? DSMode.Linked : DSMode.Unlinked;
            foreach (var e in this.Events)
            {
                e.active = e.guiActive = false;
            }
        }

        [KSPEvent(name = "Unlink", active = false, guiName = "Unlink", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void Unlink()
        {
            this.UnlinkPartner();
            this.UnlinkSelf();
        }

        [KSPAction("UnlinkAction", KSPActionGroup.None, guiName = "Unlink")]
        public void UnlinkAction(KSPActionParam param)
        {
            if (this.Mode == DSMode.Linked)
            {
                this.Unlink();
            }
        }

        public abstract void UnlinkPartner(bool secondary = false);
        protected abstract void UnlinkSelf();
        internal abstract void UpdateGui();
        protected abstract void UpdateLink();
    }
}