using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.ServiceModel;
using System.ServiceProcess;
using System.Configuration;
using System.Configuration.Install;
using System.ServiceModel.Web;
using Microsoft.Office.Core;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;


namespace StandupService
{
    public partial class StandupService : ServiceBase
    {
        private static readonly TimeSpan UpdateEngineTimerFrequency = TimeSpan.FromMinutes(3);
        private Timer UpdateEngineTimer { get; set; }
        private static string sSource;
        private static string sLog;

        public ServiceHost serviceHost = null;
        public StandupService()
        {
            //EventLog.WriteEntry(sSource, "In StandupService constructor", EventLogEntryType.Information, 234);
            // Name the Windows Service
            ServiceName = "StandupService.YaharaEmployeeStatusService";
        }

        public static void Main()
        {
            sSource = "YaharaEmployeeStatusService";
            sLog = "Application";

            if (!EventLog.SourceExists(sSource))
                EventLog.CreateEventSource(sSource, sLog);

            //EventLog.WriteEntry(sSource, "In Main method", EventLogEntryType.Information, 234);

            ServiceBase.Run(new StandupService());
        }

        // Start the Windows service.
        protected override void OnStart(string[] args)
        {
            //EventLog.WriteEntry(sSource, "In OnStart method", EventLogEntryType.Information, 234);

            this.RequestAdditionalTime(20000);

            if (serviceHost != null)
            {
                serviceHost.Close();
            }

            serviceHost = new ServiceHost(typeof(YaharaEmployeeStatusService));


            serviceHost.Open();

            YaharaEmployeeStatusService.DoWork(DateTime.Today, true);

            this.UpdateEngineTimer = new Timer(MyTimerAction,
                                               null, /* or whatever state object you need to pass */
                                               UpdateEngineTimerFrequency,
                                               UpdateEngineTimerFrequency);
        }

        protected override void OnStop()
        {
            //EventLog.WriteEntry(sSource, "In OnStart method", EventLogEntryType.Information, 234);

            if (serviceHost != null)
            {
                serviceHost.Close();
                serviceHost = null;
            }
        }

        private void MyTimerAction(object state)
        {
            // do engine work here - call other servers, bake cookies, etc.
            YaharaEmployeeStatusService.DoWork(DateTime.Today, true);
        }

    }

    // Define a service contract.
    //[ServiceContract(Namespace = "http://Microsoft.ServiceModel.Samples")]
    //public interface ICalculator
    //{
    //    [OperationContract]
    //    double Add(double n1, double n2);
    //    [OperationContract]
    //    double Subtract(double n1, double n2);
    //    [OperationContract]
    //    double Multiply(double n1, double n2);
    //    [OperationContract]
    //    double Divide(double n1, double n2);
    //}

