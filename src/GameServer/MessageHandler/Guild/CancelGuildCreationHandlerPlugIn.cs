// <copyright file="CancelGuildCreationHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Guild;

using System;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.Guild;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handler for guild master answer packets.
/// </summary>
[PlugIn]
[Display(
    Name = nameof(PlugInResources.CancelGuildCreationHandlerPlugIn_Name),
    Description = nameof(PlugInResources.CancelGuildCreationHandlerPlugIn_Description),
    ResourceType = typeof(PlugInResources))]
[Guid("CDB87F7F-24FC-42E1-B375-9EAAEAAC0F8C")]
internal class CancelGuildCreationHandlerPlugIn : IPacketHandlerPlugIn
{
    private readonly GuildMasterAnswerAction _answerAction = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => CancelGuildCreation.Code;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        Console.WriteLine(
            $"[GUILD CANCEL DEBUG] CancelGuildCreation recibido. " +
            $"PlayerState={player.PlayerState.CurrentState}, " +
            $"OpenedNpc={player.OpenedNpc?.Definition?.NpcWindow}");

        await this._answerAction
            .ProcessAnswerAsync(
                player,
                GuildMasterAnswerAction.Answer.Cancel)
            .ConfigureAwait(false);

        Console.WriteLine(
            $"[GUILD CANCEL DEBUG] Cancel procesado. " +
            $"PlayerState={player.PlayerState.CurrentState}, " +
            $"OpenedNpc={player.OpenedNpc?.Definition?.NpcWindow}");
    }
}