using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Discord;
using Discord.WebSocket;
using GetosDirtLocker.gui;
using GetosDirtLocker.Properties;
using GetosDirtLocker.utils;

namespace GetosDirtLocker.requests
{
    /// <summary>
    /// This is a kind of rough API Wrapper for the Discord API using user tokens.
    /// </summary>
    public static class DiscordInteractions
    {
        
        /// <summary>
        /// The discord client that is used to interact with the API. Updated with UpdateStatesBasedOnToken.
        /// </summary>
        public static DiscordSocketClient Client { get; set; }
        
        /// <summary>
        /// Updates all the fields that are dependent on the token to be functional to either be visible or not
        /// based on whether the token is valid or not.
        /// </summary>
        /// <param name="token">The encrypted token to check.</param>
        public static async Task<bool> UpdateStatesBasedOnToken(byte[] token)
        {
            if (!await LoginClient(token)) return false;
            
            // Whenever the client is ready, unlock the token configuration interface and load the user's avatar.
            Client.Ready += () =>
            {
                // If the client doesn't, then don't do anything.
                if (Client.CurrentUser == null) return Task.CompletedTask;

                Mainframe.Instance.Invoke(new MethodInvoker(async () =>
                {
                    // Get the avatar URL and load it into the picture box.
                    string avatar_url = Client.CurrentUser.GetAvatarUrl();
                    Mainframe.LockerAddition.PictureLoading.Load(avatar_url);

                    // Enable the token configuration interface and change the control states.
                    Mainframe.TokenInterface.TextBoxToken.Enabled = true;
                    Mainframe.Instance.ChangeControlStates(true);
                    
                    await Mainframe.LockerAddition.ReloadEntriesAsync();
                }));
                
                return Task.CompletedTask;
            };

            Client.Disconnected += (e) =>
            {
                // If the client is disconnected, then we disable the token configuration interface and show a warning.
                Mainframe.Instance.Invoke(new MethodInvoker(() =>
                {
                    Mainframe.TokenInterface.TextBoxToken.Enabled = true;
                    Mainframe.Instance.ChangeControlStates(false);
                    Mainframe.Instance.reloadEntriesToolStripMenuItem.Available = false;
                    Mainframe.LockerAddition.PictureLoading.Image = FileUtilExtensions.GetImageFromFileStream(Resources.warning);
                }));

                Client.StopAsync();
                return Task.CompletedTask;
            };
            
            return true;
        }

        /// <summary>
        /// Logs in the client with the provided token.
        /// </summary>
        /// <param name="token">The byte[] containing the token</param>
        public static async Task<bool> LoginClient(byte[] token)
        {
            try
            {
                // Decode the token from the encrypted byte array.
                string tokenString = TokenConfigurationInterface.DecodeToken(token);
                TokenUtils.ValidateToken(TokenType.Bearer, tokenString);

                // Create a new client and log in with the token as a 'bot'
                Client = new DiscordSocketClient();
                Client.Log += Log;

                await Client.LoginAsync(TokenType.Bot, tokenString, false);
                await Client.StartAsync();
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Logs a message to the console.
        /// </summary>
        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}