using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FishingFun
{
    public partial class KeyBindChooser : UserControl
    {
        private static readonly string Filename = "keybind.txt";

        public EventHandler CastKeyChanged;

        public KeyBindChooser()
        {
            CastKeyChanged += (s, e) => { };

            InitializeComponent();
            ReadConfiguration();
        }

        public ConsoleKey CastKey { get; set; } = ConsoleKey.D4;

        private void ReadConfiguration()
        {
            try
            {
                if (File.Exists(Filename))
                {
                    var contents = File.ReadAllText(Filename);
                    CastKey = (ConsoleKey)int.Parse(contents);
                    KeyBind.Text = GetCastKeyText(CastKey);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                CastKey = ConsoleKey.D4;
                KeyBind.Text = GetCastKeyText(CastKey);
            }
        }

        private void WriteConfiguration()
        {
            File.WriteAllText(Filename, ((int)CastKey).ToString());
        }

        private void CastKey_Focus(object sender, RoutedEventArgs e)
        {
            KeyBind.Text = "";
        }

        private void KeyBind_KeyUp(object sender, KeyEventArgs e)
        {
            var key = e.Key.ToString();
            ProcessKeybindText(key);
        }

        private void ProcessKeybindText(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                ConsoleKey ck;
                if (Enum.TryParse(key, out ck))
                {
                    CastKey = ck;
                    WriteConfiguration();
                    CastKeyChanged?.Invoke(this, null);
                    return;
                }
            }

            KeyBind.Text = "";
        }

        private string GetCastKeyText(ConsoleKey ck)
        {
            var keyText = ck.ToString();
            if (keyText.Length == 1) return keyText;
            if (keyText.StartsWith("D") && keyText.Length == 2) return keyText.Substring(1, 1);
            return "?";
        }
    }
}