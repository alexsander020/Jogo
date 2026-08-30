Add-Type -AssemblyName System.IO.Compression.FileSystem
$docxPath = "Assets/NetShift_GDD_V3.docx"
$zip = [System.IO.Compression.ZipFile]::OpenRead($docxPath)
$entry = $zip.GetEntry("word/document.xml")
$stream = $entry.Open()
$reader = New-Object System.IO.StreamReader($stream)
$content = $reader.ReadToEnd()
$reader.Close()
$stream.Close()
$zip.Dispose()

$xml = [xml]$content
$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main")

$paragraphs = $xml.SelectNodes("//w:p", $ns)
$lines = foreach ($p in $paragraphs) {
    $textNodes = $p.SelectNodes(".//w:t", $ns)
    if ($textNodes) {
        ($textNodes | ForEach-Object { $_.InnerText }) -join ""
    } else {
        ""
    }
}
$fullText = $lines -join "`r`n"
$fullText | Out-File -FilePath "Assets/NetShift_GDD_Extracted.txt" -Encoding utf8
Write-Output "Extracted $($lines.Count) lines to Assets/NetShift_GDD_Extracted.txt"
