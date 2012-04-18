using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace jLogger
{
    public class Logger
    {

        // Logging
        public FileStream fs;
        public StreamWriter sw;
        DateTime _logDate = DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0));
        string _logFileName = "";
        string _logFolder = "";
        string _logFileSuffix = "";

        private object _lock = new object();

        public Logger(string folder, string suffix)
        {
            _logFolder = folder;
            _logFileSuffix = suffix;
        }

        public void Log(string text, Exception ex, MethodBase callingMethod)
        {
            // SomethingHappened = DateTime.Now;
            LogText(text, ex, callingMethod);
        }

        public void Log(string text, MethodBase callingMethod)
        {
            // SomethingHappened = DateTime.Now;
            LogText(text, callingMethod);
        }

        public void Log(string text, MethodBase callingMethod, params Object[] args)
        {
            // SomethingHappened = DateTime.Now;
            LogText(text, new Exception(), callingMethod, args);
        }

        public void LogText(string text, Exception ex, MethodBase currentMethod, params object[] args)
        {
            LogText(string.Format(text, args), ex, currentMethod);
        }

        public void LogText(string text, MethodBase currentMethod, params object[] args)
        {
            LogText(string.Format(text, args), null, currentMethod);
        }

        public void LogText(string text, MethodBase currentMethod)
        {
            LogText(text, null, currentMethod);
        }

        public void LogText(string text, Exception ex, MethodBase currentMethod)
        {
            lock (_lock)
            {
                int tc = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;
                if (text.Trim() != "")
                {
                    string _startupPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    _startupPath = Path.Combine(_startupPath, "Log");
                    if (!Directory.Exists(_startupPath))
                        Directory.CreateDirectory(_startupPath);

                    if (_logFolder.Trim().Length > 0)
                    {
                        _startupPath = Path.Combine(_startupPath, _logFolder);
                        if (!Directory.Exists(_startupPath))
                            Directory.CreateDirectory(_startupPath);
                    }

                    // Console.WriteLine(DateTime.Now.ToString("MMM dd HH:mm:ss.ttt") + " tc " + tc + " - " + Math.Round(((decimal)System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024), 2).ToString() + "mb - " + text + GenerateErrorString(ex, "", currentMethod));

                    if (_logDate.Hour != DateTime.Now.Hour || _logFileName == "")
                    {
                        if (sw != null)
                        {
                            try
                            {
                                sw.Flush();
                                sw.Close();
                            }
                            catch { }
                            finally
                            {
                                sw = null;
                            }
                        }

                        if (fs != null)
                            fs = null;

                        _logDate = DateTime.Now;
                        _logFileName = Path.Combine(_startupPath, _logDate.ToString("MM-dd-yyyy-[HH]") + "cimonserver" + _logFileSuffix + ".log");
                    }

                    if (fs == null)
                    {
                        fs = new FileStream(_logFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        sw = new StreamWriter(fs);
                        sw.BaseStream.Seek(0, SeekOrigin.End);
                    }

                    if (sw != null)
                    {
                        sw.WriteLine(DateTime.Now.ToString("MMM dd HH:mm:ss.ttt") + " - " + tc + " - " + Math.Round(((decimal)System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024), 2).ToString() + "mb - " + text + GenerateErrorString(ex, "", currentMethod));
                        sw.Flush();
                    }
                }
            }

        }

        private static bool DateTimeComp(DateTime date1, DateTime date2)
        {
            if (date1.Day == date2.Day && date1.Month == date2.Month && date1.Year == date2.Year)
                return true;
            else
                return false;
        }

        private static string GenerateErrorString(Exception ex, string additionalInfo, MethodBase callingMethod)
        {

            if (ex == null)
                return "";

            string strOut = "Exception: " + ex.Message + "\n";
            strOut += " occured in " + callingMethod.Name + "\n";

            if (additionalInfo != "")
                strOut += "while: " + additionalInfo + "\n";

            if (ex.TargetSite != null)
                strOut += "Target site: " + ex.TargetSite.Name + "\n";

            if (ex.StackTrace != null)
                strOut += "Stack Trace: " + ex.StackTrace + "\n";

            Exception exTemp = ex.InnerException;
            while (exTemp != null)
            {
                strOut += new string(Convert.ToChar("-"), 25) + "\n";
                strOut += exTemp.Message;
                if (ex.TargetSite != null)
                    strOut += " in " + exTemp.TargetSite.Name;
                if (ex.StackTrace != null)
                    strOut += "\n" + exTemp.StackTrace + "\n";
                exTemp = exTemp.InnerException;
            }

            return "\n\n Exception:\n" + strOut + "\n--------------\n";
        }

    }
}
