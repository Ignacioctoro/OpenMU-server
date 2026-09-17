// <copyright file="GuildKickPlayerAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Guild;

using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Guild;
using MUnique.OpenMU.Interfaces;

/// <summary>
/// Action to kick a player out of a guild.
/// </summary>
public class GuildKickPlayerAction
{
    /// <summary>
    /// Kicks the player out of the guild.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="nickname">The nickname.</param>
    /// <param name="securityCode">The security code.</param>
    public async ValueTask KickPlayerAsync(Player player, string nickname, string securityCode)
    {
        using var loggerScope = player.Logger.BeginScope(this.GetType());
        if (player.PlayerState.CurrentState != PlayerState.EnteredWorld)
        {
            player.Logger.LogError($"Account {player.Account?.LoginName} not in the right state, but {player.PlayerState.CurrentState}.");
            return;
        }

        if (player.GuildStatus is null)
        {
            player.Logger.LogError($"Player {player} not in a guild.");
            return;
        }

        var gameServerContext = player.GameContext as IGameServerContext;
        var guildServer = gameServerContext?.GuildServer;

        if (guildServer is null || gameServerContext is null)
        {
            player.Logger.LogWarning("No guild server available");
            return;
        }

        if (player.Account!.SecurityCode != null && player.Account.SecurityCode != securityCode)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.WrongSecurityCode)).ConfigureAwait(false);
            player.Logger.LogDebug("Wrong Security Code: [{0}] <> [{1}], Player: {2}", securityCode, player.Account.SecurityCode, player.SelectedCharacter?.Name);

            await player.InvokeViewPlugInAsync<IGuildKickResultPlugIn>(p => p.GuildKickResultAsync(GuildKickSuccess.Failed)).ConfigureAwait(false);
            return;
        }

        var isKickingHimself = player.SelectedCharacter!.Name == nickname;
        if (!isKickingHimself && player.GuildStatus?.Position != GuildPosition.GuildMaster)
        {
            player.Logger.LogWarning("Suspicious kick request for player with name: {0} (player is not a guild master) to kick {1}, could be hack attempt.", player.Name, nickname);
            await player.InvokeViewPlugInAsync<IGuildKickResultPlugIn>(p => p.GuildKickResultAsync(GuildKickSuccess.FailedBecausePlayerIsNotGuildMaster)).ConfigureAwait(false);
            return;
        }

        if (isKickingHimself
            && player.GuildStatus?.Position == GuildPosition.GuildMaster)
        {
            var guildId = player.GuildStatus.GuildId;

            // Guild ids are runtime ids. They can become stale if the GuildServer
            // unloaded/recreated its guild container.
            if (await guildServer.GetGuildAsync(guildId).ConfigureAwait(false) is null)
            {
                player.Logger.LogWarning(
                    "Guild runtime id {GuildId} is stale for {Player}. Reloading guild membership.",
                    guildId,
                    player.Name);

                // Remove the player from the stale GameServer-side guild index.
                await gameServerContext
                    .UnregisterGuildMemberAsync(player)
                    .ConfigureAwait(false);

                // Reload the persistent GuildMember.
                // This recreates the GuildServer container when required and
                // publishes a fresh GuildStatus back to the GameServer.
                await guildServer
                    .PlayerEnteredGameAsync(
                        player.SelectedCharacter.Id,
                        player.SelectedCharacter.Name!,
                        gameServerContext.Id)
                    .ConfigureAwait(false);

                guildId = player.GuildStatus?.GuildId ?? 0;

                if (guildId == 0
                    || await guildServer.GetGuildAsync(guildId)
                        .ConfigureAwait(false) is null)
                {
                    player.Logger.LogError(
                        "Couldn't reload guild membership for {Player}.",
                        player.Name);

                    await player
                        .InvokeViewPlugInAsync<IGuildKickResultPlugIn>(
                            p => p.GuildKickResultAsync(
                                GuildKickSuccess.Failed))
                        .ConfigureAwait(false);

                    return;
                }
            }

            await guildServer
                .KickMemberAsync(guildId, nickname)
                .ConfigureAwait(false);

            await player
                .InvokeViewPlugInAsync<IGuildKickResultPlugIn>(
                    p => p.GuildKickResultAsync(
                        GuildKickSuccess.GuildDisband))
                .ConfigureAwait(false);

            return;
        }

        await guildServer.KickMemberAsync(player.GuildStatus!.GuildId, nickname).ConfigureAwait(false);
    }
}