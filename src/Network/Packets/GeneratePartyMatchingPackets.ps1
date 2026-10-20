$ErrorActionPreference = 'Stop'
# Regenerates only this extension, without a build or modifications to package versions.
$packetRoot = $PSScriptRoot
foreach ($kind in @('Structs', 'RefStructs', 'Extensions')) {
    $file = switch ($kind) {
        'Structs' { 'PartyMatchingExtendedPackets.cs' }
        'RefStructs' { 'PartyMatchingExtendedPacketsRef.cs' }
        'Extensions' { 'PartyMatchingConnectionExtensions.cs' }
    }
    $transform = [System.Xml.Xsl.XslCompiledTransform]::new()
    $transform.Load((Join-Path $packetRoot "Generate$kind.xslt"), [System.Xml.Xsl.XsltSettings]::Default, [System.Xml.XmlUrlResolver]::new())
    $arguments = [System.Xml.Xsl.XsltArgumentList]::new()
    $arguments.AddParam('resultFileName', '', $file)
    $arguments.AddParam('subNamespace', '', 'PartyMatching')
    $writer = [System.Xml.XmlWriter]::Create((Join-Path $packetRoot $file), $transform.OutputSettings)
    try { $transform.Transform((Join-Path $packetRoot 'PartyMatchingExtendedPackets.xml'), $arguments, $writer) }
    finally { $writer.Dispose() }
}
$transform = [System.Xml.Xsl.XslCompiledTransform]::new()
$transform.Load((Join-Path $packetRoot 'GenerateTests.xslt'), [System.Xml.Xsl.XsltSettings]::Default, [System.Xml.XmlUrlResolver]::new())
$arguments = [System.Xml.Xsl.XsltArgumentList]::new()
$arguments.AddParam('resultFileName', '', 'PartyMatchingExtendedPacketTests.cs')
$arguments.AddParam('testClassName', '', 'PartyMatchingExtendedPacketTests')
$arguments.AddParam('subNamespace', '', 'PartyMatching')
$testFile = Join-Path $packetRoot '../../../tests/MUnique.OpenMU.Network.Packets.Tests/PartyMatchingExtendedPacketTests.cs'
$writer = [System.Xml.XmlWriter]::Create($testFile, $transform.OutputSettings)
try { $transform.Transform((Join-Path $packetRoot 'PartyMatchingExtendedPackets.xml'), $arguments, $writer) }
finally { $writer.Dispose() }
