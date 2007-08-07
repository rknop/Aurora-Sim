using System.Collections.Generic;
using System.Text;
using Axiom.Math;
using libsecondlife;
using libsecondlife.Packets;
using OpenSim.Framework.Interfaces;
using OpenSim.Framework.Types;
using OpenSim.Physics.Manager;

namespace OpenSim.Region.Environment.Scenes
{
    // public delegate void PrimCountTaintedDelegate();

    public class AllNewSceneObjectGroup2 : EntityBase
    {
        private Encoding enc = Encoding.ASCII;

        protected AllNewSceneObjectPart2 m_rootPart;
        protected Dictionary<LLUUID, AllNewSceneObjectPart2> m_parts = new Dictionary<LLUUID, AllNewSceneObjectPart2>();

        protected ulong m_regionHandle;

        public event PrimCountTaintedDelegate OnPrimCountTainted;

        /// <summary>
        /// 
        /// </summary>
        public int PrimCount
        {
            get { return 1; }
        }

        /// <summary>
        /// 
        /// </summary>
        public LLVector3 GroupCentrePoint
        {
            get { return new LLVector3(0, 0, 0); }
        }

        public override LLVector3 Pos
        {
            get { return m_rootPart.GroupPosition; }
            set
            {
                lock (this.m_parts)
                {
                    foreach (AllNewSceneObjectPart2 part in this.m_parts.Values)
                    {
                        part.GroupPosition = value;
                    }
                }
            } 
        }

        public override uint LocalId
        {
            get { return m_rootPart.LocalID; }
            set { m_rootPart.LocalID = value; }
        }

