// <copyright file="GuildRoleAssignHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Guild;

using System;

using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Views.Guild;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handles requests of a Guild Master
/// to change the role of a guild member.
/// </summary>
[PlugIn]
[Guid("7B73162F-93D2-4C7F-9276-4D2DF498E301")]
internal class GuildRoleAssignHandlerPlugIn
    : IPacketHandlerPlugIn
{
/// <inheritdoc/>
    public bool IsEncryptionExpected =>
        false;

    /// <inheritdoc/>
    public byte Key =>
        GuildRoleAssignRequest.Code;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(
        Player player,
        Memory<byte> packet)
    {
        GuildRoleAssignRequest request =
            packet;

        if (player.GuildStatus is not { } guildStatus)
        {
            player.Logger.LogWarning(
                "Guild role change rejected: player is not in a guild.");

            return;
        }

        if (guildStatus.Position !=
            GuildPosition.GuildMaster)
        {
            player.Logger.LogWarning(
                "Guild role change rejected: {Player} is not Guild Master.",
                player.Name);

            return;
        }

        string targetName =
            request.PlayerName;

        if (string.IsNullOrWhiteSpace(
                targetName))
        {
            return;
        }

        // No permitir cambiar el rango propio
        // desde este sistema.
        if (string.Equals(
                targetName,
                player.SelectedCharacter?.Name,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        GuildPosition newPosition =
            request.Role switch
            {
                GuildMemberRole.NormalMember =>
                    GuildPosition.NormalMember,

                GuildMemberRole.BattleMaster =>
                    GuildPosition.BattleMaster,

                GuildMemberRole.AssistantMaster =>
                    GuildPosition.AssistantMaster,

                // No permitimos transferir Guild Master
                // mediante este botón.
                _ =>
                    GuildPosition.Undefined,
            };

        if (newPosition ==
            GuildPosition.Undefined)
        {
            player.Logger.LogWarning(
                "Invalid guild role requested: {Role}.",
                request.Role);

            return;
        }

        if (player.GameContext is not
            IGameServerContext gameContext)
        {
            return;
        }

        var guildServer =
            gameContext.GuildServer;

        // Confirmar que el target pertenece
        // realmente a ESTA guild.
        var members =
            await guildServer
                .GetGuildListAsync(
                    guildStatus.GuildId)
                .ConfigureAwait(false);

        bool targetBelongsToGuild =
            members.Any(
                member =>
                    string.Equals(
                        member.PlayerName,
                        targetName,
                        StringComparison.OrdinalIgnoreCase));

        if (!targetBelongsToGuild)
        {
            player.Logger.LogWarning(
                "Guild role change rejected: {Target} is not in guild {GuildId}.",
                targetName,
                guildStatus.GuildId);

            return;
        }

        // Obtener el Guid persistente del personaje.
        var account =
            await player.PersistenceContext
                .GetAccountByCharacterNameAsync(
                    targetName)
                .ConfigureAwait(false);

        var targetCharacter =
            account?
                .Characters
                .FirstOrDefault(
                    character =>
                        string.Equals(
                            character.Name,
                            targetName,
                            StringComparison.OrdinalIgnoreCase));

        if (targetCharacter is null)
        {
            player.Logger.LogWarning(
                "Guild role change failed: character {Target} not found.",
                targetName);

            return;
        }

        await guildServer
            .ChangeGuildMemberPositionAsync(
                guildStatus.GuildId,
                targetCharacter.Id,
                newPosition)
            .ConfigureAwait(false);

        player.Logger.LogInformation(
            "Guild role changed: {Target} -> {Role}.",
            targetName,
            newPosition);

        // Enviar inmediatamente el roster actualizado
        // al Guild Master.
        var guild =
            await guildServer
                .GetGuildAsync(
                    guildStatus.GuildId)
                .ConfigureAwait(false);

        if (guild is not null)
        {
            var updatedMembers =
                await guildServer
                    .GetGuildListAsync(
                        guildStatus.GuildId)
                    .ConfigureAwait(false);

            await player
                .InvokeViewPlugInAsync<IShowGuildListPlugIn>(
                    plugIn =>
                        plugIn.ShowGuildListAsync(
                            updatedMembers,
                            guild))
                .ConfigureAwait(false);
        }
    }
}