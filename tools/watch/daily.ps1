# Daily watch (doc 03 §5b): has Deadlock or Risk of Rain 2 updated on this PC, and do Melty players on the live release
# hit breaks? If either, a Claude run checks the mod, fixes it on a branch watch/<date>, tests it and publishes the fix.
#   powershell -File tools/watch/daily.ps1             the scheduled run (tools/watch/install.ps1 registers it)
#   -DryRun     read the builds and the guards, start no Claude
#   -NoPublish  the fix run may do everything except publish on Melty
#   -SkipMelty  no Melty triage (builds only)
# State and logs live in build/watch/ (state.json, <date>.log, <date>-report.md, triage.json).
param([switch]$DryRun, [switch]$NoPublish, [switch]$SkipMelty)

# Continue, not Stop: Windows PowerShell turns git's and claude's stderr lines into errors under 2>&1
$ErrorActionPreference = 'Continue'
$Root = Split-Path (Split-Path $PSScriptRoot)
$Watch = Join-Path $Root 'build\watch'
$Steam = 'C:\Program Files (x86)\Steam'
$Apps = @{ ror2 = 632360; deadlock = 1422450 }
$Date = Get-Date -Format 'yyyy-MM-dd'
New-Item -ItemType Directory -Force $Watch | Out-Null
$Log = Join-Path $Watch "$Date.log"

function Say($msg) {
    $line = "[{0}] {1}" -f (Get-Date -Format 'HH:mm:ss'), $msg
    Write-Host $line
    Add-Content -Path $Log -Value $line -Encoding utf8
}

function Notify($title, $text) {
    try {
        [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
        $xml = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02)
        $t = $xml.GetElementsByTagName('text')
        $t.Item(0).AppendChild($xml.CreateTextNode($title)) | Out-Null
        $t.Item(1).AppendChild($xml.CreateTextNode($text)) | Out-Null
        $app = '{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}\WindowsPowerShell\v1.0\powershell.exe'
        [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier($app).Show([Windows.UI.Notifications.ToastNotification]::new($xml))
    } catch { Say "notification failed: $_" }
}

function Read-Builds {
    $b = @{}
    foreach ($k in $Apps.Keys) {
        $acf = Join-Path $Steam "steamapps\appmanifest_$($Apps[$k]).acf"
        $text = Get-Content $acf -Raw
        $b[$k] = [regex]::Match($text, '"buildid"\s+"(\d+)"').Groups[1].Value
        $b["${k}State"] = [regex]::Match($text, '"StateFlags"\s+"(\d+)"').Groups[1].Value
    }
    $inf = Join-Path $Steam 'steamapps\common\Deadlock\game\citadel\steam.inf'
    $b.deadlockClient = [regex]::Match((Get-Content $inf -Raw), '(?m)^ClientVersion=(\d+)').Groups[1].Value
    return $b
}

function Game-Running {
    $id = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -Name RunningAppID -ErrorAction SilentlyContinue).RunningAppID
    if ($id -and $id -ne 0) { return "Steam app $id" }
    $p = Get-Process -Name 'Risk of Rain 2', 'deadlock' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($p) { return $p.ProcessName }
    return $null
}

function Claude-Exe {
    $c = Get-Command claude -ErrorAction SilentlyContinue
    if ($c) { return $c.Source }
    return Join-Path $env:USERPROFILE '.local\bin\claude.exe'
}

