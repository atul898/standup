using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Yahara.Scheduler.Contracts
{
    [DataContract]
    public class Project
    {
        [DataMember]
        public int ProjectId { get; set; }

        [DataMember]
        public string ProjectName { get; set; }

        [DataMember]
        public Client Parent { get; set; }

        [DataMember]
        public double HoursEstimated { get; set; }

        [DataMember]
        public double HoursRecorded { get; set; }
    }
}
