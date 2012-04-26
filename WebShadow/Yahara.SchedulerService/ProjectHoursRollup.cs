using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yahara.SchedulerService
{
    public class ProjectHoursRollup
    {
        public string ResourceID { get; set; }
        public int ProjectID { get; set; }
        public int ClientID { get; set; }
        public double BillableHours { get; set; }
        public double NonBillableHours { get; set; }
        public double HoursScheduled { get; set; }


        public double TotalHoursRealized
        {
            get
            {
                return BillableHours + NonBillableHours;
            }
        }

        public bool IsScheduled
        {
            get
            {
                return HoursScheduled > 0;
            }
        }


        public class ProjectIdComparer : IEqualityComparer<ProjectHoursRollup> 
        {

            public bool Equals(ProjectHoursRollup x, ProjectHoursRollup y)
            {
                return x.ProjectID.Equals(y.ProjectID);
            }

            public int GetHashCode(ProjectHoursRollup obj)
            {
                return obj.ProjectID.GetHashCode();
            }
        }
    }
}
