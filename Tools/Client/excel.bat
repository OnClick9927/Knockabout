set script_base_path=%1
set config_base_path=%2


set CONF_ROOT=..\..\excels
set LUBAN_DLL=..\Luban\Luban.dll
dotnet %LUBAN_DLL% ^
    -t client ^
    -c cs-bin ^
    -d bin  ^
    --conf ..\luban.conf ^
    -x outputCodeDir=%script_base_path% ^
    -x outputDataDir=%config_base_path% 






set code_path=%script_base_path%


powershell -ExecutionPolicy Bypass -NoProfile -Command ^
"$path_code = '%code_path%'; ^
Add-Type -AssemblyName System; ^
$map = New-Object 'System.Collections.Generic.Dictionary[string,string]'; ^
$contexts = New-Object 'System.Collections.Generic.List[PSObject]'; ^
$tables_path = [System.IO.Path]::Combine($path_code, 'Tables.cs'); ^
if (-not [System.IO.File]::Exists($tables_path)) { Write-Host 'Tables.cs not found'; exit 1; } ^
$tables_content = [System.IO.File]::ReadAllText($tables_path); ^
$contexts.Add(@{ path = $tables_path; content = $tables_content }); ^
$dirs = [System.IO.Directory]::GetDirectories($path_code, '*.*', 'AllDirectories'); ^
foreach ($_dir in $dirs) { ^
    $dirName = [System.IO.Path]::GetFileName($_dir); ^
    $files = [System.IO.Directory]::GetFiles($_dir, '*.cs'); ^
    foreach ($file in $files) { ^
        $name = [System.IO.Path]::GetFileNameWithoutExtension($file); ^
        $map.Add($dirName + '.' + $name, $name); ^
        $content = [System.IO.File]::ReadAllText($file); ^
        $content = $content.Replace('namespace Luban.' + $dirName, 'namespace Luban'); ^
        $contexts.Add(@{ path = $file; content = $content }); ^
    } ^
} ^
foreach ($item in $contexts) { ^
    $temp = $item.content; ^
    foreach ($pair in $map.GetEnumerator()) { ^
        $temp = $temp.Replace($pair.Key, $pair.Value); ^
    } ^
    $out_file = [System.IO.Path]::Combine($path_code, [System.IO.Path]::GetFileName($item.path)); ^
    [System.IO.File]::WriteAllText($out_file, $temp); ^
} ^
foreach ($_dir in $dirs) { ^
    [System.IO.Directory]::Delete($_dir, $true); ^
}"
echo Done.




pause
