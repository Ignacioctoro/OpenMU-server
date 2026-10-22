// <copyright file="PartyMatchingApplicant.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartyMatching;

/// <summary>Version 1 party matching data, shared by the extended client and server.</summary>
public sealed class PartyMatchingApplicant
{
    /// <summary>Gets or sets Id.</summary>
    public string Id { get; set; } = "";
    /// <summary>Gets or sets Name.</summary>
    public string Name { get; set; } = "";
    /// <summary>Gets or sets Level.</summary>
    public int Level { get; set; } = 0;
    /// <summary>Gets or sets Class.</summary>
    public int Class { get; set; } = 0;
}
