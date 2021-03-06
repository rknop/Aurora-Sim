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
using System.Text;
using OpenSim.Services.Connectors;
using OpenSim.Services;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Framework;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules 
{
    public class IWCAgentInfoConnector : IAgentInfoService, IService
    {
        protected AgentInfoService m_localService;
        protected AgentInfoConnector m_remoteService;
        protected IRegistryCore m_registry;

        #region IService Members

        public string Name
        {
            get { return GetType().Name; }
        }

        public IAgentInfoService InnerService
        {
            get
            {
                //If we are getting URls for an IWC connection, we don't want to be calling other things, as they are calling us about only our info
                //If we arn't, its ar region we are serving, so give it everything we know
                if (m_registry.RequestModuleInterface<InterWorldCommunications> ().IsGettingUrlsForIWCConnection)
                    return m_localService;
                else
                    return this;
            }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AgentInfoHandler", "") != Name)
                return;

            m_localService = new AgentInfoService();
            m_localService.Initialize(config, registry);
            m_remoteService = new AgentInfoConnector();
            m_remoteService.Initialize(config, registry);
            registry.RegisterModuleInterface<IAgentInfoService>(this);
            m_registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            if (m_localService != null)
                m_localService.Start(config, registry);
        }

        public void FinishedStartup()
        {
            if (m_localService != null)
                m_localService.FinishedStartup();
        }

        #endregion

        #region IAgentInfoService Members

        public UserInfo GetUserInfo(string userID)
        {
            UserInfo info = m_localService.GetUserInfo(userID);
            if (info == null)
                info = m_remoteService.GetUserInfo(userID);
            return info;
        }

        public UserInfo[] GetUserInfos(string[] userIDs)
        {
            UserInfo[] info = m_localService.GetUserInfos(userIDs);
            if (info == null)
                info = m_remoteService.GetUserInfos(userIDs);
            return info;
        }

        public string[] GetAgentsLocations(string[] userIDs)
        {
            string[] info = m_localService.GetAgentsLocations(userIDs);
            string[] info2 = m_remoteService.GetAgentsLocations (userIDs);
            if (info == null)
                info = info2;
            else
            {
                for (int i = 0; i < userIDs.Length; i++)
                {
                    if(info[i] == "NotOnline")
                        info[i] = info2[i];
                }
            }
            return info;
        }

        public bool SetHomePosition(string userID, UUID homeID, Vector3 homePosition, Vector3 homeLookAt)
        {
            return m_localService.SetHomePosition(userID, homeID, homePosition, homeLookAt);
        }

        public void SetLastPosition(string userID, UUID regionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            m_localService.SetLastPosition(userID, regionID, lastPosition, lastLookAt);
        }

        public void SetLoggedIn(string userID, bool loggingIn, bool fireLoggedInEvent, UUID enteringRegion)
        {
            m_localService.SetLoggedIn (userID, loggingIn, fireLoggedInEvent, enteringRegion);
        }

        public void LockLoggedInStatus(string userID, bool locked)
        {
            m_localService.LockLoggedInStatus (userID, locked);
        }

        #endregion
    }
}
