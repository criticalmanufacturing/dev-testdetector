# Test Detector

![Build Status](https://github.com/criticalmanufacturing/dev-testdetector/workflows/.NET%20Core/badge.svg)

Test Detector is a Critical Manufacturing developer tool that performs automatic test detection (currently only in MSTest C#), allowing for on-the-fly test changes.
The primary goal of this tool is to identify new or modified test methods in feature branches, allowing this tests to be run on Pull Requests.

If you have enhancement requests or want to contribute, feel free to open an issue, fork the repository and create pull requests.

## Getting Started

Test Detector is a command line tool. To get started just run:

```sh
Cmf.Tools.TestDetector.Console --help
```

This will show all available options.

### Integrating with Azure DevOps Pipeline

Bellow you can find an example on how to integrate Test Detector in your Azure DevOps YAML pipeline. Please make sure to adapt the tempalte to your needs by replacing all ```{{ }}``` tokens.

```yml
- task: DownloadGitHubRelease@0
  displayName: 'Download GitHub Release dev-testdetector'
  inputs:
    connection: Github
    userRepository: 'criticalmanufacturing/dev-testdetector'
    downloadPath: '$(System.ArtifactsDirectory)/dev-testdetector'
    defaultVersionType: specificTag
    version: {{currentTestDetectorVersion}}
    
- task: ExtractFiles@1
  displayName: 'Extract dev-testdetector tool'
  inputs:
    archiveFilePatterns: '$(System.ArtifactsDirectory)/dev-testdetector/*.zip'
    destinationFolder: '$(System.ArtifactsDirectory)/dev-testdetector-tool'

- task: PowerShell@1
    displayName: 'Run Cmf.Tools.TestDetector.Console'
    inputs:
      scriptType: inlineScript
      inlineScript: |
      
      $separator = "/"
      $parts = $ENV:SYSTEM_PULLREQUEST_TARGETBRANCH.Split($separator)

      $PRTargetBranchName = $parts[$parts.Count-1]
    
      & git remote update
      $latestCommitFromOrigin = & git rev-parse "origin/$PRTargetBranchName"
      
      .\Cmf.Tools.TestDetector.Console.exe --repositorypath $ENV:BUILD_SOURCESDIRECTORY --testsolutionpath $ENV:BUILD_SOURCESDIRECTORY/{{PathToSolution}} --testcategory "{{TestCategoryToAdd}}" --sourcecommitid $ENV:BUILD_SOURCEVERSION --targetcommitid $latestCommitFromOrigin --filter "{{glob}}"
      workingFolder: '$(System.ArtifactsDirectory)/dev-testdetector-tool'
```

Tokens description:
- ```{{currentTestDetectorVersion}}```: Version of the release of dev-testdetector to use. (eg: 0.1.0)
- ```{{PathToSolution}}```: Path to the C# solution to analyze. (eg: 'Tests\TestSolution.sln')
- ```{{TestCategoryToAdd}}```: Test Category to add to new or modified test cases. This test category should be used in the VSTest filter so that these tests will subsequently run.  (eg: 'PullRequest')
- ```{{glob}}```: Filter the files that the tool should look into for changes. (eg: 'Tests\\*\*\\\*.cs')

## License

Copyright (c) Critical Manufacturing. All rights reserved.
