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
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.CoreModules.Avatar.Vegetation
{
    public class VegetationModule : INonSharedRegionModule, IVegetationModule
    { 
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        protected Scene m_scene;
        
        protected static readonly PCode[] creationCapabilities = new PCode[] { PCode.Grass, PCode.NewTree, PCode.Tree };
        public PCode[] CreationCapabilities { get { return creationCapabilities; } }
        
        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(Scene scene)
        {
            m_scene = scene;
            m_scene.RegisterModuleInterface<IVegetationModule>(this);
            m_scene.SceneGraph.RegisterEntityCreatorModule(this);
        }

        public void RemoveRegion(Scene scene)
        {

        }

        public void RegionLoaded(Scene scene)
        {

        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }
        
        public void PostInitialise() {}
        public void Close() {}
        public string Name { get { return "Vegetation Module"; } }
        public bool IsSharedModule { get { return false; } }

        public ISceneEntity AddTree (
            UUID uuid, UUID groupID, Vector3 scale, Quaternion rotation, Vector3 position, Tree treeType, bool newTree)
        {
            PrimitiveBaseShape treeShape = new PrimitiveBaseShape();
            treeShape.PathCurve = 16;
            treeShape.PathEnd = 49900;
            treeShape.PCode = newTree ? (byte)PCode.NewTree : (byte)PCode.Tree;
            treeShape.Scale = scale;
            treeShape.State = (byte)treeType;

            return m_scene.SceneGraph.AddNewPrim(uuid, groupID, position, rotation, treeShape);
        }
        
        public ISceneEntity CreateEntity(
            ISceneEntity baseEntity, UUID ownerID, UUID groupID, Vector3 pos, Quaternion rot, PrimitiveBaseShape shape)
        {
            if (Array.IndexOf(creationCapabilities, (PCode)shape.PCode) < 0)
            {
                m_log.DebugFormat("[VEGETATION]: PCode {0} not handled by {1}", shape.PCode, Name);
                return null;
            }

            SceneObjectGroup sceneObject = baseEntity as SceneObjectGroup;
            SceneObjectPart rootPart = (SceneObjectPart)sceneObject.GetChildPart(sceneObject.UUID);
            
            // if grass or tree, make phantom
            //rootPart.TrimPermissions();
            rootPart.AddFlag(PrimFlags.Phantom);
            if (rootPart.Shape.PCode != (byte)PCode.Grass)
                AdaptTree(ref shape);

            m_scene.SceneGraph.AddPrimToScene(sceneObject);
            sceneObject.ScheduleGroupUpdate (PrimUpdateFlags.ForcedFullUpdate);
            sceneObject.SetGroup(groupID, null);
            
            return sceneObject;
        }
        
        protected void AdaptTree(ref PrimitiveBaseShape tree)
        {
            // Tree size has to be adapted depending on its type
            switch ((Tree)tree.State)
            {
                case Tree.Cypress1:
                case Tree.Cypress2:
                case Tree.Palm1:
                case Tree.Palm2:
                case Tree.WinterAspen:
                    tree.Scale = new Vector3(4, 4, 10);
                    break;
                case Tree.WinterPine1:
                case Tree.WinterPine2:
                    tree.Scale = new Vector3(4, 4, 20);
                    break;

                case Tree.Dogwood:
                    tree.Scale = new Vector3(6.5f, 6.5f, 6.5f);
                    break;

                // case... other tree types
                // tree.Scale = new Vector3(?, ?, ?);
                // break;

                default:
                    tree.Scale = new Vector3(4, 4, 4);
                    break;
            }
        }
    }
}
