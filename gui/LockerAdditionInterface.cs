﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using GetosDirtLocker.utils;
using LaminariaCore_Databases.sqlserver;

namespace GetosDirtLocker.gui
{
    /// <summary>
    /// The main interface for adding new dirt into the locker. Also displays the last few entries.
    /// </summary>
    public partial class LockerAdditionInterface : Form
    {
        
        /// <summary>
        /// The database manager used to access and interact with the db
        /// </summary>
        private SQLDatabaseManager Database { get; }

        /// <summary>
        /// The dirt manager used to download and cache pictures relative to dirt
        /// </summary>
        private DirtStorageManager DirtManager { get; }
        
        /// <summary>
        /// The currently selected row in the DataGridView
        /// </summary>
        private DataGridViewRow SelectedRow { get; set; }
        
        /// <summary>
        /// Main constructor of the class
        /// </summary>
        /// <param name="manager">The database manager used to access and interact with the db</param>
        public LockerAdditionInterface(SQLDatabaseManager manager)
        {
            InitializeComponent();
            PictureBoxPermanentLoading.Image = Program.LoaderImage;
            this.Database = manager;
            this.DirtManager = new DirtStorageManager(Database);
            
            GridDirt.RowTemplate.Height = 100;
            GridDirt.ClearSelection();
        }

        /// <returns>
        /// Returns the frame of the form, containing all the elements.
        /// </returns>
        public Panel GetLayout() => this.Frame;

        /// <summary>
        /// Returns the list of entries allowed to be displayed based on the set filters
        /// </summary>
        /// <returns>The list of rows to add to the database</returns>
        private List<string[]> GetFilteredEntries()
        {
            string indexationFilter = !TextBoxIndexLookup.Text.Equals(string.Empty) ? $"indexation_id LIKE '%{TextBoxIndexLookup.Text}%'" : "";
            string userUUIDFilter = !TextBoxUserUUIDLookup.Text.Equals(string.Empty) ? $"user_id LIKE '%{TextBoxUserUUIDLookup.Text}%'" : "";
            string notesFilter = !TextBoxNotesLookup.Text.Equals(string.Empty) ? $"notes LIKE '%{TextBoxNotesLookup.Text}%'" : "";
            string finalFilter = string.Empty;
            
            // If all the filters are empty, return all the entries
            if (indexationFilter == "" && userUUIDFilter == "" && notesFilter == "")
                return this.Database.Select("Dirt");
            
            // Puts the filters together in a string ignoring any empty ones
            if (indexationFilter != "") finalFilter += indexationFilter;
            if (userUUIDFilter != "") finalFilter += finalFilter == "" ? userUUIDFilter : $" AND {userUUIDFilter}";
            if (notesFilter != "") finalFilter += finalFilter == "" ? notesFilter : $" AND {notesFilter}";
            
            // Returns the filtered entries
            return this.Database.Select("Dirt", finalFilter);
        }
        
        /// <summary>
        /// Add all the entries into the DataGridView
        /// </summary>
        public async Task ReloadEntries()
        { 
            // Get all the dirt entries existent in the database and initialise an array for the rows
            Mainframe.Instance.reloadEntriesToolStripMenuItem.Available = false;
            List<string[]> dirtEntries = GetFilteredEntries();

            dirtEntries.Reverse();
            GridDirt.Rows.Clear();
            this.ForceClearSelections();

            // The list of contents to add in tasks
            List<Task> taskList = dirtEntries.Select(ProcessEntryAddition).ToList();
            await Task.WhenAll(taskList);
            
            Mainframe.Instance.reloadEntriesToolStripMenuItem.Available = true;
        }

        /// <summary>
        /// Forcibly clears the selection of the DataGridView so that the state of the custom
        /// selection is as if the program had just started
        /// </summary>
        private void ForceClearSelections()
        {
            GridLoadingFlag = true;  // Tags the next selection change event as the first one

            // Resets the previously selected row to the default colour
            if (this.SelectedRow != null)
                this.SelectedRow.DefaultCellStyle.BackColor = Color.White;

            // Clears the selection and resets the selected row to null
            this.SelectedRow = null;
            GridDirt.ClearSelection();
        }

