using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ActiveStruts.Util
{
    /* Modified by marce 2014
     *  
     * Copyright (C) 2013 FW Industries
    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
    The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
    */

    public static class OSD
    {
        private const string Prefix = "[ActiveStruts] ";
        private static readonly object ListLock = new object();

        private static readonly List<Message> Msgs = new List<Message>();

        public static void AddMessage(String text, Color color, float shownFor)
        {
            lock (ListLock)
            {
                var msg = new Message {Text = Prefix + text, Color = color, HideAt = Time.time + shownFor};
                Msgs.Add(msg);
            }
        }

        private static float CalcHeight()
        {
            lock (ListLock)
            {
                var style = CreateStyle(Color.white);
                return Msgs.Aggregate(.0f, (a, m) => a + style.CalcSize(new GUIContent(m.Text)).y);
            }
        }

        private static GUIStyle CreateStyle(Color color)
        {
            var style = new GUIStyle {stretchWidth = true, alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold, normal = {textColor = color}};
            return style;
        }

        public static void Error(String text, float showDuration = 3.7f)
        {
            AddMessage(text, XKCDColors.LightRed, showDuration);
        }

        public static void Info(String text, float showDuration = 3.7f)
        {
            AddMessage(text, XKCDColors.OffWhite, showDuration);
        }

        public static void Success(String text, float showDuration = 3.7f)
        {
            AddMessage(text, XKCDColors.Cerulean, showDuration);
        }

        public static void Update()
        {
            lock (ListLock)
            {
                if (Msgs.Count == 0)
                {
                    return;
                }
                Msgs.RemoveAll(m => Time.time >= m.HideAt);
                var h = CalcHeight();
                GUILayout.BeginArea(new Rect(0, Screen.height*0.1f, Screen.width, h), CreateStyle(Color.white));
                Msgs.ForEach(m => GUILayout.Label(m.Text, CreateStyle(m.Color)));
                GUILayout.EndArea();
            }
        }

        public static void Warn(String text, float showDuration = 3.7f)
        {
            AddMessage(text, XKCDColors.Yellow, showDuration);
        }

        private struct Message
        {
            public Color Color { get; set; }
            public float HideAt { get; set; }
            public String Text { get; set; }
        }
    }
}