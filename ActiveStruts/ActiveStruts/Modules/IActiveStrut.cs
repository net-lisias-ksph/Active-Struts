using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace ActiveStruts.Modules
{
	public interface IActiveStrut
	{
		void PlaceFreeAttach(Part targetPart, bool isStraightOut = false);
		void UpdateGui();

//		Transform StrutOrigin_();
		Transform Origin_();
		Transform transform_();
		Vector3 RealModelForward { get; }

		Part Part();

		void SetLink(ModuleActiveStrut_v3 target);
		void RemoveLink();

	}
}
