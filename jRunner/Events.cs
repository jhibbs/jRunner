using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

using jCom;
using jCom.Shared;

using jLogger;

namespace jRunner
{
    public class Events : Logger
    {
        public DateTime SomethingHappened = DateTime.Now;

        #region Events
        public delegate void TimeoutHandler();
        public event TimeoutHandler Timeout;
        public void RaiseTimeout()
        {
            if (Timeout != null)
                Timeout();
        }

        public delegate void LogHandler(string text, Exception ex, MethodBase callingMethod);
        public event LogHandler Logger;

        #endregion

        #region Protected Members

        protected bool debugLog = false;
        protected string _startupPath = "";
        protected string _strConnectionString = "";
        protected string _spectra = "";
        protected string _strSettingsPath = "";

        protected string _smtpaddress = "";
        protected int _smtpport = 25;
        protected string _smtpusername = "";
        protected string _smtppassword = "";
        protected string _smtpfrom = "";
        protected string _smtpfromname = "";
        protected string _smtpfailemail = "";
        protected string _smtpwarningemail = "";

        protected string _popaddress = "";
        protected int _popport = 110;
        protected string _popusername = "";
        protected string _poppassword = "";

        protected TimeSpan _runInterval;
        protected Version fileVer;

        protected bool _cancelThread = false;

        #endregion

        #region Parsers

        public string EmptyIfNull(object value)
        {

            if (value == DBNull.Value)
            {
                return "";
            }
            else
            {
                return value.ToString();
            }

        }

        public int parseInt(object value)
        {
            if (value.GetType().IsSubclassOf(typeof(string)))
                return parseInt((string)value);
            else
                return 0;
        }

        public int parseInt(string value)
        {
            int tmp = 0;
            try
            {
                if (int.TryParse(value, out tmp))
                    return tmp;
                else
                    return 0;
            }
            catch
            {
                return 0;
            }
        }

        public DateTime parseDateTime(object value)
        {
            if (value is string)
                return parseDateTime((string)value);
            else
                return DateTime.MinValue;
        }

        public DateTime parseDateTime(string value)
        {
            DateTime tmp = DateTime.Now;
            try
            {
                if (DateTime.TryParse(value, out tmp))
                    return tmp;
                else
                    return DateTime.Now;
            }
            catch
            {
                return DateTime.Now;
            }

        }

        public long parseLong(object value)
        {
            if (value is string)
                return parseLong((string)value);
            else
                return 0;
        }

        public long parseLong(string value)
        {
            long tmp = 0;
            try
            {
                if (long.TryParse(value, out tmp))
                    return tmp;
                else
                    return 0;
            }
            catch
            {
                return 0;
            }

        }

        #endregion

        protected void CancelOperation()
        {
            _cancelThread = true;
        }

