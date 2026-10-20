// <copyright file="PartyMatchingAd.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartyMatching;

/// <summary>Version 1 party matching data, shared by the extended client and server.</summary>
public sealed class PartyMatchingAd
{
    /// <summary>Gets or sets Id.</summary>
    public string Id { get; set; } = "";
    /// <summary>Gets or sets Leader.</summary>
    public string Leader { get; set; } = "";
    /// <summary>Gets or sets Title.</summary>
    public string Title { get; set; } = "";
    /// <summary>Gets or sets Map.</summary>
    public int Map { get; set; } = 0;
    /// <summary>Gets or sets Count.</summary>
    public int Count { get; set; } = 1;
    /// <summary>Gets or sets Capacity.</summary>
    public int Capacity { get; set; } = 5;
    /// <summary>Gets or sets MinLevel.</summary>
    public int MinLevel { get; set; } = 1;
    /// <summary>Gets or sets MaxLevel.</summary>
    public int MaxLevel { get; set; } = 400;
    /// <summary>Gets or sets ClassMask.</summary>
    public int ClassMask { get; set; } = 127;
    /// <summary>Gets or sets Locked.</summary>
    public bool Locked { get; set; } = false;
    /// <summary>Gets or sets AutoAccept.</summary>
    public bool AutoAccept { get; set; } = false;
}
