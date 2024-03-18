using System;
using System.Windows.Forms;
using GetosDirtLocker.requests;
using GetosDirtLocker.utils;

namespace GetosDirtLocker.gui;

/// <summary>
/// This form is used to display the contents of a single entry in the database.
/// </summary>
public partial class EntryViewingDialog : Form
{
    
    /// <summary>
    /// The user associated with this entry.
    /// </summary>
    private DiscordUser User { get; }
    
    /// <summary>
    /// The database entry data for this specific dirt.
    /// </summary>
    private string[] DatabaseEntry { get; }
    
    /// <summary>
    /// The DirtManager object that is used to manage the dirt's files.
    /// </summary>
    private DirtStorageManager DirtManager { get; }
    
    /// <summary>
    /// The database image accessor used to manage database image storage tables.
    /// </summary>
    private DatabaseImageAccessor ImageAccessor { get; }
    
    /// <summary>
    /// General constructor for the EntryViewingDialog. Sets up the user and indexationID properties as well
    /// as the page title.
    /// </summary>
    /// <param name="user">The discord user associated to this entry</param>
    /// <param name="entry">The database row entry data</param>
    /// <param name="dirtManager">The dirt storage manager used to retrieve the dirt information</param>
    /// <param name="accessor">The database image accessor used to manage database image storage tables</param>
    public EntryViewingDialog(DiscordUser user, string[] entry, DirtStorageManager dirtManager, DatabaseImageAccessor accessor)
    {
        InitializeComponent(); 
        this.Text += entry[0];  // Sets the title of the form to the dirt's indexationID
        
        this.User = user;
        this.DatabaseEntry = entry;
        this.DirtManager = dirtManager;
        this.ImageAccessor = accessor;
    }

    /// <summary>
    /// Loads in the data from the database entry into the form's fields.
    /// </summary>
    private async void EntryViewingDialog_Load(object sender, EventArgs e)
    {
        this.CenterToParent();
        PictureDirt.Image = FileUtilExtensions.GetImageFromFileStream(await DirtManager.GetDirtPicture(DatabaseEntry[2]));
        PictureBoxAvatar.Image = FileUtilExtensions.GetImageFromFileStream(await User.GetUserAvatar(ImageAccessor));
        
        string additionalInfo = DatabaseEntry[4].Length > 0 ? $@"{Environment.NewLine}Additional Information:{Environment.NewLine} {DatabaseEntry[4]}" : string.Empty;

        LabelUserInformation.Text = $@"UUID: {User.Uuid}{Environment.NewLine}{Environment.NewLine}Username: {DatabaseEntry[3]}";
        LabelDirtInformation.Text = $@"Indexation ID: {DatabaseEntry[0]}{Environment.NewLine}Attachment ID: {DatabaseEntry[2]}{Environment.NewLine}{additionalInfo}";
    }
}