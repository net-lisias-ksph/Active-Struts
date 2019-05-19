using System;

namespace ActiveStruts.Modules
{
	/*
	 * this class identifies the possible targets to connect to
	 */

	public class ModuleIRActiveStrutTarget_v3 : PartModule
	{
		public ModuleIRActiveStrut_v3 connectedPart = null;

		public bool IsValidTarget
		{ get { return !connectedPart; } }
	}
}
