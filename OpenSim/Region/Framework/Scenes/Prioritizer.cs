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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using log4net;
using Nini.Config;
using OpenSim.Framework;
using OpenMetaverse;
using OpenSim.Region.Physics.Manager;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

/*
 * Steps to add a new prioritization policy:
 * 
 *  - Add a new value to the UpdatePrioritizationSchemes enum.
 *  - Specify this new value in the [InterestManagement] section of your
 *    Aurora.ini. The name in the config file must match the enum value name
 *    (although it is not case sensitive).
 *  - Write a new GetPriorityBy*() method in this class.
 *  - Add a new entry to the switch statement in GetUpdatePriority() that calls
 *    your method.
 */

namespace OpenSim.Region.Framework.Scenes
{
    public enum UpdatePrioritizationSchemes
    {
        Time = 0,
        Distance = 1,
        SimpleAngularDistance = 2,
        FrontBack = 3,
        BestAvatarResponsiveness = 4,
        OOB = 5
    }

    public class Culler : ICuller
    {
        private bool m_useDistanceCulling = true;
        private bool m_useCulling = true;
        private int m_cachedXOffset = 0;
        private int m_cachedYOffset = 0;
        private float m_sizeToForceDualCulling = 10f;

        public bool UseCulling
        {
            get { return m_useCulling; }
            set { m_useCulling = value; }
        }

        public Culler (IScene scene)
        {
            IConfig interestConfig = scene.Config.Configs["InterestManagement"];
            if (interestConfig != null)
            {
                m_useCulling = interestConfig.GetBoolean ("UseCulling", m_useCulling);
                m_useDistanceCulling = interestConfig.GetBoolean ("UseDistanceBasedCulling", m_useDistanceCulling);
            }
        }

        public void Reset ()
        {
            m_cachedXOffset = 0;
            m_cachedYOffset = 0;
        }

        #region ICuller Members

        public bool ShowEntityToClient (IScenePresence client, IEntity entity)
        {
            if (!m_useCulling)
                return true; //If we arn't using culling, return true by default to show all prims

            if (m_useDistanceCulling && !DistanceCulling (client, entity))
                return false;

            //No more, guess its fine
            return true;
        }

