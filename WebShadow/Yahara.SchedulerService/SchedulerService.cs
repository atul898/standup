using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Yahara.Scheduler.Contracts;
using Yahara.Scheduler.Contracts.Scheduler;
using Yahara.SchedulerModel;
using System.Collections;

namespace Yahara.SchedulerService
{
    /// <summary>
    /// Refactor this whole messs.
    /// </summary>
    public class SchedulerService : ISchedulerService
    {
        private Dictionary<int, Project> projects;
        private Dictionary<int, Client> clients;
        private Dictionary<string, Resource> resources;

        public SchedulerService()
        {
            projects = new Dictionary<int, Project>();
            clients = new Dictionary<int, Client>();
            resources = new Dictionary<string, Resource>();
        }

        /// <summary>
        /// Build a forecast all nasty style.  Refactor this later when we know what we really want.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public ForecastSummary GetForecast(DateTime start, DateTime end)
        {
            using (var context = new SchedulingEntities())
            {
                var results = new List<ResourceSummary>();
                string currentUser = string.Empty;

                ResourceSummary current = null;
                
                var scheduleView = GetScheduleView(context, start, end);
                var resourceIds = (from item in scheduleView select item.EmployeeID).Distinct();

                foreach (var resourceId in resourceIds )
                {
                    // for each distinct resource set up a ResourceSummary
                    if (current == null || current.Resource == null || current.Resource.UserId != resourceId)
                    {
                        // The summary view has some generic stuff we need the first time we set up a dude
                        var viewRecord = scheduleView.Where(v => v.EmployeeID == resourceId).First();

                        current = new ResourceSummary();
                        results.Add(current);
                        current.Resource = GetResource(viewRecord);
                        current.StartDate = viewRecord.Week;
                    }
                   

                    // Next we need to take a combination of what the resource was scheduled for, against what they have recorded in webshadow.
                    Dictionary<int, ProjectHoursRollup> projectHours = GetProjectHoursRollup(context, resourceId, start, end).ToDictionary( dk => dk.ProjectID) ;
                    foreach (var scheduleItem in (from sv in scheduleView where sv.EmployeeID == resourceId select sv))
                    {
                        int id = int.Parse(scheduleItem.AppProjectID);

                        if (projectHours.ContainsKey(id))
                        {
                            projectHours[id].HoursScheduled = (double)scheduleItem.ForecastHours;
                        }
                        else
                        {
                            projectHours.Add(id,
                                new ProjectHoursRollup()
                                {
                                    BillableHours = 0,
                                    NonBillableHours = 0,
                                    HoursScheduled = (double)scheduleItem.ForecastHours,
                                    ProjectID = id,
                                    ResourceID = resourceId,
                                    ClientID = scheduleItem.ClientID
                                });
                        }
                    }


                    foreach (var projectItem in projectHours.Values.OrderBy( ph => ph.IsScheduled ))
                    {
                        current.TotalHoursRecorded += projectItem.TotalHoursRealized ;
                        current.TotalHoursScheduled += projectItem.HoursScheduled;
                        current.BillableHoursScheduled += projectItem.BillableHours;

                        current.Assignments.Add(new ScheduledAssignment()
                        {
                            AssignedProject = GetProject(context, projectItem.ProjectID),
                            AssignedResource = current.Resource,
                            HoursScheduled = projectItem.HoursScheduled,
                            HoursRecorded = projectItem.TotalHoursRealized,
                            IsScheduled = projectItem.IsScheduled
                        });
                    }
                }

                var summary = new ForecastSummary()
                                  {
                                      BillableHoursScheduled = (from rs in results select rs.BillableHoursScheduled).Sum(),
                                      TotalHoursScheduled = (from rs in results select rs.TotalHoursScheduled).Sum(),
                                      EndDate = end,
                                      StartDate = start,
                                      Resources = results
                                  };


                ScheduledAssignment.ScheduledFirstSorter sorter = new ScheduledAssignment.ScheduledFirstSorter();
                foreach (var rs in summary.Resources)
                {
                    rs.Assignments.Sort(sorter);
                }
                
                return summary;
            }
        }

        private Resource GetResource(SchedulerView view)
        {
            if (!resources.ContainsKey(view.UserID))
            {
                resources.Add(view.UserID, new Resource()
                {
                    EmailAddress = view.EmailAddress,
                    FirstName = view.FirstName,
                    LastName = view.LastName,
                    UserId = view.UserID
                });
            }

            return resources[view.UserID];
        }

