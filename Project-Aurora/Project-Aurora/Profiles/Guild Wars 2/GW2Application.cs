﻿using Aurora.Settings;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Aurora.Profiles.Guild_Wars_2
{
    public class GW2 : Application
    {
        public GW2()
            : base(new LightEventConfig { Name = "Guild Wars 2", ID = "GW2", ProcessNames = new string[] { "gw2.exe", "gw2-64.exe" }, SettingsType = typeof(FirstTimeApplicationSettings), ProfileType = typeof(GW2Profile), OverviewControlType = typeof(Control_GW2), GameStateType = typeof(GameState_Wrapper), Event = new GameEvent_Generic(), IconURI = "Resources/gw2_48x48.png" })
        {
            Config.ExtraAvailableLayers.Add("WrapperLights");
        }
    }
}
