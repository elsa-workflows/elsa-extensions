using System.DirectoryServices.Protocols;
using Elsa.Ldap.Contracts;
using Elsa.Ldap.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Ldap.Activities;

/// <summary>
/// Modifies attributes of an existing entry in an LDAP directory.
/// </summary>
[Activity(
    Namespace = "Elsa",
    Category = "LDAP",
    DisplayName = "Modify LDAP entry",
    Description = "Modifies one or more attributes of an existing entry in an LDAP directory.")]
public class ModifyLdapEntry : CodeActivity<bool>
{
    [Input(
        DisplayName = "Connection Name",
        Description = "The name of the LDAP connection to use, as configured in the module options. Defaults to 'Default'.")]
    public Input<string?> ConnectionName { get; set; } = default!;

    [Input(
        DisplayName = "Entry DN",
        Description = "The distinguished name of the entry to modify.")]
    public Input<string> EntryDn { get; set; } = default!;

    [Input(
        DisplayName = "Modifications",
        Description = "The list of attribute modifications to apply to the entry.")]
    public Input<IEnumerable<DirectoryAttributeModification>> Modifications { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var logger = context.GetRequiredService<ILogger<ModifyLdapEntry>>();
        var ldapConnectionFactory = context.GetRequiredService<ILdapConnectionFactory>();

        var connectionName = context.Get(ConnectionName);
        var entryDn = context.Get(EntryDn)!;
        var modifications = context.Get(Modifications) ?? [];

        using var connection = ldapConnectionFactory.CreateConnection(connectionName);

        var request = new ModifyRequest(entryDn, modifications.ToArray());

        var response = await connection.SendRequestAsync(request);

        if (response.ResultCode.IsError())
        {
            logger.LogError("{Status} - LDAP request (modify entry) failed: {Message}", response.ResultCode, response.ErrorMessage);
        }

        context.Set(Result, response.ResultCode.IsSuccess());

        context.JournalData.Add("ResultCode", response.ResultCode);
        context.JournalData.Add("ModifiedEntry", entryDn);
        context.JournalData.Add("ModificationCount", request.Modifications.Count);
    }
}