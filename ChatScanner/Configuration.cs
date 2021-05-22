﻿using Dalamud.Configuration;
using Dalamud.Plugin;
using Dalamud.Game.Text;
using System;
using System.Linq;
using System.Collections.Generic;

namespace ChatScanner
{
  [Serializable]
  public class Configuration : IPluginConfiguration
  {
    public int Version { get; set; } = 0;

    public List<XivChatType> AllowedChannels { get; set; } = new List<XivChatType>() {
      XivChatType.StandardEmote, XivChatType.CustomEmote, XivChatType.Party, XivChatType.Say, XivChatType.TellIncoming, XivChatType.TellOutgoing
    };
    // the below exist just to make saving less cumbersome

    [NonSerialized]
    private DalamudPluginInterface pluginInterface;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
      this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
      this.pluginInterface.SavePluginConfig(this);
    }
  }
}