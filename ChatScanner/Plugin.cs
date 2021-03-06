using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Gui;
using Dalamud.Plugin;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Logging;
using Dalamud.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ChatScanner
{
  public class ChatScannerPlugin : IDalamudPlugin
  {
    public string Name => "Chat Scanner";

    [PluginService]
    internal DalamudPluginInterface PluginInterface { get; init; }

    [PluginService]
    internal ChatGui ChatGui { get; init; }

    [PluginService]
    internal ClientState ClientState { get; init; }

    [PluginService]
    internal CommandManager CommandManager { get; init; }

    internal PluginState PluginState { get; init; }

    internal Configuration Configuration { get; }
    internal PluginUI PluginUI { get; }

    private List<string> commandAliases = new List<string>() {
      "/chatScanner",
      "/cScanner",
      "/cscan"
    };
    private List<string> settingsArgumentAliases = new List<string>() {
      "settings",
      "config",
      "c"
    };


    public ChatScannerPlugin()
    {
      Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
      Configuration.Initialize(PluginInterface);

      PluginState = PluginInterface.Create<PluginState>(Configuration);
      PluginUI = new PluginUI(Configuration, PluginState);

      foreach (string commandAlias in commandAliases)
      {
        CommandManager.AddHandler(commandAlias, new CommandInfo(OnCommand)
        {
          HelpMessage = commandAliases.First() == commandAlias ?
              "Opens the Chat Scanner window." : "Alias for /chatScanner."
        });
      }

      ChatGui.ChatMessage += Chat_OnChatMessage;

      ClientState.Login += OnLogin;
      ClientState.Logout += OnLogout;
      
      PluginInterface.UiBuilder.Draw += DrawUI;
      PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
    }

    public void Dispose()
    {
      PluginUI.Dispose();

      ChatGui.ChatMessage -= Chat_OnChatMessage;

      ClientState.Login -= OnLogin;
      ClientState.Logout -= OnLogout;

      PluginInterface.UiBuilder.Draw -= DrawUI;
      PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;

      foreach (string commandAlias in commandAliases)
      {
        CommandManager.RemoveHandler(commandAlias);
      }
    }

    private void OnCommand(string command, string args)
    {
      if (settingsArgumentAliases.Contains(args.ToLower()))
      {
        PluginUI.SettingsVisible = true;
      }
      else
      {
        PluginUI.Visible = true;
      }
    }

    private void DrawUI()
    {
      PluginUI.Draw();
    }

    private void DrawConfigUI()
    {
      PluginUI.SettingsVisible = true;
    }

    private void OnLogin(object sender, EventArgs args)
    {
      PluginUI.Visible = Configuration.OpenOnLogin;
    }

    private void OnLogout(object sender, EventArgs args)
    {
      PluginUI.Visible = false;
      PluginState.ClearAllFocusTabs();

      if (Configuration.MessageLog_PreserveOnLogout == false)
      {
        PluginState.ClearMessageHistory();
      }
    }

    private void Chat_OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString cmessage, ref bool isHandled)
    {
      if (Configuration.DebugLogging && Configuration.DebugLoggingMessages && Configuration.ActiveChannels.Any(t => t == type))
      {
        PluginLog.Log("NEW CHAT MESSAGE RECEIVED");
        PluginLog.Log("=======================================================");
        PluginLog.Log("Message Type: " + type.ToString());
        PluginLog.Log("Is Marked Handled: " + isHandled.ToString());
        PluginLog.Log("Raw Sender: " + sender.TextValue);
        PluginLog.Log("Parsed Sender: " + ParseSenderName(type, sender));

        if (Configuration.DebugLoggingMessagePayloads && sender.Payloads.Any())
        {
          PluginLog.Log("");
          PluginLog.Log("SenderPayloads");
          foreach (var payload in sender.Payloads)
          {
            PluginLog.Log("Type: " + payload.Type.ToString());
            PluginLog.Log(payload.ToString());
          }
        }

        if (Configuration.DebugLoggingMessageContents)
        {
          PluginLog.Log("");
          PluginLog.Log("Message: " + cmessage.TextValue);
        }

        if (Configuration.DebugLoggingMessageAsJson)
        {
          PluginLog.Log("");
          PluginLog.Log("CMessage Json: ");
          try
          {
            // PluginLog.Log(JsonSerializer.Serialize(cmessage));
          }
          catch (Exception ex)
          {
            PluginLog.Log("An error occurred during serialization.");
            PluginLog.Log(ex.Message);
          }

          PluginLog.Log("Sender Json: ");
          try
          {
            // PluginLog.Log(JsonSerializer.Serialize(sender));
          }
          catch (Exception ex)
          {
            PluginLog.Log("An error occurred during serialization.");
            PluginLog.Log(ex.Message);
          }
        }
      }

      if (isHandled || !Configuration.ActiveChannels.Any(t => t == type))
      {
        return;
      }

      PluginState.AddChatMessage(new Models.ChatEntry()
      {
        ChatType = type,
        Message = cmessage.TextValue,
        SenderId = senderId,
        SenderName = ParseSenderName(type, sender)
      });
    }

    private string ParseSenderName(XivChatType type, SeString sender)
    {
      if (type == XivChatType.TellIncoming)
      {
        var playerPayload = sender.Payloads.FirstOrDefault(t => t.Type == PayloadType.Player);

        return (playerPayload as PlayerPayload).PlayerName;
      }

      if (type == XivChatType.TellOutgoing)
      {
        return PluginState.GetPlayerName();
      }

      if (type == XivChatType.CustomEmote || type == XivChatType.StandardEmote)
      {
        var playerPayload = sender.Payloads.FirstOrDefault(t => t.Type == PayloadType.Player);

        if (playerPayload != null)
        {
          return (playerPayload as PlayerPayload).PlayerName;
        }

        var textPayload = sender.Payloads.FirstOrDefault(t => t.Type == PayloadType.RawText);

        if (textPayload != null)
        {
          return (textPayload as TextPayload).Text;
        }
      }

      if (type == XivChatType.Party || type == XivChatType.Say)
      {
        var playerPayload = sender.Payloads.FirstOrDefault(t => t.Type == PayloadType.Player);

        if (playerPayload != null)
        {
          return (playerPayload as PlayerPayload).PlayerName;
        }
        else
        {
          return PluginState.GetPlayerName();
        }
      }

      return "N/A|BadType";
    }
  }
}
