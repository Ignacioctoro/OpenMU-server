// <copyright file="ChatMessageWhisperProcessor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Chat;

using System.ComponentModel;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views;

/// <summary>
/// A chat message processor which sends the message to the whisper receiver.
/// </summary>
public class ChatMessageWhisperProcessor : BannableChatMessageBaseProcessor
{
    /// <inheritdoc />
    public override async ValueTask SubclassProcessMessageAsync(
        Player sender,
        (string Message, string PlayerName) content)
    {
        var whisperReceiver =
            sender.GameContext.GetPlayerByCharacterName(content.PlayerName);

        // Receiver is not online.
        if (whisperReceiver is null)
        {
            await sender
                .InvokeViewPlugInAsync<IChatViewPlugIn>(
                    p => p.ChatMessageAsync(
                        $"Player '{content.PlayerName}' is not online.",
                        "System",
                        ChatMessageType.Whisper))
                .ConfigureAwait(false);

            return;
        }

        var eventArgs = new CancelEventArgs();

        sender.GameContext.PlugInManager
            .GetPlugInPoint<IWhisperMessageReceivedPlugIn>()?
            .WhisperMessageReceived(
                sender,
                whisperReceiver,
                content.Message,
                eventArgs);

        if (eventArgs.Cancel)
        {
            return;
        }

        // Send the actual whisper to the receiver.
        await whisperReceiver
            .InvokeViewPlugInAsync<IChatViewPlugIn>(
                p => p.ChatMessageAsync(
                    content.Message,
                    sender.SelectedCharacter!.Name,
                    ChatMessageType.Whisper))
            .ConfigureAwait(false);

        // Echo the successfully delivered whisper back to the sender.
        // We use the receiver name as "sender" so the sender sees:
        //
        // ancientDk : hi
        //
        // This also confirms that the receiver was found online.
        if (!ReferenceEquals(sender, whisperReceiver))
        {
            await sender
                .InvokeViewPlugInAsync<IChatViewPlugIn>(
                    p => p.ChatMessageAsync(
                        $"Para {content.PlayerName}: {content.Message}",
                        string.Empty,
                        ChatMessageType.Whisper))
                .ConfigureAwait(false);
        }
    }
}