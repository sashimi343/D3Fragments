# Build the documentation site per language into _site/<lang>.
# Run after `dotnet build -c <Configuration>` (SeparateDocs must have produced bin/**/ja/*.xml).
Param( $Configuration = 'Release' )

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$langs = @('en', 'ja')

# Stage dll + per-language xml side by side, because DocFX reads <name>.xml next to <name>.dll.
# Projects are discovered by bin/**/ja/*.xml (only doc-generating projects have it, and only in their own bin).
foreach ($lang in $langs) {
    $staging = Join-Path $root "obj/docs/staging/$lang"
    Remove-Item $staging -Recurse -Force -ErrorAction Ignore
    New-Item -ItemType Directory $staging | Out-Null

    foreach ($ja in Get-ChildItem (Join-Path $root "src/*/bin/$Configuration/*/ja/*.xml")) {
        $bin = $ja.Directory.Parent.FullName
        $name = $ja.BaseName
        Copy-Item (Join-Path $bin "$name.dll") $staging
        Copy-Item ($lang -eq 'en' ? (Join-Path $bin "$name.xml") : $ja.FullName) $staging
    }
}

# Stage dependency assemblies (NuGet packages are not copied to bin of class libraries) so that DocFX can resolve
# base types such as DbContext. They are passed as "references", not as inputs, so no API pages are generated for them.
$refs = Join-Path $root 'obj/docs/refs'
Remove-Item $refs -Recurse -Force -ErrorAction Ignore
$publish = Join-Path $root 'obj/docs/publish'
$ownNames = (Get-ChildItem (Join-Path $root 'obj/docs/staging/en') -Filter *.dll).Name
foreach ($ja in Get-ChildItem (Join-Path $root "src/*/bin/$Configuration/*/ja/*.xml")) {
    $project = Join-Path $root "src/$($ja.BaseName)"
    Remove-Item $publish -Recurse -Force -ErrorAction Ignore
    dotnet publish $project -c $Configuration --no-build -o $publish | Out-Null
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
    New-Item -ItemType Directory $refs -Force | Out-Null
    Get-ChildItem $publish -Filter *.dll | Where-Object Name -NotIn $ownNames | Copy-Item -Destination $refs -Force
}
Remove-Item $publish -Recurse -Force -ErrorAction Ignore

& (Join-Path $PSScriptRoot 'GenerateToc.ps1')

Remove-Item (Join-Path $root '_site') -Recurse -Force -ErrorAction Ignore
foreach ($lang in $langs) {
    dotnet docfx (Join-Path $root "docfx/$lang.json") --warningsAsErrors
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}

# Language selection page at the site root.
Copy-Item (Join-Path $root 'docfx/index.html') (Join-Path $root '_site/index.html')
