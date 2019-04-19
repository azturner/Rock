﻿using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Rock.Data;

namespace church.ccv.CCVCore.PlanAVisit.Model
{
    [Table( "_church_ccv_PlanAVisit_Visit ")]
    [DataContract]
    public class PlanAVisit : Entity<PlanAVisit>, IRockEntity
    {
        [DataMember]
        public int PersonAliasId { get; set; }

        [DataMember]
        public int FamilyId { get; set; }

        [DataMember]
        public int CampusId { get; set; }

        [DataMember]
        public DateTime? ScheduledDate { get; set; }

        [DataMember]
        public DateTime? AttendedDate { get; set; }

        [DataMember]
        public int ServiceTimeScheduleId { get; set; }

        [DataMember]
        public bool BringingSpouse { get; set; }

        [DataMember]
        public bool BringingChildren { get; set; }

        [DataMember]
        public string SurveyResponse { get; set; }

        [DataMember]
        public DateTime CreatedDateTime { get; set; }

        [DataMember]
        public DateTime ModifiedDateTime { get; set; }

        [DataMember]
        public int CreatedByPersonAliasId { get; set; }

        [DataMember]
        public int ModifiedByPersonAliasId { get; set; }
    }
}
