using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Diagnostics;
using Yahara.Standup.Helper;
using System.Net;
using System.Web.Script.Serialization;
using System.IO;
using System.Runtime.InteropServices;
using Yahara.Scheduler.Contracts;
using Yahara.Scheduler.Contracts.Scheduler;
using Yahara.SchedulerModel;
using System.Device.Location;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;

namespace Yahara.Standup
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
                 ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class YaharaEmployeeStatusService : IYaharaEmployeeStatusService
    {
        public const int NumberOfPastDaysToCache = 30;
        public const int NumberOfFutureDaysToCache = 30;

        private static string sSource;
        private static string sLog;

        private Dictionary<int, Project> projects;
        private Dictionary<int, Client> clients;
        private Dictionary<string, Resource> resources;

        //for sync
        private readonly static object _doOutlookWorkSync = new object();
        private readonly static object _doTPWorkSync = new object();
        private readonly static object _doWSWorkSync = new object();
        private readonly static object _doWelcomeMessageWorkSync = new object();
        private readonly static object _doTransferLocationInfoWorkSync = new object();
        
        
        #region Public Properties
        //we will cache dailty status data (email/calender/outlook) in this
        private static List<Status> listStatus;
        public static List<Status> ListStatus
        {
            get { return YaharaEmployeeStatusService.listStatus; }
            set { YaharaEmployeeStatusService.listStatus = value; }
        }

        private static List<Summary> listTPSummary;
        public static List<Summary> ListTPSummary
        {
            get { return YaharaEmployeeStatusService.listTPSummary; }
            set { YaharaEmployeeStatusService.listTPSummary = value; }
        }

        private static List<Summary> listWSSummary;
        public static List<Summary> ListWSSummary
        {
            get { return YaharaEmployeeStatusService.listWSSummary; }
            set { YaharaEmployeeStatusService.listWSSummary = value; }
        }

        //location related
        private static LocationSummary _locationSummary;
        public static LocationSummary LocationSummary
        {
            get
            {
                if (_locationSummary == null)
                    _locationSummary = new LocationSummary();
                return YaharaEmployeeStatusService._locationSummary;
            }
        }
        #endregion

        #region Constructor

        private YaharaEmployeeStatusService()
        {
            sSource = "YaharaEmployeeStatusService";
            sLog = "Application";

            if (!EventLog.SourceExists(sSource))
                EventLog.CreateEventSource(sSource, sLog);

            EventLog.WriteEntry(sSource, "In constructor of YaharaEmployeeStatusService", EventLogEntryType.Information, 234);

            projects = new Dictionary<int, Project>();
            clients = new Dictionary<int, Client>();
            resources = new Dictionary<string, Resource>();

            ListTPSummary = new List<Summary>();
            ListWSSummary = new List<Summary>();
        }

        #endregion

        #region Implementaion of IYaharaEmployeeStatusService

        public Status GetStatus(string strDate) //mmddyyyy
        {
            DateTime date = strDate.StringToDateTime();

            //'March 05, 2012 12:00 AM'
            Status status = DoWork(date);

            return status;
        }

        public WrappedBool WelcomeMessage(string message)
        {
            WrappedBool b = new WrappedBool();
            try
            {
                lock (_doWelcomeMessageWorkSync)
                {
                    StringBuilder newFile = new StringBuilder();

                    string[] file = System.IO.File.ReadAllLines(@"\\10.111.124.47\c$\RecBoard\Welcome.txt");

                    string[] splitC = { "<br />" };
                    string[] ls = message.Split(splitC, StringSplitOptions.None);

                    foreach (string s in ls)
                    {
                        newFile.Append(s + "\r\n");
                    }

                    newFile.Append("\r\n\r\n\r\n\r\n\r\n");

                    foreach (string line in file)
                    {
                        newFile.Append(line + "\r\n");
                    }
                    System.IO.File.WriteAllText(@"\\10.111.124.47\c$\RecBoard\Welcome.txt", newFile.ToString());
                }
            }
            catch
            {
                b.Success = false;
                return b;
            }
            b.Success = true;
            return b;
        }

        public WrappedString ReadCurrentMessage()
        {
            WrappedString s = new WrappedString();
            StringBuilder returnString = new StringBuilder();
            try
            {
                string[] file = System.IO.File.ReadAllLines(@"\\10.111.124.47\c$\RecBoard\Welcome.txt");

                int lines = 0;
                //get first 5 lines
                foreach (string line in file)
                {
                    lines++;
                    line.Replace("  ", " &nbsp;");
                    returnString.Append(line + "<br>");
                    if (lines == 5)
                        break;
                }
            }
            catch
            {
                s.Message = "Could not read/retreive welcome message!";
                return s;
            }

            s.Message = returnString.ToString();
            return s;
        }

        public Summary GetEmployeeTargetProcessSummary(string strDate) //mmddyyyy format
        {
            var s = (from l in ListTPSummary
                     where (l.Date == strDate)
                     select l).FirstOrDefault();

            if (s != null)
            {
                //Is the data older than 10 minutes
                if (DateTime.Now.Subtract(s.Timestamp).CompareTo(new TimeSpan(0, 10, 0)) > 1)
                {
                    ListTPSummary.Remove(s);
                    s = TargetProcessSummary(strDate);
                    ListTPSummary.Add(s);
                }
            }
            else
            {
                s = TargetProcessSummary(strDate);
                ListTPSummary.Add(s);
            }
            return s;
        }

        public Summary GetEmployeeWebShadowSummary(string strDate) //mmddyyyy format
        {
            var s = (from l in ListWSSummary
                     where (l.Date == strDate)
                     select l).FirstOrDefault();

            if (s != null)
            {
                //Is the data older than 10 minutes
                if (DateTime.Now.Subtract(s.Timestamp).CompareTo(new TimeSpan(0, 10, 0)) > 1)
                {
                    ListWSSummary.Remove(s);
                    s = WebShadowSummary(strDate);
                    ListWSSummary.Add(s);
                }
            }
            else
            {
                s = WebShadowSummary(strDate);
                ListWSSummary.Add(s);
            }
            return s;
        }
        
        public LocationSummary TransferLocationInfo(string clientName, string latitude, string longitude)
        {
            try
            {
                lock (_doTransferLocationInfoWorkSync)
                {
                    string realName = string.Empty;
                    try
                    {
                        if (string.IsNullOrEmpty(clientName))
                            throw new ApplicationException("invalid client name");
                        var loc = new GeoCoordinate(double.Parse(latitude), double.Parse(longitude));

                        //Let us find the real name
                        if (ListTPSummary.Count > 0 && ListTPSummary[0].ListOfItems != null && ListTPSummary[0].ListOfItems.Count>0)
                        {
                            foreach (EmployeeDetail ed in ListTPSummary[0].ListOfItems)
                            {
                                if (ed.Email.ToLower().Contains(clientName.ToLower()))
                                {
                                    realName = ed.Name;
                                    continue;
                                }
                            }
                        }
                    }
                    catch
                    {
                        LocationSummary ls = new LocationSummary();
                        Location l = new Location();
                        l.ClientName = "No cookie for you!";
                        ls.ListOfItems.Add(l);
                        return ls;
                    }

                    bool clientFound = false;
                    //Find if this clientName exists in the list
                    foreach (Location l in LocationSummary.ListOfItems)
                    {
                        if (l.ClientName.ToLower() == clientName.ToLower())
                        {
                            l.Latitude = latitude;
                            l.Longitude = longitude;
                            l.Timestamp = DateTime.Now;
                            l.RealName = realName;
                            clientFound = true;
                            break;
                        }
                    }

                    //This is a new client
                    if (clientFound == false)
                    {
                        Location newLocation = new Location();
                        newLocation.Longitude = longitude;
                        newLocation.Latitude = latitude;
                        newLocation.ClientName = clientName;
                        newLocation.Timestamp = DateTime.Now; //DateTime.Now.ToString("D");
                        newLocation.Link = @"http://maps.google.com/maps?q=" + clientName + @"@" + latitude + "," + longitude;
                        LocationSummary.ListOfItems.Add(newLocation);
                    }

                    var selfCoord = new GeoCoordinate(double.Parse(latitude), double.Parse(longitude));
                    foreach (Location l in LocationSummary.ListOfItems)
                    {
                        var otherCoord = new GeoCoordinate(double.Parse(l.Latitude), double.Parse(l.Longitude));
                        var distance = selfCoord.GetDistanceTo(otherCoord);
                        var distanceInMiles = 0.000621371192 * distance;
                        if (distanceInMiles >= 1)
                            l.Distance = distanceInMiles.ToString("F") + " miles";
                        else
                            l.Distance = (distanceInMiles * 1760).ToString("F") + " yards";
                    }

                    LocationSummary.DisplayDate = DateTime.Now.ToString("D");
                    LocationSummary.UpdateAge();

                    //Map display related info
                    //Step 1: fill up AllLocationsForMap
                    //[
                    //  ['Bondi Beach', -33.890542, 151.274856, 4],
                    //  ['Coogee Beach', -33.923036, 151.259052, 5],
                    //  ['Cronulla Beach', -34.028249, 151.157507, 3],
                    //  ['Manly Beach', -33.80010128657071, 151.28747820854187, 2],
                    //  ['Maroubra Beach', -33.950198, 151.259302, 1]
                    //]

                    StringBuilder st = new StringBuilder();
                    int count = 0;

                    var arr = new List<object>();
                    foreach (Location l in LocationSummary.ListOfItems)
                    {
                        if(string.IsNullOrEmpty(l.RealName))
                            st.Append("['" + l.ClientName + "'," + l.Latitude + "," + l.Longitude + "," + (++count).ToString() + "]");
                        else
                            st.Append("['" + l.RealName + "'," + l.Latitude + "," + l.Longitude + "," + (++count).ToString() + "]");
                        if (count < LocationSummary.ListOfItems.Count())
                            st.Append(",");
                    }
                    LocationSummary.AllLocationsForMap = "[" + Environment.NewLine + st.ToString() + Environment.NewLine + "]";
                }
            }
            catch
            {
                LocationSummary ls = new LocationSummary();
                Location l = new Location();
                l.ClientName = "No cookie for you!";
                ls.ListOfItems.Add(l);
                return ls;
            }
            return LocationSummary;
        }

        #endregion

        #region Private Methods

        private Summary TargetProcessSummary(string strDate) //mmddyyyy format
        {
            Summary summary = null;
            try
            {
                lock (_doTPWorkSync)
                {
                    DateTime requestedDate = strDate.StringToDateTime();
                    DateTime previousMonday = requestedDate.StartOfWeek(DayOfWeek.Monday);
                    previousMonday = previousMonday.AddDays(-1); //hack
                    requestedDate = requestedDate.AddDays(1); // & hack to get the number right (as they should be)

                    //Step 1 : Collect all emails
                    summary = new Summary(strDate.StringToDateTime());
                    var webClient = new WebClient();
                    //webClient.Headers.Add("Authorization", "Basic YXR1bGM6dGVzdGluZw=="); //atul's key
                    webClient.Headers.Add("Authorization", "Basic a2V2aW5tOlJVQ3Jhenky"); //kevin's key 
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    jss.RegisterConverters(new JavaScriptConverter[] { new DynamicJsonConverter() });

                    var nextLink = "http://tp.yaharasoftware.com/api/v1/Users?format=json";

                    while (nextLink != null)
                    {
                        Stream data = webClient.OpenRead(nextLink);
                        StreamReader reader = new StreamReader(data);
                        string json = reader.ReadToEnd();
                        //Debug.WriteLine(json);
                        data.Close();
                        reader.Close();

                        dynamic tpEntry = jss.Deserialize(json, typeof(object)) as dynamic;
                        int count = tpEntry.Items.Count;
                        for (int i = 0; i < count; i++)
                        {
                            try
                            {
                                string email = tpEntry.Items[i]["Email"].ToLower();
                                if (!email.Contains("yahara") || tpEntry.Items[i]["IsActive"] == false)
                                    continue;

                                EmployeeDetail ed = new EmployeeDetail();
                                ed.Id = tpEntry.Items[i]["Id"];
                                ed.Name = tpEntry.Items[i]["FirstName"] + " " + tpEntry.Items[i]["LastName"];
                                ed.Email = tpEntry.Items[i]["Email"];
                                summary.ListOfItems.Add(ed);
                            }
                            catch
                            {
                                //ignore and move on
                            }
                        }
                        try
                        {
                            if (tpEntry.Next != null)
                                nextLink = tpEntry.Next;
                            else
                                nextLink = null;
                        }
                        catch (KeyNotFoundException)
                        {
                            nextLink = null;
                        }
                    }

                    //Step 2 : Collect all times for this week
                    //var nextTimeLink = "http://tp.yaharasoftware.com/api/v1/Times?format=json&take=1000&where=(Date%20gt%20'2012-04-16')%20and%20(Date%20lt%20'2012-04-19')";
                    var nextTimeLink = "http://tp.yaharasoftware.com/api/v1/Times?format=json&take=1000&where=(Date%20gt%20'"
                                        + previousMonday.ToString("yyyy-MM-dd")
                                        + "')%20and%20(Date%20lt%20'"
                                        + requestedDate.ToString("yyyy-MM-dd")
                                        + "')";

                    while (nextTimeLink != null)
                    {
                        Stream data = webClient.OpenRead(nextTimeLink);
                        StreamReader reader = new StreamReader(data);
                        string json = reader.ReadToEnd();
                        //Debug.WriteLine(json);
                        data.Close();
                        reader.Close();

                        dynamic tpEntry = jss.Deserialize(json, typeof(object)) as dynamic;
                        int count = tpEntry.Items.Count;
                        for (int i = 0; i < count; i++)
                        {
                            try
                            {
                                int Id = tpEntry.Items[i]["User"]["Id"];
                                var ed = (from l in summary.ListOfItems where l.Id == Id select l).FirstOrDefault();
                                if (ed == null)
                                    continue;

                                DateTime dateTimeSpentOn = tpEntry.Items[i]["Date"];
                                decimal timeSpent = tpEntry.Items[i]["Spent"];

                                if ((requestedDate.GetEndOfDay().CompareTo(dateTimeSpentOn) >= 0) && (previousMonday.Date.CompareTo(dateTimeSpentOn) <= 0))
                                {
                                    (from l in summary.ListOfItems where l.Id == Id select l).First().TotalHoursLogged += timeSpent;
                                }
                            }
                            catch
                            {
                                //ignore and move on
                            }
                        }
                        try
                        {
                            if (tpEntry.Next != null)
                                nextTimeLink = tpEntry.Next;
                            else
                                nextTimeLink = null;
                        }
                        catch (KeyNotFoundException)
                        {
                            nextTimeLink = null;
                        }
                    }

                    EmployeeDetailComparer dc = new EmployeeDetailComparer();
                    summary.ListOfItems.Sort(dc);

                }
            }
            catch
            {
                ;//Exception swallowing technology
            }
            return summary;
        }

        private Summary WebShadowSummary(string strDate) //mmddyyyy format
        {
            Summary summary = null;
            try
            {
                lock (_doTPWorkSync)
                {
                    DateTime requestedDate = strDate.StringToDateTime();
                    DateTime previousMonday = requestedDate.StartOfWeek(DayOfWeek.Monday);

                    ForecastSummary fs = GetForecast(previousMonday.DateTimeToString(), requestedDate.DateTimeToString());

                    summary = new Summary(strDate.StringToDateTime());
                    summary.ListOfItems = new List<EmployeeDetail>(fs.Resources.Count);

                    foreach (ResourceSummary rs in fs.Resources)
                    {
                        EmployeeDetail ed = new EmployeeDetail();
                        ed.TotalHoursLogged = (decimal)rs.TotalHoursRecorded;
                        ed.Name = rs.Resource.FirstName + " " + rs.Resource.LastName;
                        summary.ListOfItems.Add(ed);
                    }

                    EmployeeDetailComparer dc = new EmployeeDetailComparer();
                    summary.ListOfItems.Sort(dc);
                }
            }
            catch
            {
                ;//Exception Swallowing Technology
            }
            return summary;
        }

        private static Microsoft.Office.Interop.Outlook.Application GetApplicationObject()
        {

            Microsoft.Office.Interop.Outlook.Application application = null;

            // Check whether there is an Outlook process running.
            if (Process.GetProcessesByName("OUTLOOK").Count() > 0)
            {

                // If so, use the GetActiveObject method to obtain the process and cast it to an Application object.
                application = Marshal.GetActiveObject("Outlook.Application") as Microsoft.Office.Interop.Outlook.Application;
            }
            else
            {

                // If not, create a new instance of Outlook and log on to the default profile.
                application = new Microsoft.Office.Interop.Outlook.Application();
                Microsoft.Office.Interop.Outlook.NameSpace nameSpace = application.GetNamespace("MAPI");
                nameSpace.Logon("yahara\test", "test1234", System.Reflection.Missing.Value, System.Reflection.Missing.Value);
                //nameSpace = null;
            }

            // Return the Outlook Application object.
            return application;
        }

        internal static Status DoWork(DateTime date, bool reallyDoWork = false)
        {
            try
            {

                if (reallyDoWork == false) //avoid contacting outlook server in this case
                {
                    var s = (from l in ListStatus
                             where l.Date == date.DateTimeToString()
                             select l).FirstOrDefault();

                    if (s != null)
                    {
                        return s;
                    }
                    else
                    {
                        return new Status(date);
                    }
                }

                lock (_doOutlookWorkSync)
                {
                    Microsoft.Office.Interop.Outlook.Application objApp = new Microsoft.Office.Interop.Outlook.Application();
                    Microsoft.Office.Interop.Outlook.NameSpace objNsp = objApp.GetNamespace("MAPI");
                    //objNsp.Logon("yahara\test", "test1234", false, true);
                    //Microsoft.Office.Interop.Outlook.Application objApp = GetApplicationObject();
                    //Microsoft.Office.Interop.Outlook.NameSpace objNsp = objApp.GetNamespace("MAPI");

                    Status s1 = null;
                    Status s2 = null;

                    Status final = null;

                    //mail
                    Microsoft.Office.Interop.Outlook.MAPIFolder objMAPIFolder = objNsp.GetDefaultFolder(Microsoft.Office.Interop.Outlook.OlDefaultFolders.olFolderInbox);

                    // Set recepient
                    Microsoft.Office.Interop.Outlook.Recipient oRecip = (Microsoft.Office.Interop.Outlook.Recipient)objNsp.CreateRecipient("CompCal@yaharasoftware.com");

                    // Get calendar folder 
                    Microsoft.Office.Interop.Outlook.MAPIFolder calendarFolder = objNsp.GetSharedDefaultFolder(oRecip, Microsoft.Office.Interop.Outlook.OlDefaultFolders.olFolderCalendar);

                    if (ListStatus == null || ListStatus.Count == 0) // first call
                    {
                        ListStatus = new List<Status>(NumberOfPastDaysToCache + NumberOfFutureDaysToCache);

                        for (int i = -1 * NumberOfFutureDaysToCache; i < NumberOfPastDaysToCache + NumberOfFutureDaysToCache; i++)
                        {
                            DateTime specificDate = date.AddDays(-1 * i);
                            Debug.WriteLine("i=" + i.ToString() + " & specificDate=" + specificDate.DateTimeToString());
                            try
                            {
                                s1 = DownloadStatusForDate(specificDate, objMAPIFolder);
                                s2 = DownloadMeetingsForDate(specificDate, calendarFolder);
                                final = new Status(s1.Date.StringToDateTime());
                                if (s1 != null && s1.ListOfItems != null && s1.ListOfItems.Count > 0)
                                    final.ListOfItems.AddRange(s1.ListOfItems);
                                if (s2 != null && s2.ListOfItems != null && s2.ListOfItems.Count > 0)
                                    final.ListOfItems.AddRange(s2.ListOfItems);
                                ListStatus.Add(final);
                            }
                            catch
                            {
                                ListStatus.Add(new Status(specificDate));
                                ;//swallow and move on
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            s1 = DownloadStatusForDate(DateTime.Today, objMAPIFolder);
                            s2 = DownloadMeetingsForDate(DateTime.Today, calendarFolder);
                            final = new Status(s1.Date.StringToDateTime());
                            if (s1 != null && s1.ListOfItems != null && s1.ListOfItems.Count > 0)
                                final.ListOfItems.AddRange(s1.ListOfItems);
                            if (s2 != null && s2.ListOfItems != null && s2.ListOfItems.Count > 0)
                                final.ListOfItems.AddRange(s2.ListOfItems);

                            //find and remove stale entry
                            var s = (from l in ListStatus
                                     where l.Date == DateTime.Today.DateTimeToString()
                                     select l).FirstOrDefault();
                            if (s != null)
                                ListStatus.Remove(s);

                            //add fresh entry
                            ListStatus.Add(final);
                        }
                        catch
                        {
                            ;//swallow and move on
                        }
                    }



                    if (calendarFolder != null)
                    {
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(calendarFolder);
                        calendarFolder = null;
                    }

                    if (oRecip != null)
                    {
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oRecip);
                        oRecip = null;
                    }

                    if (objMAPIFolder != null)
                    {
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(objMAPIFolder);
                        objMAPIFolder = null;
                    }

                    objApp.Session.Logoff();
                    if (objApp.Session != null)
                    {
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(objApp.Session);
                    }

                    if (objNsp != null)
                    {
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(objNsp);
                        objNsp = null;
                    }

                    (objApp as Microsoft.Office.Interop.Outlook._Application).Quit();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    if (objApp != null)
                    {
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(objApp);
                        objApp = null;
                    }
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    var st = (from l in ListStatus
                              where l.Date == date.DateTimeToString()
                              select l).FirstOrDefault();
                    return st;
                }
            }
            catch (Exception e)
            {
                EventLog.WriteEntry(sSource, "Exception in YaharaEmployeeStatusService" + Environment.NewLine + "Message = " + e.Message
                    + Environment.NewLine + "StackTrace" + Environment.NewLine + e.StackTrace
                    , EventLogEntryType.Error, 234);
                return new Status(date);
                ;//Exception swallowing technology at work here
            }
        }

        private static Status DownloadStatusForDate(DateTime date, Microsoft.Office.Interop.Outlook.MAPIFolder objMAPIFolder)
        {
            Status status = new Status(date);

            string strDate1 = date.Date.ToString(@"MMMM dd, yyyy hh:mm tt");
            string strDate2 = date.Date.AddDays(1).ToString(@"MMMM dd, yyyy hh:mm tt");

            Microsoft.Office.Interop.Outlook.MailItem item = null;
            string sFilter = "[SentOn] >= '" + strDate1 + "'"
                            + " and " +
                            "[SentOn] <= '" + strDate2 + "'";

            Microsoft.Office.Interop.Outlook.Items restrictedFolder = objMAPIFolder.Items.Restrict(sFilter);
            Debug.WriteLine("Count after Restrict: {0}", restrictedFolder.Count);

            foreach (var v in restrictedFolder)
            {
                try
                {
                    item = (Microsoft.Office.Interop.Outlook.MailItem)v;

                    var subject = item.Subject.ToLower();
                    var body = item.Body.ToLower();

                    if (subject.Contains("work")
                        || subject.Contains("standup")
                        || subject.Contains("stand up")
                        || subject.Contains("wfh")
                        || subject.Contains("pto")
                        || subject.Contains("sick")
                        || subject.Contains("home")
                        || subject.Contains("leaving")
                        || subject.Contains("today")
                        || subject.Contains("late")
                        //|| body.Contains("standup")
                        //|| body.Contains("stand up")
                        //|| body.Contains("wfh")
                        //|| body.Contains("pto")
                        //|| body.Contains("sick")
                        //|| body.Contains("home")
                        //|| body.Contains("leaving")
                        )
                    {
                        Debug.WriteLine("Sent: {0} {1} {2}", item.SentOn.ToLongDateString(), item.SenderName, item.Subject);
                        OutlookItem c = new OutlookItem();
                        c.MessageBody = item.Body;
                        c.MessageHTMLBody = item.HTMLBody;
                        c.Subject = item.Subject;
                        c.Name = item.SenderName;
                        c.DisplayTime = item.SentOn.ToString("t");
                        status.ListOfItems.Add(c);
                    }

                }
                catch
                {
                    continue;
                }
                finally
                {
                    if (item != null)
                    {
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(item);
                        item = null;
                    }
                }
            }
            return status;
        }

        private static Status DownloadMeetingsForDate(DateTime date, Microsoft.Office.Interop.Outlook.MAPIFolder calendarFolder)
        {
            Status status = new Status(date);

            string strDate1 = date.Date.ToString(@"MMMM dd, yyyy hh:mm tt");
            string strDate2 = date.Date.AddDays(1).ToString(@"MMMM dd, yyyy hh:mm tt");

            Microsoft.Office.Interop.Outlook.AppointmentItem item = null;
            //Single Appt for any recurring apointment
            Microsoft.Office.Interop.Outlook.AppointmentItem singleAppt = null;

            //string sFilter = "[Start] >= '" + strDate1 + "'"
            //                + " and " +
            //                "[End] <= '" + strDate2 + "'";

            //string sFilter = "([Start] >= '" +
            //        date.Date.ToString("g")
            //        + "' AND [End] <= '" +
            //        date.AddDays(1).Date.ToString("g") + "'";

            //appointments that start and end within the time
            string sFilter1 = "[Start] >= '" +
                    date.Date.ToString("g")
                    + "' AND [End] <= '" +
                    date.AddDays(1).Date.ToString("g") + "'";

            //appointments that start before starttime and end after starttime
            string sFilter2 = "[Start] < '" +
                    date.Date.ToString("g")
                    + "' AND [End] > '" +
                    date.Date.ToString("g") + "'";

            //appointments that start before endtime and end after endtime
            string sFilter3 = "[Start] < '" +
                    date.AddDays(1).ToString("g")
                    + "' AND [End] > '" +
                    date.AddDays(1).Date.ToString("g") + "'";

            string sFilter = ("( " + sFilter1 + " ) OR ( " + sFilter2 + " ) OR ( " + sFilter3 + " )");


            calendarFolder.Items.IncludeRecurrences = true;
            Microsoft.Office.Interop.Outlook.Items outlookCalendarItems = calendarFolder.Items.Restrict(sFilter);
            outlookCalendarItems.Sort("[Start]", Type.Missing);

            Debug.WriteLine("Count after Restrict: {0}", outlookCalendarItems.Count);

            foreach (var v in outlookCalendarItems)
            {
                try
                {
                    //This Appointment
                    item = (Microsoft.Office.Interop.Outlook.AppointmentItem)v;

                    if (item.IsRecurring)
                    {
                        try
                        {
                            DateTime startDateTime = item.StartInStartTimeZone;
                            Microsoft.Office.Interop.Outlook.RecurrencePattern pattern =
                                item.GetRecurrencePattern();
                            singleAppt =
                                pattern.GetOccurrence(date.Date.Add(startDateTime.TimeOfDay))
                                as Microsoft.Office.Interop.Outlook.AppointmentItem;
                            if (singleAppt == null)
                            {
                                continue;
                            }
                            else
                            {
                                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(item);
                                item = null;

                                item = singleAppt;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }


                    var subject = item.Subject;
                    var body = item.Body;
                    var start = item.StartInStartTimeZone;
                    var end = item.EndInEndTimeZone;
                    var location = item.Location;
                    var organizer = item.Organizer;

                    OutlookItem c = new OutlookItem();
                    c.Subject = "From " + start.ToString("t") + " To " + end.ToString("t");
                    if (c.Subject == "From 12:00 AM To 12:00 AM")
                        c.Subject = "All Day";
                    c.Name = item.Subject;
                    if (!string.IsNullOrWhiteSpace(location))
                        c.MessageBody = "Location : " + location + Environment.NewLine;
                    c.MessageBody += "Organized by " + organizer + Environment.NewLine + body;

                    c.MessageHTMLBody = c.MessageBody;
                    c.DisplayTime = start.ToString("t");
                    if (c.DisplayTime == "12:00 AM")
                        c.DisplayTime = " "; //empty string, no point is displaying 12:00 AM

                    Debug.WriteLine(c.Name);
                    Debug.WriteLine(c.Subject);
                    Debug.WriteLine(c.MessageBody);

                    status.ListOfItems.Add(c);
                }
                catch
                {
                    continue;
                }
                finally
                {
                    if (item != null)
                    {
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(item);
                        item = null;
                    }
                    if (singleAppt != null)
                    {
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(singleAppt);
                        singleAppt = null;
                    }
                }
            }
            return status;
        }

        #region WebShadow Stuff

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
                if (client == null)
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
                        select new ProjectHoursRollup
                        {
                            ProjectID = egrp.Key,
                            ResourceID = resourceId,
                            ClientID = (from i in egrp select i).FirstOrDefault().ShadowCLID,
                            BillableHours = (from i in egrp where i.Billable select i.Time ?? 0).Sum(),
                            NonBillableHours = (from i in egrp where !i.Billable select i.Time ?? 0).Sum(),
                        };

            return query.ToList();
        }

        /// <summary>
        /// Build a forecast all nasty style.  Refactor this later when we know what we really want.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public ForecastSummary GetForecast(string sStart, string sEnd)
        {
            DateTime start = sStart.StringToDateTime();
            DateTime end = sEnd.StringToDateTime();

            using (var context = new SchedulingEntities())
            {
                var results = new List<ResourceSummary>();
                string currentUser = string.Empty;

                ResourceSummary current = null;

                var scheduleView = GetScheduleView(context, start, end);
                var resourceIds = (from item in scheduleView select item.EmployeeID).Distinct();

                foreach (var resourceId in resourceIds)
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
                    Dictionary<int, ProjectHoursRollup> projectHours = GetProjectHoursRollup(context, resourceId, start, end).ToDictionary(dk => dk.ProjectID);
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


                    foreach (var projectItem in projectHours.Values.OrderBy(ph => ph.IsScheduled))
                    {
                        current.TotalHoursRecorded += projectItem.TotalHoursRealized;
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

        #endregion

        #endregion
    }
}
