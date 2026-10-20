// <copyright file="PartyMatchingRequestExtendedPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Party;

using System.Runtime.InteropServices;
using System.Text.Json;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PartyMatching;
using MUnique.OpenMU.GameLogic.Views.Party;
using MUnique.OpenMU.GameServer.RemoteView.Party;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>Parses the opt-in extended party matching protocol, version 1.</summary>
[PlugIn]
[Guid("658aa77c-9273-448c-b801-c17807625ecd")]
[Display(Name = nameof(PartyMatchingResources.Name), Description = nameof(PartyMatchingResources.Description), ResourceType = typeof(PartyMatchingResources))]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class PartyMatchingRequestExtendedPlugIn : IPacketHandlerPlugIn
{
    private static readonly JsonSerializerOptions JsonOptions = new() { MaxDepth = 8 };

    /// <inheritdoc />
    public byte Key => 0xED;

    /// <inheritdoc />
    public bool IsEncryptionExpected => true;

    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        PartyMatchingSnapshot snapshot;
        try
        {
            if (packet.Length is < 7 or > 2048 || packet.Span[4] != 1) return;
            var request = JsonSerializer.Deserialize<PartyMatchingRequest>(packet.Span[5..], JsonOptions);
            if (request == null || request.Id == null || request.Title == null || request.Password == null || request.Search == null ||
                request.Id.Length > 32 || request.Title.Length > 40 || request.Password.Length > 16 || request.Search.Length > 40) return;
            snapshot = await PartyMatchingDirectory.For(player).ExecuteAsync(player, request).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            snapshot = new PartyMatchingSnapshot { Status = 4 };
        }

        await player.InvokeViewPlugInAsync<IPartyMatchingViewPlugIn>(view => view.ShowAsync(snapshot)).ConfigureAwait(false);
    }
}
