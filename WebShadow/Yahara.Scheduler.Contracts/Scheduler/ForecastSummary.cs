using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Yahara.Scheduler.Contracts.Scheduler
{
    
    [DataContract]
    public class ForecastSummary : Summary
    {
        [DataMember]
        public List<ResourceSummary> Resources { get; set; }

        public ForecastSummary()
        {
            Resources = new List<ResourceSummary>();
        }
    }
}
