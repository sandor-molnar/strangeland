using Facepunch;
using Newtonsoft.Json;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Core;
using Oxide.Game.Rust;
using Rust;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Linq;
using System;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ATP", "mspeedie", "0.1.3")]
    [Description("Teleport for admins")]
    class ATP : RustPlugin
    {
		#region Configuration

		private Configuration config;

		public class Configuration
		{
			[JsonProperty(PropertyName = "TP permission")]
			public string PermTp { get; set; } =  "atp.tp";

			[JsonProperty(PropertyName = "TP Players to you permission")]
			public string PermTp2 { get; set; } =  "atp.tp2";

			[JsonProperty(PropertyName = "Allow TP to sleepers")]
			public string PermSleepers { get; set; } = "atp.sleeper";

			[JsonProperty(PropertyName = "Version")]
			public Oxide.Core.VersionNumber Version { get; set; }
			
		}

		protected override void LoadDefaultConfig() => config = new Configuration();

		protected override void LoadConfig()
		{
			try
			{
				base.LoadConfig();
				config = Config.ReadObject<Configuration>();
				if (config == null)
				{
					throw new JsonException();
				}

				if (config.Version < Version)
				{
					Puts("Configuration appears to be outdated; updating and saving");
					config.Version = Version;
					SaveConfig();
				}
			}
			catch
			{
				Puts($"Configuration file {Name}.json is invalid; using defaults");
				LoadDefaultConfig();
			}

			// CheckConfig();
		}

		protected override void SaveConfig()
		{
			Puts($"Configuration changes saved to {Name}.json");
			Config.WriteObject(config, true);
		}

		private void OnServerInitialized()
		{
			// set up permission to run chat command
			if (!permission.PermissionExists(config.PermTp))
				permission.RegisterPermission(config.PermTp,this);
			if (!permission.PermissionExists(config.PermTp2))
				permission.RegisterPermission(config.PermTp2,this);
			if (!permission.PermissionExists(config.PermSleepers))
				permission.RegisterPermission(config.PermSleepers,this);
		}

		#endregion Configuration

		string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["multipleplayers"] = "Multiple players found:\n\r",
				["nopermission"] = "You do not have permission for atp",
				["noplayerfound"] = "No player found for name: ",
				["noplayergiven"] = "No player name or ID given ",
				["noplayertogiven"] = "No player name or ID given for to",
				["noplayerpos"] = "No player position found for name: ",
				["syntaxatp"] = "please provide either a target player (to) or two players (from to)",
				["syntaxatp2"] = "please provide a target player",
				["teleportingtwo"] = "Teleporting from to: ",
				["teleportingto"] = "Teleporting to: ",
				["teleportingtoyou"] = "Teleporting to you: "
			}, this);
		}

       private BasePlayer FindPlayer(string PlayerFind, BasePlayer player)
       {
			BasePlayer	from_player = null;
			int			player_count = 0;
			string		returnstring = "";
			ulong		fp_user_ID = 0u;

           if (string.IsNullOrEmpty(PlayerFind))
           {
				SendReply(player, Lang("noplayergiven", player.UserIDString));
				return null as BasePlayer;
           }

			if (!string.IsNullOrEmpty(PlayerFind))
			{
				ulong.TryParse ( PlayerFind, out fp_user_ID );
			}
			if (fp_user_ID != 0u)
			{
				foreach (BasePlayer plyr in BasePlayer.activePlayerList.Where(x=>x.userID == fp_user_ID))
				{
					from_player = plyr;
					player_count++;
					returnstring += string.Format("{0} - {1}\n\r", plyr.userID, plyr.displayName);
				}
				if (from_player == null && permission.UserHasPermission(player.UserIDString, config.PermSleepers))
				{
					foreach (BasePlayer splyr in BasePlayer.sleepingPlayerList.Where(x=>x.userID == fp_user_ID))
					{
						from_player = splyr;
						player_count++;
						returnstring += string.Format("{0} - {1}\n\r", splyr.UserIDString, splyr.displayName);
					}
				}
				if(player_count > 1)
				{
					SendReply(player, String.Concat(Lang("multipleplayers", player.UserIDString), returnstring));
					return null as BasePlayer;
				}
			}
			else if (from_player == null)
			{
				foreach (BasePlayer plyr in BasePlayer.activePlayerList)
				{
					if (plyr.displayName.Contains(PlayerFind, CompareOptions.OrdinalIgnoreCase))
					{
						from_player = plyr;
						player_count++;
						returnstring += string.Format("{0} - {1}\n\r", plyr.userID, plyr.displayName);
					}
				}
				if (from_player == null && permission.UserHasPermission(player.UserIDString, config.PermSleepers))
				{
					foreach (BasePlayer splyr in BasePlayer.sleepingPlayerList)
					{
						if (splyr.displayName.Contains(PlayerFind, CompareOptions.OrdinalIgnoreCase))
						{
							from_player = splyr;
							player_count++;
							returnstring += string.Format("{0} - {1}\n\r", splyr.UserIDString, splyr.displayName);
						}
					}
				}
				if(player_count > 1)
				{
					SendReply(player, String.Concat(Lang("multipleplayers", player.UserIDString), returnstring));
					return null as BasePlayer;
				}
			}

			if (from_player == null)
			{
				SendReply(player, Lang("noplayerfound", PlayerFind));
               return null as BasePlayer;
			}
			// SendReply(player,"found: " + from_player.displayName); // debug
			return from_player;
       }

		[ChatCommand("atp")]
		private void ChatCommandatp(BasePlayer player, string cmd, string[] args)
		{
			BasePlayer from_player = null as BasePlayer;
			BasePlayer to_player = null as BasePlayer;
			if (!permission.UserHasPermission(player.UserIDString, config.PermTp))
			{
				SendReply(player, Lang("nopermission", player.UserIDString));
				return;
			}
			else if (args == null || args.Length < 1 || string.IsNullOrEmpty(args[0]))
			{
				SendReply(player, Lang("noplayergiven", player.UserIDString));
				return;
			}
			else if (args.Length > 2)
			{
				SendReply(player, Lang("syntaxatp", player.UserIDString));
				return;
			}
			else if (args.Length == 1)
			{
				to_player = FindPlayer(args[0].ToLower(), player);
				if (to_player != null)
				{
					var to_player_pos = to_player.transform.position;
					if (to_player_pos != null)
					{
						SendReply(player, Lang("teleportingto", player.UserIDString)+ to_player.displayName);
						if (player.HasParent())
						{
							player.SetParent(null, true, true);
						}
						player.Teleport(to_player_pos);
						player.ClientRPCPlayer(null, player, "ForcePositionTo", to_player_pos);
					}
					else
					{
						SendReply(player, String.Concat(Lang("noplayerpos", player.UserIDString),args[0]));
					}
				}
				return;
			}
			else if (args.Length == 2)
			{
				if (string.IsNullOrEmpty(args[1]))
				{
					SendReply(player, Lang("noplayertogiven", player.UserIDString));
					return;
				}
				else
				{
					from_player = FindPlayer(args[0].ToLower(), player);
					to_player = FindPlayer(args[1].ToLower(), player);
					if (from_player != null && to_player != null)
					{
						var to_player_pos = to_player.transform.position;
						if (to_player_pos != null)
						{
							SendReply(player, string.Format(Lang("teleportingtwo", player.UserIDString)+ "{0} -> {1}", from_player.displayName, to_player.displayName));
							if (from_player.HasParent())
							{
								from_player.SetParent(null, true, true);
							}
							if (from_player.IsConnected)
							{
								from_player.EndLooting();
							}
							from_player.Teleport(to_player_pos);
							from_player.ClientRPCPlayer(null, player, "ForcePositionTo", to_player_pos);
						}
						else
						{
							SendReply(player, String.Concat(Lang("noplayerpos", player.UserIDString),args[0]));
						}
					}
				}
			}
		}

		[ChatCommand("atp2")]
		private void ChatCommandatp2(BasePlayer player, string cmd, string[] args)
		{
			BasePlayer to_player = null as BasePlayer;
			if (!permission.UserHasPermission(player.UserIDString, config.PermTp2))
			{
				SendReply(player, Lang("nopermission", player.UserIDString));
				return;
			}
			else if (args == null || args.Length < 1 || string.IsNullOrEmpty(args[0]))
			{
				SendReply(player, Lang("noplayergiven", player.UserIDString));
				return;
			}
			else if (args.Length > 1)
			{
				SendReply(player, Lang("syntaxatp2", player.UserIDString));
				return;
				
			}
			else
			{
				to_player = FindPlayer(args[0].ToLower(), player);
				if (to_player != null)
				{
					var player_pos = player.transform.position;
					if (player_pos != null)
					{
						SendReply(player, Lang("teleportingtoyou", player.UserIDString)+ to_player.displayName);
						if (to_player.HasParent())
						{
							to_player.SetParent(null, true, true);
						}
						if (to_player.IsConnected)
						{
							to_player.EndLooting();
						}
						to_player.Teleport(player_pos);
						to_player.ClientRPCPlayer(null, to_player, "ForcePositionTo", player_pos);
						to_player.SendEntityUpdate();
					}
					else
					{
						SendReply(player, String.Concat(Lang("noplayerpos", player.UserIDString),args[0]));
					}
				}
				return;
			}
		}

	}
}