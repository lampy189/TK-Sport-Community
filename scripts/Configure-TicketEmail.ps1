# Run locally in PowerShell. The App Password is entered with masking and saved outside Git.
$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot '../be_da/be_da.csproj'
[xml]$projectXml = Get-Content -LiteralPath $projectPath
$secretsId = [string]($projectXml.Project.PropertyGroup.UserSecretsId | Where-Object { $_ } | Select-Object -First 1)
if ([string]::IsNullOrWhiteSpace($secretsId)) { throw 'Project has no UserSecretsId.' }
$secretsDir = Join-Path $env:APPDATA "Microsoft/UserSecrets/$secretsId"
$secretsPath = Join-Path $secretsDir 'secrets.json'
$settings = @{}
if (Test-Path -LiteralPath $secretsPath) {
    $existing = Get-Content -LiteralPath $secretsPath -Raw | ConvertFrom-Json
    foreach ($property in $existing.PSObject.Properties) { $settings[$property.Name] = $property.Value }
}
if (!$settings['Email:Username']) { $settings['Email:Username'] = Read-Host 'Gmail gui thong bao' }
$secret = Read-Host 'Nhap Gmail App Password (khong phai mat khau dang nhap Gmail)' -AsSecureString
$buffer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
try {
    $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($buffer).Replace(' ', '')
    if ($password.Length -ne 16) { throw 'Gmail App Password can 16 ky tu. Cau hinh chua duoc thay doi.' }
    $settings['Email:Password'] = $password
    $settings['Email:From'] = $settings['Email:Username']
    $settings['Email:Host'] = 'smtp.gmail.com'
    $settings['Email:Port'] = '587'
    $settings['Email:Enabled'] = 'true'
    if (!$settings['Email:WebUrl']) { $settings['Email:WebUrl'] = 'http://localhost:5173' }
    New-Item -ItemType Directory -Path $secretsDir -Force | Out-Null
    $settings | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $secretsPath -Encoding UTF8
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($buffer)
    $password = $null
    $settings.Remove('Email:Password')
    $secret.Dispose()
}
Write-Host 'Da luu cau hinh Gmail ngoai Git. Chay lai backend bang profile https (F5).'
Write-Host 'Email da xep hang se duoc gui sau khi backend chay; nguoi nhan la email tai khoan cua nguoi choi/nguoi tra tien.'
