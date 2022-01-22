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

    public bool DebugLogging = false;
    public bool DebugLoggingMessages = true;
    public bool DebugLoggingMessageContents = true;
    public bool DebugLoggingMessagePayloads = true;
    public bool DebugLoggingCreatingTab = true;
    public bool DebugLoggingTargetChanging = true;
    public bool DebugLoggingMessageAsJson = false;
    // public bool DebugLogging = false;

    public bool OpenOnLogin = false;

    public bool   MessageLog_PreserveOnLogout = true;
    public bool   MessageLog_ArchiveLogs = false;
    public bool   MessageLog_DeleteOldMessages = true;
    public int    MessageLog_DaysToKeepOldMessages = 7;
    public string MessageLog_FilePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\XIVLauncher\\pluginConfigs\\ChatScanner";
    public string MessageLog_FileName = "ChatLogs.json";

    public List<XivChatType> AllowedChannels { get; set; } = new List<XivChatType>() {
      XivChatType.StandardEmote, XivChatType.CustomEmote, XivChatType.Party, XivChatType.Say, XivChatType.TellIncoming, XivChatType.TellOutgoing
    };
    public List<XivChatType> TrackableChannels { get; set; } = new List<XivChatType>() {
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