        private Project GetProject(SchedulingEntities context, int ProjectID)
        {
            if (!projects.ContainsKey(ProjectID))
            {
                var project = (from p in context.ProjectView where p.ShadowPID == ProjectID select p).FirstOrDefault();
                if (project == null)
                {
                    throw new Exception(string.Format("Invalid ProjectID: {0}", ProjectID));
                }

                projects.Add(ProjectID, new Project()
                                                 {
                                                     ProjectId = ProjectID,
                                                     ProjectName = project.Name,
                                                     Parent = GetClient(context, project.ShadowCLID),
                                                     HoursEstimated = project.EstimatedDuration ?? 0.0d,
                                                     HoursRecorded = GetAllTimeForProject(context, ProjectID)
                                                 });
            }

            return projects[ProjectID];
        }

        private Client GetClient(SchedulingEntities context, int ClientID)
        {
            if (!clients.ContainsKey(ClientID))
            {
                var client = (from c in context.ClientView where c.ShadowCLID == ClientID select c).FirstOrDefault();
                if(client == null)
                {
                    throw new Exception(string.Format("Invalid Client ID: {0}", ClientID));
                }


                clients.Add(ClientID, new Client()
                {
                    ClientId = ClientID,
                    ClientName = client.Name
                });
            }

            return clients[ClientID];
        }



        /*
        private double GetHoursForProjectByResource(SchedulingEntities context, int ProjectId, string resourceId, DateTime start, DateTime end)
        {
            // Yes... the performance here is terribad. Fixme.  Also validate that the logic is correct.
            var query = from te in context.TimeEntryView
                        where te.EmployeeID == resourceId
                        && (te.H1 == ProjectId
                        || te.H2 == ProjectId
                        || te.H3 == ProjectId
                        || te.H4 == ProjectId
                        || te.H5 == ProjectId
                        || te.H6 == ProjectId
                        || te.H7 == ProjectId
                        || te.H8 == ProjectId)
                        && (te.EntryDate.Value >= start     // Should have logic errors at the edge cases, 
                        && te.EntryDate.Value <= end)       // need to make sure this matches on date alone without time component.
                        select te.EntryTime;

            return SumTimeEntries(query);
        }
        */

        private static double GetAllTimeForProject(SchedulingEntities context, int ProjectID)
        {
            var query = from te in context.TimeEntryView
                        where te.H1 == ProjectID
                        || te.H2 == ProjectID
                        || te.H3 == ProjectID
                        || te.H4 == ProjectID
                        || te.H5 == ProjectID
                        || te.H6 == ProjectID
                        || te.H7 == ProjectID
                        || te.H8 == ProjectID
                        select te.EntryTime;

            return SumTimeEntries(query);
        }

        private static double SumTimeEntries(IQueryable<Nullable<double>> query)
        {
            double sum = 0.0;
            foreach (var item in query)
            {
                if (item.HasValue)
                    sum += item.Value;
            }

            return sum; // Values from webShadow are in minutes
        }

        public List<SchedulerView> GetScheduleView(SchedulingEntities context, DateTime start, DateTime end)
        {
            var query = from row in context.SchedulerView
                        where row.Week >= start.Date && row.Week <= end.Date
                        orderby row.UserID
                        select row;

            return query.ToList();
        }

        public IList<ProjectHoursRollup> GetProjectHoursRollup(SchedulingEntities context, string resourceId, DateTime start, DateTime end)
        {
            // Let's see how well this works if we say that generally, we want H1 for this and don't want any further detail.
            // This should give us the highest level project for this time entry.  For instance, we would fetch the Product Development PID under Yahara Software LLC
            var query = from entries in context.yps_GetTimeEntriesInRange(resourceId, start, end)
                        //group entries by entries.ShadowPID into egrp
                        group entries by entries.H1 ?? -1 into egrp
                        select new ProjectHoursRollup {
                            ProjectID = egrp.Key,
                            ResourceID = resourceId,
                            ClientID = (from i in egrp select i).FirstOrDefault().ShadowCLID,
                            BillableHours = (from i in egrp where i.Billable select i.Time ?? 0).Sum(),
                            NonBillableHours = (from i in egrp where !i.Billable select i.Time ?? 0).Sum(),
                        };

            return query.ToList();
        }
    }
}
