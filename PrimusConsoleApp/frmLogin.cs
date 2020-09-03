using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;

namespace PrimusConsoleApp
{
    public partial class frmLogin : Form
    {
        public ChromiumWebBrowser chromeBrowser;
        public string CLientId = string.Empty;
        int count = 0;
        public frmLogin(string url)
        {
            InitializeComponent();
            //webBrowser1.Navigate(url);
            IntializeChromium(url);
        }

        private void IntializeChromium(string url)
        {
            CefSettings settings = new CefSettings();
            Cef.Initialize(settings);
            chromeBrowser = new ChromiumWebBrowser(url);
            chromeBrowser.TitleChanged += ChromeBrowser_TitleChanged;
            //chromeBrowser.GetNextControl
            this.Controls.Add(chromeBrowser);
            chromeBrowser.Dock = DockStyle.Fill;

        }

        private void ChromeBrowser_TitleChanged(object sender, TitleChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => ChromeBrowser_TitleChanged(sender,e)));
                return;
            }
            var CurrentAddress = e.Title.ToString().Split('=');
            var login = e.Title.ToString().Split('=');

            if (login.Count() > 2 && count == 1)
            {
                CLientId = login[2].Split('&')[0];
            }
            if (CurrentAddress.Count() > 1)
            {
                Global.appCode = CurrentAddress[1].Substring(0, CurrentAddress[1].Length - 6);
				if (CurrentAddress[0].Contains("code"))
                    this.Close();
            }
            count++;
        }

    }
}
