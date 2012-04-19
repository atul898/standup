using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Threading;



namespace Where
{
    public class Global : System.Web.HttpApplication
    {
        private static readonly TimeSpan UpdateEngineTimerFrequency = TimeSpan.FromMinutes(30);
        //private static readonly TimeSpan UpdateEngineTimerFrequency = TimeSpan.FromMinutes(Convert.ToInt32(GetValue("refreshDataFromOutlook")));

        private Timer UpdateEngineTimer { get; set; }

        //private static string GetValue(string strCustonSetting)
        //{
        //    System.Configuration.Configuration rootWebConfig1 =
        //        System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(null);
        //    if (rootWebConfig1.AppSettings.Settings.Count > 0)
        //    {
        //        System.Configuration.KeyValueConfigurationElement customSetting = 
        //            rootWebConfig1.AppSettings.Settings[strCustonSetting];
        //        if (customSetting != null)
        //        {
        //            Console.WriteLine("customsetting application string = \"{0}\"", customSetting.Value);
        //            return customSetting.Value.ToString();
        //        }
        //        else
        //        {
        //            Console.WriteLine("No customsetting application string");
        //            return string.Empty;
        //        }
        //    }
        //    return string.Empty;
        //}

        private void MyTimerAction(object state)
        {
            // do engine work here - call other servers, bake cookies, etc.
            YaharaEmployeeStatusService.DoWork(DateTime.Today, true);
        }


        protected void Application_Start(object sender, EventArgs e)
        {
            YaharaEmployeeStatusService.DoWork(DateTime.Today, true);

            this.UpdateEngineTimer = new Timer(MyTimerAction,
                                               null, /* or whatever state object you need to pass */
                                               UpdateEngineTimerFrequency,
                                               UpdateEngineTimerFrequency);
        }
         
        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {
            this.UpdateEngineTimer = null;
        }
    }
}