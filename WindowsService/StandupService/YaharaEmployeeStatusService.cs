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

namespace Yahara.Standup
{
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

        public WrappedBool WelcomeMessage(string message)
        {
            WrappedBool b = new WrappedBool();
            try
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
            DateTime requestedDate = strDate.StringToDateTime();
            DateTime previousMonday = requestedDate.StartOfWeek(DayOfWeek.Monday);
            previousMonday = previousMonday.AddDays(-1); //hack, but hey it works

            //Step 1 : Collect all emails
            Summary summary = new Summary(strDate.StringToDateTime());
            var webClient = new WebClient();
            webClient.Headers.Add("Authorization", "Basic YXR1bGM6dGVzdGluZw==");
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

            return summary;
        }

        public Summary GetEmployeeWebShadowSummary(string date) //mmddyyyy format
        {
            Summary s = new Summary(date.StringToDateTime());
            EmployeeDetail ed = new EmployeeDetail();
            ed.Name = "Atul Chauhan";
            ed.TotalHoursLogged = 40;
            s.ListOfItems.Add(ed);
            return s;
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
                        || subject.Contains("home")
                        || body.Contains("home")
                        || subject.Contains("leaving")
                        || body.Contains("leaving")
                        || subject.Contains("today")
                        || body.Contains("today")
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
}
