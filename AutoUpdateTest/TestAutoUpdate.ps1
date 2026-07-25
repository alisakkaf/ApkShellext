# ===================================================================
#   AliSakkaF ApkShellext - Auto-Update Safe Test Runner (v1.1.0)
# ===================================================================

Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "  ApkShellext Auto-Updater Safe Simulation Test" -ForegroundColor Green
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Simulate current version (Set lower than v1.1.0 to test update trigger)
$currentVersion = [Version]"1.0.0.0"
Write-Host "[INFO] Simulated Local Installed Version: $currentVersion" -ForegroundColor Yellow

# 2. Query GitHub latest release endpoint
$url = "https://github.com/alisakkaf/ApkShellext/releases/latest"
Write-Host "[INFO] Querying GitHub latest release: $url ..." -ForegroundColor Cyan

try {
    $request = [System.Net.HttpWebRequest]::Create($url)
    $request.Method = "HEAD"
    $request.AllowAutoRedirect = $true
    $request.UserAgent = "ApkShellext-UpdaterTest"

    $response = $request.GetResponse()
    $finalUrl = $response.ResponseUri.ToString()
    $response.Close()

    $tag = Split-Path $finalUrl -Leaf
    Write-Host "[SUCCESS] Found Latest Release Tag on GitHub: $tag" -ForegroundColor Green

    $cleanVersion = $tag.TrimStart('v')
    $latestVersion = [Version]($cleanVersion + ".0")

    if ($latestVersion -gt $currentVersion) {
        Write-Host "[UPDATE DETECTED] $tag is newer than local version $currentVersion" -ForegroundColor Green

        # 3. Determine System Drive dynamically
        $sysDrive = [System.IO.Path]::GetPathRoot([Environment]::SystemDirectory)
        $testExtractDir = Join-Path $sysDrive "ApkShellext_ByAliSakkaf_Test"
        Write-Host "[INFO] System Drive detected: $sysDrive" -ForegroundColor Cyan
        Write-Host "[INFO] Safe Test Extract Directory: $testExtractDir" -ForegroundColor Cyan

        # 4. Display interactive MessageBox notification (simulating Service notification)
        $msgText = "A new update ($tag) for ApkShellext was detected on GitHub!`n`nWould you like to simulate the automatic download & extraction test now?`n(This test will NOT restart Explorer or touch your installed DLLs)."
        $msgCaption = "ApkShellext Auto-Update Test"
        
        Add-Type -AssemblyName System.Windows.Forms
        $result = [System.Windows.Forms.MessageBox]::Show($msgText, $msgCaption, [System.Windows.Forms.MessageBoxButtons]::YesNo, [System.Windows.Forms.MessageBoxIcon]::Information)

        if ($result -eq [System.Windows.Forms.DialogResult]::Yes) {
            Write-Host "[INFO] User selected YES. Downloading release asset..." -ForegroundColor Yellow

            $downloadUrl = "https://github.com/alisakkaf/ApkShellext/releases/download/$tag/ApkShellext-$tag.zip"
            $tempZip = Join-Path $env:TEMP "ApkShellext-Test-$tag.zip"

            Write-Host "[INFO] Downloading from: $downloadUrl" -ForegroundColor Cyan
            $wc = New-Object System.Net.WebClient
            $wc.Headers.Add("User-Agent", "ApkShellext-UpdaterTest")
            $wc.DownloadFile($downloadUrl, $tempZip)

            Write-Host "[SUCCESS] Downloaded release package to: $tempZip" -ForegroundColor Green

            # Extract archive to safe test directory
            if (Test-Path $testExtractDir) {
                Remove-Item $testExtractDir -Recurse -Force
            }
            New-Item -ItemType Directory -Path $testExtractDir -Force | Out-Null

            Write-Host "[INFO] Extracting files to $testExtractDir ..." -ForegroundColor Cyan
            Expand-Archive -Path $tempZip -DestinationPath $testExtractDir -Force

            # Unblock downloaded files
            Get-ChildItem -Path $testExtractDir -Recurse | Unblock-File

            Write-Host ""
            Write-Host "===================================================================" -ForegroundColor Green
            Write-Host "  AUTO-UPDATE TEST COMPLETED SUCCESSFULLY!" -ForegroundColor Green
            Write-Host "  - Release Asset Downloaded: $tempZip" -ForegroundColor Green
            Write-Host "  - Files Extracted Safely to: $testExtractDir" -ForegroundColor Green
            Write-Host "  - Note: Explorer was NOT restarted & system binaries were NOT modified." -ForegroundColor Green
            Write-Host "===================================================================" -ForegroundColor Green

            [System.Windows.Forms.MessageBox]::Show("Auto-Update Simulation Test Succeeded!`n`nRelease Asset $tag downloaded and extracted safely to:`n$testExtractDir`n`nNo system DLLs or Explorer processes were interrupted.", "Test Successful", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information)
        } else {
            Write-Host "[INFO] User selected NO. Auto-update test cancelled." -ForegroundColor Yellow
        }
    } else {
        Write-Host "[INFO] Local version is already up to date ($currentVersion)." -ForegroundColor Yellow
    }
} catch {
    Write-Host "[ERROR] Auto-update test failed: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
