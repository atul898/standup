using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Yahara.Scheduler.Contracts.Scheduler
{
    [DataContract]
    public class ScheduledAssignment
    {       
        [DataMember]
        public Resource AssignedResource { get; set; }

        [DataMember]
        public Project AssignedProject { get; set; }
        
        [DataMember]
        public double HoursScheduled { get; set; }    

        [DataMember]
        public double HoursRecorded { get; set; }

        [DataMember]
        public bool IsScheduled { get; set; }



        public class ScheduledFirstSorter : IComparer<ScheduledAssignment>
        {

            public int Compare(ScheduledAssignment x, ScheduledAssignment y)
            {
                return x.IsScheduled.CompareTo(y.IsScheduled);
            }
        }
    }
}
