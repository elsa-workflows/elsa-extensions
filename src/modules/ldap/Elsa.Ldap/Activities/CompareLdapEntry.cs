using System.DirectoryServices.Protocols;
using Elsa.Ldap.Contracts;
using Elsa.Ldap.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Ldap.Activities;

/// <summary>
/// Compares the specified attributes of an existing entry in an LDAP directory.
/// </summary>
[Activity(
    Namespace = "Elsa",
    Category = "LDAP",
    DisplayName = "Compare LDAP entry",
    Description = "Compares the specified attributes of an existing entry in an LDAP directory.")]
public class CompareLdapEntry : CodeActivity<bool>
{
    [Input(
        DisplayName = "Connection Name",
        Description = "The name of the LDAP connection to use, as configured in the module options. Defaults to 'Default'.")]
    public Input<string?> ConnectionName { get; set; } = default!;

    [Input(
        DisplayName = "Entry DN",
        Description = "The distinguished name of the entry to compare.")]
    public Input<string> EntryDn { get; set; } = default!;

    [Input(
        DisplayName = "Attribute",
        Description = "The attribute to assert. This attribute and value needs to be matched for success.")]
    public Input<DirectoryAttribute> Attribute { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var logger = context.GetRequiredService<ILogger<CompareLdapEntry>>();
        var ldapConnectionFactory = context.GetRequiredService<ILdapConnectionFactory>();

        var connectionName = context.Get(ConnectionName);
        var entryDn = context.Get(EntryDn)!;
        var attribute = context.Get(Attribute);

        using var connection = ldapConnectionFactory.CreateConnection(connectionName);

        var request = new CompareRequest(entryDn, attribute);

        var response = await connection.SendRequestAsync(request);

        if (response.ResultCode.IsError())
        {
            logger.LogError("{Status} - LDAP request (compare entry) failed: {Message}", response.ResultCode, response.ErrorMessage);
        }

        context.Set(Result, response.ResultCode == ResultCode.CompareTrue);

        context.JournalData.Add("ResultCode", response.ResultCode);
        context.JournalData.Add("ComparedEntry", entryDn);
    }
}