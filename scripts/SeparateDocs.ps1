# Separate Docs
# A tool to split XML document comment files based on 'xml:lang' attribute.

Param( [parameter(mandatory)] $projectName,$outdir,$xmldocdir )

Write-Host "Separate Docs (C) Copyright 2020 FROM/Kenji Yamazaki"

# XPath for target documents
$targets = @("//member/*", "//member/*/*")
# target languages
$langs = @("en", "ja")

$script:ErrorActionPreference = 'Stop'

$xmlpath = "${outdir}${projectName}.xml"
Copy-Item ${xmlpath} "${xmlpath}.org"

foreach( $dn in $langs ) {

    $xml = [xml] (Get-Content -Encoding UTF8 "${xmlpath}.org" )

    # namespace for 'xml:lang'
    $ns = New-Object -TypeName System.Xml.XmlNamespaceManager -ArgumentList $xml.NameTable
    $ns.AddNamespace('xml', 'http://www.w3.org/XML/1998/namespace')

    foreach( $target in $targets )
    {
        foreach( $item in $xml.SelectNodes("${target}[@xml:lang!='${dn}']", $ns) )
        {
            $item.ParentNode.RemoveChild( $item ) | Out-Null
        }
    }

    if( ${dn} -eq "en" )
    {
        # en for default
        $docfolder = ${xmldocdir}
    }
    else
    {
        # other into subfolder
        $docfolder = "${xmldocdir}\${dn}"
    }

    if( !( Test-Path $docfolder ) )
    {
        mkdir $docfolder | Out-Null
    }

    $outpath = "${docfolder}\${projectName}.xml"
    $xml.Save( $outpath )
    
    Write-Host "--> xml:lang='${dn}', ${outpath} done."

    # feedback xml from generatedDir to outdir 
    if( ${dn} -eq "en" )
    {
        # en for default
        Copy-Item ${outpath} ${xmlpath}
    }
    else
    {
        $feedbackDir = "${outdir}\${dn}"
        if( !( Test-Path $feedbackDir ) )
        {
            mkdir $feedbackDir | Out-Null
        }
        Copy-Item ${outpath} "${feedbackDir}\${projectName}.xml"
    }
}

