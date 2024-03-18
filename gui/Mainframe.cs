using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GetosDirtLocker.Properties;
using GetosDirtLocker.requests;
using GetosDirtLocker.utils;
using LaminariaCore_Databases.sqlserver;
using LaminariaCore_General.common;
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
        /// The token configuration interface used to configure the token used to access the Discord API.
        /// </summary>
        public static TokenConfigurationInterface TokenInterface { get; set; }
        
        /// <summary>
        /// The locker addition interface used to add new dirt into the locker.
        /// </summary>
        public static LockerInterface LockerAddition { get; set; }
        
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
            
            // Sets the three interfaces into the properties defined above.
            TokenInterface = new TokenConfigurationInterface();
            LockerAddition = new LockerInterface();
            
            // Set the token configuration interface as the default interface.
            MainLayout.SetAllFrom(TokenInterface.GetLayout());
            Instance = this;
            
            // Load the token from the file if it exists.
            Section data = Program.FileManager.GetFirstSectionNamed("data");
            if (data == null) return;
            
            // If the token file doesn't exist, then we create it.
            string path = data.GetFirstDocumentNamed("token.gl");
            if (!File.Exists(path)) return;
                
            // Decrypt the token and set the stored token to it.
            byte[] token = FileUtilExtensions.ReadBytesFromBinary(path)?[0];

            if (token == null) return;
            this.StoredToken = TokenConfigurationInterface.DecodeToken(token);
        }
        
        /// <summary>
        /// Loads the mainframe, setting up any necessary configurations and settings needed.
        /// </summary>
        private void Mainframe_Load(object sender, EventArgs e)
        {
            this.Text = TokenInterface.Text;
            TextBox tokenBox = TokenInterface.TextBoxToken;
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
            if (!await DiscordInteractions.UpdateStatesBasedOnToken(TokenInterface.GetToken()))
            {
                ChangeControlStates(false);

                this.Invoke(() =>
                {
                    LockerAddition.PictureLoading.Image = Resources.warning;
                    TokenInterface.TextBoxToken.Enabled = true;
                    Instance.reloadEntriesToolStripMenuItem.Available = false;
                });
                
                RefreshFlag = false;
                return;
            }

            // Load the token file and write the encrypted token to it if it doesn't exist.
            Section data = Program.FileManager.AddSection("data");
            string path = data.AddDocument("token.gl");
                
            FileUtilExtensions.DumpBytesToFileBinary(path, [TokenInterface.GetToken()]);
            this.StoredToken = TokenConfigurationInterface.DecodeToken(TokenInterface.GetToken());
            
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
                TokenInterface.TextBoxToken.Enabled = state;

                foreach (Control control in MainLayout.Controls.OfType<Control>())
                {
                    if (control.GetType() == typeof(PictureBox)) continue;
                    control.Enabled = state;
                }
            });
        }

        /// <summary>
        /// Switches the displayed interface to the new entry interface.
        /// </summary>
        private void ToolStripNewEntry_Click(object sender, EventArgs e)
        {
            this.MainLayout.SetAllFrom(LockerAddition.GetLayout());
            this.Text = LockerAddition.Text;
            
            if (RefreshFlag)
            {
                LockerAddition.PictureLoading.Image = Resources.loader;
                Task.Run(RefreshToken);
            }
        }

        /// <summary>
        /// Switches the displayed interface to the token configuration interface, setting the
        /// token refresh flag to true.
        /// </summary>
        private void ToolStripTokenConfig_Click(object sender, EventArgs e)
        {
            this.MainLayout.SetAllFrom(TokenInterface.GetLayout());
            this.reloadEntriesToolStripMenuItem.Available = false;
            this.Text = TokenInterface.Text;
            this.RefreshFlag = true;
        }

        /// <summary>
        /// Reload all entries in both the locker addition interface and the lookup interface.
        /// </summary>
        private async void reloadEntriesToolStripMenuItem_Click(object sender, EventArgs e) => await LockerAddition.ReloadEntriesAsync();
        
    }
}