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
using System.IO;
using System.Reflection;
using System.Text;
using OpenSim.Framework;
using log4net;

namespace Aurora.Framework
{
    public static class AuroraModuleLoader
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static List<string> dllBlackList;
        private static List<string> firstLoad = new List<string>();
        private static Dictionary<string, List<Type>> LoadedDlls = new Dictionary<string, List<Type>> ();
        private const bool ALLOW_CACHE = true;
        private static Dictionary<string, Assembly> LoadedAssemblys = new Dictionary<string, Assembly>();

        #region Module Loaders

        
        /// <summary>
        /// Find all T modules in the current directory
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<T> PickupModules<T>()
        {
            return LoadModules<T>(Util.BasePathCombine(""));
        }
        
        /// <summary>
        /// Gets all modules found in the given directory. 
        /// Identifier is the name of the interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="moduleDir"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public static List<T> LoadModules<T>(string moduleDir)
        {
            if (moduleDir == "")
                moduleDir = Util.BasePathCombine ("");
            List<T> modules = new List<T>();
            if (!firstLoad.Contains(moduleDir))
            {
                DirectoryInfo dir = new DirectoryInfo(moduleDir);
                if (dllBlackList == null || dllBlackList.Count == 0)
                {
                    dllBlackList = new List<string> ();
                    dllBlackList.Add (Path.Combine (dir.FullName, "AsyncCtpLibrary.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "NHibernate.ByteCode.Castle.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Antlr3.Runtime.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "AprSharp.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Axiom.MathLib.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "BclExtras35.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "BulletDotNET.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "C5.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Castle.Core.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Castle.DynamicProxy.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Castle.DynamicProxy2.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Community.CsharpSqlite.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Community.CsharpSqlite.Sqlite.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "CookComputing.XmlRpcV2.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "CSJ2K.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "DotNetOpenId.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "DotNetOpenMail.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "DotSets.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Fadd.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Fadd.Globalization.Yaml.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "FluentNHibernate.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Glacier2.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "GlynnTucker.Cache.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Google.ProtocolBuffers.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "GoogleTranslateAPI.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "HttpServer.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "HttpServer_OpenSim.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Ice.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Iesi.Collections.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "intl3_svn.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Kds.Serialization.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "libapr.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "libapriconv.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "libaprutil.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "libbulletnet.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "libdb44d.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "libdb_dotNET43.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "libeay32.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "log4net.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Modified.XnaDevRu.BulletX.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Mono.Addins.CecilReflector.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Mono.Addins.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Mono.Addins.Setup.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Mono.Data.Sqlite.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Mono.Data.SqliteClient.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Mono.GetOptions.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Mono.PEToolkit.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Mono.Security.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "MonoXnaCompactMaths.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "MXP.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "MySql.Data.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "NDesk.Options.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Newtonsoft.Json.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Newtonsoft.Json.Net20.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "NHibernate.ByteCode.Castle.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "NHibernate.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "HttpServer_OpenSim.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Nini.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Npgsql.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "nunit.framework.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "ode.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Ode.NET.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Ode.NET.Single.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Ode.NET.Double.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "openjpeg-dotnet-x86_64.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "openjpeg-dotnet.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "openjpeg.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "OpenMetaverse.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "OpenMetaverse.GUI.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "OpenMetaverse.Rendering.Simple.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "OpenMetaverse.Rendering.Meshmerizer.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "OpenMetaverse.Http.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "OpenMetaverse.StructuredData.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "OpenMetaverse.Utilities.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "OpenMetaverseTypes.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "OpenMetaverse.Tests.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "PhysX-wrapper.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "PhysX_Wrapper_Dotnet.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "PrimMesher.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "protobuf-net.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "PumaCode.SvnDotNet.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "RAIL.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "SmartThreadPool.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "sqlite3.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "ssleay32.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "SubversionSharp.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "svn_client-1.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "System.Data.SQLite.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Tools.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "xunit.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "XMLRPC.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "Warp3D.dll"));
                    dllBlackList.Add (Path.Combine (dir.FullName, "zlib.net.dll"));
                }
                if (ALLOW_CACHE)
                    LoadedDlls.Add (moduleDir, new List<Type> ());
                foreach (FileInfo fileInfo in dir.GetFiles("*.dll"))
                {
                    modules.AddRange (LoadModulesFromDLL<T> (moduleDir, fileInfo.FullName));
                }
                if (ALLOW_CACHE)
                    firstLoad.Add (moduleDir);
            }
            else
            {
                try
                {
                    List<Type> loadedDllModules;
                    LoadedDlls.TryGetValue (moduleDir, out loadedDllModules);
                    foreach (Type pluginType in loadedDllModules)
                    {
                        try
                        {
                            if (pluginType.IsPublic)
                            {
                                if (!pluginType.IsAbstract)
                                {
                                    if (pluginType.GetInterface(typeof(T).Name) != null)
                                    {
                                        modules.Add((T)Activator.CreateInstance(pluginType));
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            return modules;
        }

        public static void ClearCache ()
        {
            LoadedDlls.Clear ();
            firstLoad.Clear ();
        }
        
        /// <summary>
        /// Load all T modules from dllname
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dllName"></param>
        /// <returns></returns>
        private static List<T> LoadModulesFromDLL<T>(string moduleDir, string dllName)
        {
            List<T> modules = new List<T>();
            if (dllBlackList.Contains(dllName))
                return modules;
            //m_log.Info ("[ModuleLoader]: Loading " + dllName);

            Assembly pluginAssembly;
            if (!LoadedAssemblys.TryGetValue(dllName, out pluginAssembly))
            {
                try
                {
                    pluginAssembly = Assembly.LoadFrom(dllName);
                    LoadedAssemblys.Add(dllName, pluginAssembly);
                }
                catch (BadImageFormatException)
                {
                }
            }

            if (pluginAssembly != null)
            {
                try
                {
                    List<Type> loadedTypes = new List<Type> ();
                    if (ALLOW_CACHE)
                        loadedTypes = LoadedDlls[moduleDir];

                    foreach (Type pluginType in pluginAssembly.GetTypes())
                    {
                        try
                        {
                            if (pluginType.IsPublic)
                            {
                                if (!pluginType.IsAbstract)
                                {
                                    if (!firstLoad.Contains(moduleDir))
                                    {
                                        //Only add on the first load
                                        if (ALLOW_CACHE)
                                        {
                                            if (!loadedTypes.Contains (pluginType))
                                                loadedTypes.Add (pluginType);
                                        }
                                    }
                                    if (pluginType.GetInterface(typeof(T).Name, true) != null)
                                    {
                                        modules.Add((T)Activator.CreateInstance(pluginType));
                                    }
                                }
                            }
                        }
                        catch (Exception ex )
                        {
                            m_log.Warn("[MODULELOADER]: Error loading module " + pluginType.Name + " in file " + dllName + " : " + ex.ToString());
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            return modules;
        }

        #endregion

        /// <summary>
        /// Load all plugins from the given .dll file with the interface 'type'
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dllName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static T LoadPlugin<T>(string dllName) 
        {
            string type = typeof(T).ToString();
            try
            {
                Assembly pluginAssembly = Assembly.LoadFrom(dllName);

                foreach (Type pluginType in pluginAssembly.GetTypes())
                {
                    if (pluginType.IsPublic)
                    {
                        try
                        {
                            Type typeInterface = pluginType.GetInterface(type, true);

                            if (typeInterface != null)
                            {
                                return (T)Activator.CreateInstance(pluginType);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            } 
            catch (ReflectionTypeLoadException e)
            {
                foreach (Exception e2 in e.LoaderExceptions)
                {
                    m_log.Error(e2.ToString());
                }
                throw e;
            }
            return default(T);
        }

        /// <summary>
        /// Load a plugin from a dll with the given class or interface
        /// </summary>
        /// <param name="dllName"></param>
        /// <param name="args">The arguments which control which constructor is invoked on the plugin</param>
        /// <returns></returns>
        public static T LoadPlugin<T>(string dllName, Object[] args) where T : class
        {
            string[] parts = dllName.Split(new char[] { ':' });

            dllName = parts[0];

            string className = String.Empty;

            if (parts.Length > 1)
                className = parts[1];

            return LoadPlugin<T>(dllName, className, args);
        }

        /// <summary>
        /// Load a plugin from a dll with the given class or interface
        /// </summary>
        /// <param name="dllName"></param>
        /// <param name="className"></param>
        /// <param name="args">The arguments which control which constructor is invoked on the plugin</param>
        /// <returns></returns>
        public static T LoadPlugin<T>(string dllName, string className, Object[] args) where T : class
        {
            string interfaceName = typeof(T).ToString();

            try
            {
                Assembly pluginAssembly = Assembly.LoadFrom(dllName);

                foreach (Type pluginType in pluginAssembly.GetTypes())
                {
                    if (pluginType.IsPublic)
                    {
                        if (className != String.Empty
                            && pluginType.ToString() != pluginType.Namespace + "." + className)
                            continue;

                        Type typeInterface = pluginType.GetInterface(interfaceName, true);

                        if (typeInterface != null)
                        {
                            T plug = null;
                            try
                            {
                                plug = (T)Activator.CreateInstance(pluginType,
                                        args);
                            }
                            catch (Exception e)
                            {
                                if (!(e is System.MissingMethodException))
                                    m_log.ErrorFormat("Error loading plugin from {0}, exception {1}", dllName, e.InnerException);
                                return null;
                            }

                            return plug;
                        }
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                m_log.Error(string.Format("Error loading plugin from {0}", dllName), e);
                return null;
            }
        }
    }
}
