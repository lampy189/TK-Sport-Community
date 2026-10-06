# Refresh searchable text after replacing one of the source DOCX files.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$requirementsDir = $PSScriptRoot
$documentNames = @(
    'RP1-Project-Introduction',
    'RP2-Project-Management-Plan',
    'RP3-Software-Requirements'
)

foreach ($documentName in $documentNames) {
    $inputPath = Join-Path $requirementsDir ($documentName + '.docx')
    $outputPath = Join-Path $requirementsDir ($documentName + '.txt')
    $archive = [System.IO.Compression.ZipFile]::OpenRead($inputPath)
    try {
        $entry = $archive.GetEntry('word/document.xml')
        if ($null -eq $entry) { throw "Missing Word document content: $inputPath" }
        $stream = $entry.Open()
        try {
            $reader = [System.IO.StreamReader]::new($stream)
            try { [xml]$document = $reader.ReadToEnd() }
            finally { $reader.Dispose() }
        }
        finally { $stream.Dispose() }

        $namespaces = [System.Xml.XmlNamespaceManager]::new($document.NameTable)
        $namespaces.AddNamespace('w', 'http://schemas.openxmlformats.org/wordprocessingml/2006/main')
        $paragraphs = foreach ($paragraph in $document.SelectNodes('//w:body//w:p', $namespaces)) {
            ($paragraph.SelectNodes('.//w:t', $namespaces) | ForEach-Object { $_.InnerText }) -join ''
        }
        [System.IO.File]::WriteAllLines($outputPath, [string[]]$paragraphs, [System.Text.UTF8Encoding]::new($false))
        Write-Output "Updated $outputPath"
    }
    finally { $archive.Dispose() }
}
