param([string]$BaseUrl = 'https://localhost:7207')
$ErrorActionPreference = 'Stop'
function Assert-True($condition, $message) { if (-not $condition) { throw $message } }
function Login-Demo([string]$email) {
    $session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
    $csrf = Invoke-RestMethod "$BaseUrl/api/auth/csrf" -WebSession $session -SkipCertificateCheck
    $body = @{ email = $email; portal = 'staff' } | ConvertTo-Json
    $null = Invoke-RestMethod "$BaseUrl/api/auth/dev-login" -Method Post -Body $body -ContentType 'application/json' -Headers @{ 'X-CSRF-TOKEN' = $csrf.token } -WebSession $session -SkipCertificateCheck
    return $session
}
$ownerSession = Login-Demo 'demo-owner-sc01@example.invalid'
$staffSession = Login-Demo 'demo-staff-sc01@example.invalid'
$ownerCenters = Invoke-RestMethod "$BaseUrl/api/v1/sport-centers" -WebSession $ownerSession -SkipCertificateCheck
$staffCenters = Invoke-RestMethod "$BaseUrl/api/v1/sport-centers" -WebSession $staffSession -SkipCertificateCheck
Assert-True $ownerCenters.canViewAll 'Owner should see the chain.'
Assert-True ($staffCenters.centers.Count -eq 1) 'Staff should see exactly one center.'
$ownId = $staffCenters.assignedVenueId
$other = $ownerCenters.centers | Where-Object { $_.name -like '[[]DEMO-SC]*' -and $_.id -ne $ownId } | Select-Object -First 1
Assert-True ($null -ne $other) 'Missing another seeded center.'
$query = 'from=2026-10-30&to=2026-11-05'
$chain = Invoke-RestMethod "$BaseUrl/api/v1/revenue/daily?$query" -WebSession $ownerSession -SkipCertificateCheck
$own = Invoke-RestMethod "$BaseUrl/api/v1/revenue/daily?$query" -WebSession $staffSession -SkipCertificateCheck
Assert-True ($chain.totals.net -eq 29065000) 'Chain total does not match seeded documents.'
Assert-True ($own.totals.net -eq 9670000) 'Staff report must contain SC01 only.'
$denied = Invoke-WebRequest "$BaseUrl/api/v1/revenue/daily?$query&venueId=$($other.id)" -WebSession $staffSession -SkipCertificateCheck -SkipHttpErrorCheck
Assert-True ($denied.StatusCode -eq 403) 'Cross-center report was not denied.'
$orders = Invoke-RestMethod "$BaseUrl/api/v1/commerce/goods-orders" -WebSession $staffSession -SkipCertificateCheck
Assert-True ($orders.Count -eq 7) 'Goods order list leaks other centers.'
$ownerOther = Invoke-RestMethod "$BaseUrl/api/v1/revenue/daily?$query&venueId=$($other.id)" -WebSession $ownerSession -SkipCertificateCheck
Assert-True ($ownerOther.totals.net -gt 0) 'Owner cannot inspect another center.'
$me = Invoke-RestMethod "$BaseUrl/api/auth/me" -WebSession $staffSession -SkipCertificateCheck
Assert-True ([long]$me.venueId -eq $ownId) 'Current assignment missing from authentication.'
Write-Output "PASS: Owner chain = $($chain.totals.net); staff center = $($own.totals.net); scoped orders = $($orders.Count); cross-center request = 403."
