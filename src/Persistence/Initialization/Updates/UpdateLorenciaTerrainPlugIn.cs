// <copyright file="UpdateLorenciaTerrainPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.Version075.Maps;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Updates the Lorencia terrain data from the embedded Terrain1.att resource.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("B938B817-977F-4C44-9EF3-AE1F6C331847")]
public class UpdateLorenciaTerrainPlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Update Lorencia Terrain";

    internal const string PlugInDescription =
        "Updates Lorencia terrain data from the embedded Terrain1.att resource.";

    public override int Version => 1;

    public override string DataInitializationKey =>
        VersionSeasonSix.DataInitialization.Id;

    public override string Name => PlugInName;

    public override string Description => PlugInDescription;

    public override bool IsMandatory => false;

    public override DateTime CreatedAt =>
        new(2026, 9, 16, 23, 0, 0, DateTimeKind.Utc);

    protected override ValueTask ApplyAsync(
        IContext context,
        GameConfiguration gameConfiguration)
    {
        var lorencia = gameConfiguration.Maps.First(map => map.Number == Lorencia.Number);

        lorencia.UpdateTerrainFromResources();

        return ValueTask.CompletedTask;
    }
}