        /// <summary>
        /// Handles the asynchronous addition of content into the DataGridView
        /// </summary>
        /// <param name="entry">The entry information to add into the grid</param>
        private async Task ProcessEntryAddition(string[] entry)
        {
            DiscordUser user = new DiscordUser(entry[1]);
            string informationString = user.GetInformationString(this.Database, entry[0]);
                
            // Gets the attachment ID from the indexation ID
            List<string[]> result = this.Database.Select(["attachment_id"], "Dirt", $"indexation_id = '{entry[0]}'");
            string dirtPath = await DirtManager.GetDirtPicture(result[0][0]);
            
            // Creates a DataGridViewRow and add it to the gridContents array
            Image userAvatar = FileUtilExtensions.GetImageFromFileStream(await user.GetUserAvatar());
            Image dirtImage = FileUtilExtensions.GetImageFromFileStream(dirtPath);
            GridDirt.Rows.Add( entry[0], entry[1], userAvatar, informationString, dirtImage);
        }

        /// <summary>
        /// Adds a new dirt entry to the locker based on the provided data.
        /// </summary>
        private async void buttonAdd_Click(object sender, EventArgs e)
        {
            this.SetAdditionInLoadingState(true);
            GeneralErrorProvider.Clear();

            if (!ulong.TryParse(TextBoxUserUUID.Text, out ulong _))
            {
                GeneralErrorProvider.SetError(TextBoxUserUUID, "Wrongly formatted UUID (Numbers only!)");
                this.SetAdditionInLoadingState(false);
                return;
            }
            
            DiscordUser user = new DiscordUser(TextBoxUserUUID.Text);
            
            // Checks if the user UUID is empty.
            if (!await user.CheckAccountExistence())
            {
                GeneralErrorProvider.SetError(TextBoxUserUUID, "This user does not exist.");
                this.SetAdditionInLoadingState(false);
                return;
            }

            // Checks if the URL is a valid url
            if (!await DirtStorageManager.UrlIsDownloadablePicture(TextBoxAttachmentURL.Text))
            {
                GeneralErrorProvider.SetError(TextBoxAttachmentURL, "Invalid URL");
                this.SetAdditionInLoadingState(false);
                return;
            }
            
            user.AddToDatabase(this.Database);  // Adds the user to the database if they don't exist.
            
            string indexationID = user.GetNextIndexationID(this.Database);
            string username = user.TryGetAdocordUsername() == "" ? (await user.GetUserFromID()).Username : "Unknown";
            
            // Gets the file type and size of the attachment
            using WebResponse response = await WebRequest.Create(TextBoxAttachmentURL.Text).GetResponseAsync();
            string fileType = response.ContentType;
            long fileSize = response.ContentLength;
            
            // Inserts the attachment into the database and then the dirt entry.
            this.Database.InsertInto("Attachment", fileType, TextBoxAttachmentURL.Text, fileSize);
            string attachmentID = this.Database.Select(["attachment_id"], "Attachment", $"attachment_url = '{TextBoxAttachmentURL.Text}'")[0][0];
            
            this.Database.InsertInto("Dirt", indexationID, user.Uuid.ToString(), int.Parse(attachmentID), username, TextBoxAdditionalNotes.Text);
            user.IncrementTotalDirtCount(this.Database);
            
            // Builds the information string
            string informationString = user.GetInformationString(this.Database, indexationID);
            
            // Inserts the dirt entry into the DataGridView
            string dirtPath = await DirtManager.GetDirtPicture(attachmentID);

            Image userAvatarCopy = FileUtilExtensions.GetImageFromFileStream(await user.DownloadUserAvatar());
            Image dirtImageCopy = FileUtilExtensions.GetImageFromFileStream(dirtPath);
            GridDirt.Rows.Insert(0, indexationID, user.Uuid, userAvatarCopy, informationString, dirtImageCopy);
            
            // Clears the textboxes and sets the addition button to a normal state
            TextBoxUserUUID.Text = TextBoxAttachmentURL.Text = TextBoxAdditionalNotes.Text = "";
            this.SetAdditionInLoadingState(false);
        }
        
        /// <summary>
        /// Sets the addition button to a loading state or a normal state based on the provided boolean. This
        /// is purely visual and serves to prevent the user from trying to add multiple entries at the same time
        /// and to be a progress indicator.
        /// </summary>
        /// <param name="state">The loading state specified</param>
        private void SetAdditionInLoadingState(bool state)
        {
            PictureBoxPermanentLoading.Visible = state;
            buttonAdd.Visible = !state;
        }

