using System.DirectoryServices.Protocols;
using Elsa.Extensions;
using Elsa.Ldap.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Ldap.Activities;

/// <summary>
/// Delete entry from an LDAP directory.
/// </summary>
[Activity(
    Namespace = "Elsa",
    Category = "LDAP",
    DisplayName = "Delete LDAP entry",
    Description = "Deletes an entry from an LDAP directory.")]
public class DeleteLdapEntry : CodeActivity<bool>
{
    [Input(
        DisplayName = "Connection Name",
        Description = "The name of the LDAP connection to use, as configured in the module options. Defaults to 'Default'.")]
    public Input<string?> ConnectionName { get; set; } = default!;

    [Input(
        DisplayName = "Entry DN",
        Description = "The distinguished name of the entry to delete.")]
    public Input<string> EntryDn { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var logger = context.GetRequiredService<ILogger<DeleteLdapEntry>>();
        var ldapConnectionFactory = context.GetRequiredService<ILdapConnectionFactory>();

        var connectionName = context.Get(ConnectionName);
        var entryDn = context.Get(EntryDn)!;

        using var connection = ldapConnectionFactory.CreateConnection(connectionName);

        var request = new DeleteRequest(entryDn);

        var response = await connection.SendRequestAsync(request);

        if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
        {
            logger.LogError("{Status} - LDAP request (delete entry) failed: {Message}", response.ResultCode, response.ErrorMessage);
        }

        var result = response.ResultCode == ResultCode.Success;

        Result.Set(context, result);

        context.JournalData.Add("ResultCode", response.ResultCode);
        context.JournalData.Add("DeletedEntry", entryDn);
    }
}