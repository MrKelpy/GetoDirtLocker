﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GetosDirtLocker.requests;
using GetosDirtLocker.utils;
using LaminariaCore_General.common;
using LaminariaCore_General.utils;
using LaminariaCore_Winforms.forms.extensions;

namespace GetosDirtLocker.gui
{
    /// <summary>
    /// The mainframe of the application, working in conjunction with the mainframe system in
    /// LaminariaCore-Winforms to provide a single-window GUI for the application.
    /// </summary>
    public partial class Mainframe : Form
    {

        /// <summary>
        /// The singleton instance of the class, used to access the form.
        /// </summary>
        public static Mainframe Instance { get; private set; }

        /// <summary>
        /// Whether or not the token needs to be refreshed.
        /// </summary>
        private bool RefreshFlag { get; set; } = true;
        
        /// <summary>
        /// The stored token, used to check if the token has changed.
        /// </summary>
        private string StoredToken { get; set; }

        /// <summary>
        /// The mainframe of the application, working in conjunction with the mainframe system in
        /// the LaminariaCore-Winforms library to provide a single-window GUI for the application.
        /// </summary>
        public Mainframe()
        {
            InitializeComponent();
            CenterToScreen();
            
            // Set the token configuration interface as the default interface.
            MainLayout.SetAllFrom(TokenConfigurationInterface.Instance.GetLayout());
            
            // Load the token from the file if it exists.
            Section data = Program.FileManager.GetFirstSectionNamed("data");
            if (data == null) return;
            
            // If the token file doesn't exist, then we create it.
            string path = data.GetFirstDocumentNamed("token.gl");
            if (!File.Exists(path)) return;
                
            // Decrypt the token and set the stored token to it.
            byte[] token = FileUtilExtensions.ReadBytesFromBinary(path)[0];
            this.StoredToken = TokenConfigurationInterface.DecodeToken(token);
            Mainframe.Instance = this;
        }
        
        /// <summary>
        /// Loads the mainframe, setting up any necessary configurations and settings needed.
        /// </summary>
        private void Mainframe_Load(object sender, EventArgs e)
        {
            this.Text = TokenConfigurationInterface.Instance.Text;
            TextBox tokenBox = TokenConfigurationInterface.Instance.TextBoxToken;
            tokenBox.Text = this.StoredToken;
            
            MainLayout.Focus();
        }
        
        /// <summary>
        /// Refreshes the token if the token refresh flag is set, changing the enabled
        /// state of the locker addition controls accordingly.
        /// </summary>
        private async Task RefreshToken()
        {
            ChangeControlStates(false);  // Preventive disabling of the locker addition controls.
            
            // If the token is invalid, then we disable the locker addition controls.
            if (!await DiscordInteractions.IsTokenValid(TokenConfigurationInterface.Instance.GetToken()))
            {
                ChangeControlStates(false);

                this.Invoke(() =>
                {
                    LockerAdditionInterface.Instance.PictureLoading.Image = Image.FromFile("./assets/warning.png");
                    TokenConfigurationInterface.Instance.TextBoxToken.Enabled = true;
                });
                
                RefreshFlag = false;
                return;
            }
            
            // Load the token file and write the encrypted token to it if it doesn't exist.
            Section data = Program.FileManager.AddSection("data");
            string path = data.AddDocument("token.gl");
                
            FileUtilExtensions.DumpBytesToFileBinary(path, [TokenConfigurationInterface.Instance.GetToken()]);
            this.StoredToken = TokenConfigurationInterface.DecodeToken(TokenConfigurationInterface.Instance.GetToken());
            RefreshFlag = false;
        }
        
        /// <summary>
        /// Changes the enabled state of the locker addition controls.
        /// </summary>
        /// <param name="state">The boolean state to set them to</param>
        public void ChangeControlStates(bool state)
        {
            this.Invoke(() =>
            {
                TokenConfigurationInterface.Instance.TextBoxToken.Enabled = state;

                foreach (Control control in MainLayout.Controls.OfType<Control>())
                {
                    if (control.GetType() == typeof(PictureBox) || control.Name.Contains("Lookup")) continue;
                    control.Enabled = state;
                }
            });
        }

        /// <summary>
        /// Switches the displayed interface to the new entry interface.
        /// </summary>
        private void ToolStripNewEntry_Click(object sender, EventArgs e)
        {
            this.MainLayout.SetAllFrom(LockerAdditionInterface.Instance.GetLayout());
            this.Text = LockerAdditionInterface.Instance.Text;
            
            if (RefreshFlag)
            {
                LockerAdditionInterface.Instance.PictureLoading.Image = Image.FromFile("./assets/loader.gif");
                Task.Run(RefreshToken);
            }
            
        }

        /// <summary>
        /// Switches the displayed interface to the dirt lookup interface. If the token refresh flag
        /// is set, then the token is refreshed.
        /// </summary>
        private void ToolStripDirtLookup_Click(object sender, EventArgs e)
        {
            this.MainLayout.SetAllFrom(DirtLookupInterface.Instance.GetLayout());
            this.Text = DirtLookupInterface.Instance.Text;
        }

        /// <summary>
        /// Switches the displayed interface to the token configuration interface, setting the
        /// token refresh flag to true.
        /// </summary>
        private void ToolStripTokenConfig_Click(object sender, EventArgs e)
        {
            this.MainLayout.SetAllFrom(TokenConfigurationInterface.Instance.GetLayout());
            this.Text = TokenConfigurationInterface.Instance.Text;
            this.RefreshFlag = true;
        }
    }
}