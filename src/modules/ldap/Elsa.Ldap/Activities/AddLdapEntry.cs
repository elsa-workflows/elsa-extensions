using System.DirectoryServices.Protocols;
using Elsa.Ldap.Contracts;
using Elsa.Ldap.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Ldap.Activities;

/// <summary>
/// Add new entry to an LDAP directory.
/// </summary>
[Activity(
    Namespace = "Elsa",
    Category = "LDAP",
    DisplayName = "Add LDAP entry",
    Description = "Adds a new entry to an LDAP directory.")]
public class AddLdapEntry : CodeActivity<bool>
{
    [Input(
        DisplayName = "Connection Name",
        Description = "The name of the LDAP connection to use, as configured in the module options. Defaults to 'Default'.")]
    public Input<string?> ConnectionName { get; set; } = default!;

    [Input(
        DisplayName = "Entry DN",
        Description = "The distinguished name of the entry to add.")]
    public Input<string> EntryDn { get; set; } = default!;

    [Input(
        DisplayName = "Attributes",
        Description = "The list of attributes to set on the new entry.")]
    public Input<IEnumerable<DirectoryAttribute>> Attributes { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var logger = context.GetRequiredService<ILogger<AddLdapEntry>>();
        var ldapConnectionFactory = context.GetRequiredService<ILdapConnectionFactory>();

        var connectionName = context.Get(ConnectionName);
        var entryDn = context.Get(EntryDn)!;
        var attributes = context.Get(Attributes) ?? [];

        using var connection = ldapConnectionFactory.CreateConnection(connectionName);

        var request = new AddRequest(entryDn, attributes.ToArray());

        var response = await connection.SendRequestAsync(request);

        if (response.ResultCode.IsError())
        {
            logger.LogError("{Status} - LDAP request (add entry) failed: {Message}", response.ResultCode, response.ErrorMessage);
        }

        context.Set(Result, response.ResultCode.IsSuccess());

        context.JournalData.Add("ResultCode", response.ResultCode);
        context.JournalData.Add("AddedEntry", entryDn);
        context.JournalData.Add("AttributesCount", request.Attributes.Count);
    }
}