﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using GestureSign.Common.Plugins;
using ManagedWinapi.Windows;
using ManagedWinapi.Audio.Mixer;
using System.Windows.Controls;

using System.Runtime.InteropServices;

namespace GestureSign.CorePlugins.Volume
{
    public class VolumePlugin : IPlugin
    {
        #region Private Variables

        private Volume _GUI = null;
        private VolumeSettings _Settings = null;

        private enum Method
        {
            VolumeUp = 0,
            VolumeDown = 1,
            Mute = 2
        }

        #endregion

        #region Public Properties

        public string Name
        {
            get { return "调整音量"; }
        }

        public string Description
        {
            get { return GetDescription(_Settings); }
        }

        public UserControl GUI
        {
            get
            {
                if (_GUI == null)
                    _GUI = CreateGUI();

                return _GUI;
            }
        }

        public Volume TypedGUI
        {
            get { return (Volume)GUI; }
        }

        public string Category
        {
            get { return "系统"; }
        }

        public bool IsAction
        {
            get { return true; }
        }

        #endregion

        #region Public Methods

        public void Initialize()
        {

        }

        public bool Gestured(Common.Plugins.PointInfo ActionPoint)
        {
            return AdjustVolume(_Settings);
        }

        public void Deserialize(string SerializedData)
        {
            // Clear existing settings if nothing was passed in
            if (String.IsNullOrEmpty(SerializedData))
            {
                _Settings = new VolumeSettings();
                return;
            }

            // Create memory stream from serialized data string
            MemoryStream memStream = new MemoryStream(Encoding.Default.GetBytes(SerializedData));

            // Create json serializer to deserialize json file
            DataContractJsonSerializer jSerial = new DataContractJsonSerializer(typeof(VolumeSettings));

            // Deserialize json file into actions list
            _Settings = jSerial.ReadObject(memStream) as VolumeSettings;

            if (_Settings == null)
                _Settings = new VolumeSettings();
        }

        public string Serialize()
        {
            _Settings = _GUI.Settings;

            if (_Settings == null)
                _Settings = new VolumeSettings();

            // Create json serializer to serialize json file
            DataContractJsonSerializer jSerial = new DataContractJsonSerializer(typeof(VolumeSettings));

            // Open json file
            MemoryStream mStream = new MemoryStream();
            StreamWriter sWrite = new StreamWriter(mStream);

            // Serialize actions into json file
            jSerial.WriteObject(mStream, _Settings);

            return Encoding.Default.GetString(mStream.ToArray());
        }

        #endregion

        #region Private Methods

        private Volume CreateGUI()
        {
            Volume newGUI = new Volume();

            newGUI.Loaded += (o, e) =>
            {
                TypedGUI.Settings = _Settings;
                TypedGUI.HostControl = HostControl;
            };

            return newGUI;
        }

        private string GetDescription(VolumeSettings Settings)
        {
            if (Settings == null)
                return "调整系统音量";

            // Create string to store final output description
            string strOutput = "";

            // Build output string
            switch (Settings.Method)
            {
                case 0:
                    strOutput = "增大音量 " + Settings.Percent.ToString() + "%";
                    break;
                case 1:
                    strOutput = "减小音量 " + Settings.Percent.ToString() + "%";
                    break;
                case 2:
                    strOutput = "静音";
                    break;
            }

            return strOutput;
        }

        private bool AdjustVolume(VolumeSettings Settings)
        {
            if (Settings == null)
                return false;


            try
            {
                switch ((Method)_Settings.Method)
                {
                    case Method.VolumeUp:
                        ChangeVolume(Method.VolumeUp);
                        break;
                    case Method.VolumeDown:
                        ChangeVolume(Method.VolumeDown);
                        break;
                    case Method.Mute:
                        SetMute();
                        break;
                }

                return true;
            }
            catch
            {
                //MessageBox.Show("Could not change volume settings.", "Volume Change Invalid", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, uint wParam, uint lParam);
        const uint WM_APPCOMMAND = 0x319;
        const uint APPCOMMAND_VOLUME_UP = 0x0a;
        const uint APPCOMMAND_VOLUME_DOWN = 0x09;
        const uint APPCOMMAND_VOLUME_MUTE = 0x08;
        private void ChangeVolume(Method ChangeMethod)
        {
            System.Diagnostics.Process p = System.Diagnostics.Process.GetCurrentProcess();
            int t = _Settings.Percent / 2;
            if (ChangeMethod == Method.VolumeUp)
            {
                for (int i = 0; i < t; i++)
                {
                    //加音量               
                    SendMessage(HostControl.TouchCapture.MessageWindowHandle, WM_APPCOMMAND, 0x30292, APPCOMMAND_VOLUME_UP * 0x10000);
                }

            }
            else
            {
                for (int i = 0; i < t; i++)
                {
                    SendMessage(HostControl.TouchCapture.MessageWindowHandle, WM_APPCOMMAND, 0x30292, APPCOMMAND_VOLUME_DOWN * 0x10000);
                }
            }
        }

        private void SetMute()
        {
            // System.Diagnostics.Process p = System.Diagnostics.Process.GetCurrentProcess();
            //  if (p.MainWindowHandle == IntPtr.Zero)
            SendMessage(HostControl.TouchCapture.MessageWindowHandle, WM_APPCOMMAND, 0x200eb0, APPCOMMAND_VOLUME_MUTE * 0x10000);
            //else SendMessage(p.MainWindowHandle, WM_APPCOMMAND, 0x200eb0, APPCOMMAND_VOLUME_MUTE * 0x10000);
        }

        #endregion

        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}