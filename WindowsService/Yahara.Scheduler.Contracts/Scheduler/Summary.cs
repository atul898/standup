using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Yahara.Scheduler.Contracts.Scheduler
{
    [DataContract]
    public class Summary
    {
        [DataMember]
        public DateTime StartDate { get; set; }

        [DataMember]
        public DateTime EndDate { get; set; }

        [DataMember]
        public double BillableHoursScheduled { get; set; }

        [DataMember]
        public double TotalHoursScheduled { get; set; }
    }
}
