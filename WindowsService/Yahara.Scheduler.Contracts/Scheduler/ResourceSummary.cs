using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Yahara.Scheduler.Contracts.Scheduler
{
    [DataContract]
    public class ResourceSummary : Summary
    {
        [DataMember]
        public Resource Resource { get; set; }

        [DataMember]
        public List<ScheduledAssignment> Assignments { get; set; }

        [DataMember]
        public double TotalHoursRecorded { get; set; }

        public ResourceSummary()
        {
            Assignments = new List<ScheduledAssignment>();
        }
    }
}
