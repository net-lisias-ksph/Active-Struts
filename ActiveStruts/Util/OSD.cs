using System;

namespace ActiveStruts.Util
{
    public static class OSD
    {
        private const string Prefix = "[ActiveStruts] ";

        public static void PostMessage(String text, float shownFor = 3.7f)
        {
            ScreenMessages.PostScreenMessage(Prefix + text, shownFor, ScreenMessageStyle.UPPER_CENTER);
        }
    }
}