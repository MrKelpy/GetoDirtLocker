using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using LaminariaCore_Databases.sqlserver;

namespace GetosDirtLocker.utils;

/// <summary>
/// This class is responsible for handling the two image tables in the database,
/// adding and deleting them.
/// </summary>
public class DatabaseImageAccessor
{
    
    /// <summary>
    /// Adds an image to the database based on a specified table name, ID and path. This method
    /// takes care of BINARY data, not URLs.
    /// </summary>
    /// <param name="id">The ID of the image to add</param>
    /// <param name="path">The path to the image to add</param>
    /// <param name="table">The table to add the image to</param>
    private Task AddImageToDatabase(string id, string path, string table)
    {
        try
        {
            // Read the specified file data and create the command in a new connection.
            byte[] data = File.ReadAllBytes(path);
            SQLDatabaseManager database = Program.CreateManagerFromCredentials(Program.DefaultHost, Program.DefaultCredentials);
            
            using SqlCommand command = new($"INSERT INTO {table} VALUES (@id, @data)", database.Connector.Connection);

            // Add the parameters to the command template.
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@data", data);

            // Execute the command and return the result.
            command.ExecuteNonQuery();

        }
        // If an exception occurs, ignore.
        catch (Exception e) { return Task.CompletedTask; }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates an image in the database based on a specified table name, ID and path. This method
    /// takes care of BINARY data, not URLs.
    /// </summary>
    /// <param name="id">The ID of the image to be update</param>
    /// <param name="path">The path to the image to update the table with</param>
    /// <param name="table">The table to update the image with</param>
    private Task UpdateImageInDatabase(string id, string path, string table)
    {
        try
        {
            // Read the specified file data and create the command in a new connection.
            byte[] data = File.ReadAllBytes(path);
            SQLDatabaseManager database = Program.CreateManagerFromCredentials(Program.DefaultHost, Program.DefaultCredentials);

            // If the image does not exist, add it to the database instead.
            if (!ImageExists(id, table))
            {
                this.AddImageToDatabase(id, path, table);
                return Task.CompletedTask;
            }
            
            using SqlCommand command = new($"UPDATE {table} SET content = @data WHERE content_id = @id", database.Connector.Connection);

            // Add the parameters to the command template.
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@data", data);

            // Execute the command and return the result.
            command.ExecuteNonQuery();

        }
        // If an exception occurs, ignore.
        catch (Exception e) { return Task.CompletedTask; }

        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Checks if an image exists in the database based on a specified table name and ID.
    /// </summary>
    /// <param name="id">The image ID to check for</param>
    /// <param name="table">The table to check for the existence in</param>
    /// <returns></returns>
    private bool ImageExists(string id, string table)
    {
        try
        {
            SQLDatabaseManager database = Program.CreateManagerFromCredentials(Program.DefaultHost, Program.DefaultCredentials);
            return database.Select(table, $"content_id = '{id}'").Count > 0;
        }
        
        // If an exception occurs, return false.
        catch (Exception e) { return false; }
    }
    
    /// <summary>
    /// Gets an image from the database based on a specified table name and ID.
    /// </summary>
    /// <param name="id">The ID to get the image from</param>
    /// <param name="table">The storate table holding the image</param>
    /// <returns>A byte[] with the image contents</returns>
    private byte[] GetImage(string id, string table)
    {
        try
        {
            SQLDatabaseManager database = Program.CreateManagerFromCredentials(Program.DefaultHost, Program.DefaultCredentials);
            
            // Create the command and add the parameters.
            using SqlCommand command = new($"SELECT * FROM {table} WHERE content_id = @id", database.Connector.Connection);
            command.Parameters.AddWithValue("@id", id);
            
            // Execute the command and return the result.
            SqlDataReader reader = command.ExecuteReader();
            return reader.Read() ? (byte[])reader["content"] : null;
        }
        
        // If an exception occurs, return null.
        catch (Exception e) { return null; }
    }
    
    /// <summary>
    /// Shortcut method to check if a dirt image exists in the database.
    /// </summary>
    /// <param name="id">The ID to check for in the database</param>
    /// <returns>Whether or not the image exists</returns>
    public bool DirtImageExists(string id) => this.ImageExists(id, "AttachmentStorage");
    
    /// <summary>
    /// Shortcut method to check if an avatar image exists in the database.
    /// </summary>
    /// <param name="id">The ID to check for in the database</param>
    /// <returns>Whether or not the image exists</returns>
    public bool AvatarImageExists(string id) => this.ImageExists(id, "AvatarStorage");
    
    /// <summary>
    /// Shortcut method to add a dirt image to the database.
    /// </summary>
    /// <param name="id">The ID to check for in the database</param>
    /// <param name="path">The path of the image to add</param>
    /// <returns>Whether or not the addition of the image was successful</returns>
    public async Task AddDirtImageToDatabase(string id, string path) => await this.AddImageToDatabase(id, path, "AttachmentStorage");
    
    /// <summary>
    /// Shortcut method to add an avatar image to the database.
    /// </summary>
    /// <param name="id">The ID to check for in the database</param>
    /// <param name="path">The path of the image to add</param>
    /// <returns>Whether or not the addition of the image was successful</returns>
    public async Task AddAvatarImageToDatabase(string id, string path) => await this.AddImageToDatabase(id, path, "AvatarStorage");
    
    /// <summary>
    /// Updates a dirt image in the database.
    /// </summary>
    /// <param name="id">The ID to check for in the database</param>
    /// <param name="path">The path of the image to update with</param>
    public async Task UpdateDirtImageInDatabase(string id, string path) => await this.UpdateImageInDatabase(id, path, "AttachmentStorage");
    
    /// <summary>
    /// Updates an avatar image in the database.
    /// </summary>
    /// <param name="id">The ID to check for in the database</param>
    /// <param name="path">The path of the image to update with</param>
    public async Task UpdateAvatarImageInDatabase(string id, string path) => await this.UpdateImageInDatabase(id, path, "AvatarStorage");
    
    /// <summary>
    /// Shortcut method to get a dirt image from the database.
    /// </summary>
    /// <param name="id">The ID to check for in the database</param>
    /// <returns>A byte[] containing the image data</returns>
    public byte[] GetDirtImage(string id) => this.GetImage(id, "AttachmentStorage");
    
    /// <summary>
    /// Shortcut method to get an avatar image from the database.
    /// </summary>
    /// <param name="id">The ID to check for in the database</param>
    /// <returns>A byte[] containing the image data</returns>
    public byte[] GetAvatarImage(string id) => this.GetImage(id, "AvatarStorage");
}