    [ServiceContract(Name = "StandupService.YaharaEmployeeStatusService", Namespace = "Where")]
    public interface IYaharaEmployeeStatusService
    {
        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "Json/GetStatus?date={date}")]
        Status GetStatus(string date); //mmddyyyy format

        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "Json/WelcomeMessage?message={message}")]
        bool WelcomeMessage(string message);
    }

    // Implement the ICalculator service contract in a service class.
    //public class CalculatorService : ICalculator
    //{
    //    // Implement the ICalculator methods.
    //    public double Add(double n1, double n2)
    //    {
    //        double result = n1 + n2;
    //        return result;
    //    }

    //    public double Subtract(double n1, double n2)
    //    {
    //        double result = n1 - n2;
    //        return result;
    //    }

    //    public double Multiply(double n1, double n2)
    //    {
    //        double result = n1 * n2;
    //        return result;
    //    }

    //    public double Divide(double n1, double n2)
    //    {
    //        double result = n1 / n2;
    //        return result;
    //    }
    //}
    //[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    //         ConcurrencyMode = ConcurrencyMode.Single)]
    //[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
                 ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class YaharaEmployeeStatusService : IYaharaEmployeeStatusService
    {
        public const int NumberOfPastDaysToCache = 20;
        public const int NumberOfFutureDaysToCache = 10;
        private static List<Status> listStatus;
        private readonly static object _doworksync = new object();
        private static string sSource;
        private static string sLog;

        private YaharaEmployeeStatusService()
        {
            sSource = "YaharaEmployeeStatusService";
            sLog = "Application";

            if (!EventLog.SourceExists(sSource))
                EventLog.CreateEventSource(sSource, sLog);

            EventLog.WriteEntry(sSource, "In constructor of YaharaEmployeeStatusService", EventLogEntryType.Information, 234);
        }

        //[AspNetCacheProfile("CacheForXSeconds")]
        public Status GetStatus(string strDate) //mmddyyyy
        {
            DateTime date = strDate.StringToDateTime();

            //'March 05, 2012 12:00 AM'
            Status status = DoWork(date);

            return status;
        }

        public bool WelcomeMessage(string message)
        {
            try
            {
                StringBuilder newFile = new StringBuilder();

                string[] file = System.IO.File.ReadAllLines(@"\\10.111.124.47\c$\RecBoard\Welcome.txt");
                newFile.Append(message + "\r\n\r\n\r\n\r\n\r\n");

                foreach (string line in file)
                {
                    newFile.Append(line + "\r\n");
                }
                System.IO.File.WriteAllText(@"\\10.111.124.47\c$\RecBoard\Welcome.txt", newFile.ToString());
                return true;
            }
            catch(Exception e)
            {
                EventLog.WriteEntry(sSource, "Exception in YaharaEmployeeStatusService" + Environment.NewLine + "Message = " + e.Message
                    + Environment.NewLine + "StackTrace" + Environment.NewLine + e.StackTrace
                    , EventLogEntryType.Error, 234);
                return false;
            }
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
                        var s = (from l in listStatus
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

                lock (_doworksync)
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

                    if (listStatus == null || listStatus.Count == 0) // first call
                    {
                        listStatus = new List<Status>(NumberOfPastDaysToCache + NumberOfFutureDaysToCache);

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
                                listStatus.Add(final);
                            }
                            catch
                            {
                                listStatus.Add(new Status(specificDate));
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
                            var s = (from l in listStatus
                                     where l.Date == DateTime.Today.DateTimeToString()
                                     select l).FirstOrDefault();
                            if (s != null)
                                listStatus.Remove(s);

                            //add fresh entry
                            listStatus.Add(final);
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

                    var st = (from l in listStatus
                              where l.Date == date.DateTimeToString()
                              select l).FirstOrDefault();
                    return st;
                }
            }
            catch (Exception e)
            {
                EventLog.WriteEntry(sSource, "Exception in YaharaEmployeeStatusService" + Environment.NewLine + "Message = " + e.Message
                    + Environment.NewLine + "StackTrace" + Environment.NewLine + e.StackTrace
                    ,EventLogEntryType.Error, 234);
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

                    if (subject.Contains("working from")
                        || body.Contains("working from")
                        || subject.Contains("working on")
                        || body.Contains("working on")
                        || subject.Contains("work on")
                        || body.Contains("work on")
                        || subject.Contains("work from")
                        || body.Contains("work from")
                        || subject.Contains("standup")
                        || body.Contains("standup")
                        || subject.Contains("wfh")
                        || body.Contains("wfh")
                        || subject.Contains("pto")
                        || body.Contains("pto")
                        || subject.Contains("sick")
                        || body.Contains("sick")
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
            string sFilter = "[Start] >= '" + strDate1 + "'"
                            + " and " +
                            "[End] <= '" + strDate2 + "'";

            Microsoft.Office.Interop.Outlook.Items outlookCalendarItems = calendarFolder.Items.Restrict(sFilter);
            Debug.WriteLine("Count after Restrict: {0}", outlookCalendarItems.Count);

            foreach (var v in outlookCalendarItems)
            {
                try
                {
                    item = (Microsoft.Office.Interop.Outlook.AppointmentItem)v;

                    if (item.IsRecurring)
                        continue;

                    var subject = item.Subject;
                    var body = item.Body;
                    var start = item.StartInStartTimeZone;
                    var end = item.EndInEndTimeZone;
                    var location = item.Location;
                    var organizer = item.Organizer;

                    OutlookItem c = new OutlookItem();
                    c.Subject = "From " + start.ToString("t") + " To " + end.ToString("t");
                    c.Name = item.Subject;
                    if (!string.IsNullOrWhiteSpace(location))
                        c.MessageBody = "Location : " + location + Environment.NewLine;
                    c.MessageBody += "Organized by " + organizer + Environment.NewLine + body;

                    c.MessageHTMLBody = c.MessageBody;
                    c.DisplayTime = start.ToString("t");

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
                }
            }
            return status;
        }


    }

    public static class ExtensionMethods
    {
        public static DateTime StringToDateTime(this string strDate)
        {
            if (strDate.Length != 8)
            {
                //return today's date
                DateTime d = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                return d;
            }
            string mm = strDate.Substring(0, 2);
            string dd = strDate.Substring(2, 2);
            string yyyy = strDate.Substring(4, 4);
            DateTime date = new DateTime(Convert.ToInt32(yyyy), Convert.ToInt32(mm), Convert.ToInt32(dd));
            return date;
        }

        public static string DateTimeToString(this DateTime date)
        {
            return date.ToString("MMddyyyy");
        }

    }

    // Provide the ProjectInstaller class which allows 
    // the service to be installed by the Installutil.exe tool
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller process;
        private ServiceInstaller service;

        public ProjectInstaller()
        {
            process = new ServiceProcessInstaller();
            process.Account = ServiceAccount.NetworkService;
            service = new ServiceInstaller();
            service.ServiceName = "YaharaStandupService";
            Installers.Add(process);
            Installers.Add(service);
        }
    }


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
