// <copyright file="ShowCharacterListExtendedPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Character;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.GameServer.RemoteView.Guild;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The default implementation of the <see cref="IShowCharacterListPlugIn"/> which is forwarding everything to the game client with specific data packets.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.ShowCharacterListExtendedPlugIn_Name), Description = nameof(PlugInResources.ShowCharacterListExtendedPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("DDDEED0A-9421-4A9B-9ED8-0691B7051666")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public class ShowCharacterListExtendedPlugIn : IShowCharacterListPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowCharacterListExtendedPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public ShowCharacterListExtendedPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc/>
    public async ValueTask ShowCharacterListAsync()
    {
        var connection = this._player.Connection;
        if (connection is null || this._player.Account is not { } account)
        {
            return;
        }

        var unlockFlags = CreateUnlockFlags(account);
        await this.SendCharacterListAsync(connection, account, unlockFlags).ConfigureAwait(false);
        if (unlockFlags > CharacterCreationUnlockFlags.None)
        {
            await connection.SendCharacterClassCreationUnlockAsync(unlockFlags).ConfigureAwait(false);
        }
    }

    private static CharacterCreationUnlockFlags CreateUnlockFlags(Account account)
    {
        byte aggregatedFlags = 0;
        var result = account.UnlockedCharacterClasses?
            .Select(c => c.CreationAllowedFlag)
            .Aggregate(aggregatedFlags, (current, flag) => (byte)(current | flag)) ?? 0;
        return (CharacterCreationUnlockFlags)result;
    }

    private async ValueTask SendCharacterListAsync(IConnection connection, Account account, CharacterCreationUnlockFlags unlockFlags)
    {
        var guildPositions = new GuildPosition?[account.Characters.Count];
        int i = 0;
        foreach (var character in account.Characters)
        {
            guildPositions[i] = await this._player.GameServerContext.GuildServer.GetGuildPositionAsync(character.Id).ConfigureAwait(false);
            i++;
        }

        var appearanceSerializer = this._player.AppearanceSerializer;
        int Write()
        {
            var size = CharacterListExtendedRef.GetRequiredSize(account.Characters.Count);
            var span = connection.Output.GetSpan(size)[..size];
            var packet = new CharacterListExtendedRef(span)
            {
                UnlockFlags = unlockFlags,
                CharacterCount = (byte)account.Characters.Count,
                IsVaultExtended = account.IsVaultExtended,
            };

            var j = 0;
            foreach (var character in account.Characters.OrderBy(c => c.CharacterSlot))
            {
                var characterData = packet[j];
                characterData.SlotIndex = character.CharacterSlot;
                characterData.Name = character.Name;
                characterData.Level = (ushort)(character.Attributes.FirstOrDefault(s => s.Definition == Stats.Level)?.Value ?? 1);
                characterData.Status = character.CharacterStatus.Convert();
                characterData.GuildPosition = guildPositions[j].Convert();
                appearanceSerializer.WriteAppearanceData(characterData.Appearance, new CharacterAppearanceDataAdapter(character), false);

                // BroyalMU character-select preview: ShinyItem reserves bits 0-1
                // of its third byte (see AppearanceSerializerExtended.ShinyItem).
                // For a right-hand Dark Raven only, carry the server-authoritative
                // *visual tier*, not a fabricated item/character level:
                //   0 = 0..19, 1 = 20..39, 2 = 40+
                // The normal level/experience still comes from C1 A9 in-game.
                // For 27-byte preview appearance, the right-hand third byte is
                // 2 header + 3 left-hand + 2 = index 7.
                var equippedRaven = character.Inventory?.Items.FirstOrDefault(item =>
                    item.ItemSlot == 1 &&
                    item.Definition?.Group == 13 &&
                    item.Definition.Number == 5);
                if (equippedRaven is not null)
                {
                    Span<byte> preview = characterData.Appearance;
                    if (preview.Length >= 27)
                    {
                        byte tier = equippedRaven.Level >= 40
                            ? (byte)2
                            : equippedRaven.Level >= 20 ? (byte)1 : (byte)0;
                        preview[7] = (byte)((preview[7] & 0xFC) | tier);
                    }
                }

                j++;
            }

            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}