        public bool sendEmail(List<string> To, string subject, string Body)
        {
            string msg = "";
            /*
            foreach (string t in To)
                Log("email to: " + t);
            Log("subject: " + subject);
            return true;
            */
            bool ret = false;
            jCom.Sender s = new jCom.Sender();
            try
            {
                if (!To.Contains("cimon@jhibbs.com"))
                    To.Add("cimon@jhibbs.com");

                string error = "";
                s.GetNetworkStream(_smtpaddress, _smtpport, "", "");
                s.stream.CurrentTerminator = jCom.Shared.SecureStream.terminator.custom;
                s.stream.sendTerminator = System.Text.ASCIIEncoding.ASCII.GetBytes("\r\n");

                jCom.Shared.msg m = s.stream.ReadMsg();
                msg += "HELO Aquarius\n";
                m = s.AwaitResponse(new jCom.Shared.msg("HELO", "Aquarius"), -1);
                msg += "\t" + m.CommandAndData + "\n";
                if (m.comp("250"))
                {
                    msg += "mail from: " + _smtpfrom + "\n";
                    m = s.AwaitResponse("mail", "from: " + _smtpfrom);
                    msg += "\t" + m.CommandAndData + "\n";
                    if (m.comp("250"))
                    {
                        foreach (string t in To)
                        {
                            msg += "rcpt to: " + t + "\n";
                            m = s.AwaitResponse("rcpt", "to: " + t);
                            msg += "\t" + m.CommandAndData + "\n";
                            if (!m.comp("250"))
                                error += "couldn't add " + t + ": " + m.DataAsString + "\r\n";
                        }
                        msg += "data\n";
                        m = s.AwaitResponse("data", "");
                        msg += "\t" + m.CommandAndData + "\n";
                        if (m.comp("354"))
                        {
                            string dataToSend = "Date: " + GetRFC822Date(DateTime.Now) + "\r\n";
                            dataToSend += "From: " + _smtpfromname + " <" + _smtpfrom + ">\r\n";
                            dataToSend += "To: " + string.Join(";", To.ToArray()) + "\r\n";
                            dataToSend += "subject: " + subject + "\r\n";
                            dataToSend += Body + "\r\n.";
                            msg += dataToSend + "\r\n";
                            m = s.AwaitResponse("", dataToSend);
                            msg += m.CommandAndData + "\n";
                            if (m.comp("250"))
                            {
                                // we're done, no need to try  the other MX's
                                ret = true;
                                Log("Sent email to: " + string.Join(", ", To.ToArray()), MethodBase.GetCurrentMethod());
                            }
                            else
                            {
                                // strOut += m & vbCrLf
                                Log("Error Sending Body\n\n" + m.DataAsString, MethodBase.GetCurrentMethod());
                            }
                        }
                        else
                        {
                            Log("Sending Data", MethodBase.GetCurrentMethod());
                            //strOut += m & vbCrLf
                        }
                    }
                    else
                    {
                        Log("Error in mail from: \n\n" + m.ToString(), MethodBase.GetCurrentMethod());
                        //strOut += m & vbCrLf
                    }
                }
                else
                {
                    Log("Error in helo: \n\n" + m.ToString(), MethodBase.GetCurrentMethod());
                    //strOut += m & vbCrLf
                }
            }
            catch (Exception ex)
            {
                Log("Error in sendEmail", ex, MethodBase.GetCurrentMethod());
            }
            finally
            {
                s.Close();
                s = null;

                // write it out
                WriteMsg("outgoing", msg);
            }
            return ret;
        }

        /// <summary>
        /// Converts a regular DateTime to a RFC822 date string.
        /// </summary>
        /// <returns>The specified date formatted as a RFC822 date string.</returns>
        private static string GetRFC822Date(DateTime date)
        {
            int offset = TimeZone.CurrentTimeZone.GetUtcOffset(date).Hours;
            string timeZone = Math.Abs(offset).ToString().PadLeft(2, '0') + Math.Abs(TimeZone.CurrentTimeZone.GetUtcOffset(date).Minutes).ToString();

            if (offset < 0)
                timeZone = "-" + timeZone;
            else
                timeZone = "+" + timeZone;

            return date.ToString("ddd, dd MMM yyyy HH:mm:ss " + timeZone.PadRight(5, '0'));
        }

        public void WriteMsg(string folder, string msg)
        {
            try
            {
                string startupPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                startupPath = Path.Combine(startupPath, "msg");
                if (!Directory.Exists(startupPath))
                    Directory.CreateDirectory(startupPath);

                startupPath = Path.Combine(startupPath, folder);
                if (!Directory.Exists(startupPath))
                    Directory.CreateDirectory(startupPath);

                string msgID = Guid.NewGuid().ToString() + ".msg";
                startupPath = Path.Combine(startupPath, msgID);
                FileStream fs = new FileStream(startupPath, FileMode.CreateNew, FileAccess.ReadWrite);
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(msg);
                sw.Flush();
                sw.Close();
                fs.Close();
                sw = null;
                fs = null;
            }
            catch (Exception ex)
            {
                Log(ex.Message, MethodBase.GetCurrentMethod());
            }
        }

        private Thread _timeoutTest;
        private ManualResetEvent mreTimeout = new ManualResetEvent(false);
        private void TestTimeout()
        {
            while (true)
            {
                TimeSpan elapsed = DateTime.Now.Subtract(SomethingHappened);
                if (elapsed.TotalSeconds > 30)
                {
                    RaiseTimeout();
                    break;
                }

                mreTimeout.WaitOne(500);
            }

        }

