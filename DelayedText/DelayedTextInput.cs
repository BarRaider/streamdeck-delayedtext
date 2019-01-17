using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

namespace Delayedtext
{
    public class DelayedTextInput : IPluginable
    {
        private class InspectorSettings : SettingsBase
        {
            public static InspectorSettings CreateDefaultSettings()
            {
                InspectorSettings instance = new InspectorSettings();
                instance.InputText = String.Empty; ;
                instance.Delay = 1;
                instance.EnterMode = false;

                return instance;
            }

            [JsonProperty(PropertyName = "inputText")]
            public string InputText { get; set; }

            [JsonProperty(PropertyName = "delay")]
            public int Delay { get; set; }

            [JsonProperty(PropertyName = "enterMode")]
            public bool EnterMode { get; set; }
        }

        #region Private members

        private const int RESET_COUNTER_KEYPRESS_LENGTH = 1;

        private bool inputRunning = false;
        private InspectorSettings settings;

        #endregion

        #region Public Methods

        public DelayedTextInput(streamdeck_client_csharp.StreamDeckConnection connection, string action, string context, JObject settings)
        {
            if (settings == null || settings.Count == 0)
            {
                this.settings = InspectorSettings.CreateDefaultSettings();
            }
            else
            {
                this.settings = settings.ToObject<InspectorSettings>();
            }

            this.settings.StreamDeckConnection = connection;
            this.settings.ActionId = action;
            this.settings.ContextId = context;
        }

        public void KeyPressed()
        {
            if (inputRunning)
            {
                return;
            }
            
            SendInput();
        }

        public void KeyReleased()
        {
        }

        public void OnTick()
        {
        }

        public void UpdateSettings(JObject payload)
        {
            if (payload["property_inspector"] != null)
            {
                switch (payload["property_inspector"].ToString().ToLower())
                {
                    case "propertyinspectorconnected":
                        settings.SendToPropertyInspectorAsync();
                        break;

                    case "propertyinspectorwilldisappear":
                        settings.SetSettingsAsync();
                        break;

                    case "updatesettings":
                        settings.Delay = (int)payload["delay"];
                        settings.InputText = (string)payload["inputText"];
                        settings.EnterMode = (bool)payload["enterMode"];
                        settings.SetSettingsAsync();
                        break;
                }
            }
        }

        #endregion

        #region Private Methods

        private async void SendInput()
        {
            inputRunning = true;
            await Task.Run(() =>
            {
                InputSimulator iis = new InputSimulator();
                string text = settings.InputText;
                int delay = settings.Delay;

                if (settings.EnterMode)
                {
                    text = text.Replace("\r\n", "\n");
                }

                for (int idx = 0; idx < text.Length; idx++)
                {
                    if (settings.EnterMode && text[idx] == '\n')
                    {
                        iis.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);
                    }
                    else
                    {
                        iis.Keyboard.TextEntry(text[idx]);
                    }
                    Thread.Sleep(delay);
                }
            });
            inputRunning = false;
        }
        #endregion
    }
}
