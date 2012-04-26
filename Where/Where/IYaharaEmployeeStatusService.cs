using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Collections;
using Yahara.Scheduler.Contracts.Scheduler;

namespace Where
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract(Name="YaharaEmployeeStatusService",Namespace="Where")]     
    public interface IYaharaEmployeeStatusService
    {
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

        /// <summary>
        /// Returns an object graph representing resource assignments over a date range.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        //[OperationContract]
        //[WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "Json/GetForecast?startdate={start}&enddate={end}")]
        ForecastSummary GetForecast(string start, string end); //mmddyyyy format
    }

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
}
