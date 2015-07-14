//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the Rock.CodeGeneration project
//     Changes to this file will be lost when the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Linq;

using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// ConnectionOpportunity Service class
    /// </summary>
    public partial class ConnectionOpportunityService : Service<ConnectionOpportunity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionOpportunityService"/> class
        /// </summary>
        /// <param name="context">The context.</param>
        public ConnectionOpportunityService(RockContext context) : base(context)
        {
        }

        /// <summary>
        /// Determines whether this instance can delete the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>
        ///   <c>true</c> if this instance can delete the specified item; otherwise, <c>false</c>.
        /// </returns>
        public bool CanDelete( ConnectionOpportunity item, out string errorMessage )
        {
            errorMessage = string.Empty;
 
            if ( new Service<ConnectionRequestActivity>( Context ).Queryable().Any( a => a.ConnectionOpportunityId == item.Id ) )
            {
                errorMessage = string.Format( "This {0} is assigned to a {1}.", ConnectionOpportunity.FriendlyTypeName, ConnectionRequestActivity.FriendlyTypeName );
                return false;
            }  
            return true;
        }
    }

    /// <summary>
    /// Generated Extension Methods
    /// </summary>
    public static partial class ConnectionOpportunityExtensionMethods
    {
        /// <summary>
        /// Clones this ConnectionOpportunity object to a new ConnectionOpportunity object
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="deepCopy">if set to <c>true</c> a deep copy is made. If false, only the basic entity properties are copied.</param>
        /// <returns></returns>
        public static ConnectionOpportunity Clone( this ConnectionOpportunity source, bool deepCopy )
        {
            if (deepCopy)
            {
                return source.Clone() as ConnectionOpportunity;
            }
            else
            {
                var target = new ConnectionOpportunity();
                target.CopyPropertiesFrom( source );
                return target;
            }
        }

        /// <summary>
        /// Copies the properties from another ConnectionOpportunity object to this ConnectionOpportunity object
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="source">The source.</param>
        public static void CopyPropertiesFrom( this ConnectionOpportunity target, ConnectionOpportunity source )
        {
            target.Id = source.Id;
            target.ConnectionTypeId = source.ConnectionTypeId;
            target.ConnectorGroupId = source.ConnectorGroupId;
            target.Description = source.Description;
            target.GroupMemberRoleId = source.GroupMemberRoleId;
            target.GroupMemberStatusId = source.GroupMemberStatusId;
            target.GroupTypeId = source.GroupTypeId;
            target.IconCssClass = source.IconCssClass;
            target.IsActive = source.IsActive;
            target.Name = source.Name;
            target.PhotoId = source.PhotoId;
            target.PublicName = source.PublicName;
            target.UseAllGroupsOfType = source.UseAllGroupsOfType;
            target.CreatedDateTime = source.CreatedDateTime;
            target.ModifiedDateTime = source.ModifiedDateTime;
            target.CreatedByPersonAliasId = source.CreatedByPersonAliasId;
            target.ModifiedByPersonAliasId = source.ModifiedByPersonAliasId;
            target.Guid = source.Guid;
            target.ForeignId = source.ForeignId;

        }
    }
}