# --- lock: never two runs at once
$Lock = Join-Path $Watch 'lock'
if ((Test-Path $Lock) -and ((Get-Date) - (Get-Item $Lock).LastWriteTime).TotalHours -lt 12) {
    Say 'another watch run holds build/watch/lock; stopping'; exit 0
}
Set-Content $Lock $PID
try {
    Say "daily watch $Date (DryRun=$DryRun NoPublish=$NoPublish SkipMelty=$SkipMelty)"

    $running = Game-Running
    if ($running) { Say "a game is running ($running); trying again tomorrow"; exit 0 }

    $now = Read-Builds
    Say ("this PC: Risk of Rain 2 build {0} (state {1}), Deadlock build {2} / ClientVersion {3} (state {4})" -f `
        $now.ror2, $now.ror2State, $now.deadlock, $now.deadlockClient, $now.deadlockState)
    if ($now.ror2State -ne '4' -or $now.deadlockState -ne '4') { Say 'a game is still updating in Steam; trying again tomorrow'; exit 0 }

    $StateFile = Join-Path $Watch 'state.json'
    if (-not (Test-Path $StateFile)) {
        $seed = [ordered]@{ ror2 = $now.ror2; deadlock = $now.deadlock; deadlockClient = $now.deadlockClient
                            meltySeenUntil = (Get-Date).ToUniversalTime().ToString('o'); lastChecked = $Date }
        if (-not $DryRun) { $seed | ConvertTo-Json | Set-Content $StateFile -Encoding utf8 }
        Say 'first run: recorded today''s builds as the baseline'
    }
    $state = Get-Content $StateFile -Raw -ErrorAction SilentlyContinue | ConvertFrom-Json
    if (-not $state) { $state = [pscustomobject]@{ ror2 = $now.ror2; deadlock = $now.deadlock; deadlockClient = $now.deadlockClient; meltySeenUntil = (Get-Date).ToUniversalTime().ToString('o') } }

    $changes = @()
    if ($now.deadlock -ne $state.deadlock) { $changes += "Deadlock updated: build $($state.deadlock) -> $($now.deadlock), ClientVersion $($state.deadlockClient) -> $($now.deadlockClient)" }
    if ($now.ror2 -ne $state.ror2) { $changes += "Risk of Rain 2 updated: build $($state.ror2) -> $($now.ror2)" }
    if ($changes) { $changes | ForEach-Object { Say $_ } } else { Say 'no game update since the last check' }

    if ($DryRun) { Say 'dry run: no Claude'; exit 0 }
    $claude = Claude-Exe
    Set-Location $Root

    # --- Melty triage: breaks reported on the live release since the last check (Sonnet, read-only Melty tools)
    $breaks = @()
    $checkedAt = (Get-Date).ToUniversalTime().ToString('o')
    if (-not $SkipMelty) {
        $prompt = (Get-Content (Join-Path $PSScriptRoot 'triage-prompt.md') -Raw).Replace('{{SINCE}}', $state.meltySeenUntil)
        $ro = 'mcp__melty__list_releases mcp__melty__mod_status mcp__melty__install_outcomes mcp__melty__player_logs mcp__melty__player_reports'
        # Windows PowerShell drops bare double quotes from native arguments, hence the \"
        $schema = ((Get-Content (Join-Path $PSScriptRoot 'triage-schema.json') -Raw) -replace '\s+', ' ').Replace('"', '\"')
        Say 'Melty triage...'
        $out = & $claude -p $prompt --model sonnet --max-turns 40 --allowedTools $ro --output-format json --json-schema $schema | Out-String
        Add-Content -Path $Log -Value $out -Encoding utf8
        try {
            $triage = ($out | ConvertFrom-Json).structured_output
            if (-not $triage) { throw 'no structured_output' }
            $triage | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $Watch 'triage.json') -Encoding utf8
            # A break starts a fix run only if it is ours and its signature isn't in build/watch/known-breaks.txt
            # (handled before: fixed but unreleased, or judged not ours). The script matches, not the model.
            $knownFile = Join-Path $Watch 'known-breaks.txt'
            $known = @(if (Test-Path $knownFile) { Get-Content $knownFile | Where-Object { $_ -and $_ -notmatch '^#' } | ForEach-Object { ($_ -split '\|')[0].Trim() } })
            $all = @($triage.breaks | Where-Object { $_ })
            foreach ($b in $all) { Say ("  {0} ours={1} {2}: {3}" -f $(if ($known -contains $b.signature) { 'known' } else { 'new  ' }), $b.ours, $b.signature, $b.summary) }
            $breaks = @($all | Where-Object { $_.ours -and ($known -notcontains $_.signature) })
            Say "Melty: live $($triage.liveVersion), $($breaks.Count) new break(s) of ours out of $($all.Count). $($triage.summary)"
        } catch {
            Say 'Melty triage gave no readable answer; treating as no breaks (see the log)'
            $checkedAt = $state.meltySeenUntil
        }
    }

    $state | Add-Member -Force NoteProperty lastChecked $Date
    if (-not $changes -and -not $breaks) {
        $state | Add-Member -Force NoteProperty meltySeenUntil $checkedAt
        $state | ConvertTo-Json | Set-Content $StateFile -Encoding utf8
        Say 'all clear'
        exit 0
    }

    # --- fix run on a branch of a clean main
    $branch = "watch/$Date"
    $dirty = git status --porcelain
    $head = git rev-parse --abbrev-ref HEAD
    if ($dirty -or $head -ne 'main') {
        Say "the repo is not a clean main ($head, $(@($dirty).Count) changed files); fix run skipped"
        Notify 'Risk of Deadlock watch' "Something needs checking ($($changes.Count) update(s), $($breaks.Count) Melty break(s)), but the repo has uncommitted work. See build/watch/$Date.log"
        exit 1
    }
    git checkout -b $branch 2>&1 | Out-Null

    $what = @()
    $what += $changes
    if ($breaks) { $what += 'Melty breaks on the live release (from build/watch/triage.json):'; $what += ($breaks | ConvertTo-Json -Depth 6) }
    $prompt = (Get-Content (Join-Path $PSScriptRoot 'fix-prompt.md') -Raw).
        Replace('{{DATE}}', $Date).Replace('{{BRANCH}}', $branch).Replace('{{WHAT}}', ($what -join "`n")).
        Replace('{{PUBLISH}}', $(if ($NoPublish) { 'NO: this is a -NoPublish run; stop before publish and say so in the report.' } else { 'YES, once every check passes (the creator''s standing permission for this watch).' }))
    $deny = @('mcp__melty__unpublish', 'mcp__melty__rollback_release', 'mcp__melty__delete_draft', 'mcp__melty__set_price')
    if ($NoPublish) { $deny += 'mcp__melty__publish' }
    Say "fix run on $branch..."
    $out = & $claude -p $prompt --settings (Join-Path $PSScriptRoot 'claude-settings.json') --max-turns 400 --disallowedTools ($deny -join ' ') 2>&1 | Out-String
    Add-Content -Path $Log -Value $out -Encoding utf8

    $report = Join-Path $Watch "$Date-report.md"
    $line = if (Test-Path $report) { (Get-Content $report -TotalCount 1) -replace '^#\s*', '' } else { 'the fix run wrote no report; see the log' }
    Say "result: $line"

    $ahead = git rev-list --count "main..$branch"
    if ((git rev-parse --abbrev-ref HEAD) -eq $branch -and -not (git status --porcelain)) {
        git checkout main 2>&1 | Out-Null
        if ($ahead -eq '0') { git branch -D $branch 2>&1 | Out-Null; Say "no commits; $branch removed" }
        else { Say "$ahead commit(s) on $branch for the creator to merge" }
    }

    $state | Add-Member -Force NoteProperty ror2 $now.ror2
    $state | Add-Member -Force NoteProperty deadlock $now.deadlock
    $state | Add-Member -Force NoteProperty deadlockClient $now.deadlockClient
    $state | Add-Member -Force NoteProperty meltySeenUntil $checkedAt
    $state | ConvertTo-Json | Set-Content $StateFile -Encoding utf8
    Notify 'Risk of Deadlock watch' $line
} finally {
    Remove-Item $Lock -ErrorAction SilentlyContinue
}
