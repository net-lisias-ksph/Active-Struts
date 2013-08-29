/* Name: Docking Strut
   Version: 1.0.1.0
   Author: JDP

   This code is free to use and modify as long as the original author is credited.
   
   Changelog:
   1.0.0.1
   - Implemented more realistic strut physics. now each end of the strut is linked to the strut anchors.
   - Fixed an exploit where relinking didn't check for valid links.
   - Added a whole bunch of extra actionGroups; unling, relink & toggle link.
   - Added a suite of cfg-configurable variables to make it a lot easier for partmodelers to create their own docking struts.

   1.0.1.0
   - Updated plugin and part to KSP 21.1 (finally).
   - Reworked joint linkage to get rid of phantom forces.
   - Added new ID system.
   - Reworked the way the visual strut is rescaled.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DockingStrut {
    public class ModuleDockingStrut : PartModule {
        float StrutX, StrutY;

        [KSPField]
        int ticksToCheckForLinkAtStart = 100;

        [KSPField]
        public float MaxDistance = 10;

        [KSPField(isPersistant = true, guiActive = true)]
        public string TargetIDs = Guid.Empty.ToString(), IDs = Guid.Empty.ToString();

        public Guid TargetID {
            get {
                return new Guid(TargetIDs);
            }
            set {
                TargetIDs = value.ToString();
            }

        }

        public Guid ID {
            get {
                return new Guid(IDs);
            }
            set {
                IDs = value.ToString();
            }
        }
        [KSPField(isPersistant = true)]
        public bool Targeted = false;

        [KSPField]
        public string
        strutName = "strut",
        rayCastOriginName = "strut",
        strutTargetName = "strut";

        public DSMode mode;

        public ModuleDockingStrut TargetDS;
        public ModuleDockingStrut TargeterDS;

        public ConfigurableJoint joint;

        private Transform mRCO, mST, mStrut;


        public Vector3 rayCastOrigin {
            get {
                return mRCO.position;
                }
            }
        

        public Vector3 strutTarget {
            get {
                return mST.position;
            }
        }

        [KSPAction("UnlinkActon", KSPActionGroup.None, guiName = "Unlink")]
        public void UnlinkActon(KSPActionParam param) {
            if (mode == DSMode.LINKED)
                Unlink();
        }

        [KSPAction("RelinkActon", KSPActionGroup.None, guiName = "Relink")]
        public void RelinkActon(KSPActionParam param) {
            if (mode != DSMode.LINKED && TargetDS != null && TargetDS.vessel == this.vessel)
                SetTarget(TargetDS);
        }

        [KSPAction("ToggleActon", KSPActionGroup.None, guiName = "Toggle link")]
        public void ToggleActon(KSPActionParam param) {
            if (mode == DSMode.LINKED)
                Unlink();
            else if (TargetDS != null && TargetDS.vessel == this.vessel)
                SetTarget(TargetDS);
        }


        [KSPEvent(name = "ErrorMessage", active = false, guiName = "", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void ErrorMessage() { }

        [KSPEvent(name = "Link", active = false, guiName = "Link", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void Link() {
            foreach (Part p in vessel.parts) {
                if (p != part && p.Modules.Contains("ModuleDockingStrut"))
                    DSUtil.setPossibleTarget(this, (p.Modules["ModuleDockingStrut"] as ModuleDockingStrut));
            }

            mode = DSMode.TARGETING;

            Events["Link"].active = Events["Link"].guiActive = false;
        }

        [KSPEvent(name = "Unlink", active = false, guiName = "Unlink", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void Unlink() {
            Targeted = false;
            mode = DSMode.UNLINKED;

            Events["Unlink"].active = Events["Unlink"].guiActive = false;
        }

        [KSPEvent(name = "Abort", active = false, guiName = "Abort link", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void Abort() {
            Targeted = false;
            mode = DSMode.UNLINKED;

            foreach (Part p in vessel.parts) {
                if (p.Modules.Contains("ModuleDockingStrut"))
                    (p.Modules["ModuleDockingStrut"] as ModuleDockingStrut).revertGUI();
            }

            Events["Abort"].active = Events["Abort"].guiActive = false;
        }

        [KSPEvent(name = "SetAsTarget", active = false, guiName = "Set as target", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void SetAsTarget() {
            TargeterDS.SetTarget(this);
            foreach (Part p in vessel.parts) {
                if (p.Modules.Contains("ModuleDockingStrut"))
                    (p.Modules["ModuleDockingStrut"] as ModuleDockingStrut).revertGUI();
            }

            Events["SetAsTarget"].active = Events["SetAsTarget"].guiActive = false;
        }

        public void SetErrorMessage(String s) {
            mode = DSMode.INVALID;
            foreach (BaseEvent e in Events) {
                if (e.name.Equals("ErrorMessage")) {
                    e.active = e.guiActive = true;
                    e.guiName = s;
                } else
                    e.active = e.guiActive = false;
            }
        }

        public void revertGUI() {
            if (Targeted)
                mode = DSMode.LINKED;
            else
                mode = DSMode.UNLINKED;

            foreach (BaseEvent e in Events)
                e.active = e.guiActive = false;
        }

        void UpdateGUI() {
            switch (mode) {
                case DSMode.LINKED:
                    Events["Unlink"].active = Events["Unlink"].guiActive = true;
                    break;
                case DSMode.TARGET:
                    Events["SetAsTarget"].active = Events["SetAsTarget"].guiActive = true;
                    break;
                case DSMode.TARGETING:
                    Events["Abort"].active = Events["Abort"].guiActive = true;
                    break;
                case DSMode.UNLINKED:
                    Events["Link"].active = Events["Link"].guiActive = true;
                    break;
            }
        }

        bool checkForReDocking = false, jointCreated = false;
        void updateLink() {
            try {
                if (Targeted && TargetDS != null) {
                    if (TargetDS.vessel != vessel) {
                        Unlink();
                        checkForReDocking = true;
                    } else {
                        TargetID = TargetDS.ID;
                    }
                }

                if (checkForReDocking) {
                    if (Targeted || TargetDS == null)
                        checkForReDocking = false;
                    else if (TargetDS.vessel == vessel) {
                        SetTarget(TargetDS);
                        checkForReDocking = false;
                    }
                }

                if (jointCreated != Targeted && !part.rigidbody.isKinematic && TargetDS != null && !TargetDS.part.rigidbody.isKinematic) {
                    if (Targeted) {
                        joint = part.rigidbody.gameObject.AddComponent<ConfigurableJoint>();
                        joint.connectedBody = TargetDS.part.rigidbody;
                        joint.breakForce = joint.breakTorque = float.PositiveInfinity;
                        joint.xMotion = ConfigurableJointMotion.Locked;
                        joint.yMotion = ConfigurableJointMotion.Locked;
                        joint.zMotion = ConfigurableJointMotion.Locked;
                        joint.angularXMotion = ConfigurableJointMotion.Locked;
                        joint.angularYMotion = ConfigurableJointMotion.Locked;
                        joint.angularZMotion = ConfigurableJointMotion.Locked;
                    } else {
                        Destroy(joint);
                        joint = null;
                        mStrut.localScale = Vector3.zero;
                    }

                    jointCreated = Targeted;
                }

            } catch { }
        }

        bool CLAS = false;
        public void SetTargetDSAtLoad() {

            if (vessel.GetDS(TargetID, out TargetDS)){
                CLAS = true;
            } else {
                foreach (BaseEvent e in Events)
                    e.active = e.guiActive = false;

                mode = DSMode.UNLINKED;
                Targeted = false;
            }
        }

        public void SetTarget(ModuleDockingStrut PosTarget) {
            if (!DSUtil.checkPossibleTarget(this, PosTarget)) {
                mode = DSMode.UNLINKED;
                Targeted = false;
                return;
            }

            foreach (BaseEvent e in Events)
                e.active = e.guiActive = false;

            TargetDS = PosTarget;
            TargetID = TargetDS.ID;
            Targeted = true;
            mode = DSMode.LINKED;

            SetStrutEnd(TargetDS.strutTarget);
        }

        void SetStrutEnd(Vector3 position) {
            mStrut.LookAt(position);
            mStrut.localScale = new Vector3(StrutX, StrutY, 1);    
            mStrut.localScale = new Vector3(StrutX, StrutY, Vector3.Distance(Vector3.zero, mStrut.InverseTransformPoint(position)));
        }

        bool started = false;
        public override void OnStart(PartModule.StartState state) {
            mStrut = part.FindModelTransform(strutName);
            
            StrutX = mStrut.localScale.x;
            StrutY = mStrut.localScale.y;
            mStrut.localScale = Vector3.zero;

            if (state == StartState.Editor) return;

            mRCO = part.FindModelTransform(rayCastOriginName);
            mST = part.FindModelTransform(strutTargetName);            

            if (ID == Guid.Empty)
                ID = Guid.NewGuid();

            if (Targeted)
                SetTargetDSAtLoad();
            else
                mode = DSMode.UNLINKED;
            started = true;
        }

        public override void OnUpdate() {
            if (!started) return;
            UpdateGUI();
            updateLink();
            if (CLAS) {
                if (DSUtil.checkPossibleTarget(this, TargetDS)) {
                    CLAS = false;
                    SetTarget(TargetDS);
                } else
                    if (ticksToCheckForLinkAtStart-- < 0)
                        CLAS = false;
            }
        }
    }
}
