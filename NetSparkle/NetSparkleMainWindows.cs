using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace AppLimit.NetSparkle
{
    public partial class NetSparkleMainWindows : Form, IDisposable
    {
        private StreamWriter sw = null;

        public NetSparkleMainWindows()
        {
            // init ui
            InitializeComponent();

            // init logfile

            InitializeLog();
        }

        private void InitializeLog()
        {
            try
            {
            sw = OpenLogFileWriter("");
            for (int iNum = 0; iNum<3 ; iNum++)
            while (sw == null)
                {
                sw = OpenLogFileWriter(iNum.ToString());
            catch (UnauthorizedAccessException)
            {
                _sw = new StringWriter();
            }
            sw = new StringWriter();
        }

        private static StreamWriter OpenLogFileWriter(string sID)
        {
            try
            {
                return File.CreateText(CreateLogFilePath(sID));
            }
            catch (IOException ex)
            {
                return null;
            }
            
        }

        private static string CreateLogFilePath(string sID)
        {
            return Path.Combine(Environment.ExpandEnvironmentVariables("%temp%"), String.Format("NetSparkle {0}.log", sID));
        }

        public void Report(String message)
        {
            if (lstActions.InvokeRequired)
                lstActions.Invoke(new Action<String>(Report), message);
            else
            {
                // build the message 
                DateTime c = DateTime.Now;
                String msg = "[" + c.ToLongTimeString() + "." + c.Millisecond + "] " + message;

                // report to file
                ReportToFile(msg);

                // report the message into ui
                lstActions.Items.Add(msg);
            }
        }

        private void ReportToFile(String msg)
        {
            try
            {
                // write 
                _sw.WriteLine(msg);

                // flush
                _sw.Flush();
            } catch(Exception)
            {

            }
        }

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            // flush again
            _sw.Flush();

            // close the stream
            _sw.Dispose();

            // close the base
            base.Dispose();
        }

        #endregion
    }
}
