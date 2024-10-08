[cmdletbinding()]
param (
    [string]$directory = (Get-Location)
)

function Get-DocumentMetadata {
    param (
        [System.IO.FileInfo] $file,
        [string] $relativepath
    )

    $properties = @{
        'Title'        = ''
        'UID'          = ''
        'RelativePath' = $relativepath.TrimStart('/')
        'Parent'       = $file.DirectoryName
        'FileName'     = $file.Name
        'tagline'      = ''
        'imageAlt'     = ''
        'imageSrc'     = ''
        'twitter'      = ''
        'location'     = ''
        'display'      = ''
        'lat'          = ''
        'long'         = ''
    }

    $awards = @{
        'dockerCaptain'             = "Docker Captain"
        'langchainCommunityCampion' = "Langchain Community Champion"
        'hashicorpAmbassador'       = "HashiCorp Ambassador"
        'gde'                       = "Google Developer Expert"
        'javaChampion'              = "Java Champion"
        'cncfAmbassador'            = "CNCF Ambassador"
        'mvp'                       = "MVP Alumni"
        'rd'                        = "RD Alumni"
        'finops'                    = "FinOps Certified Practitioner"
    }

    $metadata = New-Object -TypeName PSObject -Prop $properties
    $metadata.PSObject.TypeNames.Insert(0, 'DocFX.DocumentMetadata')



    if ($file.Extension -eq '.yml') {
        if ($file.Name -eq 'index.html.yml') {
            $script:indexTitle = Get-IndexTitle -file $file
        }

        $title = Get-YamlProp -file $file -propName 'name'
        $metadata.Title = if ($title) { $title } 

        $uid = Get-YamlProp -file $file -propName 'uid'
        $metadata.UID = if ($uid) { $uid }

        $metadata.tagline = Get-YamlProp -file $file -propName 'tagline'
        $metadata.imageAlt = Get-YamlProp -file $file -propName '  alt'
        $metadata.imageSrc = Get-YamlProp -file $file -propName '  src'
        $metadata.display = Get-YamlProp -file $file -propName '  display'
        $metadata.lat = Get-YamlProp -file $file -propName '  lat'
        $metadata.long = Get-YamlProp -file $file -propName '  long'

        # iterate through awards and append to the metadata.tagline property
        foreach ($award in $awards.Keys) {
            $awardValue = Get-YamlProp -file $file -propName $award
            if ($awardValue) {
                $metadata.tagline = $metadata.tagline + " | " + $awards[$award]
            }
        }

        $metadata.twitter = Get-Twitter -file $file

        return $metadata
    }

    if ($file.Extension -eq '.md') {
        #$title = Get-MarkdownMetadata -file $file
        #$metadata.Title = if ($title) { $title } else { $file.Name }

        if ($file.Name -eq 'index.md') {
            $script:indexTitle = Get-IndexTitle -file $file
        }
        
        $title = Get-YamlProp -file $file -propName 'name'
        $metadata.Title = if ($title) { $title } 
        return $metadata
    }
}

function Get-IndexTitle {
    param (
        [System.IO.FileInfo] $file
    )

    $title = ([regex]'^title\:.+')

    # Look for the metadata.name property.
    foreach ($linegroup in (Get-Content $file.FullName -ReadCount 1000)) {
        if ($title.Match($linegroup).Success) {
            return $title.Match($linegroup).Groups[0].Value.Replace('title:', '').TrimStart(' ')
        }
    }    
}

function Get-YamlName {
    param (
        [System.IO.FileInfo] $file
    )

    $name = ([regex]'^name\:.+')

    # Look for the metadata.name property.
    foreach ($linegroup in (Get-Content $file.FullName -ReadCount 1000)) {
        if ($name.Match($linegroup).Success) {
            return $name.Match($linegroup).Groups[0].Value.Replace('name:', '').TrimStart(' ')
        }
    }    
}

