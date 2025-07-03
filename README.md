# Asset Organizer

A Unity Editor tool for organizing assets by type or folder structure.

## Features
- Sorts selected assets and their dependencies into folders by type (e.g. Animation, Texture, Material, Prefab, etc.)
- Allows move, copy, or skip for each asset type and special folder

## Installation
- **VPM (VRChat Package Manager):**
  - Use VCC (VRChat Creator Companion) to add this package from a release zip, or from a git URL.
- **Unity Package Manager:**
  - To install from git: In Unity, open Window > Package Manager. Click the + button and select "Add package from git URL..." and enter:
    ```
    https://github.com/Neuru5278/AssetOrganizer.git
    ```
    > Note: You must have [git](https://git-scm.com/) installed on your computer to use this method.
  - To install from local folder:
    1. Download this repository (via "Download ZIP" or `git clone`).
    2. In Unity, open Window > Package Manager. Click the + button and select "Add package from disk..."
    3. Select the downloaded folder.

## Usage
- Menu: Tools > NeuruTools > Asset Organizer
- Select an asset or folder, click "Get Assets"
- Set actions if needed, then click "Organize by Type"
- If you selected a folder as the main asset, you can also use "Keep Structure" to preserve the original folder layout 