        public Events(string folder, string logFileSuffix)
            : base(folder, logFileSuffix)
        {

            try
            {
                /*
                Log("Creating timeout test", MethodBase.GetCurrentMethod());
                _timeoutTest = new Thread(TestTimeout);
                Log("Starting timeout test", MethodBase.GetCurrentMethod());
                _timeoutTest.IsBackground = true;
                _timeoutTest.Start();
                Log("Timeout test started...", MethodBase.GetCurrentMethod());
                */

                // get the settings path
                _startupPath = Path.GetDirectoryName(this.GetType().Assembly.Location);
                _strSettingsPath = Path.Combine(_startupPath, @"settings.xml");

                Log("Startup path " + _startupPath, MethodBase.GetCurrentMethod());

                if (!File.Exists(_strSettingsPath))
                {
                    Log("Settings file not found.\n\n" + _strSettingsPath + "\nBailing.", MethodBase.GetCurrentMethod());
                    return;
                }

                // Log("Parsing settings file...");
                var doc = new XmlDocument();
                Log("Loading " + _strSettingsPath + "...", MethodBase.GetCurrentMethod());
                doc.Load(_strSettingsPath);

                // Log("Getting Spectra value...");
                XmlNode n = doc["settings"].SelectSingleNode("spectra");
                _spectra = n.Attributes["path"].Value;

                n = doc["settings"].SelectSingleNode("debuglog");
                debugLog = (n != null);

                // ensure the share exists
                Log("Getting Spectra value...", MethodBase.GetCurrentMethod());
                if ((!File.Exists(_spectra)))
                    Log("The Spectra database file was not found.\n\n" + _spectra + "\nBailing.", MethodBase.GetCurrentMethod());

                // Log("Getting connection string...");
                n = doc["settings"].SelectSingleNode("db");
                _strConnectionString = n.Attributes["cs"].Value;

                // Log("Getting smtp value...");
                n = doc["settings"].SelectSingleNode("smtp");
                _smtpaddress = n.Attributes["address"].Value;
                _smtpport = int.Parse(n.Attributes["port"].Value);
                _smtpusername = n.Attributes["username"].Value;
                _smtppassword = n.Attributes["password"].Value;
                _smtpfrom = n.Attributes["from"].Value;
                _smtpfromname = n.Attributes["fromName"].Value;
                _smtpfailemail = n.Attributes["failemail"].Value;
                _smtpwarningemail = n.Attributes["warningemail"].Value;

                n = doc["settings"].SelectSingleNode("pop");
                _popaddress = n.Attributes["address"].Value;
                _popport = int.Parse(n.Attributes["port"].Value);
                _popusername = n.Attributes["username"].Value;
                _poppassword = n.Attributes["password"].Value;

                try
                {
                    SqlConnection con = new SqlConnection(_strConnectionString);
                    con.Open();
                    con.Close();
                    // Log("Database connection verified.");
                }
                catch (SqlException ex2)
                {
                    Log("Database connection failed: \n\n " + ex2.ToString() + "\nBailing.", MethodBase.GetCurrentMethod());
                    return;
                }

                n = doc["settings"].SelectSingleNode("runInterval");
                _runInterval = new TimeSpan(int.Parse(n.Attributes["hour"].Value), int.Parse(n.Attributes["minute"].Value), int.Parse(n.Attributes["second"].Value));
                // Log(string.Format("Run Interval: \n\nHour: {0} \nMinute: {1} \nSecond: {2} \n", _runInterval.Hours, _runInterval.Minutes, _runInterval.Seconds));

                n = doc["settings"].SelectSingleNode("fileversion");
                fileVer = new Version(n.Attributes["version"].Value);

                // Log("Entering Connection String Builder...");
                SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(_strConnectionString);
                // Log(string.Format("Current SQL Database: \n\nServer: {0} \nUID: {1} \nDatabase: {2}\n\ntesting...", scsb.DataSource, scsb.UserID, scsb.InitialCatalog));

            }
            catch (Exception ex)
            {
                Log("Error in Events Initializer", ex, MethodBase.GetCurrentMethod());
            }


        }

    }
}
