using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Whitelist", "Wulf/lukespragg", "3.3.0")]
    [Description("Restricts server access to whitelisted players only")]

    class Whitelist : CovalencePlugin
    {
        #region Initialization

        //const string permAdmin = "whitelist.admin";
        const string permAllow = "whitelist.allow";

        bool adminExcluded;
        bool resetOnRestart;

        protected override void LoadDefaultConfig()
        {
            // Options
            Config["Admin Excluded (true/false)"] = adminExcluded = GetConfig("Admin Excluded (true/false)", true);
            Config["Reset On Restart (true/false)"] = resetOnRestart = GetConfig("Reset On Restart (true/false)", false);

            // Cleanup
            Config.Remove("AdminExcluded");
            Config.Remove("ResetOnRestart");

            SaveConfig();
        }

        void OnServerInitialized()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();

            //permission.RegisterPermission(permAdmin, this);
            permission.RegisterPermission(permAllow, this);

            foreach (var player in players.All)
            {
                if (!player.HasPermission("whitelist.allowed")) continue;
                permission.GrantUserPermission(player.Id, permAllow, null);
                permission.RevokeUserPermission(player.Id, "whitelist.allowed");
            }

            foreach (var group in permission.GetGroups())
            {
                if (!permission.GroupHasPermission(group, "whitelist.allowed")) continue;
                permission.GrantGroupPermission(group, permAllow, null);
                permission.RevokeGroupPermission(group, "whitelist.allowed");
            }

            if (!resetOnRestart) return;
            foreach (var group in permission.GetGroups())
                if (permission.GroupHasPermission(group, permAllow)) permission.RevokeGroupPermission(group, permAllow);
            foreach (var user in permission.GetPermissionUsers(permAllow))
                permission.RevokeUserPermission(Regex.Replace(user, "[^0-9]", ""), permAllow);
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                //["CommandUsage"] = "Usage: {0} <name or id> <permission>",
                //["NoPlayersFound"] = "No players were found using '{0}'",
                //["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["NotWhitelisted"] = "Server is currently under development.",
                //["WhitelistAdd"] = "'{0}' has been added to the whitelist",
                //["WhitelistRemove"] = "'{0}' has been removed from the whitelist"
            }, this);
        }

        #endregion

        #region Whitelisting

        bool IsWhitelisted(string id)
        {
            var player = players.FindPlayerById(id);
            return player != null && adminExcluded && player.IsAdmin || permission.UserHasPermission(id, permAllow);
        }

        object CanUserLogin(string name, string id) => !IsWhitelisted(id) ? Lang("NotWhitelisted", id) : null;

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}