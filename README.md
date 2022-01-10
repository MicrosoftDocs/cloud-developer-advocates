# Microsoft Open Source Code of Conduct
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Adding your profile

1. Fork the [main repository](https://github.com/MicrosoftDocs/cloud-developer-advocates) to your account. Clone the repository locally and switch to the `main` branch.
2. Add a YAML file with the following convention `firstname-lastname.yml` at the path `/advocates/`.
3. Add your profile picture (PNG/JPG) to `/advocates/media/profiles/` with the same convention. Ensure that your picture don't go above 450px and that it is perfectly square (eg. 450x450)

## Filling up your profile

For your convenience, a snippet has been created to allow you to fill your profile.

Start by opening the YAML we just created (eg. `firstname-lastname.yml`) and make sure that your cursor is at the beginning of the file.

Press `CTRL-SHIFT-P` (CMD-SHIFT-P on Mac), and type `Insert snippet`. Select the `Advocate Profile` loaded from your workspace. Make sure to tab through the different options as it will ensure consistency without relying on copy/paste.

Make sure to update your social profiles and the longitude/latitude of your city. For security reasons, do not use your home's longitude/latitude but your city's.

Feel free to add any other links to the `connect` section but to limit it to ~6. You can use any title you want.

### After filling up your profile

1. Commit those changes to your local repository.
2. Push those changes to your fork.
3. Open a pull request to the [main repository](https://github.com/MicrosoftDocs/cloud-developer-advocates).

### Final Merger Instructions

If you have merging rights on this repository, those are instructions for you.

1. Clone the repository of the pull request in a local folder
2. Open a PowerShell console, navigate to the folder of the repo and run `./FromYmlToTOC-INDEX.ps1`. This will update 3 files (`index.html.yml`, `map.yml`, `toc.yml`) that ensure the home page, the map, and the table of content (on the left) are updated with the latest updates. 
3. Commit those changes to your local repository.
4. Pushing it back will update the PR

## Merging to `live`

The Advocates website will only show what's inside the `live` branch. This allow us to work on multiple iteration and make mistakes in `main` before we publish.

To publish all of your changes, [create a new PR to live](https://github.com/MicrosoftDocs/cloud-developer-advocates/compare/live...main?title=live%20%3C=%20main) and submit it. Once all the validation and checks are green, you can merge it.

Changes can take up to 15 minutes to be applied. 

## Q&A

### I see a warning on `advocates/index.html.yml`. Invalid file link for './map' and './tweets'

```
Line 10, Column 8: [Warning-file-not-found] Invalid file link: './map'.
```

Those are perfectly normal. Those links are dynamic and do not exist. Docfx can't find those files so it's warning you.

### I don't have PowerShell installed, do I need to run the `FromYmltoTOC-INDEX.ps1`?

Unless you are the final merger, you do not need to run that command. You therefor don't need PowerShell.
