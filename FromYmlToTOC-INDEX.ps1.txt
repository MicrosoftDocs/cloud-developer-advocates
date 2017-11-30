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
        'Title' = $file.Name
		'UID' = $file.Name
        'RelativePath' = $relativepath.TrimStart('/')
        'Parent' = $file.DirectoryName
        'FileName' = $file.Name
        'tagline' = ''
        'imageAlt' = ''
        'imageSrc' = ''
        'twitter' = ''
        'location' = ''

    }

    $metadata = New-Object -TypeName PSObject -Prop $properties
    $metadata.PSObject.TypeNames.Insert(0,'DocFX.DocumentMetadata')

    if ($file.Extension -eq '.md') {
			$title = Get-MarkdownMetadata -file $file
			$metadata.Title = if ($title) { $title } else { $file.Name }
			return $metadata
    }

    if ($file.Extension -eq '.yml') {
        
        if($file.Name -eq 'index.html.yml')
        {
			$script:indexTitle = Get-IndexTitle -file $file
        }
        
        $title = Get-YamlProp -file $file -propName 'name'
        $metadata.Title = if ($title) { $title } else { $file.Name }
		
		$uid = Get-YamlProp -file $file -propName 'uid'
        $metadata.UID = if ($uid) { $uid } else { $file.Name }

        $metadata.tagline = Get-YamlProp -file $file -propName 'tagline'
        $metadata.twitter = Get-YamlProp -file $file -propName 'twitter'
        $metadata.location = Get-YamlProp -file $file -propName 'location'
        $metadata.imageAlt = Get-YamlProp -file $file -propName '  alt'
        $metadata.imageSrc = Get-YamlProp -file $file -propName '  src'

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

    $propRegex = ([regex]'^{$propName}\:.+')

    # Look for the metadata.title property.
    foreach ($linegroup in (Get-Content $file.FullName -ReadCount 1000)) {
		
        #if ($propRegex.Match($linegroup).Success) {
        if ($linegroup -match '^'+$propName+'\:.+') 
        {    
            return $linegroup.Replace($propName+':', '').TrimStart(' ')
        }
    }    
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

        return $match
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
        $name = $startobject + 'name: ' + $indexTitle
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
        } else {
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
                    $uid =  $startobject + 'uid: ' + $_.UID
                    $name = $indent + 'name: ' + $_.Title
                    $tagline = $indent + 'tagline: ' + $_.tagline
                    $image = $indent + 'image:'
                    $imageSrc = $indent + '  src: ' + $_.imageSrc
                    $imageAlt = $indent + '  alt: ' + $_.imageAlt
                    $location = $indent + 'location: ' + $_.location
                    $twitter = $indent + 'twitter: ' + $_.twitter.Replace('https://twitter.com/', '')
                
                    return $uid, $name, $tagline, $image, $imageSrc, $imageAlt, $location, $twitter -join [Environment]::NewLine
                }
        } else {
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

$indexTitle = ''

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
    $excludes = "**/toc.*" # Exclude TOC files.

    #using @' '@  to avoid new line formatting notation 
    $content = @'
metadata:
  name: advocates_toc
items:
'@  
    $IndexFilecontent = @' 
### YamlMime:ProfileList
title: Cloud Developer Advocates
description: |
  We write, speak, and dream in code.  Our global team is maniacal about making the world amazing for developers of all backgrounds. Connect with us, write code with us, and let’s meet up and talk cloud and all things developer!
  > [!div class="banner-container"]
  ![Microsoft + Advocate logo](https://developer.microsoft.com/en-us/advocates/media/bitmicrosoft.png)
metadata:
  title: Microsoft Cloud Developer Advocates
  description: Trusted advisors to developer and IT professionals.
  twitterWidgets: true
profiles:
'@

    $objects = Get-ChildItem -Path $docfx_dir -Recurse -File `
        | Where-Object { (-Not (Check-GlobMatch -patterns $excludes -matchpath ( Remove-RootPath -rootpath $docfx_dir -fullpath $_.FullName ))) -and (Check-GlobMatch -patterns $includes -matchpath ( Remove-RootPath -rootpath $docfx_dir -fullpath $_.FullName )) } `
        | Sort-Object FullName `
        | ForEach-Object { Get-DocumentMetadata -file $_ -relativepath ( Remove-RootPath -rootpath $docfx_dir -fullpath $_.FullName ) } `
        | Group-Object { Remove-RootPath -rootpath $docfx_dir -fullpath $_.Parent } `
        | Sort-Object Name `
        
    # writing to TOC file    
    ForEach-Object -Begin { return $content } -Process { Format-Yaml -object $objects } `
        | Out-File -filepath $toc_path

    # writing to Index file 
    ForEach-Object -Begin { return $IndexFilecontent } -Process { Format-Index-Yaml -object $objects } `
        | Out-File -filepath $index_path


    Write-Verbose "Generated table of contents at $toc_path"
    Write-Verbose "Generated Index file at $index_path"
}