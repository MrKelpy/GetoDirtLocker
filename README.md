<p align="center">
    <img width="300" src="https://raw.githubusercontent.com/MrKelpy/GetosDirtLocker/master/icon.ico" alt="logo">
</p>

# Geto's Dirt Locker 2: Electric Boogaloo
This is a joke program used to archive 'dirt' on people specifically crafted for [adocord](https://discord.gg/ado1024) in Discord. This project has been submitted as my final project for my programming class in 2024.

# Installation
Please note that this program has only been tested on **Windows** and might not work on other systems.<br>

1. Download the [installer present on the releases page](https://github.com/MrKelpy/GetosDirtLocker/releases/latest) or clone this repository using.:

```
$ git clone https://github.com/MrKelpy/GetosDirtLocker
```

2. Install [SQL Server 2022](https://go.microsoft.com/fwlink/p/?linkid=2216019&clcid=0x409&culture=en-us&country=us) and set it up as a basic installation.
3. If you've installed the locker through the executable, extract it to a folder and keep all the files in it.

### - REMOTE DATABASE CONNECTION
In order to setup the database on a remote server, you must run the program once and navigate to `%appdata%/.GetosLocker/data`. In there, you'll find a `database.cfg` file that configures the host of the database in the following format: `IPADDRESS:PORT:USERNAME:PASSWORD`
