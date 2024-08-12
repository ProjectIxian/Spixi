# Spixi
Spixi is a multiplatform, decentralized and privacy-oriented chat application that brings together users from different platforms. Because Spixi operates on top of Ixian S2 and DLT networks, it includes a set of distinctive features:

* Decentralized architecture ensures practically no downtime. Backend is based on the Ixian DLT and Ixian S2 streaming network to provide ultimate decentralization and security.
* Cryptographically secure (dual end-to-end encryption), which means that only the intended recipient can read messages.
* Multi-platform (PC, iOS, Android, others)
* Ixian DLT integration, which enables cryptocurrency wallet features right in the IM client.
* Messages are not stored in any one country or central location. There is no legal or corporate entity that possesses all of the messages, so no entity can meaningfully demand or obtain decryption keys.


## Development branches

There are two main development branches:
* **master**: This branch is used to build the binaries for the latest stable release of Spixi. It should change slowly and be quite well-tested. This is also the default branch for anyone who wishes to build their Ixian software from source.
* **development**: This is the main development branch. The branch might not always be kept bug-free, if an extensive new feature is being worked on. If you are simply looking to build a current binary yourself, please use one of the release tags which will be associated with the master branch.

## Documentation

You can find documentation on how to build, APIs and other documents on [Ixian Documentation Pages](https://docs.ixian.io).

## Build Instructions

### Prerequisites

Before you start, ensure you have the following prerequisites installed:

- .NET 8 SDK or later
- Visual Studio 2022 or later (with .NET MAUI workload installed)
- Git

### Cloning the Repository

First, you need to clone the repository to your local machine. Open your terminal or Git Bash and run the following command:

```bash
git clone https://github.com/ProjectIxian/Spixi.git
cd Spixi
```

### Building and Running with Visual Studio

1. **Open the Solution:**
   - Launch Visual Studio.
   - Open the cloned repository folder and double-click on the solution file (`Spixi.sln`) to open it in Visual Studio.

2. **Restore NuGet Packages:**
   - Visual Studio should automatically restore the NuGet packages. If not, go to `Tools` > `NuGet Package Manager` > `Package Manager Console` and run:
     ```powershell
     dotnet restore
     ```

3. **Build the Solution:**
   - Build the solution by clicking on `Build` > `Build Solution` or by pressing `Ctrl+Shift+B`.

4. **Run the Application:**
   - Select the target platform (Android, iOS, Windows, etc.) from the toolbar.
   - Click on the `Start` button or press `F5` to run the application.

### Building and Running via Terminal

1. **Restore NuGet Packages:**
   - Open your terminal and navigate to the cloned repository folder:
     ```bash
     cd Spixi
     ```
   - Restore the NuGet packages by running:
     ```bash
     dotnet restore
     ```

2. **Build and Run the Application:**
   - To build and run the application on a specific platform, use the following command:
     ```bash
     dotnet build -t:Run -f net8.0-android              # For Android
     dotnet build -t:Run -f net8.0-ios                  # For iOS
     dotnet build -t:Run -f net8.0-windows10.0.19041.0 -p:Platform=x64  # For Windows
     dotnet build -t:Run -f net8.0-maccatalyst          # For macOS
     ```
   - Ensure you have the appropriate SDKs and emulators/simulators installed for the target platform.

3. **Build the Application in Release mode:**
   - To build and run the application on a specific platform, use the following command:
     ```bash
     dotnet build --configuration Release -f net8.0-android # For Android
     dotnet build --configuration Release -f net8.0-ios # For iOS
     dotnet build --configuration Release -f net8.0-windows10.0.19041.0 -p:Platform=x64  # For Windows
     dotnet build --configuration Release -f net8.0-maccatalyst # For macOS
     ```

### Additional Notes

- For detailed guidance on setting up your development environment, refer to the official [Microsoft .NET MAUI documentation](https://docs.microsoft.com/en-us/dotnet/maui/).
- If you encounter any issues, please check the [issues](https://github.com/ProjectIxian/Spixi/issues) section on the repository for existing solutions or open a new issue.
- **For iOS Development:**
  - You need a Mac to build and run the application on iOS.
  - Follow the .NET MAUI documentation to set up your Mac environment. This includes enabling remote access, installing Xcode, and setting up Xcode command line tools.
  - Detailed instructions can be found in the [official .NET MAUI documentation for iOS setup](https://learn.microsoft.com/en-us/dotnet/maui/ios/pair-to-mac?view=net-maui-8.0).


## Get in touch / Contributing

If you feel like you can contribute to the project, or have questions or comments, you can get in touch with the team through Discord: https://discord.gg/pdJNVhv

## Pull requests

If you would like to send an improvement or a bug fix to this repository, but without permanently joining the team, follow these approximate steps:

1. Fork this repository
2. Create a branch from **development** branch (preferably with a name that describes the change)
3. Create commits (the commit messages should contain some information on what and why was changed)
4. Create a pull request to this repository's **development** branch for review and inclusion


## About Ixian

Ixian DLT is a revolutionary blockchain that brings several innovative advantages, such as processing a high volume of micro-transactions quickly while consuming a low amount of processing power, disk space and energy.

**Homepage**: https://www.ixian.io

**Discord**: https://discord.gg/pdJNVhv

**Bitcointalk**: https://bitcointalk.org/index.php?topic=4631942.0

**Documentation**: https://docs.ixian.io

**GitHub**: https://www.github.com/ProjectIxian