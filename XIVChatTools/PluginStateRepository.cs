using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Plugin;
using Dalamud.Game.Command;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Logging;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Toast;
using Dalamud.IoC;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text;
using Dalamud.Game.Text.Sanitizer;
using Dalamud.Game.Text.SeStringHandling;
using XIVChatTools.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;

namespace XIVChatTools
{
    [PluginInterface]
    public class PluginState : IDisposable
    {
        private List<FocusTab> _focusTabs { get; set; }
        private List<ChatEntry> _chatEntries { get; set; }

        private Configuration Configuration { get; set; }

        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
        [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
        [PluginService] internal static IPluginLog Logger { get; private set; } = null!;

        public PluginState(Configuration config)
        {
            this.Configuration = config;
            this._focusTabs = new List<FocusTab>();
            if (Configuration.MessageLog_PreserveOnLogout && File.Exists($"{Configuration.MessageLog_FilePath}\\{Configuration.MessageLog_FileName}"))
            {
                try
                {
                    var FileResult = File.ReadAllText($"{Configuration.MessageLog_FilePath}\\{Configuration.MessageLog_FileName}");
                    var options = new JsonSerializerOptions
                    {
                        IncludeFields = true
                    };
                    
                    List<ChatEntry> ChatEntries = JsonSerializer.Deserialize<List<ChatEntry>>(FileResult, options) ?? [];

                    if (Configuration.MessageLog_DeleteOldMessages)
                    {
                        ChatEntries = ChatEntries
                          .Where(t => (DateTime.Now - t.DateSent).TotalDays < Configuration.MessageLog_DaysToKeepOldMessages)
                          .ToList();
                    }

                    this._chatEntries = ChatEntries;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Could not load Chat Logs!");
                    this._chatEntries = [];
                }
            }
            else
            {
                this._chatEntries = [];
            }
        }

        public void Dispose()
        {

        }

        public string GetPlayerName()
        {
            if (ClientState.LocalPlayer != null)
            {
                return ClientState.LocalPlayer.Name.TextValue;
            }
            else
            {
                return "";
            }
        }

        public List<IPlayerCharacter> GetNearbyPlayers()
        {
            return ObjectTable
              .Where(t => t.Name.TextValue != GetPlayerName() && t.ObjectKind == ObjectKind.Player)
              .Cast<IPlayerCharacter>()
              .OrderBy(t => t.Name.TextValue)
              .ToList();
        }

        public IPlayerCharacter? GetCurrentOrMouseoverTarget()
        {
            IGameObject? focusTarget = TargetManager.Target;

            if (focusTarget == null || focusTarget.ObjectKind != ObjectKind.Player)
            {
                focusTarget = TargetManager.MouseOverTarget;
            }

            if (focusTarget != null && focusTarget.ObjectKind != ObjectKind.Player)
            {
                focusTarget = null;
            }

            return focusTarget as IPlayerCharacter;
        }

        public List<ChatEntry> GetAllMessages()
        {
            return this._chatEntries
              .Where(t => t.OwnerId == GetPlayerName())
              .ToList();
        }

        public List<ChatEntry> GetMessagesForFocusTarget()
        {
            IPlayerCharacter? focusTarget = this.GetCurrentOrMouseoverTarget();

            if (focusTarget == null)
            {
                return [];
            }

            return this._chatEntries
              .Where(t => t.OwnerId == GetPlayerName())
              .Where(t => t.SenderName == focusTarget.Name.TextValue || t.SenderName.StartsWith(focusTarget.Name.TextValue))
              .ToList();
        }

        public List<ChatEntry> GetMessagesByPlayerNames(List<string> names)
        {
            return this._chatEntries
              .Where(t => t.OwnerId == GetPlayerName())
              .Where(t => names.Any(name => t.SenderName == name || t.SenderName.StartsWith(name)))
              .ToList();
        }

        public List<ChatEntry> SearchMessages(string searchText)
        {
            return this._chatEntries
                .Where(t =>
                    t.Message.ToLower().Contains(searchText.ToLower()) ||
                    t.SenderName.ToLower().Contains(searchText.ToLower()))
                .ToList();
        }

        public void AddChatMessage(ChatEntry chatEntry)
        {
            this._chatEntries.Add(chatEntry);

            if (Configuration.MessageLog_PreserveOnLogout)
            {
                PersistMessages();
            }
        }

        public void ClearMessageHistory()
        {
            this._chatEntries.Clear();
        }

        public List<FocusTab> GetFocusTabs()
        {
            return this._focusTabs;
        }

        public void AddFocusTabFromTarget()
        {
            var focusTarget = GetCurrentOrMouseoverTarget();

            if (focusTarget != null)
            {
                if (Configuration.DebugLogging && Configuration.DebugLoggingCreatingTab)
                {
                    Logger.Debug("CREATING FOCUS TAB");
                    Logger.Debug("=======================================================");
                    Logger.Debug("     Focus Target: " + focusTarget.Name);
                    Logger.Debug("");
                    Logger.Debug("");
                    Logger.Debug("");
                    Logger.Debug("");
                }

                var focusTab = new FocusTab(focusTarget.Name.TextValue);

                this._focusTabs.Add(focusTab);
            }
        }

        public void RemoveClosedFocusTabs()
        {
            this._focusTabs.RemoveAll(t => t.Open == false);
        }

        public void ClearAllFocusTabs()
        {
            this._focusTabs.Clear();
        }

        private async void PersistMessages()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    IncludeFields = true
                };
                var jsonData = JsonSerializer.Serialize(this._chatEntries.ToArray(), options);

                await File.WriteAllTextAsync($"{Configuration.MessageLog_FilePath}\\{Configuration.MessageLog_FileName}", jsonData);
            }
            catch (Exception ex)
            {
                Logger.Error("An error has occurred while trying to save chat history:" + ex.Message);
            }
        }
    }
}