        /// <summary>
        /// Keeps clearing the selection on the DataGridView so that it never displays a blue selected colour on it.
        /// After that, change the foreground colour of the cell to a slightly darker one, and set the previously
        /// selected cell to the default colour.
        /// </summary>
        private bool GridLoadingFlag { get; set; } = true;
        
        private void GridDirt_SelectionChanged(object sender, EventArgs e)
        {
            // Ignores the first selection change event, as it is triggered when the grid is loading.
            if (GridLoadingFlag)
            {
                GridLoadingFlag = false;
                return;
            };
            
            // Resets the previous selected row to the default colour
            if (SelectedRow != null) SelectedRow.DefaultCellStyle.BackColor = Color.White;
            if (GridDirt.CurrentRow != null && this.SelectedRow != null) GridDirt.CurrentRow.DefaultCellStyle.BackColor = Color.Khaki;
            
            this.SelectedRow = GridDirt.CurrentRow;
            GridDirt.ClearSelection();
        }

        /// <summary>
        /// Copies the content inside any of the information column cells showing a "copied to clipboard" prompt
        /// followed by the reposition of the original content.
        /// </summary>
        private async void GridDirt_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != 3) return;

            // Gets the clicked cell and the relevant contents
            DataGridViewCell cell = GridDirt.Rows[e.RowIndex].Cells[3];
            string originalContent = cell.Value.ToString();

            if (originalContent == "Copied to Clipboard") return;
            
            string userId = GridDirt.Rows[e.RowIndex].Cells[1].Value.ToString();
            string indexationId = GridDirt.Rows[e.RowIndex].Cells[0].Value.ToString();
            DiscordUser user = new DiscordUser(userId);
            
            // Gets the information formatted in the discord-pasteable format
            string information = user.GetInformationString(this.Database, indexationId, true);
            Clipboard.SetData(DataFormats.Text, information);

            cell.Value = "Copied to Clipboard";
            await Task.Delay(1 * 1000);
            cell.Value = originalContent;
        }
        
        /// <summary>
        /// Highlights the row when the mouse enters it by changing the background colour to a light grey, unless
        /// it is the currently selected row, in which case it does nothing.
        /// </summary>
        private void GridDirt_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex <= -1) return;
            if (GridDirt.Rows[e.RowIndex] == SelectedRow) return;
            
            DataGridViewRow row = GridDirt.Rows[e.RowIndex];
            row.DefaultCellStyle.BackColor = Color.LightGray;
        }
        
        /// <summary>
        /// Returns the row to its original colour when the mouse leaves it, unless it is the currently selected row,
        /// in which case it does nothing.
        /// </summary>
        private void GridDirt_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex <= -1) return;
            if (GridDirt.Rows[e.RowIndex] == SelectedRow) return;
            
            DataGridViewRow row = GridDirt.Rows[e.RowIndex];
            row.DefaultCellStyle.BackColor = Color.White;
        }
        
        private void ButtonViewEntry_Click(object sender, EventArgs e)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Deletes the selected entry from the database and the DataGridView
        /// </summary>
        private void ButtonDeleteEntry_Click(object sender, EventArgs e)
        {
            if (this.SelectedRow == null) return;
            if (MessageBox.Show(@"Are you sure you want to delete this entry?", @"Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            
            string indexationId = this.SelectedRow.Cells[0].Value.ToString();
            string attachmentId = this.Database.Select(["attachment_id"], "Dirt", $"indexation_id = '{indexationId}'")[0][0];
            
            this.Database.DeleteFrom("Attachment", $"attachment_id = '{attachmentId}'");
            this.Database.DeleteFrom("Dirt", $"indexation_id = '{indexationId}'");
            GridDirt.Rows.Remove(this.SelectedRow);
            
            this.SelectedRow = null;
        }
        
        /// <summary>
        /// Reloads the grid with the filters, disabling the button and setting a "Loading" message on it until it
        /// the process is finished.
        /// </summary>
        private async void ButtonApplyFilters_Click(object sender, EventArgs e)
        {
            ButtonApplyFilters.Text = @"Loading...";
            ButtonApplyFilters.Enabled = false;
            await ReloadEntries();
            ButtonApplyFilters.Enabled = true;
            ButtonApplyFilters.Text = @"Apply Filters";
        }
    }
}