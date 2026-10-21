// <copyright file="StoreCurrencyUpdateResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Globalization;
using System.Resources;

/// <summary>Localized captions for the deployment's store-currency update.</summary>
public static class StoreCurrencyUpdateResources
{
    private static readonly ResourceManager Resources = new(typeof(StoreCurrencyUpdateResources));

    /// <summary>Gets the update name.</summary>
    public static string Name => Resources.GetString(nameof(Name), CultureInfo.CurrentUICulture) ?? nameof(Name);

    /// <summary>Gets the update description.</summary>
    public static string Description => Resources.GetString(nameof(Description), CultureInfo.CurrentUICulture) ?? nameof(Description);
}
