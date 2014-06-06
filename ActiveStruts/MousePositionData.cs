using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ActiveStruts
{
    public class MousePositionData
    {
        public float AngleFromOriginExact { get; set; }
        public float AngleFromOriginPart { get; set; }
        public float DistanceFromReferenceOriginExact { get; set; }
        public float DistanceFromReferenceOriginPart { get; set; }
        public Vector3 ExactHitPosition { get; set; }
        public RaycastHit Hit { get; set; }
        public bool HitCurrentVessel { get; set; }
        public Part HittedPart { get; set; }
        public bool OriginValid { get; set; }
        public Vector3 PartHitPosition { get; set; }
        public Ray Ray { get; set; }
        public float RayDistance { get; set; }
        public Vector3 ReferenceOrigin { get; set; }
    }
}
