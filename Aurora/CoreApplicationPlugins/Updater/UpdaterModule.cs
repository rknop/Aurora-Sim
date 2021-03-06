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
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using log4net;
using Nini.Config;
using OpenSim;
using OpenSim.Framework;
using Aurora.Framework;

namespace OpenSim.CoreApplicationPlugins
{
    public class UpdaterPlugin : IApplicationPlugin
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private string m_urlToCheckForUpdates = "http://aurora-sim.org/updates.xml";

        public void Initialize(ISimulationBase openSim)
        {
            try
            {
                //Check whether this is enabled
                IConfig updateConfig = openSim.ConfigSource.Configs["Update"];
                if (updateConfig == null)
                    return;

                if (!updateConfig.GetBoolean("Enabled", false))
                    return;
                
                m_log.Info("[AURORAUPDATOR]: Checking for updates...");
                string CurrentVersion = OpenSim.Framework.VersionInfo.VERSION_NUMBER;
                string LastestVersionToBlock = updateConfig.GetString ("LatestRelease", OpenSim.Framework.VersionInfo.VERSION_NUMBER);

                string WebSite = updateConfig.GetString("URLToCheckForUpdates", m_urlToCheckForUpdates);
                //Pull the xml from the website
                string XmlData = Utilities.ReadExternalWebsite(WebSite);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(XmlData);

                XmlNodeList parts = doc.GetElementsByTagName("Updater");
                XmlNode UpdaterNode = parts[0];

                //[0] - Minimum supported release #
                //[1] - Minimum supported release date
                //[2] - Newest version #
                //[3] - Date released
                //[4] - Release notes
                //[5] - Download link

                //Read the newest version [2] and see if it is higher than the current version and less than the version the user last told us to block
                if (Compare (UpdaterNode.ChildNodes[2].InnerText, CurrentVersion) && Compare (UpdaterNode.ChildNodes[2].InnerText, LastestVersionToBlock))
                {
                    //Ask if they would like to update
                    DialogResult result = MessageBox.Show("A new version of Aurora has been released, version " +
                        UpdaterNode.ChildNodes[2].InnerText +
                        " released " + UpdaterNode.ChildNodes[3].InnerText +
                        ". Release notes: " + UpdaterNode.ChildNodes[4].InnerText +
                        ", do you want to download the update?", "Aurora Update",
                        System.Windows.Forms.MessageBoxButtons.YesNo);

                    //If so, download the new version
                    if (result == DialogResult.Yes)
                    {
                        Utilities.DownloadFile(UpdaterNode.ChildNodes[5].InnerText,
                            "AuroraVersion" + UpdaterNode.ChildNodes[2].InnerText + ".zip");
                        MessageBox.Show (string.Format("Downloaded to {0}, exiting for user to upgrade.", "AuroraVersion" + UpdaterNode.ChildNodes[2].InnerText + ".zip"), "Aurora Update");
                        Environment.Exit (0);
                    }
                    //Update the config so that we do not ask again
                    updateConfig.Set("LatestRelease", UpdaterNode.ChildNodes[2].InnerText);
                    updateConfig.ConfigSource.Save();
                }
                else if (Compare (UpdaterNode.ChildNodes[0].InnerText, CurrentVersion) && Compare (UpdaterNode.ChildNodes[2].InnerText, LastestVersionToBlock))
                {
                    //This version is not supported anymore
                    MessageBox.Show("Your version of Aurora (" + CurrentVersion + ", Released " + UpdaterNode.ChildNodes[1].InnerText + ") is not supported anymore.", "Aurora Update");
                }
            }
            catch
            {
            }
        }

        private bool Compare (string givenVersion, string CurrentVersion)
        {
            string[] given = givenVersion.Split ('.');
            string[] current = CurrentVersion.Split ('.');
            for (int i = 0; i < (int)Math.Max (given.Length, current.Length); i++)
            {
                if (i == given.Length || i == current.Length)
                    break;
                if (int.Parse (given[i]) > int.Parse (current[i]))
                    return true;
            }
            return false;
        }

        public void ReloadConfiguration (IConfigSource config)
        {
        }

        public void PostInitialise()
        {
        }

        public void Start()
        {
        }

        public void PostStart()
        {
        }

        public string Name
        {
            get { return "AuroraDataStartupPlugin"; }
        }

        public void Dispose()
        {
        }

        public void Close()
        {
        }
    }
}