        public bool DistanceCulling (IScenePresence client, IEntity entity)
        {
            IScene scene = client.Scene;
            float DD = client.DrawDistance;
            if (DD < 32) //Limit to a small distance
                DD = 32;
            if (DD > scene.RegionInfo.RegionSizeX && 
                DD > scene.RegionInfo.RegionSizeY)
                return true; //Its larger than the region, no culling check even necessary
            Vector3 posToCheckFrom = client.AbsolutePosition;
            if (client.IsChildAgent)
            {
                if (m_cachedXOffset == 0 && m_cachedYOffset == 0) //Not found yet
                {
                    IAgentInfoService agentInfoService = scene.RequestModuleInterface<IAgentInfoService>();
                    if (agentInfoService != null)
                    {
                        UserInfo info = agentInfoService.GetUserInfo (client.UUID.ToString ());
                        if (info != null)
                        {
                            GridRegion r = scene.GridService.GetRegionByUUID(scene.RegionInfo.ScopeID, 
                                info.CurrentRegionID);
                            if (r != null)
                            {
                                m_cachedXOffset = scene.RegionInfo.RegionLocX - r.RegionLocX;
                                m_cachedYOffset = scene.RegionInfo.RegionLocY - r.RegionLocY; 
                            }
                        }
                    }
                }
                //We need to add the offset so that we can check from the right place in child regions
                if (m_cachedXOffset < 0)
                    posToCheckFrom.X = scene.RegionInfo.RegionSizeX - (scene.RegionInfo.RegionSizeX + client.AbsolutePosition.X + m_cachedXOffset);
                if (m_cachedYOffset < 0)
                    posToCheckFrom.Y = scene.RegionInfo.RegionSizeY - (scene.RegionInfo.RegionSizeY + client.AbsolutePosition.Y + m_cachedYOffset);
                if (m_cachedXOffset > scene.RegionInfo.RegionSizeX)
                    posToCheckFrom.X = scene.RegionInfo.RegionSizeX - (scene.RegionInfo.RegionSizeX - (client.AbsolutePosition.X + m_cachedXOffset));
                if (m_cachedYOffset > scene.RegionInfo.RegionSizeY)
                    posToCheckFrom.Y = scene.RegionInfo.RegionSizeY - (scene.RegionInfo.RegionSizeY - (client.AbsolutePosition.Y + m_cachedYOffset));
            }
            Vector3 entityPosToCheckFrom = entity.AbsolutePosition;
            if (entity is ISceneEntity)
            {
                //We need to check whether this object is an attachment, and if so, set it so that we check from the avatar's
                // position, rather than from the offset of the attachment
                ISceneEntity sEntity = (ISceneEntity)entity;
                if (sEntity.IsAttachment)
                {
                    IScenePresence attachedAvatar = sEntity.Scene.GetScenePresence (sEntity.RootChild.AttachedAvatar);
                    if (attachedAvatar != null)
                        entityPosToCheckFrom = attachedAvatar.AbsolutePosition;
                }
            }
            if (entity is IScenePresence)
            {
                //We need to check whether this presence is sitting on anything, so that we can check from the object's
                // position, rather than the offset position of the object that the avatar is sitting on
                IScenePresence pEntity = (IScenePresence)entity;
                if (pEntity.SittingOnUUID != UUID.Zero)
                {
                    ISceneChildEntity sittingEntity = pEntity.Scene.GetSceneObjectPart (pEntity.SittingOnUUID);
                    if(sittingEntity != null)
                        entityPosToCheckFrom = sittingEntity.AbsolutePosition;
                }
            }
            //If the distance is greater than the clients draw distance, its out of range
            if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom) >
                DD * DD) //Use squares to make it faster than having to do the sqrt
            {
                ISceneEntity childEntity = (entity as ISceneEntity);
                if (childEntity != null && HardCullingCheck(childEntity))
                {
                    #region Side culling check (X, Y, Z) plane checks
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom + new Vector3 (childEntity.OOBsize.X, 0, 0)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom - new Vector3 (childEntity.OOBsize.X, 0, 0)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom + new Vector3 (0, childEntity.OOBsize.Y, 0)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom - new Vector3 (0, childEntity.OOBsize.Y, 0)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom + new Vector3 (0, 0, childEntity.OOBsize.Z)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom - new Vector3 (0, 0, childEntity.OOBsize.Z)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    #endregion
                    #region Corner checks ((x,y),(-x,-y),(x,-y),(-x,y), (y,z),(-y,-z),(y,-z),(-y,z), (x,z),(-x,-z),(x,-z),(-x,z))
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom + new Vector3 (childEntity.OOBsize.X, childEntity.OOBsize.Y, 0)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom - new Vector3 (childEntity.OOBsize.X, childEntity.OOBsize.Y, 0)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom + new Vector3 (childEntity.OOBsize.X, -childEntity.OOBsize.Y, 0)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom - new Vector3 (childEntity.OOBsize.X, -childEntity.OOBsize.Y, 0)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom + new Vector3 (0, childEntity.OOBsize.Y, childEntity.OOBsize.Z)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom - new Vector3 (0, childEntity.OOBsize.Y, childEntity.OOBsize.Z)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom + new Vector3 (0, childEntity.OOBsize.Y, -childEntity.OOBsize.Z)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom - new Vector3 (0, childEntity.OOBsize.Y, -childEntity.OOBsize.Z)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom + new Vector3 (childEntity.OOBsize.X, 0, childEntity.OOBsize.Z)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom - new Vector3 (childEntity.OOBsize.X, 0, childEntity.OOBsize.Z)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom + new Vector3 (-childEntity.OOBsize.X, 0, childEntity.OOBsize.Z)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (Vector3.DistanceSquared (posToCheckFrom, entityPosToCheckFrom - new Vector3 (-childEntity.OOBsize.X, 0, childEntity.OOBsize.Z)) <
                        DD * DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    #endregion
                }
                return false;
            }
            
            return true;
        }

        private bool HardCullingCheck (ISceneEntity childEntity)
        {
            if (LengthSquared (childEntity.OOBsize.X, childEntity.OOBsize.Y) > m_sizeToForceDualCulling * m_sizeToForceDualCulling ||
                LengthSquared (childEntity.OOBsize.Y, childEntity.OOBsize.Z) > m_sizeToForceDualCulling * m_sizeToForceDualCulling ||
                LengthSquared (childEntity.OOBsize.Z, childEntity.OOBsize.X) > m_sizeToForceDualCulling * m_sizeToForceDualCulling)
                return true;
            return false;
        }

        private float LengthSquared (float a, float b)
        {
            return (a * a) + (b * b);
        }

