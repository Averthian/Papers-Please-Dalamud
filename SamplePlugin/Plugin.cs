using System;
using Dalamud.Game.Command;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Lumina.Excel.Sheets;

namespace PapersPlease // 🔴 MUST MATCH PROJECT NAME
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "TomeLink";

        private const string CommandName = "/tome";

        // ===== Dalamud services (API v14) =====
        [PluginService] private static ICommandManager CommandManager { get; set; } = null!;
        [PluginService] private static IContextMenu ContextMenu { get; set; } = null!;
        [PluginService] private static IDataManager DataManager { get; set; } = null!;
        [PluginService] private static IObjectTable ObjectTable { get; set; } = null!;
        [PluginService] private static IDalamudPluginInterface PluginInterface { get; set; } = null!;

        public Plugin()
        {
            CommandManager.AddHandler(CommandName, new CommandInfo(OnTomeCommand)
            {
                HelpMessage = "Open your character on tomestone.gg",
            });

            ContextMenu.OnMenuOpened += OnMenuOpened;
        }

        public void Dispose()
        {
            ContextMenu.OnMenuOpened -= OnMenuOpened;
            CommandManager.RemoveHandler(CommandName);
        }

        // =====================
        // /tome → YOUR profile
        // =====================
        private void OnTomeCommand(string command, string args)
        {
            var player = ObjectTable.LocalPlayer;
            if (player == null)
                return;

            var name = player.Name.TextValue;
            if (string.IsNullOrWhiteSpace(name))
                return;

            var world = player.HomeWorld.Value.Name.ToString();
            if (string.IsNullOrWhiteSpace(world))
                return;

            OpenTomestone(world, name);
        }

        // ==================================
        // Right-click → Open on Tomestone
        // ==================================
        private void OnMenuOpened(IMenuOpenedArgs args)
        {
            if (!PluginInterface.UiBuilder.ShouldModifyUi)
                return;

            if (args.Target is not MenuTargetDefault target)
                return;

            if (string.IsNullOrWhiteSpace(target.TargetName))
                return;

            var worldRowId = target.TargetHomeWorld.RowId;
            if (worldRowId == 0)
                return;

            var world = GetWorldName(worldRowId);
            if (string.IsNullOrWhiteSpace(world))
                return;

            args.AddMenuItem(new MenuItem
            {
                Name = "Open on Tomestone",
                OnClicked = _ => OpenTomestone(world!, target.TargetName),
            });
        }

        // =====================
        // Helpers
        // =====================
        private static void OpenTomestone(string world, string name)
        {
            var encodedWorld = Uri.EscapeDataString(world.ToLowerInvariant());
            var encodedName = Uri.EscapeDataString(name);

            var url = $"https://tomestone.gg/character-name/{encodedWorld}/{encodedName}";
            Util.OpenLink(url);
        }

        private static string? GetWorldName(uint rowId)
        {
            var sheet = DataManager.GetExcelSheet<World>();
            if (sheet == null)
                return null;

            return sheet.TryGetRow(rowId, out var world)
                ? world.Name.ToString()
                : null;
        }
    }
}