function Get-Twitter {
    param (
        [System.IO.FileInfo] $file
    )

    foreach ($linegroup in (Get-Content $file.FullName -ReadCount 1000)) {
        if ($linegroup -match '\s*url\: https\:\/\/twitter\.com.*') {
            return ($linegroup -split '/')[-1]
        }
    }
}

function Get-YamlUID {
    param (
        [System.IO.FileInfo] $file
    )

    $uid = ([regex]'^uid\:.+')

    # Look for the metadata.title property.
    foreach ($linegroup in (Get-Content $file.FullName -ReadCount 1000)) {
		
        if ($uid.Match($linegroup).Success) {
            return $linegroup.Replace('uid:', '').TrimStart(' ')
        }
    }    
}

function Get-YamlProp {
    param (
        [System.IO.FileInfo] $file,
        [System.String] $propName
    )

    # Look for the metadata.title property.
    foreach ($linegroup in (Get-Content $file.FullName -ReadCount 1000 -encoding UTF8)) {
        if ($linegroup -match '^\s*' + $propName + '\:.+') {    
            return $linegroup.Replace($propName + ':', '').TrimStart(' ')
        }
    } 
    return "";
}

function Remove-RootPath {
    param (
        [string] $rootpath,
        [string] $fullpath
    )

    return $fullpath.Replace($rootpath, '').Replace([IO.Path]::DirectorySeparatorChar, '/')
}

function Check-GlobMatch {
    param (
        [string[]] $patterns,
        [string] $matchpath
    )

    foreach ($pattern in $patterns) {
        $pattern_regex = ($pattern -split '\*\*' | ForEach-Object { $_.Replace('*', '[^\/]+') }) -join '.*'
        $match = $matchpath -match $pattern_regex

        Write-Verbose "Path $matchpath matches $pattern_regex : $match"
        if ($match -eq $true) {
            return $match
        }
    }

    return $false
}

