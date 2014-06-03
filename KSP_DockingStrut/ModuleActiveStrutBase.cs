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

namespace ActiveStruts
{
    public abstract class ModuleActiveStrutBase : PartModule
    {
        protected const float MaxDistance = 15;
        public const string TargeterModuleName = "ModuleActiveStrutTargeter";
        public const string TargetModuleName = "ModuleActiveStrutTarget";
        [KSPField(isPersistant = true)] public bool HasPartner = false;
        [KSPField(isPersistant = true, guiActive = false)] protected string Id = Guid.Empty.ToString();
        protected bool Initialized;
        protected ModuleActiveStrutBase Partner;

        [KSPField(guiActive = true)] public string State = "n.a.";

        private ASMode _mode = ASMode.Undefined;

        public Guid ID
        {
            get { return new Guid(this.Id); }
            set { this.Id = value.ToString(); }
        }

        protected abstract bool Linked { get; }

        public ASMode Mode
        {
            get
            {
                //ReSharper don't invert if
                if (this._mode == ASMode.Undefined)
                {
                    if (this is ModuleActiveStrutTargeter)
                    {
                        var moduleActiveStrutTargeter = this as ModuleActiveStrutTargeter;
                        this._mode = moduleActiveStrutTargeter != null && moduleActiveStrutTargeter.IsLinked ? ASMode.Linked : ASMode.Unlinked;
                    }
                    else
                    {
                        this._mode = ASMode.Unlinked;
                    }
                    this.State = this._mode.ToString();
                }
                return this._mode;
            }
            set
            {
                this._mode = value;
                if (value == ASMode.Linked && this is ModuleActiveStrutTargeter)
                {
                    var moduleActiveStrutTargeter = this as ModuleActiveStrutTargeter;
                    if (moduleActiveStrutTargeter != null)
                    {
                        moduleActiveStrutTargeter.IsLinked = true;
                    }
                }
                if (this is ModuleActiveStrutTargeter && this.HasPartner)
                {
                    var moduleActiveStrutTargeter = this as ModuleActiveStrutTargeter;
                    if (moduleActiveStrutTargeter != null)
                    {
                        moduleActiveStrutTargeter.StateManager.SetOwnMode(value);
                    }
                }
                this.State = value.ToString();
            }
        }

        public bool ConnectionInUse(bool rec = false)
        {
            if (!this.HasPartner || rec)
            {
                return this.Mode == ASMode.Linked;
            }
            return this.Partner.ConnectionInUse(true) || this.Mode == ASMode.Linked;
        }

        public abstract override void OnStart(StartState state);

        public override void OnUpdate()
        {
            if (!this.Initialized)
            {
                return;
            }
            this.UpdateGui();
            var moduleActiveStrutTargeter = this as ModuleActiveStrutTargeter;
            if (moduleActiveStrutTargeter != null)
            {
                moduleActiveStrutTargeter.UpdateLink();
            }
        }

        public void RevertGui()
        {
            this.Mode = this.Linked ? ASMode.Linked : ASMode.Unlinked;
            foreach (var e in this.Events)
            {
                e.active = e.guiActive = false;
            }
        }

        internal abstract void UpdateGui();
    }
}