        public override LLUUID UUID
        {
            get { return m_rootPart.UUID; }
            set { m_rootPart.UUID = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public AllNewSceneObjectGroup2()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public AllNewSceneObjectGroup2(byte[] data)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public AllNewSceneObjectGroup2(Scene scene, ulong regionHandle, LLUUID ownerID, uint localID, LLVector3 pos, PrimitiveBaseShape shape)
        {
            m_regionHandle = regionHandle;
            m_scene = scene;

            this.Pos = pos;
            LLVector3 rootOffset = new LLVector3(0, 0, 0);
            AllNewSceneObjectPart2 newPart = new AllNewSceneObjectPart2(m_regionHandle, this, ownerID, localID, shape, pos, rootOffset);
            this.m_parts.Add(newPart.UUID, newPart);
            this.SetPartAsRoot(newPart);
        }


        #region Copying
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public new AllNewSceneObjectGroup2 Copy()
        {
            AllNewSceneObjectGroup2 dupe = (AllNewSceneObjectGroup2)this.MemberwiseClone();
            dupe.Pos = new LLVector3(Pos.X, Pos.Y, Pos.Z);
            dupe.m_scene = m_scene;
            dupe.m_regionHandle = this.m_regionHandle;

            dupe.CopyRootPart(this.m_rootPart);

            foreach (AllNewSceneObjectPart2 part in this.m_parts.Values)
            {
                if (part.UUID != this.m_rootPart.UUID)
                {
                    dupe.CopyPart(part);
                }
            }
            return dupe;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="part"></param>
        public void CopyRootPart(AllNewSceneObjectPart2 part)
        {
            AllNewSceneObjectPart2 newPart = part.Copy(m_scene.PrimIDAllocate());
            this.m_parts.Add(newPart.UUID, newPart);
            this.SetPartAsRoot(newPart);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="part"></param>
        public void CopyPart(AllNewSceneObjectPart2 part)
        {
            AllNewSceneObjectPart2 newPart = part.Copy(m_scene.PrimIDAllocate());
            this.m_parts.Add(newPart.UUID, newPart);
            this.SetPartAsNonRoot(newPart);
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public override void Update()
        {
            foreach (AllNewSceneObjectPart2 part in this.m_parts.Values)
            {
                part.SendScheduledUpdates();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void ScheduleGroupForFullUpdate()
        {
            foreach (AllNewSceneObjectPart2 part in this.m_parts.Values)
            {
                part.ScheduleFullUpdate();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void ScheduleGroupForTerseUpdate()
        {
            foreach (AllNewSceneObjectPart2 part in this.m_parts.Values)
            {
                part.ScheduleTerseUpdate();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void SendGroupFullUpdate()
        {
            foreach (AllNewSceneObjectPart2 part in this.m_parts.Values)
            {
                part.SendFullUpdateToAllClients();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void SendGroupTerseUpdate()
        {
            foreach (AllNewSceneObjectPart2 part in this.m_parts.Values)
            {
                part.SendTerseUpdateToAllClients();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectGroup"></param>
        public void LinkToGroup(AllNewSceneObjectGroup2 objectGroup)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="primID"></param>
        /// <returns></returns>
        private AllNewSceneObjectPart2 GetChildPrim(LLUUID primID)
        {
            AllNewSceneObjectPart2 childPart = null;
            if (this.m_parts.ContainsKey(primID))
            {
                childPart = this.m_parts[primID];
            }
            return childPart;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="localID"></param>
        /// <returns></returns>
        private AllNewSceneObjectPart2 GetChildPrim(uint localID)
        {
            foreach (AllNewSceneObjectPart2 part in this.m_parts.Values)
            {
                if (part.LocalID == localID)
                {
                    return part;
                }
            }
            return null;
        }

        /// <summary>
        /// Does this group contain the child prim
        /// should be able to remove these methods once we have a entity index in scene
        /// </summary>
        /// <param name="primID"></param>
        /// <returns></returns>
        public bool HasChildPrim(LLUUID primID)
        {
            AllNewSceneObjectPart2 childPart = null;
            if (this.m_parts.ContainsKey(primID))
            {
                childPart = this.m_parts[primID];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Does this group contain the child prim
        /// should be able to remove these methods once we have a entity index in scene
        /// </summary>
        /// <param name="localID"></param>
        /// <returns></returns>
        public bool HasChildPrim(uint localID)
        {
            foreach (AllNewSceneObjectPart2 part in this.m_parts.Values)
            {
                if (part.LocalID == localID)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void TriggerTainted()
        {
            if (OnPrimCountTainted != null)
            {
                this.OnPrimCountTainted();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="pos"></param>
        /// <param name="remoteClient"></param>
        public void GrapMovement(LLVector3 offset, LLVector3 pos, IClientAPI remoteClient)
        {
            this.Pos = pos;
            this.m_rootPart.SendTerseUpdateToAllClients();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        public void GetProperites(IClientAPI client)
        {
            ObjectPropertiesPacket proper = new ObjectPropertiesPacket();
            proper.ObjectData = new ObjectPropertiesPacket.ObjectDataBlock[1];
            proper.ObjectData[0] = new ObjectPropertiesPacket.ObjectDataBlock();
            proper.ObjectData[0].ItemID = LLUUID.Zero;
            proper.ObjectData[0].CreationDate = (ulong)this.m_rootPart.CreationDate;
            proper.ObjectData[0].CreatorID = this.m_rootPart.CreatorID;
            proper.ObjectData[0].FolderID = LLUUID.Zero;
            proper.ObjectData[0].FromTaskID = LLUUID.Zero;
            proper.ObjectData[0].GroupID = LLUUID.Zero;
            proper.ObjectData[0].InventorySerial = 0;
            proper.ObjectData[0].LastOwnerID = this.m_rootPart.LastOwnerID;
            proper.ObjectData[0].ObjectID = this.m_uuid;
            proper.ObjectData[0].OwnerID = this.m_rootPart.OwnerID;
            proper.ObjectData[0].TouchName = enc.GetBytes(this.m_rootPart.TouchName + "\0");
            proper.ObjectData[0].TextureID = new byte[0];
            proper.ObjectData[0].SitName = enc.GetBytes(this.m_rootPart.SitName + "\0");
            proper.ObjectData[0].Name = enc.GetBytes(this.m_rootPart.PartName + "\0");
            proper.ObjectData[0].Description = enc.GetBytes(this.m_rootPart.Description + "\0");
            proper.ObjectData[0].OwnerMask = this.m_rootPart.OwnerMask;
            proper.ObjectData[0].NextOwnerMask = this.m_rootPart.NextOwnerMask;
            proper.ObjectData[0].GroupMask = this.m_rootPart.GroupMask;
            proper.ObjectData[0].EveryoneMask = this.m_rootPart.EveryoneMask;
            proper.ObjectData[0].BaseMask = this.m_rootPart.BaseMask;

            client.OutPacket(proper);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="localID"></param>
        public void GetInventory(IClientAPI remoteClient, uint localID)
        {
            AllNewSceneObjectPart2 part = this.GetChildPrim(localID);
            if (part != null)
            {
                part.GetInventory(remoteClient, localID);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="type"></param>
        /// <param name="inUse"></param>
        /// <param name="data"></param>
        public void UpdateExtraParam(uint localID, ushort type, bool inUse, byte[] data)
        {
            AllNewSceneObjectPart2 part = this.GetChildPrim(localID);
            if (part != null)
            {
                part.UpdateExtraParam(type, inUse, data);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="textureEntry"></param>
        public void UpdateTextureEntry(uint localID, byte[] textureEntry)
        {
            AllNewSceneObjectPart2 part = this.GetChildPrim(localID);
            if (part != null)
            {
                part.UpdateTextureEntry(textureEntry);
            }
        }

        #region Shape
        /// <summary>
        /// 
        /// </summary>
        /// <param name="shapeBlock"></param>
        public void UpdateShape(ObjectShapePacket.ObjectDataBlock shapeBlock, uint localID)
        {
            AllNewSceneObjectPart2 part = this.GetChildPrim(localID);
            if (part != null)
            {
                part.UpdateShape(shapeBlock);
            }
        }
        #endregion

        #region Position
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        public void UpdateGroupPosition(LLVector3 pos)
        {
            this.Pos = pos;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="localID"></param>
        public void UpdateSinglePosition(LLVector3 pos, uint localID)
        {
            AllNewSceneObjectPart2 part = this.GetChildPrim(localID);
            if (part != null)
            {
                if (part.UUID == this.m_rootPart.UUID)
                {
                    this.UpdateRootPosition(pos);
                }
                else
                {
                    part.UpdateOffSet(pos);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        private void UpdateRootPosition(LLVector3 pos)
        {
            LLVector3 newPos = new LLVector3(pos.X, pos.Y, pos.Z);
            LLVector3 oldPos = new LLVector3(this.Pos.X + this.m_rootPart.OffsetPosition.X, this.Pos.Y + this.m_rootPart.OffsetPosition.Y, this.Pos.Z + this.m_rootPart.OffsetPosition.Z);
            LLVector3 diff = oldPos - newPos;
            Axiom.Math.Vector3 axDiff = new Vector3(diff.X, diff.Y, diff.Z);
            Axiom.Math.Quaternion partRotation = new Quaternion(this.m_rootPart.RotationOffset.W, this.m_rootPart.RotationOffset.X, this.m_rootPart.RotationOffset.Y, this.m_rootPart.RotationOffset.Z);
            axDiff = partRotation.Inverse() * axDiff;
            diff.X = axDiff.x;
            diff.Y = axDiff.y;
            diff.Z = axDiff.z;

            foreach (AllNewSceneObjectPart2 obPart in this.m_parts.Values)
            {
                if (obPart.UUID != this.m_rootPart.UUID)
                {
                    obPart.OffsetPosition = obPart.OffsetPosition + diff;
                }
            }
            this.Pos = newPos;
            pos.X = newPos.X;
            pos.Y = newPos.Y;
            pos.Z = newPos.Z;
        }
        #endregion

        #region Roation
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rot"></param>
        public void UpdateGroupRotation(LLQuaternion rot)
        {
            this.m_rootPart.UpdateRotation(rot);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        public void UpdateGroupRotation(LLVector3 pos, LLQuaternion rot)
        {
            this.m_rootPart.UpdateRotation(rot);
            this.Pos = pos;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rot"></param>
        /// <param name="localID"></param>
        public void UpdateSingleRotation(LLQuaternion rot, uint localID)
        {
            AllNewSceneObjectPart2 part = this.GetChildPrim(localID);
            if (part != null)
            {
                if (part.UUID == this.m_rootPart.UUID)
                {
                    this.UpdateRootRotation(rot);
                }
                else
                {
                    part.UpdateRotation(rot);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rot"></param>
        private void UpdateRootRotation(LLQuaternion rot)
        {
            this.m_rootPart.UpdateRotation(rot);
            Axiom.Math.Quaternion axRot = new Quaternion(rot.W, rot.X, rot.Y, rot.Z);
            Axiom.Math.Quaternion oldParentRot = new Quaternion(this.m_rootPart.RotationOffset.W, this.m_rootPart.RotationOffset.X, this.m_rootPart.RotationOffset.Y, this.m_rootPart.RotationOffset.Z);

            foreach (AllNewSceneObjectPart2 prim in this.m_parts.Values)
            {
                if (prim.UUID != this.m_rootPart.UUID)
                {
                    Vector3 axPos = new Vector3(prim.OffsetPosition.X, prim.OffsetPosition.Y, prim.OffsetPosition.Z);
                    axPos = oldParentRot * axPos;
                    axPos = axRot.Inverse() * axPos;
                    prim.OffsetPosition = new LLVector3(axPos.x, axPos.y, axPos.z);
                    Axiom.Math.Quaternion primsRot = new Quaternion(prim.RotationOffset.W, prim.RotationOffset.X, prim.RotationOffset.Y, prim.RotationOffset.Z);
                    Axiom.Math.Quaternion newRot = oldParentRot * primsRot;
                    newRot = axRot.Inverse() * newRot;
                    prim.RotationOffset = new LLQuaternion(newRot.w, newRot.x, newRot.y, newRot.z);
                }
            }
        }

        #endregion
        /// <summary>
        /// 
        /// </summary>
        /// <param name="part"></param>
        private void SetPartAsRoot(AllNewSceneObjectPart2 part)
        {
            this.m_rootPart = part;
            //this.m_uuid= part.UUID;
           // this.m_localId = part.LocalID;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="part"></param>
        private void SetPartAsNonRoot(AllNewSceneObjectPart2 part)
        {
            part.ParentID = this.m_rootPart.LocalID;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<ScenePresence> RequestSceneAvatars()
        {
            return m_scene.RequestAvatarList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="part"></param>
        internal void SendPartFullUpdate(IClientAPI remoteClient, AllNewSceneObjectPart2 part)
        {
            if( m_rootPart == part )
            {
                part.SendFullUpdateToClient( remoteClient, Pos );
            }
            else
            {
                part.SendFullUpdateToClient( remoteClient );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="part"></param>
        internal void SendPartTerseUpdate(IClientAPI remoteClient, AllNewSceneObjectPart2 part)
        {
            if (m_rootPart == part)
            {
                part.SendTerseUpdateToClient(remoteClient, Pos);
            }
            else
            {
                part.SendTerseUpdateToClient(remoteClient);
            }
        }
    }
}
