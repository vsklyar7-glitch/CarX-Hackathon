$ErrorActionPreference = 'Stop'
$taskRoot = (Get-Location).Path
$taskUtf8 = [System.Text.UTF8Encoding]::new($false, $true)
$taskBinaryExtensions = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($taskExtension in @('.png','.jpg','.jpeg','.gif','.bmp','.tga','.psd','.exr','.hdr','.fbx','.blend','.ttf','.otf','.wav','.mp3','.ogg','.mp4','.dll','.zip','.7z','.unitypackage')) {
    [void]$taskBinaryExtensions.Add($taskExtension)
}
$taskPaths = @(git -c core.quotepath=false ls-files --cached --others --exclude-standard)
if ($LASTEXITCODE -ne 0) { throw 'Cannot list project files.' }
$taskChanged = [System.Collections.Generic.List[string]]::new()
$taskTextCount = 0
$taskSkipped = 0
foreach ($taskRelative in $taskPaths) {
    if ($taskRelative -match '^(?i:Library|Temp|Logs|obj|.git|.codex|.agents)/' -or
        $taskRelative -match '(?i).(csproj|slnx|sln)$') { $taskSkipped++; continue }
    $taskPath = Join-Path $taskRoot $taskRelative
    if (-not [System.IO.File]::Exists($taskPath)) { continue }
    $taskExtension = [System.IO.Path]::GetExtension($taskPath)
    if ($taskBinaryExtensions.Contains($taskExtension)) { $taskSkipped++; continue }
    $taskBytes = [System.IO.File]::ReadAllBytes($taskPath)
    if ([Array]::IndexOf($taskBytes, [byte]0) -ge 0) { $taskSkipped++; continue }
    try { $taskText = $taskUtf8.GetString($taskBytes) }
    catch [System.Text.DecoderFallbackException] { $taskSkipped++; continue }
    $taskTextCount++
    $taskLf = $taskText.Replace("`r`n", "`n").Replace("`r", "`n")
    $taskNewText = if ($taskExtension -in @('.bat', '.cmd')) { $taskLf.Replace("`n", "`r`n") } else { $taskLf }
    if ($taskNewText -cne $taskText) {
        [System.IO.File]::WriteAllText($taskPath, $taskNewText, $taskUtf8)
        $taskReadBack = $taskUtf8.GetString([System.IO.File]::ReadAllBytes($taskPath))
        if ($taskReadBack -cne $taskNewText) { throw "Write verification failed: $taskRelative" }
        if ($taskReadBack.Replace("`r`n", "`n").Replace("`r", "`n") -cne $taskLf) {
            throw "Content changed beyond line endings: $taskRelative"
        }
        $taskChanged.Add($taskRelative)
    }
}
$taskReport = @(
    "Text files examined: $taskTextCount"
    "Files normalized: $($taskChanged.Count)"
    "Binary/generated/protected files skipped: $taskSkipped"
    'Content preserved except line endings; no Git staging or global config changes.'
    ''
) + $taskChanged.ToArray()
[System.IO.File]::WriteAllText((Join-Path $taskRoot 'output/line-endings/result.txt'), ($taskReport -join "`n") + "`n", $taskUtf8)
$taskReport | Select-Object -First 5
