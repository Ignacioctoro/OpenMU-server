// <copyright file="PartyMatchingResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Party;

using System.Globalization;
using System.Resources;

/// <summary>Localized names for the optional extended-client protocol.</summary>
public static class PartyMatchingResources
{
    private static readonly ResourceManager Resources = new(typeof(PartyMatchingResources));

    /// <summary>Gets the plugin name.</summary>
    public static string Name => Resources.GetString(nameof(Name), CultureInfo.CurrentUICulture) ?? nameof(PartyMatchingResources);

    /// <summary>Gets the plugin description.</summary>
    public static string Description => Resources.GetString(nameof(Description), CultureInfo.CurrentUICulture) ?? Name;
}
