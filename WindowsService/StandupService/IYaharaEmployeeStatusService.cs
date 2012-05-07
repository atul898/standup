using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Runtime.Serialization;
using Yahara.Standup.Helper;
using Yahara.Scheduler.Contracts.Scheduler;

namespace Yahara.Standup
{
    [ServiceContract(Name = "Yahara.Standup.YaharaEmployeeStatusService", Namespace = "Where")]
    public interface IYaharaEmployeeStatusService
    {
        #region Operation Contracts

        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "Json/GetStatus?date={date}")]
        Status GetStatus(string date); //mmddyyyy format

        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "Json/WelcomeMessage?message={message}")]
        WrappedBool WelcomeMessage(string message);

        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "Json/ReadCurrentMessage")]
        WrappedString ReadCurrentMessage();

        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "Json/GetEmployeeTargetProcessSummary?date={date}")]
        Summary GetEmployeeTargetProcessSummary(string date); //mmddyyyy format

        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "Json/GetEmployeeWebShadowSummary?date={date}")]
        Summary GetEmployeeWebShadowSummary(string date); //mmddyyyy format

        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "Json/TransferLocationInfo?clientName={clientName}&latitude={latitude}&longitude={longitude}")]
        LocationSummary TransferLocationInfo(string clientName, string latitude, string longitude); //mmddyyyy format

        /// <summary>
        /// Returns an object graph representing resource assignments over a date range.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        //[OperationContract]
        //[WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "Json/GetForecast?startdate={start}&enddate={end}")]
        ForecastSummary GetForecast(string start, string end); //mmddyyyy format

        #endregion
    }

    #region Data Contracts

    [DataContract]
    public class WrappedBool
    {
        bool success = false;
        [DataMember]
        public bool Success
        {
            get { return success; }
            set { success = value; }
        }
    }

    [DataContract]
    public class WrappedString
    {
        string message = string.Empty;
        [DataMember]
        public string Message
        {
            get { return message; }
            set { message = value; }
        }
    }


    [DataContract]
    public class Summary
    {
        public Summary(DateTime d)
        {
            listOfItems = new List<EmployeeDetail>();
            date = d.DateTimeToString();
            displayDate = d.ToString("D");
            timestamp = DateTime.Now;
        }

        List<EmployeeDetail> listOfItems = null;

        [DataMember]
        public List<EmployeeDetail> ListOfItems
        {
            get { return listOfItems; }
            set { listOfItems = value; }
        }

        string date = string.Empty;
        [DataMember]
        public string Date
        {
            get { return date; }
            set { date = value; }
        }

        string displayDate = string.Empty;
        [DataMember]
        public string DisplayDate
        {
            get { return displayDate; }
            internal set { displayDate = value; }
        }

        DateTime timestamp = DateTime.Now;
        //[DataMember]
        public DateTime Timestamp
        {
            get { return timestamp; }
            internal set { timestamp = value; }
        }
    }

    [DataContract]
    public class EmployeeDetail
    {
        string name = string.Empty;
        [DataMember]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        string email = string.Empty;
        [DataMember]
        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        int id = 0;
        [DataMember]
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        decimal totalHoursLogged = 0;
        [DataMember]
        public decimal TotalHoursLogged
        {
            get { return totalHoursLogged; }
            set { totalHoursLogged = value; }
        }
    }

    #region IComparer implementation

    /// <summary>
    /// Comparer helper class.
    /// Sort in order of time.
    /// </summary>
    public class EmployeeDetailComparer : IComparer<EmployeeDetail>
    {
        public int Compare(EmployeeDetail o1, EmployeeDetail o2)
        {
            EmployeeDetail p1 = o1 as EmployeeDetail;
            EmployeeDetail p2 = o2 as EmployeeDetail;

            if (p1.TotalHoursLogged < p2.TotalHoursLogged)
            {
                return 1;
            }
            else if (p1.TotalHoursLogged > p2.TotalHoursLogged)
            {
                return -1;
            }
            else
            {
                return 0;
            }

        }
    }
    #endregion

    [DataContract]
    public class Status
    {
        public Status(DateTime d)
        {
            listOfItems = new List<OutlookItem>();
            date = d.DateTimeToString();
            displayDate = d.ToString("D");
        }

        List<OutlookItem> listOfItems = null;

        [DataMember]
        public List<OutlookItem> ListOfItems
        {
            get { return listOfItems; }
            set { listOfItems = value; }
        }

        string date = string.Empty;
        [DataMember]
        public string Date
        {
            get { return date; }
            set { date = value; }
        }

        string displayDate = string.Empty;
        [DataMember]
        public string DisplayDate
        {
            get { return displayDate; }
            internal set { displayDate = value; }
        }
    }

    [DataContract]
    public class OutlookItem
    {
        string name = string.Empty;
        [DataMember]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        string subject = string.Empty;
        [DataMember]
        public string Subject
        {
            get { return subject; }
            set { subject = value; }
        }

        string messageBody = string.Empty;
        [DataMember]
        public string MessageBody
        {
            get { return messageBody; }
            set { messageBody = value; }
        }


        string messageHTMLBody = string.Empty;
        [DataMember]
        public string MessageHTMLBody
        {
            get { return messageHTMLBody; }
            set { messageHTMLBody = value; }
        }


        string displayTime = string.Empty;
        [DataMember]
        public string DisplayTime
        {
            get { return displayTime; }
            set { displayTime = value; }
        }
    }

    [DataContract]
    public class LocationSummary
    {
        public LocationSummary()
        {
            listOfItems = new List<Location>();
        }

        List<Location> listOfItems = null;

        [DataMember]
        public List<Location> ListOfItems
        {
            get { return listOfItems; }
            set { listOfItems = value; }
        }

        string displayDate = string.Empty;
        [DataMember]
        public string DisplayDate
        {
            get { return displayDate; }
            set { displayDate = value; }
        }

        //eg. [
        //      ['Bondi Beach', -33.890542, 151.274856, 4],
        //      ['Coogee Beach', -33.923036, 151.259052, 5],
        //      ['Cronulla Beach', -34.028249, 151.157507, 3],
        //      ['Manly Beach', -33.80010128657071, 151.28747820854187, 2],
        //      ['Maroubra Beach', -33.950198, 151.259302, 1]
        //    ]
        string allLocationsForMap = string.Empty;
        [DataMember]
        public string AllLocationsForMap
        {
            get { return allLocationsForMap; }
            set { allLocationsForMap = value; }
        }

        public void UpdateAge()
        {
            DateTime dt = DateTime.Now;
            foreach (Location l in listOfItems)
            {
                TimeSpan t = dt.Subtract(l.Timestamp);
                if (t >= new TimeSpan(1, 0, 0, 0))
                    l.Age = t.Days.ToString() + " days";
                else if (t >= new TimeSpan(1, 0, 0))
                    l.Age = t.Hours.ToString() + " hours";
                else if (t >= new TimeSpan(0, 1, 0))
                    l.Age = t.Minutes.ToString() + " minutes";
                else if (t >= new TimeSpan(0, 0, 1))
                    l.Age = t.Seconds.ToString() + " seconds";
                else
                    l.Age = "0 seconds";
            }
        }
    }

    [DataContract]
    public class Location
    {
        string latitude = string.Empty;
        [DataMember]
        public string Latitude
        {
            get { return latitude; }
            set { latitude = value; }
        }

        string longitude = string.Empty;
        [DataMember]
        public string Longitude
        {
            get { return longitude; }
            set { longitude = value; }
        }

        string clientName = string.Empty;
        [DataMember]
        public string ClientName
        {
            get { return clientName; }
            set { clientName = value; }
        }

        string realName = string.Empty;
        [DataMember]
        public string RealName
        {
            get { return realName; }
            set { realName = value; }
        }

        DateTime timestamp = DateTime.Now;
        public DateTime Timestamp
        {
            get { return timestamp; }
            set { timestamp = value; }
        }

        string age = string.Empty;
        [DataMember]
        public string Age
        {
            get { return age; }
            set { age = value; }
        }

        string link = string.Empty;
        [DataMember]
        public string Link
        {
            get { return link; }
            set { link = value; }
        }

        string distance = string.Empty;
        [DataMember]
        public string Distance
        {
            get { return distance; }
            set { distance = value; }
        }
    }

    #endregion
}