function Format-Yaml {
    param (
        $object
    )

    foreach ($item in $object) {
        $depth = $item.Name.Split('/').Length
        $index = $item.Group | Where-Object { $_.FileName -match "index.*" } | Select-Object -First 1
        $children = $item.Group | Where-Object { -Not ($_.FileName -match "index.*") }
        $indent = '  ' * $depth
        $startobject = ('  ' * ($depth - 1)) + '- '
        $uid = if ($index) { $index.UID } else { $item.Name.Split('/') | Select-Object -Last 1 }
        Write-Host $index
        $name = $startobject + 'name: Cloud Advocates'
        $href = if ($index.RelativePath) { $indent, 'href: ', $index.RelativePath -join '' } else { "" } 
        $expanded = $indent + 'expanded: true'
        $items = $indent + 'items: '

        if (([array]$children).Length -gt 0) {
            $contents = @(
                ($name, $href, $expanded, $items | Where-Object { $_.Length -gt 0 }) -join [Environment]::NewLine
            )

            $indent = '  ' * ($depth + 1)
            $startobject = ('  ' * ($depth)) + '- '
    
            $contents += $children `
            | ForEach-Object {
                $name = $startobject + 'name: ' + $_.Title
                $uid = $indent + 'uid: ' + $_.UID
                
                return $name, $uid -join [Environment]::NewLine
            }
        }
        else {
            $contents = @(
                ($name, $uid | Where-Object { $_.Length -gt 0 }) -join [Environment]::NewLine
            )
        }
        $contents += '# Add more to the list. Be careful to preserve spaces and hyphen format.'
                        
        return $contents
    }
}

function Format-Index-Yaml {
    param (
        $object
    )

    foreach ($item in $object) {
        $depth = $item.Name.Split('/').Length
        $index = $item.Group | Where-Object { $_.FileName -match "index.*" } | Select-Object -First 1
        $children = $item.Group | Where-Object { -Not ($_.FileName -match "index.*") }
        $indent = '  ' * $depth
        $startobject = ('  ' * ($depth - 1)) + '- '
        
        $uid = if ($index) { $index.UID } else { $item.Name.Split('/') | Select-Object -Last 1 }
        
        if (([array]$children).Length -gt 0) {

            $indent = ' ' * ($depth + 1)
            $startobject = ('' * ($depth)) + '- '

            $IndexFilecontent = $children `
            | ForEach-Object {

                $uidVal = $_.UID
                IF ([string]::IsNullOrEmpty($uidVal)) {
                    $uid = ''
                }
                else {
                    $uid = $startobject + 'uid: ' + $_.UID 
                }
                    
                $nameVal = $_.Title
                IF ([string]::IsNullOrEmpty($nameVal)) {
                    $name = ''
                }
                else {
                    $name = $indent + 'name: ' + $_.Title 
                }

                $taglineVal = $_.tagline
                IF ([string]::IsNullOrEmpty($taglineVal)) {
                    $tagline = ''
                }
                else {
                    $tagline = $indent + 'tagline: ' + $_.tagline
                }

                ### Image properties
                $imageSrcVal = $_.imageSrc
                IF ([string]::IsNullOrEmpty($imageSrcVal)) {
                    $imageSrc = ''
                }
                else {
                    $imageSrc = $indent + '  src: ' + $_.imageSrc 
                }

                $imageAltVal = $_.imageAlt
                IF ([string]::IsNullOrEmpty($imageAltVal)) {
                    $imageAlt = ''
                }
                else {
                    $imageAlt = $indent + '  alt: ' + $_.imageAlt
                }

                IF ([string]::IsNullOrEmpty($imageAltVal) -and [string]::IsNullOrEmpty($imageSrcVal)) {
                    $image = ''
                }
                else {
                    $image = $indent + 'image:' 
                }

                ### Location properties
                $displayVal = $_.display
                IF ([string]::IsNullOrEmpty($displayVal)) {
                    $display = ''
                }
                else {
                    $display = $indent + '  display: ' + $_.display 
                }


                $latVal = $_.lat
                IF ([string]::IsNullOrEmpty($latVal)) {
                    $lat = ''
                }
                else {
                    $lat = $indent + '  lat: ' + $_.lat 
                }

                $longVal = $_.long
                IF ([string]::IsNullOrEmpty($longVal)) {
                    $long = ''
                }
                else {
                    $long = $indent + '  long: ' + $_.long 
                }
                    
                IF ([string]::IsNullOrEmpty($displayVal) -and [string]::IsNullOrEmpty($latVal) -and [string]::IsNullOrEmpty($longVal)) {
                    $location = $null
                }
                else {
                    $location = $indent + 'location: ' 
                }

                $twitterval = $_.twitter
                IF ([string]::IsNullOrEmpty($twitterval)) {
                    $twitter = ''
                }
                else {
                    $twitter = $indent + 'twitter: "' + $_.twitter.Replace('https://twitter.com/', '') + '"'
                }
                return ($uid , $name, $tagline, $image, $imageSrc, $imageAlt, $location, $display, $lat, $long, $twitter | Where-Object { $_.Length -gt 0 } ) -join [Environment]::NewLine
            }
        }
        else {
            Write-Host "No children found"
        }

        return $IndexFilecontent
    }
}

function Format-Markdown {
    param (
        $object
    )

    $object.

    $heading = '#' * $object.RelativePath.Split('/').Length
    return $heading + " [" + $object.Title + "](" + $object.RelativePath + ")"
}

# Set location to directory path
$directory = [System.IO.Path]::GetFullPath(($directory))
$opc_path = [System.IO.Path]::GetFullPath((Join-Path $directory '.openpublishing.publish.config.json'))
Write-Verbose "Working directory is $opc_path"

