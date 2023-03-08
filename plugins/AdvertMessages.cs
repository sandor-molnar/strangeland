using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{
    [Info("Advert Messages", "LaserHydra", "3.0.2", ResourceId = 1510)]
    [Description("Allows to set up messages which are broadcasted in a configured interval")]
    internal class AdvertMessages : CovalencePlugin
    {
        private Configuration _config;
        private int _previousAdvert = -1;
        private bool commandAdvertEnabled = true;

        #region Hooks

        private void Loaded()
        {
            LoadConfig();
            
            Puts($"{Title} is showing adverts every {_config.AdvertInterval} minutes.");
            timer.Every(_config.AdvertInterval * 60, BroadcastNextAdvert);
        }

        #endregion

        #region Helper Methods

        private void BroadcastNextAdvert()
        {
            if (_config.Messages.Count == 0)
                return;

            int advert = GetNextAdvertIndex();

            server.Broadcast("<color=" + _config.MessageColor + ">" + _config.Prefix + _config.Messages[advert] + "</color>");

            if (_config.BroadcastToConsole)
                Puts(Formatter.ToPlaintext(_config.Messages[advert]));

            _previousAdvert = advert;
        }

        private int GetNextAdvertIndex()
        {
            if (!_config.ChooseMessageAtRandom)
                return (_previousAdvert + 1) % _config.Messages.Count;

            int advert;
            if (_config.Messages.Count > 1)
            {
                do advert = Random.Range(0, _config.Messages.Count);
                while (advert == _previousAdvert);
            }
            else
                advert = 0;

            return advert;
        }
        
        object OnPlayerChat(BasePlayer player, string message)
        {
            for (int i = 0; i < _config.CommandMessages.Count; i++) {
                if (_config.CommandMessages[i] == "DISABLED") {
                   continue;
                }
                
                if (message == _config.CommandMessages[i]) {
                    if (commandAdvertEnabled == false) {
                        return false;
                    }
                    
                    server.Broadcast("<color=" + _config.MessageColor + ">" + _config.Prefix + _config.Messages[i] + "</color>");
                    commandAdvertEnabled = false;
                    timer.Once(_config.CommandAdvertLimit, () =>
                    {
                        commandAdvertEnabled = true;
                    });
                    
                    return false;
                }
            }
            return null;
        }
        #endregion

        #region Configuration

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = Configuration.CreateDefault();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        private class Configuration
        {
            [JsonProperty("Prefix")]
            public string Prefix { get; private set; }  = "Server";
            
            [JsonProperty("Color Of Messages")]
            public string MessageColor { get; private set; }  = "#AAFFAA";
            
            [JsonProperty("Messages")]
            public List<string> Messages { get; private set; }
            
            [JsonProperty("CommandMessages")]
            public List<string> CommandMessages { get; private set; }

            [JsonProperty("Advert Interval (in Minutes)")]
            public float AdvertInterval { get; private set; }  = 10;
            
            [JsonProperty("Command Advert Limit (in Seconds)")]
            public int CommandAdvertLimit { get; private set; }  = 60;

            [JsonProperty("Broadcast to Console (true/false)")]
            public bool BroadcastToConsole { get; private set; } = true;

            [JsonProperty("Choose Message at Random (true/false)")]
            public bool ChooseMessageAtRandom { get; private set; } = false;

            public static Configuration CreateDefault()
            {
                return new Configuration
                {
                    Messages = new List<string>
                    {
                        "Welcome to our server, have fun!",
                        "Please treat everybody respectfully.",
                        "Cheating will result in a [#red]permanent[/#] ban."
                    }
                };
            }
        }

        #endregion
    }
}