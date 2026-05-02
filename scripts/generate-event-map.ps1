$backendPath = "src/backend"
$contractsPath = "$backendPath/ECommerce.Contracts"

# Get all events
$events = @{}
Get-ChildItem -Path $contractsPath -Filter "*IntegrationEvent.cs" -Recurse | ForEach-Object {
    $eventName = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
    $events[$eventName] = $eventName
}

# Find Publishers
# Look for "new SomeIntegrationEvent("
$publishers = @()
Get-ChildItem -Path $backendPath -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notmatch "ECommerce.Contracts|Tests|obj|bin" } | ForEach-Object {
    $file = $_
    $content = Get-Content $file.FullName -Raw
    
    # Extract module from path
    $relativePath = $file.FullName.Substring((Resolve-Path $backendPath).Path.Length).Trim('\','/')
    $parts = $relativePath.Split('\/')
    $module = $parts[0]
    
    # Ignore generic infrastructure projects
    if ($module -eq "ECommerce.Infrastructure" -or $module -eq "ECommerce.API" -or $module -eq "ECommerce.SharedKernel") {
        return
    }
    
    # regex to find "new XyzIntegrationEvent"
    $matches = [regex]::Matches($content, "new\s+(\w+IntegrationEvent)")
    foreach ($match in $matches) {
        $eventName = $match.Groups[1].Value
        if ($events.ContainsKey($eventName)) {
            $publishers += [PSCustomObject]@{
                Module = $module
                Event = $eventName
            }
        }
    }
}

# Find Consumers
# Look for "INotificationHandler<XyzIntegrationEvent>"
$consumers = @()
Get-ChildItem -Path $backendPath -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notmatch "ECommerce.Contracts|Tests|obj|bin" } | ForEach-Object {
    $file = $_
    $content = Get-Content $file.FullName -Raw
    
    $relativePath = $file.FullName.Substring((Resolve-Path $backendPath).Path.Length).Trim('\','/')
    $parts = $relativePath.Split('\/')
    $module = $parts[0]
    
    if ($module -eq "ECommerce.Infrastructure" -or $module -eq "ECommerce.API" -or $module -eq "ECommerce.SharedKernel") {
        return
    }
    
    $matches = [regex]::Matches($content, "INotificationHandler<(\w+IntegrationEvent)>")
    foreach ($match in $matches) {
        $eventName = $match.Groups[1].Value
        if ($events.ContainsKey($eventName)) {
            $consumers += [PSCustomObject]@{
                Module = $module
                Event = $eventName
            }
        }
    }
}

$publishers = $publishers | Sort-Object -Property Module, Event -Unique
$consumers = $consumers | Sort-Object -Property Module, Event -Unique

# Generate Mermaid
$mermaid = "```mermaid`n"
$mermaid += "graph TD`n"

$mermaid += "`n    %% Modules`n"
$modules = ($publishers.Module + $consumers.Module) | Sort-Object | Select-Object -Unique
foreach ($m in $modules) {
    $mermaid += "    $m[<b>$m</b>]`n"
    $mermaid += "    style $m fill:#2b3a42,stroke:#4a6572,stroke-width:2px,color:#ffffff,rx:5,ry:5`n"
}

$mermaid += "`n    %% Events`n"
$activeEvents = ($publishers.Event + $consumers.Event) | Sort-Object | Select-Object -Unique
foreach ($e in $activeEvents) {
    # Remove 'IntegrationEvent' suffix for cleaner diagram
    $shortName = $e -replace "IntegrationEvent", ""
    $mermaid += "    $e([$shortName])`n"
    $mermaid += "    style $e fill:#e8f4f8,stroke:#82b1ff,stroke-width:2px,color:#000000`n"
}

$mermaid += "`n    %% Publishing Links`n"
foreach ($p in $publishers) {
    $mermaid += "    $($p.Module) -->|Publishes| $($p.Event)`n"
}

$mermaid += "`n    %% Consuming Links`n"
foreach ($c in $consumers) {
    $mermaid += "    $($c.Event) -->|Consumes| $($c.Module)`n"
}

$mermaid += "```"

$mermaid | Out-File -FilePath "scripts/event-map.md" -Encoding utf8
Write-Output "Mermaid diagram successfully generated."
