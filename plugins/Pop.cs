using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Pop", "SaSha", "1.0.0")]
    [Description("Allow players to use !pop and /pop as command to show the current population of the server")]
    public class Pop : CovalencePlugin
    {
        bool popLimiter = false;
        
        #region Commands
        [Command("pop")]
        void PopCommand(IPlayer player, string command, string[] args)
        {
            ShowPop();
        }
        #endregion
        
        object OnPlayerChat(BasePlayer player, string message) {
            if (message == "!pop") {
                ShowPop();
                return false;
            }
            
            return null;
        }
        
        
        void ShowPop() {
            if (popLimiter) {
                return;
            }
            
            int maxPlayers = ConVar.Server.maxplayers;
            int currentPlayers = BasePlayer.activePlayerList.Count;
            int queuePlayers = ServerMgr.Instance.connectionQueue.Queued;
            int joiningPlayers = ServerMgr.Instance.connectionQueue.Joining;
			
            server.Broadcast(
                string.Format(
                    "<color=#BDC3C7>[ <color=#E74C3C>Strangeland</color> ] <color=#E74C3C>{0}</color> players out of <color=#E74C3C>{1}</color> is in the server. <color=#E74C3C>{2}</color> connecting and <color=#E74C3C>{3}</color> in queue.</color>",
                    currentPlayers,
                    maxPlayers,
                    joiningPlayers,
                    queuePlayers
                )
            );
            popLimiter = true;
                
            timer.Once(10, () => {
                popLimiter = false;
            });
        }
    
    }
}