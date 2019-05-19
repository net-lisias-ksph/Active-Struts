using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using ActiveStruts.Util;


namespace ActiveStruts.Modules
{
	public interface IActiveStrut
	{
		void UpdateGui();

//		Transform StrutOrigin_();
		Transform Origin_();
		Transform transform_();
		Vector3 RealModelForward { get; }

		Part Part();

		void SetLink(ModuleIRActiveStrutTarget_v3 target);
		void SetFreeLink(RaycastResult raycast);
		void RemoveLink();

	}
}
