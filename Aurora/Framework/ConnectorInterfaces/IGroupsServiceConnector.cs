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
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenSim.Framework;
using Nini.Config;


namespace Aurora.Framework
{
    public interface IGroupsServiceConnector : IAuroraDataPlugin
	{
		void CreateGroup(UUID groupID, string name, string charter, bool showInList, UUID insigniaID, int membershipFee, bool openEnrollment, bool allowPublish, bool maturePublish, UUID founderID,
            ulong EveryonePowers, UUID OwnerRoleID, ulong OwnerPowers);
        void AddGroupNotice(UUID requestingAgentID, UUID groupID, UUID noticeID, string fromName, string subject, string message, UUID ItemID, int AssetType, string ItemName);
        void SetAgentActiveGroup(UUID AgentID, UUID GroupID);
		void SetAgentGroupSelectedRole(UUID AgentID, UUID GroupID, UUID RoleID);
        void AddAgentToGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID);
        void AddRoleToGroup(UUID requestingAgentID, UUID GroupID, UUID RoleID, string Name, string Description, string Title, ulong Powers);
		void UpdateGroup(UUID requestingAgentID, UUID groupID, string charter, int showInList, UUID insigniaID, int membershipFee, int openEnrollment, int allowPublish, int maturePublish);
        void RemoveRoleFromGroup(UUID requestingAgentID, UUID RoleID, UUID GroupID);
        void UpdateRole(UUID requestingAgentID, UUID GroupID, UUID RoleID, string Name, string Desc, string Title, ulong Powers);
        void SetAgentGroupInfo(UUID requestingAgentID, UUID AgentID, UUID GroupID, int AcceptNotices, int ListInProfile);
        void AddAgentGroupInvite(UUID requestingAgentID, UUID inviteID, UUID GroupID, UUID roleID, UUID AgentID, string FromAgentName);
        void RemoveAgentInvite(UUID requestingAgentID, UUID inviteID);
        void AddAgentToRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID);
        void RemoveAgentFromRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID);
        
        GroupRecord GetGroupRecord(UUID requestingAgentID, UUID GroupID, string GroupName);
		GroupProfileData GetMemberGroupProfile(UUID requestingAgentID, UUID GroupID, UUID AgentID);
		GroupMembershipData GetGroupMembershipData(UUID requestingAgentID, UUID GroupID, UUID AgentID);
		bool RemoveAgentFromGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID);
        UUID GetAgentActiveGroup(UUID RequestingAgentID, UUID AgentID);
        GroupInviteInfo GetAgentToGroupInvite(UUID requestingAgentID, UUID inviteID);
        GroupMembersData GetAgentGroupMemberData(UUID requestingAgentID, UUID GroupID, UUID AgentID);
		GroupNoticeInfo GetGroupNotice(UUID requestingAgentID, UUID noticeID);
		
        List<GroupMembershipData> GetAgentGroupMemberships(UUID requestingAgentID, UUID AgentID);
        List<DirGroupsReplyData> FindGroups(UUID requestingAgentID, string search, int StartQuery, uint queryflags);
        List<GroupRolesData> GetAgentGroupRoles(UUID requestingAgentID, UUID AgentID, UUID GroupID);
        List<GroupRolesData> GetGroupRoles(UUID requestingAgentID, UUID GroupID);
        List<GroupMembersData> GetGroupMembers(UUID requestingAgentID, UUID GroupID);
        List<GroupRoleMembersData> GetGroupRoleMembers(UUID requestingAgentID, UUID GroupID);
        List<GroupNoticeData> GetGroupNotices(UUID requestingAgentID, UUID GroupID);
        List<GroupInviteInfo> GetGroupInvites(UUID requestingAgentID);
        void AddGroupProposal(UUID agentID, GroupProposalInfo info);
    }
}
