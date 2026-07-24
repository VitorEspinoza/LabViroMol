param(
    [Parameter(Mandatory = $true)]
    [string]$Url,

    [int]$Connections = 50,
    [int]$DurationSeconds = 30,

    [switch]$InsecureTls
)

$bombardierArgs = @(
    "-c", $Connections,
    "-d", "${DurationSeconds}s",
    "-l"
)

if ($InsecureTls) {
    $bombardierArgs += "-k"
}

$bombardierArgs += $Url

& bombardier @bombardierArgs
