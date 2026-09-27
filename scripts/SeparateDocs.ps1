# Separate Docs
# A tool to split XML document comment files based on 'xml:lang' attribute.
#
# Source: https://github.com/from-dtx/SeparateDocs
# Modified: build paths with Join-Path for cross-platform (Linux) support.
#
# MIT License
#
# Copyright (c) 2021 FROM / Kenji Yamazaki
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in all
# copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
# SOFTWARE.

Param( [parameter(mandatory)] $projectName,$outdir,$xmldocdir )

Write-Host "Separate Docs (C) Copyright 2020 FROM/Kenji Yamazaki"

# XPath for target documents
$targets = @("//member/*", "//member/*/*")
# target languages
$langs = @("en", "ja")

$script:ErrorActionPreference = 'Stop'

$xmlpath = Join-Path $outdir "${projectName}.xml"
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
        $docfolder = Join-Path $xmldocdir $dn
    }

    if( !( Test-Path $docfolder ) )
    {
        mkdir $docfolder | Out-Null
    }

    $outpath = Join-Path $docfolder "${projectName}.xml"
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
        $feedbackDir = Join-Path $outdir $dn
        if( !( Test-Path $feedbackDir ) )
        {
            mkdir $feedbackDir | Out-Null
        }
        Copy-Item ${outpath} (Join-Path $feedbackDir "${projectName}.xml")
    }
}

