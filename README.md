# XliffCheck
Checks for missing translations in an Azure DevOps build pipeline for Business Central AL Extensions

# Usage

```yaml
- task: CmdLine@2
  displayName: "Check Xliff Files"
  inputs:
    script: '"\\files\DevOps\Common\XliffCheck\App.exe" $(Build.SourcesDirectory)\Translations\'
```

# Credits
XliffCheck is inspired by https://github.com/ReportsForNAV/xliffCompare
