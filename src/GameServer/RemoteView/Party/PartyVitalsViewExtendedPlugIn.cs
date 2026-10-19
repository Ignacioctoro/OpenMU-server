// <copyright file="PartyVitalsViewExtendedPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Party;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views.Party;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends full health and mana to extended clients without changing the Season 6 packet.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.PartyHealthViewPlugIn_Name), Description = nameof(PlugInResources.PartyHealthViewPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("4fa79d62-ebc8-4db5-a1a1-2717576ea292")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public class PartyVitalsViewExtendedPlugIn : IPartyHealthViewPlugIn
{
    private readonly RemotePlayer _player;
    private (string Name, uint Health, uint MaximumHealth, uint Mana, uint MaximumMana)[]? _values;
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartyVitalsViewExtendedPlugIn"/> class.
    /// </summary>
    /// <param name="player">The receiving player.</param>
    public PartyVitalsViewExtendedPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc/>
    public bool IsHealthUpdateNeeded() => this.RefreshValues();

    /// <inheritdoc/>
    public async ValueTask UpdatePartyHealthAsync()
    {
        this.RefreshValues();
        var connection = this._player.Connection;
        if (connection is null || this._count == 0 || this._values is null)
        {
            return;
        }

        int Write()
        {
            var size = PartyVitalsUpdateExtendedRef.GetRequiredSize(this._count);
            var span = connection.Output.GetSpan(size)[..size];
            var packet = new PartyVitalsUpdateExtendedRef(span) { Count = (byte)this._count };
            for (int i = 0; i < this._count; i++)
            {
                var value = this._values[i];
                var member = packet[i];
                member.Index = (byte)i;
                member.CurrentHealth = value.Health;
                member.MaximumHealth = value.MaximumHealth;
                member.CurrentMana = value.Mana;
                member.MaximumMana = value.MaximumMana;
            }

            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    private bool RefreshValues()
    {
        var party = this._player.Party;
        if (party is null)
        {
            this._count = 0;
            return false;
        }

        this._values ??= new (string, uint, uint, uint, uint)[party.MaxPartySize];
        var members = party.PartyList;
        bool changed = this._count != members.Count;
        this._count = members.Count;
        for (int i = 0; i < this._count; i++)
        {
            var member = members[i];
            var attributes = (member as Player)?.Attributes;
            var value = (member.Name, member.CurrentHealth, member.MaximumHealth,
                (uint)Math.Max(0, attributes?[Stats.CurrentMana] ?? 0),
                (uint)Math.Max(0, attributes?[Stats.MaximumMana] ?? 0));
            if (this._values[i] != value)
            {
                this._values[i] = value;
                changed = true;
            }
        }

        return changed;
    }
}
