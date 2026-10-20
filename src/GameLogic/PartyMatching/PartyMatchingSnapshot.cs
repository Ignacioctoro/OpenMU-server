// <copyright file="PartyMatchingSnapshot.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartyMatching;

/// <summary>Version 1 party matching data, shared by the extended client and server.</summary>
public sealed class PartyMatchingSnapshot
{
    /// <summary>Gets or sets Status.</summary>
    public int Status { get; set; } = 0;
    /// <summary>Gets or sets Page.</summary>
    public int Page { get; set; } = 0;
    /// <summary>Gets or sets Pages.</summary>
    public int Pages { get; set; } = 1;
    /// <summary>Gets or sets OwnAd.</summary>
    public string OwnAd { get; set; } = "";
    /// <summary>Gets or sets PendingAd.</summary>
    public string PendingAd { get; set; } = "";
    /// <summary>Gets or sets MaximumLevel.</summary>
    public int MaximumLevel { get; set; } = 400;
    /// <summary>Gets or sets Ads.</summary>
    public PartyMatchingAd[] Ads { get; set; } = Array.Empty<PartyMatchingAd>();
    /// <summary>Gets or sets Requests.</summary>
    public PartyMatchingApplicant[] Requests { get; set; } = Array.Empty<PartyMatchingApplicant>();
}
