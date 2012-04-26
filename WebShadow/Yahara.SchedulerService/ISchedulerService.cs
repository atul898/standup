using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Yahara.Scheduler.Contracts.Scheduler;

namespace Yahara.SchedulerService
{
    [ServiceContract]
    public interface ISchedulerService
    {
        /// <summary>
        /// Returns an object graph representing resource assignments over a date range.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [OperationContract]
        ForecastSummary GetForecast(DateTime start, DateTime end);
    }
}
