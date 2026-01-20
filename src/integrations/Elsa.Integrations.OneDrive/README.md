# Elsa Workflows OneDrive Integration

This module provides integration with OneDrive using the Microsoft Graph API, allowing workflows to interact with OneDrive for file and folder operations.

## Getting Started

### Registration

Register the OneDrive module in your Elsa application:

```csharp
services.AddElsa(elsa => 
{
    elsa.UseOneDrive(options => 
    {
        options.TenantId = "your-tenant-id";
        options.ClientId = "your-client-id";
        options.ClientSecret = "your-client-secret";
        options.Scopes = new[] { "https://graph.microsoft.com/.default" };
    });
});
```

### Authentication

This module uses Azure AD authentication to connect to Microsoft Graph API. You'll need:

1. An Azure AD registered app with appropriate permissions for OneDrive/Microsoft Graph
2. A client secret for your app
3. The tenant ID of your Azure AD directory

## Available Activities

### File Operations

- **CopyFile**: Copies a file to a new location in OneDrive
- **DeleteFileOrFolder**: Deletes a file or folder from OneDrive
- **DownloadFile**: Downloads a file from OneDrive
- **GetFile**: Gets metadata for a file or folder
- **MoveFileOrFolder**: Moves a file or folder to a new location
- **RenameFileOrFolder**: Renames a file or folder
- **UploadFile**: Uploads a file to OneDrive
- **UploadFileByURL**: Uploads a file to OneDrive from a URL (only for OneDrive Personal)

### Folder Operations

- **CreateFolder**: Creates a new folder in OneDrive

### Sharing

- **GetShareLink**: Gets a sharing link for a file or folder
- **SendSharingInvitation**: Sends sharing invitations with customizable permissions

### Search and Listing

- **ListDrives**: Lists available drives in OneDrive
- **SearchFilesOrFolders**: Searches for files or folders
- **SearchSites**: Searches for SharePoint sites

### Triggers

- **WatchFiles**: Triggers when files are created or modified
- **WatchFilesOrFolders**: Triggers when files or folders are created or modified

### Advanced

- **MakeAPICall**: Makes an arbitrary API call to Microsoft Graph API

## Example Usage

### Uploading a File

```csharp
// Create a simple workflow that uploads a file to OneDrive
var workflow = new WorkflowDefinition
{
    Activities =
    {
        new UploadFile
        {
            Content = new Input<object>("Hello, World!"),
            DestinationPath = new Input<string>("Documents/hello.txt")
        }
    }
};
```

### Creating a Folder and Copying a File

```csharp
// Create a workflow that creates a folder and copies a file into it
var workflow = new WorkflowDefinition
{
    Activities =
    {
        new CreateFolder
        {
            Id = "createFolder",
            FolderName = new Input<string>("NewFolder")
        },
        new CopyFile
        {
            ItemIdOrPath = new Input<string>("Documents/original.docx"),
            DestinationFolderId = new Input<string>(context => context.GetResult<DriveItem>("createFolder").Id),
            NewName = new Input<string>("copied.docx")
        }
    }
};
```

## References

- [Microsoft Graph API Documentation](https://learn.microsoft.com/en-us/graph/api/overview)
- [OneDrive API Reference](https://learn.microsoft.com/en-us/onedrive/developer/)