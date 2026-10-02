$port = 8088
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://+:$port/")
try {
    $listener.Start()
} catch {
    $listener = New-Object System.Net.HttpListener
    $listener.Prefixes.Add("http://*:$port/")
    $listener.Start()
}

Write-Host "HTTP Server listening on http://192.168.0.134:$port/"
$apkPath = "E:\Tapa\APK\OfficeBite.Mobile\bin\Release\net9.0-android\com.companyname.officebite.mobile-Signed.apk"

while ($listener.IsListening) {
    $context = $listener.GetContext()
    $response = $context.Response
    
    if (Test-Path $apkPath) {
        $bytes = [System.IO.File]::ReadAllBytes($apkPath)
        $response.ContentType = "application/vnd.android.package-archive"
        $response.ContentLength64 = $bytes.Length
        $response.AddHeader("Content-Disposition", "attachment; filename=CityBite.apk")
        $response.OutputStream.Write($bytes, 0, $bytes.Length)
    } else {
        $response.StatusCode = 404
        $buffer = [System.Text.Encoding]::UTF8.GetBytes("APK not found")
        $response.OutputStream.Write($buffer, 0, $buffer.Length)
    }
    $response.Close()
}
