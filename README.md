# Ixian Project - Spixi
Ixian decentralized chat client for desktop and mobile.

## About Ixian

Ixian DLT is a revolutionary blockchain that brings several innovative advantages, such as processing a high volume of micro-transactions quickly while consuming a low amount of processing power, disk space and energy.

**Homepage**: https://www.ixian.io

**Discord**: https://discord.gg/pdJNVhv

**Bitcointalk**: https://bitcointalk.org/index.php?topic=4631942.0

**Documentation**: https://docs.ixian.io

## The repository

The Ixian GitHub project is divided into seven main parts:

* [Ixian-Core](https://github.com/ProjectIxian/Ixian-Core): Functionality common to all other projects.
* [Ixian-DLT](https://github.com/ProjectIxian/Ixian-DLT): Implementation of the blockchain-processing part (the Master Node software).
* [Ixian-S2](https://github.com/ProjectIxian/Ixian-S2): Implementation of the streaming network (the S2 Node software).
* [Spixi](https://github.com/ProjectIxian/Spixi): Implementation of the Spixi messaging client for Windows, Android and iOS.
* [Ixian-Miner](https://github.com/ProjectIxian/Ixian-Miner): Implementation of the Ixian standalone mining software.
* [Ixian-LiteWallet](https://github.com/ProjectIxian/Ixian-LiteWallet): Simple CLI wallet for the Ixian DLT network.
* [Ixian-Pool](https://github.com/ProjectIxian/Ixian-Pool): Mining pool software.

## About Spixi

Spixi is a multiplatform, decentralized and privacy-oriented chat application that brings together users from different platforms. Because Spixi operates on top of Ixian S2 and DLT networks, it includes a set of distinctive features:

* Decentralized architecture ensures practically no downtime. Backend is based on the Ixian DLT and Ixian S2 streaming network to provide ultimate decentralization and security.
* Cryptographically secure (dual end-to-end encryption), which means that only the intended recipient can read messages.
* Multi-platform (PC, iOS, Android, others)
* Ixian DLT integration, which enables cryptocurrency wallet features right in the IM client.
* Messages are not stored in any one country or central location. There is no legal or corporate entity that possesses all of the messages, so no entity can meaningfully demand or obtain decryption keys.

## Development branches

There are two main development branches:
* **master**: This branch is used to build the binaries for the official Ixian DLT network. It should change slowly and be quite well tested. This is also the default branch for anyone who wishes to build their Ixian software from source.
* **development**: This is the main development branch and the source for testnet binaries. The branch might not always be kept bug-free, if an extensive new feature is being worked on. If you are simply looking to build a current binary yourself, please use one of the release tags, which will be associated with the master branch.

## Documentation

You can find documentation on how to build, APIs and other documents on [Ixian Documentation Pages](https://docs.ixian.io).

## Get in touch / Contributing

If you feel like you can contribute to the project, or have questions or comments, you can get in touch with the team through Discord: https://discord.gg/pdJNVhv

## Pull requests

If you would like to send an improvement or a bug fix to this repository, but without permanently joining the team, follow these approximate steps:

1. Fork this repository
2. Create a branch from **development** branch (preferably with a name that describes the change)
3. Create commits (the commit messages should contain some information on what and why was changed)
4. Create a pull request to this repository's **development** branch for review and inclusion
