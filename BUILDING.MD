# Building

So you want to build your own DebugToolkit?

Here's how to get started:

1. Acquire a copy of the project:
    * Fork the project, and clone it from there. (Recommended for Contributing)
    * Clone the project directly (Recommended for Primary Contributors)
    * Download the zip file (Recommended for people without Git)
2. Setup the libraries
    * Run the setup.bat in the libs folder. This file requires Git.
    * You may also acquire the dependencies manually from your local risk of rain 2 installation and bepinex folder.
    * If that also isn't an option, the setup.bat pulls from the ror2libs github repository, where you can find a powershell script to download it.
3. *[OPTIONAL]* Download the [NetworkWeaver files](https://cdn.discordapp.com/attachments/697919673664274563/697919946143039588/NetworkWeaver.zip). Extract this such that the `/NetworkWeaver` sits next to `/Resources` and all other folders.
    * These are needed if you want to build networking.
4. Open the project in Visual Studio.
    * Additionally, you may want to select your configuration:
        * Release: For what you will see on Thunderstore.
        * BLEEDING-EDGE: The same as Release, save that it contains a warning that the build is not final.
        * Debug: Contains additional commands for debugging other commands.
        * NO-UNET: Skips the network weaving post-build step.
        * NO-DYNAMICS: Skips packing the Microsoft.CSharp.dll into the final file. This means you will need to manually place that dll in your plugins folder.
            * You can find it in your nuget folders. [This site contains their locations for various platforms](https://lastexitcode.com/projects/NuGet/FileLocations/)
        * BARE: Combines NO-UNET and NO-DYNAMICS
5. Press build! The file should appear in the main project directory.
