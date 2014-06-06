using KSP.IO;

namespace ActiveStruts.Util
{
    public class Config
    {
        public double ColorTransparency = 0.5f;
        public double ConnectorDimension = 0.5f;
        public double FreeAttachDistanceTolerance = 0.1f;
        public string FreeAttachHelpText = "Click left on a valid position to establish a link. Click right to abort.";
        public double FreeAttachStrutExtension = 0.05f;
        public string LinkHelpText = "Click left on a possible target to establish a link. Press 'x' to abort. You can also right click -> 'Set as Target' on a valid target and right click -> 'Abort' on the targeter.";
        public int MaxAngle = 95;
        public int MaxDistance = 15;
        public int MaximalJointStrength = 40000;
        public string ModuleName = "ModuleActiveStrut";
        public int NormalJointStrength = 2000;
        public int StartDelay = 60;
        public int StrutRealignInterval = 10;
        public int WeakJointStrength = 50;

        public void Load()
        {
            var cfg = PluginConfiguration.CreateForType<Config>((Vessel) null);
            cfg.load();
            //var saveAndReload = false;
            //try
            //{
            //    var initLoad = cfg.GetValue<bool>("InitialLoad");
            //    if (!initLoad)
            //    {
            //        saveAndReload = true;
            //    }
            //}
            //catch (Exception)
            //{
            //    saveAndReload = true;
            //}
            //if (saveAndReload)
            //{
            //    InitialLoad = true;
            //    Save();
            //    cfg.load();
            //}
            this.ModuleName = cfg.GetValue<string>("ModuleName");
            this.MaxDistance = cfg.GetValue<int>("MaxDistance");
            this.MaxAngle = cfg.GetValue<int>("MaxAngle");
            this.WeakJointStrength = cfg.GetValue<int>("WeakJointStrength");
            this.NormalJointStrength = cfg.GetValue<int>("NormalJointStrength");
            this.MaximalJointStrength = cfg.GetValue<int>("MaximalJointStrength");
            this.LinkHelpText = cfg.GetValue<string>("LinkHelpText");
            this.FreeAttachHelpText = cfg.GetValue<string>("FreeAttachHelpText");
            this.ConnectorDimension = cfg.GetValue<float>("ConectorDimension");
            this.ColorTransparency = cfg.GetValue<float>("ColorTransparency");
            this.FreeAttachDistanceTolerance = cfg.GetValue<float>("FreeAttachDistanceTolerance");
            this.FreeAttachStrutExtension = cfg.GetValue<float>("FreeAttachStrutExtension");
            this.StartDelay = cfg.GetValue<int>("StartDelay");
            this.StrutRealignInterval = cfg.GetValue<int>("StrutRealignInterval");
        }

        public void Save()
        {
            var cfg = PluginConfiguration.CreateForType<Config>((Vessel) null);
            cfg.SetValue("ModuleName", this.ModuleName);
            cfg.SetValue("MaxDistance", this.MaxDistance);
            cfg.SetValue("MaxAngle", this.MaxAngle);
            cfg.SetValue("WeakJointStrength", this.WeakJointStrength);
            cfg.SetValue("NormalJointStrength", this.NormalJointStrength);
            cfg.SetValue("MaximalJointStrength", this.MaximalJointStrength);
            cfg.SetValue("LinkHelptext", this.LinkHelpText);
            cfg.SetValue("FreeAttachHelpText", this.FreeAttachHelpText);
            cfg.SetValue("ConnectorDimension", this.ConnectorDimension);
            cfg.SetValue("ColorTransparency", this.ColorTransparency);
            cfg.SetValue("FreeAttachDistanceTolerance", this.FreeAttachDistanceTolerance);
            cfg.SetValue("FreeAttachStrutExtension", this.FreeAttachStrutExtension);
            cfg.SetValue("StartDelay", this.StartDelay);
            cfg.SetValue("StrutRealignInterval", this.StrutRealignInterval);
            cfg.save();
        }
    }
}