# Read .openpublishing.publish.config.json (opconfig)
# Look at opconfig's "docsets_to_publish" array. Each item's "build_source_folder" tells us where a docfx.json, toc.md/yml and content is located.
$opc_json = Get-Content -Raw $opc_path | Out-String | ConvertFrom-Json
$source_folders = $opc_json.docsets_to_publish.build_source_folder

$indexTitle = 'Cloud Advocates'

# Read the docfx.json file's "build.content" array. This tells us the glob patterns to use to locate content, and which content to exclude.
foreach ($source_folder in $source_folders) {
    Write-Verbose "Finding docfx.json in source folder: $source_folder"
    $docfx_dir = [System.IO.Path]::GetFullPath((Join-Path $directory $source_folder))
    $docfx_path = [System.IO.Path]::GetFullPath((Join-Path $docfx_dir 'docfx.json'))
    $toc_path = [System.IO.Path]::GetFullPath((Join-Path $docfx_dir 'toc.yml'))
    $index_path = [System.IO.Path]::GetFullPath((Join-Path $docfx_dir 'index.html.yml'))

    Write-Verbose "Found docfx json: $docfx_path"

    # Use the globs to locate the files on disk, and build the TOC structure, serializing to md or yml.
    # TOC should go in top-level output folder from 'docsets_to_publish' array, it seems.
    $docfx_json = Get-Content -Raw $docfx_path | Out-String | ConvertFrom-Json
    $includes = $docfx_json.build.content.files
    $excludes = $docfx_json.build.content.exclude
    $excludes = "**/toc.*", "**/map.*", "**/tweets.*"  # Exclude TOC, map and tweets files.

    #using @' '@  to avoid new line formatting notation 
    $content = @'
########################################################################
#############  AUTO-GENERATED FROM FromYmlToTOC-INDEX.ps1  #############
########################################################################
metadata:
  name: advocates_toc
items:
'@  
    $IndexFilecontent = @' 
### YamlMime:ProfileList
########################################################################
#############  AUTO-GENERATED FROM FromYmlToTOC-INDEX.ps1  #############
########################################################################
title: Cloud Advocates
description: |
  Our team's charter is to help every technologist on the planet succeed, be they students or those working in enterprises or startups. We engage in outreach to developers and others in the software ecosystem, all designed to further technical education and proficiency with the Microsoft Cloud + AI platform.
focalImage:
  src: https://developer.microsoft.com/en-us/advocates/media/bitda.png
  alt: "Developer Advocate Bit in a Red T-Shirt with Developer Advocate label."
metadata:
  title: Microsoft Cloud Advocates
  description: Trusted advisors to developer and IT professionals.
  twitterWidgets: true
  hide_bc: true
filterText: Cloud Advocates
profiles:
'@

    $objects = Get-ChildItem -Path $docfx_dir -Recurse -File
    | Where-Object { (-Not (Check-GlobMatch -patterns $excludes -matchpath ( Remove-RootPath -rootpath $docfx_dir -fullpath $_.FullName ))) -and (Check-GlobMatch -patterns $includes -matchpath ( Remove-RootPath -rootpath $docfx_dir -fullpath $_.FullName )) }
    | Sort-Object FullName
    | ForEach-Object { Get-DocumentMetadata -file $_ -relativepath ( Remove-RootPath -rootpath $docfx_dir -fullpath $_.FullName ) }
    | Group-Object { Remove-RootPath -rootpath $docfx_dir -fullpath $_.Parent }
    | Sort-Object Name

    # writing to TOC file
    ForEach-Object -Begin { return $content } -Process { Format-Yaml -object $objects }
    | Out-File -filepath $toc_path -encoding utf8

    # writing to Index file 
    ForEach-Object -Begin { return $IndexFilecontent } -Process { Format-Index-Yaml -object $objects }
    | Out-File -filepath $index_path -encoding utf8

    Write-Verbose "Generated table of contents at $toc_path"
    Write-Verbose "Generated Index file at $index_path"
}
