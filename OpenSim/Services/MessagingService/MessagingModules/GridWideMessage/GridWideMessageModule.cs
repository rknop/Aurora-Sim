/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using OpenSim.Framework;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using log4net;

namespace OpenSim.Services.MessagingService.MessagingModules.GridWideMessage
{
    public class GridWideMessageModule : IService, IGridWideMessageModule
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger (MethodBase.GetCurrentMethod ().DeclaringType);

        protected IRegistryCore m_registry;

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            registry.RegisterModuleInterface<IGridWideMessageModule> (this);
            if (MainConsole.Instance != null)
            {
                MainConsole.Instance.Commands.AddCommand ("grid send alert",
                    "grid send alert <message>", "Sends a message to all users in the grid", SendGridAlert);
                MainConsole.Instance.Commands.AddCommand ("grid send message",
                    "grid send message <first> <last> <message>", "Sends a message to a user in the grid", SendGridMessage);
                MainConsole.Instance.Commands.AddCommand ("grid kick user",
                    "grid kick user <first> <last> <message>", "Kicks a user from the grid", KickUserMessage);
            }

            //Also look for incoming messages to display
            registry.RequestModuleInterface<IAsyncMessageRecievedService>().OnMessageReceived += OnMessageReceived;
        }

        public void FinishedStartup()
        {
        }

        #endregion

        #region Commands

        protected void SendGridAlert(string[] cmd)
        {
            //Combine the params and figure out the message
            string message = CombineParams(cmd, 3);

            SendAlert (message);
        }

        protected void SendGridMessage(string[] cmd)
        {
            //Combine the params and figure out the message
            string user = CombineParams(cmd, 3, 5);
            string message = CombineParams(cmd, 5);

            IUserAccountService userService = m_registry.RequestModuleInterface<IUserAccountService> ();
            UserAccount account = userService.GetUserAccount (UUID.Zero, user.Split (' ')[0], user.Split (' ')[1]);
            if (account == null)
            {
                m_log.Info ("User does not exist.");
                return;
            }
            MessageUser (account.PrincipalID, message);
        }

        protected void KickUserMessage(string[] cmd)
        {
            //Combine the params and figure out the message
            string user = CombineParams(cmd, 2, 4);
            if (user.EndsWith(" "))
                user = user.Remove(user.Length - 1);
            string message = CombineParams(cmd, 5);
            IUserAccountService userService = m_registry.RequestModuleInterface<IUserAccountService> ();
            UserAccount account = userService.GetUserAccount (UUID.Zero, user);
            if (account == null)
            {
                m_log.Info ("User does not exist.");
                return;
            }

            KickUser (account.PrincipalID, message);
        }

        private string CombineParams(string[] commandParams, int pos)
        {
            string result = string.Empty;
            for (int i = pos; i < commandParams.Length; i++)
            {
                result += commandParams[i] + " ";
            }

            return result;
        }

        private string CombineParams(string[] commandParams, int pos, int end)
        {
            string result = string.Empty;
            for (int i = pos; i < commandParams.Length && i < end; i++)
            {
                result += commandParams[i] + " ";
            }

            return result;
        }

        private OSDMap BuildRequest(string name, string value, string user)
        {
            OSDMap map = new OSDMap();

            map["Method"] = name;
            map["Value"] = value;
            map["User"] = user;

            return map;
        }

        #endregion

        #region Message Received

        protected OSDMap OnMessageReceived(OSDMap message)
        {
            if (message.ContainsKey("Method") && message["Method"] == "GridWideMessage")
            {
                //We got a message, now display it
                string user = message["User"].AsString();
                string value = message["Value"].AsString();

                //Get the Scene registry since IDialogModule is a region module, and isn't in the ISimulationBase registry
                SceneManager manager = m_registry.RequestModuleInterface<SceneManager>();
                if (manager != null && manager.Scenes.Count > 0)
                {
                    IDialogModule dialogModule = manager.Scenes[0].RequestModuleInterface<IDialogModule>();
                    if (dialogModule != null)
                    {
                        //Send the message to the user now
                        dialogModule.SendAlertToUser(UUID.Parse(user), value);
                    }
                }
            }
            else if (message.ContainsKey("Method") && message["Method"] == "KickUserMessage")
            {
                //We got a message, now display it
                string user = message["User"].AsString();
                string value = message["Value"].AsString();

                //Get the Scene registry since IDialogModule is a region module, and isn't in the ISimulationBase registry
                SceneManager manager = m_registry.RequestModuleInterface<SceneManager>();
                if (manager != null && manager.Scenes.Count > 0)
                {
                    foreach (Scene scene in manager.Scenes)
                    {
                        IScenePresence sp = null;
                        if (scene.TryGetScenePresence(UUID.Parse(user), out sp))
                        {
                            sp.ControllingClient.Kick(value == "" ? "The Aurora Grid Manager kicked you out." : value);
                            IEntityTransferModule transferModule = scene.RequestModuleInterface<IEntityTransferModule> ();
                            if (transferModule != null)
                                transferModule.IncomingCloseAgent (scene, sp.UUID);
                        }
                    }
                }
            }
            return null;
        }

        #endregion

        public void KickUser(UUID avatarID, string message)
        {
            //Get required interfaces
            IAsyncMessagePostService messagePost = m_registry.RequestModuleInterface<IAsyncMessagePostService> ();
            ICapsService capsService = m_registry.RequestModuleInterface<ICapsService> ();
            IClientCapsService client = capsService.GetClientCapsService (avatarID);
            if (client != null)
            {
                IRegionClientCapsService regionClient = client.GetRootCapsService ();
                if (regionClient != null)
                {
                    //Send the message to the client
                    messagePost.Post (regionClient.RegionHandle, BuildRequest ("KickUserMessage", message, regionClient.AgentID.ToString ()));
                    IAgentProcessing agentProcessor = m_registry.RequestModuleInterface<IAgentProcessing> ();
                    if (agentProcessor != null)
                        agentProcessor.LogoutAgent (regionClient);
                    m_log.Info ("User Kicked sent.");
                    return;
                }
            }
            m_log.Info ("Could not find user to send message to.");
        }

        public void MessageUser(UUID avatarID, string message)
        {
            //Get required interfaces
            IAsyncMessagePostService messagePost = m_registry.RequestModuleInterface<IAsyncMessagePostService> ();
            ICapsService capsService = m_registry.RequestModuleInterface<ICapsService> ();
            IClientCapsService client = capsService.GetClientCapsService (avatarID);
            if (client != null)
            {
                IRegionClientCapsService regionClient = client.GetRootCapsService ();
                if (regionClient != null)
                {
                    //Send the message to the client
                    messagePost.Post (regionClient.RegionHandle, BuildRequest ("GridWideMessage", message, regionClient.AgentID.ToString ()));
                    m_log.Info ("Message sent.");
                    return;
                }
            }
            m_log.Info ("Could not find user to send message to.");
        }

        public void SendAlert(string message)
        {
            //Get required interfaces
            IAsyncMessagePostService messagePost = m_registry.RequestModuleInterface<IAsyncMessagePostService> ();
            ICapsService capsService = m_registry.RequestModuleInterface<ICapsService> ();
            List<IClientCapsService> clients = capsService.GetClientsCapsServices ();

            //Go through all clients, and send the message asyncly to all agents that are root
            foreach (IClientCapsService client in clients)
            {
                foreach (IRegionClientCapsService regionClient in client.GetCapsServices ())
                {
                    if (regionClient.RootAgent)
                    {
                        //Send the message to the client
                        messagePost.Post (regionClient.RegionHandle, BuildRequest ("GridWideMessage", message, regionClient.AgentID.ToString ()));
                    }
                }
            }
        }
    }
}
