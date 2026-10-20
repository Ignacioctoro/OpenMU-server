// <copyright file="IPartyMatchingViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Party;

using MUnique.OpenMU.GameLogic.PartyMatching;

/// <summary>Shows the authoritative recruitment directory to an extended client.</summary>
public interface IPartyMatchingViewPlugIn : IViewPlugIn
{
    /// <summary>Sends the requester's directory and pending applications.</summary>
    ValueTask ShowAsync(PartyMatchingSnapshot snapshot);
}