        #endregion
    }

    public class Prioritizer : IPrioritizer
    {
        private static readonly ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public UpdatePrioritizationSchemes UpdatePrioritizationScheme = UpdatePrioritizationSchemes.BestAvatarResponsiveness;

        private double m_childReprioritizationDistance = 20.0;

        public double ChildReprioritizationDistance { get { return m_childReprioritizationDistance; } }

        public Prioritizer(IScene scene)
        {
            IConfig interestConfig = scene.Config.Configs["InterestManagement"];
            if (interestConfig != null)
            {
                string update_prioritization_scheme = interestConfig.GetString("UpdatePrioritizationScheme", "BestAvatarResponsiveness").Trim().ToLower();
                m_childReprioritizationDistance = interestConfig.GetDouble("ChildReprioritizationDistance", 20.0);
                try
                {
                    UpdatePrioritizationScheme = (UpdatePrioritizationSchemes)Enum.Parse(typeof(UpdatePrioritizationSchemes), update_prioritization_scheme, true);
                }
                catch (Exception)
                {
                    m_log.Warn("[Prioritizer]: UpdatePrioritizationScheme was not recognized, setting to default prioritizer BestAvatarResponsiveness");
                    UpdatePrioritizationScheme = UpdatePrioritizationSchemes.BestAvatarResponsiveness;
                }
            }

            //m_log.Info("[Prioritizer]: Using the " + UpdatePrioritizationScheme + " prioritization scheme");
        }

        public double GetUpdatePriority(IScenePresence client, IEntity entity)
        {
            double priority = 0;

            if (entity == null)
                return double.PositiveInfinity;

            bool adjustRootPriority = true;
            try
            {
                /*switch (UpdatePrioritizationScheme)
                {
                    case UpdatePrioritizationSchemes.Time:
                        priority = GetPriorityByTime();
                        break;
                    case UpdatePrioritizationSchemes.Distance:
                        priority = GetPriorityByDistance(client, entity);
                        break;
                    case UpdatePrioritizationSchemes.SimpleAngularDistance:
                        priority = GetPriorityByDistance(client, entity); //This (afaik) always has been the same in OpenSim as just distance (it is in 0.6.9 anyway)
                        break;
                    case UpdatePrioritizationSchemes.FrontBack:
                        priority = GetPriorityByFrontBack(client, entity);
                        break;
                    case UpdatePrioritizationSchemes.BestAvatarResponsiveness:
                        priority = GetPriorityByBestAvatarResponsiveness(client, entity);
                        break;
                    case UpdatePrioritizationSchemes.OOB:*/
                        adjustRootPriority = false; //It doesn't need it
                        priority = GetPriorityByOOBDistance(client, entity);
                        /*break;
                    default:
                        throw new InvalidOperationException("UpdatePrioritizationScheme not defined.");
                }*/
            }
            catch (Exception ex)
            {
                if (!(ex is InvalidOperationException))
                {
                    m_log.Warn("[PRIORITY]: Error in finding priority of a prim/user:" + ex.ToString());
                }
                //Set it to max if it errors
                priority = double.PositiveInfinity;
            }

            // Adjust priority so that root prims are sent to the viewer first.  This is especially important for 
            // attachments acting as huds, since current viewers fail to display hud child prims if their updates
            // arrive before the root one.

            if (adjustRootPriority && entity is ISceneChildEntity)
            {
                ISceneChildEntity sop = ((ISceneChildEntity)entity);
                if (sop.IsRoot)
                {
                    ISceneEntity grp = sop.ParentEntity;
                    priority -= (grp.BSphereRadiusSQ + 0.5f);
                }


                if (sop.IsRoot)
                {
                    if (priority >= double.MinValue + 0.05)
                        priority -= 0.05;
                }
                else
                {
                    if (priority <= double.MaxValue - 0.05)
                        priority += 0.05;
                }
            }

            return priority;
        }

        private double GetPriorityByOOBDistance (IScenePresence presence, IEntity entity)
        {
            // If this is an update for our own avatar give it the highest priority
            if (presence == entity)
                return 0.0;

            // Use the camera position for local agents and avatar position for remote agents
            Vector3 presencePos = (presence.IsChildAgent) ?
                presence.AbsolutePosition :
                presence.CameraPosition;

            // Use group position for child prims
            Vector3 entityPos;
            float oobSQ;
            if (entity is SceneObjectGroup)
            {
                SceneObjectGroup p = (SceneObjectGroup)entity;
                entityPos = p.AbsolutePosition + p.OOBoffset * p.GroupRotation;
                oobSQ = p.BSphereRadiusSQ;
            }
            else
            {
                entityPos = entity.AbsolutePosition;
                oobSQ = 0;
            }

            float distsq = Vector3.DistanceSquared (presencePos, entityPos);
            distsq -= oobSQ;
            if (distsq < 0)
                distsq = 0;

            return -distsq;
        }

        private double GetPriorityByTime()
        {
            return DateTime.UtcNow.ToOADate();
        }

        private double GetPriorityByDistance (IScenePresence presence, IEntity entity)
        {
            // If this is an update for our own avatar give it the highest priority
            if (presence == entity)
                return 0.0;

            // Use the camera position for local agents and avatar position for remote agents
            Vector3 presencePos = (presence.IsChildAgent) ?
                presence.AbsolutePosition :
                presence.CameraPosition;

            // Use group position for child prims
            Vector3 entityPos = entity.AbsolutePosition;

            return Vector3.DistanceSquared (presencePos, entityPos);
        }

        private double GetPriorityByFrontBack (IScenePresence presence, IEntity entity)
        {
            // If this is an update for our own avatar give it the highest priority
            if (presence == entity)
                return 0.0;

            // Use group position for child prims
            Vector3 entityPos = entity.AbsolutePosition;
            if (entity is SceneObjectPart)
            {
                // Can't use Scene.GetGroupByPrim() here, since the entity may have been delete from the scene
                // before its scheduled update was triggered
                //entityPos = m_scene.GetGroupByPrim(entity.LocalId).AbsolutePosition;
                entityPos = ((SceneObjectPart)entity).ParentGroup.AbsolutePosition;
            }
            else
            {
                entityPos = entity.AbsolutePosition;
            }

            if (!presence.IsChildAgent)
            {
                // Root agent. Use distance from camera and a priority decrease for objects behind us
                Vector3 camPosition = presence.CameraPosition;
                Vector3 camAtAxis = presence.CameraAtAxis;

                // Distance
                double priority = Vector3.DistanceSquared (camPosition, entityPos);

                // Plane equation
                float d = -Vector3.Dot (camPosition, camAtAxis);
                float p = Vector3.Dot (camAtAxis, entityPos) + d;
                if (p < 0.0f) priority *= 2.0;

                return priority;
            }
            else
            {
                // Child agent. Use the normal distance method
                Vector3 presencePos = presence.AbsolutePosition;

                return Vector3.DistanceSquared (presencePos, entityPos);
            }
        }

        private double GetPriorityByBestAvatarResponsiveness (IScenePresence presence, IEntity entity)
        {
            // If this is an update for our own avatar give it the highest priority
            if (presence.UUID == entity.UUID)
                return 0.0;
            if (entity == null)
                return double.NaN;
            if (entity is IScenePresence)
                return 1.0;

            // Use group position for child prims
            Vector3 entityPos = entity.AbsolutePosition;

            if (!presence.IsChildAgent)
            {
                // Root agent. Use distance from camera and a priority decrease for objects behind us
                Vector3 camPosition = presence.CameraPosition;
                Vector3 camAtAxis = presence.CameraAtAxis;

                // Distance
                double priority = Vector3.DistanceSquared (camPosition, entityPos);

                // Plane equation
                float d = -Vector3.Dot (camPosition, camAtAxis);
                float p = Vector3.Dot (camAtAxis, entityPos) + d;
                if (p < 0.0f) priority *= 2.0;

                //Add distance again to really emphasize it
                priority += Vector3.DistanceSquared (presence.AbsolutePosition, entityPos);

                if ((Vector3.Distance (presence.AbsolutePosition, entityPos) / 2) > presence.DrawDistance)
                {
                    //Outside of draw distance!
                    priority *= 2;
                }

                SceneObjectPart rootPart = null;
                if (entity is SceneObjectPart)
                {
                    if (((SceneObjectPart)entity).ParentGroup != null &&
                        ((SceneObjectPart)entity).ParentGroup.RootPart != null)
                        rootPart = ((SceneObjectPart)entity).ParentGroup.RootPart;
                }
                if (entity is SceneObjectGroup)
                {
                    if (((SceneObjectGroup)entity).RootPart != null)
                        rootPart = ((SceneObjectGroup)entity).RootPart;
                }

                if (rootPart != null)
                {
                    PhysicsActor physActor = rootPart.PhysActor;

                    // Objects avatars are sitting on should be prioritized more
                    if (presence.ParentID == rootPart.UUID)
                    {
                        //Objects that are physical get more priority.
                        if (physActor != null && physActor.IsPhysical)
                            return 0.0;
                        else
                            return 1.2;
                    }

                    if (physActor == null || physActor.IsPhysical)
                        priority /= 2; //Emphasize physical objs

                    //Factor in the size of objects as well, big ones are MUCH more important than small ones
                    float size = rootPart.ParentGroup.GroupScale ().Length ();
                    //Cap size at 200 so that it doesn't completely overwhelm other objects
                    if (size > 200)
                        size = 200;

                    //Do it dynamically as well so that larger prims get smaller quicker
                    priority /= size > 40 ? (size / 35) : (size > 20 ? (size / 17) : 1);

                    if (rootPart.IsAttachment)
                    {
                        //Attachments are always high!
                        priority = 0.5;
                    }
                }
                //Closest first!
                return priority;
            }
            else
            {
                // Child agent. Use the normal distance method
                Vector3 presencePos = presence.AbsolutePosition;

                return Vector3.DistanceSquared (presencePos, entityPos);
            }
        }
    }
}
