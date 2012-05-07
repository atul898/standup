using System.Threading;
using System.ServiceProcess;
using System;
using System.ServiceModel;
using System.Diagnostics;
using System.IO;

namespace Yahara.Standup
{
    public partial class StandupService : ServiceBase
    {
        private static readonly TimeSpan UpdateEngineTimerFrequency = TimeSpan.FromMinutes(10);
        private Timer UpdateEngineTimer { get; set; }
        private static string sSource;
        private static string sLog;

        public ServiceHost serviceHost = null;
        public StandupService()
        {
            //EventLog.WriteEntry(sSource, "In StandupService constructor", EventLogEntryType.Information, 234);
            // Name the Windows Service
            ServiceName = "Yahara.Standup.YaharaEmployeeStatusService";
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

            this.RequestAdditionalTime(600000);

            if (serviceHost != null)
            {
                serviceHost.Close();
            }

            serviceHost = new ServiceHost(typeof(YaharaEmployeeStatusService));

            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(LocationSummary));
            serviceHost.Open();

            try
            {

                using (TextReader tr = new StreamReader("C:\\LocationSummary.xml"))
                {
                    LocationSummary lc = (LocationSummary)serializer.Deserialize(tr);
                    YaharaEmployeeStatusService.LocationSummary = lc;
                }
            }
            catch
            {
                YaharaEmployeeStatusService.LocationSummary = null;
                ;//Exception Swallowing Technology :-)
            }

            YaharaEmployeeStatusService.DoWork(DateTime.Today, true);

            this.UpdateEngineTimer = new Timer(MyTimerAction,
                                               null, /* or whatever state object you need to pass */
                                               UpdateEngineTimerFrequency,
                                               UpdateEngineTimerFrequency);
        }

        protected override void OnStop()
        {
            //EventLog.WriteEntry(sSource, "In OnStart method", EventLogEntryType.Information, 234);

            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(LocationSummary));

            using (TextWriter tw = new StreamWriter("C:\\LocationSummary.xml"))
            {
                serializer.Serialize(tw, YaharaEmployeeStatusService.LocationSummary);
                tw.Close(); 
            }
 
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
}
