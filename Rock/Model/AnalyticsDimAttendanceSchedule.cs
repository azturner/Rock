﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// AnalyticsDimAttendanceSchedule is a SQL View off of the Schedule table
    /// </summary>
    [Table( "AnalyticsDimAttendanceSchedule" )]
    [DataContract]
    [HideFromReporting]
    public class AnalyticsDimAttendanceSchedule
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets the schedule identifier.
        /// </summary>
        /// <value>
        /// The schedule identifier.
        /// </value>
        [Key]
        public int ScheduleId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [MaxLength( 250 )]
        [DataMember( IsRequired = true )]
        public string Name { get; set; }

        #endregion
    }
}