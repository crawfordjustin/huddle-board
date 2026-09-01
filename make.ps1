<#
  Windows equivalent of the Makefile, for a box without GNU make.
      .\make.ps1 build     data -> dist\
      .\make.ps1 check     fast play-library check
      .\make.ps1 test      build, then the full suite
      .\make.ps1 print     the paper playbook, cards and rotation sheet
      .\make.ps1 clean
#>
param([string]$Target = "build")
$ErrorActionPreference = "Stop"
$py = if ($env:PY) { $env:PY } else { "python" }
Push-Location $PSScriptRoot
try {
  switch ($Target) {
    "build" {
      & $py export_proto.py
      if (-not (Test-Path "dist\deploy\icon-192.png")) { & $py make_icons.py }
      & $py build_app.py
    }
    "check" { & $py check_plays.py }
    "test"  { & $PSCommandPath build; & $py tests\run_all.py }
    "icons" { & $py make_icons.py }
    "print" {
      & $py print\render.py; & $py print\cards.py; & $py print\rotation.py
      & $py print\topdf.py
      & $py print\topdf2.py dist\print\cards.html dist\print\8U-Field-Cards.pdf landscape
      & $py print\topdf2.py dist\print\rotation.html dist\print\8U-Rotation-Sheet.pdf landscape
    }
    "clean" { Remove-Item -Recurse -Force dist, __pycache__, */__pycache__ -ErrorAction SilentlyContinue }
    default { Write-Error "unknown target '$Target' — try build, check, test, print, clean" }
  }
} finally { Pop-Location }
