// <copyright file="PartyMatchingViewExtendedPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Party;

using System.Runtime.InteropServices;
using System.Text.Json;
using MUnique.OpenMU.GameLogic.PartyMatching;
using MUnique.OpenMU.GameLogic.Views.Party;
using MUnique.OpenMU.Network.Packets.PartyMatching;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>Replies only to extended clients which request matching, preserving legacy S6 party packets.</summary>
[PlugIn]
[Guid("32ec92fc-df81-4571-ae0d-e043dfe382a1")]
[Display(Name = nameof(PartyMatchingResources.Name), Description = nameof(PartyMatchingResources.Description), ResourceType = typeof(PartyMatchingResources))]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class PartyMatchingViewExtendedPlugIn : IPartyMatchingViewPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>Initializes the view for one player.</summary>
    public PartyMatchingViewExtendedPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowAsync(PartyMatchingSnapshot snapshot)
    {
        if (this._player.Connection is not { } connection) return;
        var body = JsonSerializer.SerializeToUtf8Bytes(snapshot);
        await connection.SendPartyMatchingSnapshotExtendedAsync(body).ConfigureAwait(false);
    }
}
