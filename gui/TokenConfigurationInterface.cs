using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using LaminariaCore_General.common;

namespace GetosDirtLocker.gui
{
    /// <summary>
    /// This interface is used to configure the token used to access the Discord API.
    /// </summary>
    public partial class TokenConfigurationInterface : Form
    {
        
        /// <summary>
        /// General constructor of the class.
        /// </summary>
        public TokenConfigurationInterface()
        {
            InitializeComponent();
            this.SetRandomAvatars();
        }

        /// <summary>
        /// Iterates through the avatar panels and sets a random avatar for each one.
        /// </summary>
        private void SetRandomAvatars()
        {
            Section avatars = Program.FileManager.AddSection("avatars");
            string[] avatarCount = Directory.GetFiles(avatars.SectionFullPath);
            Random random = new();
            
            // Sets a random avatar for each avatar slot based on the number of avatars available.
            foreach (var avatarSlot in AvatarsLayout.Controls.OfType<PictureBox>())
            {
                // If there are no avatars, set the default avatar.
                if (avatarCount.Length <= 0)
                {
                    avatarSlot.Load(ConfigurationManager.AppSettings.Get("default-avatar"));
                    continue;
                }

                // Chooses a random avatar from the list of avatars and sets it to the slot.
                string filepath = avatarCount[random.Next(0, avatarCount.Length)];
                
                byte[] bytes = File.ReadAllBytes(filepath);
                MemoryStream ms = new MemoryStream(bytes);
                avatarSlot.Image = Image.FromStream(ms);
            }
        }

        /// <returns>
        /// Prepares the form and returns the frame, containing all the elements.
        /// </returns>
        public Panel GetLayout() => this.Frame;

        /// <summary>
        /// Returns the token stored in the text box.
        /// </summary>
        public byte[] GetToken()
        {
            byte[] muid = Encoding.UTF8.GetBytes(GetUniqueMachineID()); // Gets the unique machine ID.
            
            return ProtectedData.Protect(Encoding.UTF8.GetBytes(TextBoxToken.Text), muid,
                DataProtectionScope.LocalMachine);
        }
        
        /// <summary>
        /// Tries to decode the token and returns it as a string.
        /// </summary>
        /// <param name="token">The token byte[] to decode</param>
        /// <returns>A string representation of the token</returns>
        public static string DecodeToken(byte[] token)
        {
            byte[] muid = Encoding.UTF8.GetBytes(GetUniqueMachineID()); // Gets the unique machine ID.
            
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(token, muid, DataProtectionScope.LocalMachine));
        }

        /// <summary>
        /// Looks into the hardware of the machine for machine-specific information, hashes it and returns
        /// it to be used as a unique machine ID for encryption and decryption.
        /// </summary>
        public static string GetUniqueMachineID()
        {
            string hardwareInfo = string.Empty;

            // Create a searcher for the motherboard and processor.
            ManagementObjectSearcher boardSearcher = new ("SELECT * FROM Win32_BaseBoard");
            ManagementObjectSearcher processorSearcher = new ("SELECT * FROM Win32_Processor");
            
            // Get the hardware information from the motherboard, adding the manufacturer and product to the string.
            foreach (var board in boardSearcher.Get())
                hardwareInfo += board["Manufacturer"].ToString() + board["Product"];

            // Get the hardware information from the processor, adding the manufacturer and processor ID to the string.
            foreach (var processor in processorSearcher.Get())
                hardwareInfo += processor["Manufacturer"].ToString() + processor["ProcessorId"];

            return Encoding.UTF8.GetString(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(hardwareInfo)));
        }
    }
}