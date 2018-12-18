﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TelimenaClient;

namespace TelimenaTestSandboxApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            this.InitializeComponent();
            this.apiUrlTextBox.Text = string.IsNullOrEmpty(Properties.Settings.Default.baseUri) ? "http://localhost:7757/" : Properties.Settings.Default.baseUri;
            this.apiKeyTextBox.Text = string.IsNullOrEmpty(Properties.Settings.Default.telemetryKey) ? "" : Properties.Settings.Default.telemetryKey;
            if (Guid.TryParse(this.apiKeyTextBox.Text, out Guid key))
            {
                this.teli = Telimena.Construct(new TelimenaStartupInfo(key, new Uri(this.apiUrlTextBox.Text))) as Telimena;
            }
            this.Text = $"Sandbox v. {TelimenaVersionReader.Read(this.GetType(), VersionTypes.FileVersion)}";
        }

        private string PresentResponse(TelimenaResponseBase response)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ContractResolver = new MyContractResolver(),
            };
            return JsonConvert.SerializeObject(response, settings);
        }

        private string PresentResponse(UpdateCheckResult response)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ContractResolver = new MyContractResolver(),
            };
            return JsonConvert.SerializeObject(response, settings);
        }

        private ITelimena teli;
        private TelimenaHammer hammer;

        private async void InitializeButton_Click(object sender, EventArgs e)
        {
            if (Guid.TryParse(this.apiKeyTextBox.Text, out Guid key))
            {
                this.teli = Telimena.Construct(new TelimenaStartupInfo( key, telemetryApiBaseUrl: new Uri(this.apiUrlTextBox.Text))) ;
            }
            else
            {
                this.resultTextBox.Text = "Cannot run without telemetry key";
                return;
            }
            TelemetryInitializeResponse response = await this.teli.Telemetry.Async.Initialize();

            this.resultTextBox.Text += this.teli.Properties.StaticProgramInfo.Name + " - " + this.PresentResponse(response) + Environment.NewLine;
        }

        private async void SendUpdateAppUsageButton_Click(object sender, EventArgs e)
        {
            TelemetryUpdateResponse result;
            Stopwatch sw = Stopwatch.StartNew();

            if (!string.IsNullOrEmpty(this.viewNameTextBox.Text))
            {
                result = await this.teli.Telemetry.Async.View(string.IsNullOrEmpty(this.viewNameTextBox.Text) ? null : this.viewNameTextBox.Text);
                sw.Stop();
            }
            else
            {
                result = await this.teli.Telemetry.Async.View("DefaultView");
                sw.Stop();
            }

            if (result.Exception == null)
            {
                this.resultTextBox.Text += $@"INSTANCE: {sw.ElapsedMilliseconds}ms " + this.teli.Properties.StaticProgramInfo.Name + " - " + this.PresentResponse(result) + Environment.NewLine;
            }
            else
            {
                MessageBox.Show(result.Exception.ToString());
            }
        }

        private void sendSync_button_Click(object sender, EventArgs e)
        {
            Stopwatch sw = Stopwatch.StartNew();

            TelemetryUpdateResponse result;

            if (!string.IsNullOrEmpty(this.viewNameTextBox.Text))
            {
                result = this.teli.Telemetry.Blocking.View(string.IsNullOrEmpty(this.viewNameTextBox.Text) ? null : this.viewNameTextBox.Text);
                sw.Stop();
            }
            else
            {
                result = this.teli.Telemetry.Blocking.View("DefaultView");
                sw.Stop();
            }

            if (result.Exception == null)
            {
                this.resultTextBox.Text += $@"BLOCKING INSTANCE: {sw.ElapsedMilliseconds}ms " + this.teli.Properties.StaticProgramInfo.Name + " - " + this.PresentResponse(result) + Environment.NewLine;
            }
            else
            {
                MessageBox.Show(result.Exception.ToString());
            }
        }

        private void UpdateText(string text)
        {
            this.resultTextBox.Text = text + "\r\n" + this.resultTextBox.Text;
        }

        private async void checkForUpdateButton_Click(object sender, EventArgs e)
        {
            var response = await this.teli.Updates.Async.CheckForUpdates();
            this.UpdateText(this.PresentResponse(response));
        }

        private async void handleUpdatesButton_Click(object sender, EventArgs e)
        {
            this.UpdateText("Handling updates...");
            var suppressAllErrors = this.teli.Properties.SuppressAllErrors;
            this.teli.Properties.SuppressAllErrors = false;
            try
            {
                await this.teli.Updates.Async.HandleUpdates(false);
            }
            catch (Exception ex)
            {
                this.UpdateText(ex.ToString());
            }

            this.teli.Properties.SuppressAllErrors = suppressAllErrors;
            this.UpdateText("Finished handling updates...");
        }

        private void setAppButton_Click(object sender, EventArgs e)
        {
            if (Guid.TryParse(this.apiKeyTextBox.Text, out Guid key))
            {
                Properties.Settings.Default.telemetryKey = this.apiKeyTextBox.Text;
                Properties.Settings.Default.Save();
                this.teli = Telimena.Construct(new TelimenaStartupInfo( key, telemetryApiBaseUrl: new Uri(this.apiUrlTextBox.Text))) as Telimena; ;
            }
            else
            {
                MessageBox.Show("Api key missing, cannot run teli");
            }
            if (!string.IsNullOrEmpty(this.appNameTextBox.Text))
            {
                (this.teli.Properties as TelimenaProperties).StaticProgramInfo = new ProgramInfo
                {
                    Name = this.appNameTextBox.Text
                    , PrimaryAssembly = new AssemblyInfo {Company = "Comp A Ny", Name = this.appNameTextBox.Text + ".dll", VersionData = new VersionData("1.0.0.0", "2.0.0.0")}
                };
            }

            if (!string.IsNullOrEmpty(this.userNameTextBox.Text))
            {
                (this.teli.Properties as TelimenaProperties).UserInfo = new UserInfo {UserName = this.userNameTextBox.Text};
            }

            Properties.Settings.Default.baseUri = this.apiUrlTextBox.Text;
            Properties.Settings.Default.Save();
            
        }

        private void useCurrentAppButton_Click(object sender, EventArgs e)
        {
            if (Guid.TryParse(this.apiKeyTextBox.Text, out Guid key))
            {
                Properties.Settings.Default.telemetryKey = this.apiKeyTextBox.Text;
                Properties.Settings.Default.Save();
                this.teli = Telimena.Construct(new TelimenaStartupInfo(key, telemetryApiBaseUrl: new Uri(this.apiUrlTextBox.Text))) as Telimena; 
            }
            else
            {
                MessageBox.Show("Api key missing, cannot run teli");
            }
            Properties.Settings.Default.baseUri = this.apiUrlTextBox.Text;
            Properties.Settings.Default.Save();
        }

        private async void static_sendUsageReportButton_Click(object sender, EventArgs e)
        {
            TelemetryUpdateResponse result;
            Stopwatch sw = Stopwatch.StartNew();
            if (Guid.TryParse(this.apiKeyTextBox.Text, out Guid key))
            {
                Properties.Settings.Default.telemetryKey = this.apiKeyTextBox.Text;
                Properties.Settings.Default.Save();
                this.teli = Telimena.Construct(new TelimenaStartupInfo(key, telemetryApiBaseUrl: new Uri(this.apiUrlTextBox.Text))) as Telimena;
            }
            else
            {
                MessageBox.Show("Api key missing, cannot run teli");
                return;
            }
            if (!string.IsNullOrEmpty(this.static_viewNameTextBox.Text))
            {
                
                result = await Telimena.Telemetry.Async.View(new TelimenaStartupInfo(key), string.IsNullOrEmpty(this.static_viewNameTextBox.Text)
                    ? null
                    : this.static_viewNameTextBox.Text);
                sw.Stop();
            }
            else
            {
                result = await Telimena.Telemetry.Async.View(new TelimenaStartupInfo(key), "No Name");
                sw.Stop();
            }

            if (result.Exception == null)
            {
                this.resultTextBox.Text += $@"STATIC: {sw.ElapsedMilliseconds}ms " + this.PresentResponse(result) + Environment.NewLine;
            }
            else
            {
                MessageBox.Show(result.Exception.ToString());
            }
        }

        private async void hammer_StartButton_Click(object sender, EventArgs e)
        {
            this.hammer?.Stop();
            if (Guid.TryParse(this.apiKeyTextBox.Text, out Guid key))
            {
                Properties.Settings.Default.telemetryKey = this.apiKeyTextBox.Text;
                Properties.Settings.Default.Save();
                this.teli = Telimena.Construct(new TelimenaStartupInfo(key, telemetryApiBaseUrl: new Uri(this.apiUrlTextBox.Text))) as Telimena;
            }
            else
            {
                MessageBox.Show("Api key missing, cannot run hammer");
                return;
            }
            this.hammer = new TelimenaHammer(key, this.apiUrlTextBox.Text,
                Convert.ToInt32(this.hammer_AppNumberSeedBox.Text),
                Convert.ToInt32(this.hammer_numberOfApps_TextBox.Text),
                 Convert.ToInt32(this.hammer_numberOfFuncs_TextBox.Text),
                 Convert.ToInt32(this.hammer_numberOfUsers_TextBox.Text),
                 Convert.ToInt32(this.hammer_delayMinTextBox.Text),
                 Convert.ToInt32(this.hammer_delayMaxTextBox.Text),
                 Convert.ToInt32(this.hammer_DurationTextBox.Text),
                this.UpdateText
                );

           await this.hammer.Hit();
        }

        private void hammer_StopBtn_Click(object sender, EventArgs e)
        {
            this.hammer?.Stop();
        }

       
    }

    class MyContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var list = base.CreateProperties(type, memberSerialization);

            foreach (var prop in list)
            {
                prop.Ignored = false; // Don't ignore any property
            }

            return list;
        }
    }
}