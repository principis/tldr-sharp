# tldr-sharp

[![Build Status](https://travis-ci.org/principis/tldr-sharp.svg?branch=main)](https://travis-ci.org/principis/tldr-sharp)

A C# based, feature-rich command-line client for [tldr-pages](https://github.com/tldr-pages/tldr).

![tldr screenshot](screenshot.png)

## Requirements
Because of needed TLS 1.2 support, Mono >= 4.8, built with TLS 1.2 support, is required. If your distro comes with an older version, please install the [latest stable](https://www.mono-project.com/download/stable/).

## Installing

### Ubuntu/Debian

Install the [latest](https://github.com/principis/tldr-sharp/releases) Debian package.

### Linux
Download and execute the install script from the latest [release](https://github.com/principis/tldr-sharp/releases).
```
wget https://github.com/principis/tldr-sharp/releases//latest/download/tldr-sharp_linux_x64.sh
chmod +x linux_install_x64.sh
./linux_install_x64.sh
```

#### Manual
Download and extract the latest [release](https://github.com/principis/tldr-sharp/releases).
```sh
mkdir tldr
tar xzf <version>.tar.gz -C tldr
sudo mv tldr /usr/local/lib
cd /usr/local/bin
sudo wget https://raw.githubusercontent.com/principis/tldr-sharp/main/tldr
sudo chmod +x tldr
```

### Windows
_Note: Your antivirus may detect tldr-sharp as a virus._

Open powershell as administrator

Run the following command:
```ps
Set-ExecutionPolicy Bypass -Scope Process -Force; iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/principis/tldr-sharp/main/scripts/windows_install.ps1'))
```
Reopen powershell and run `tldr`.

#### Manual

Extract the latest [release](https://github.com/principis/tldr-sharp/releases) to a folder of choice (for example `C:\ProgramData\tldr-sharp`), add it to the Path and reboot.

## Usage
```
Usage: tldr command [options]
Simplified and community-driven man pages
  -a, --list-all             List all pages
  -c, --clear-cache          Clear the local cache
  -f, --render=VALUE         Render a specific markdown file
  -h, --help                 Display this help text
  -l, --list                 List all pages for the current platform and
                               language
      --list-os              List all OS's
      --list-languages       List all languages
  -L, --language, --lang=VALUE
                             Specifies the preferred language
  -m, --markdown             Show the markdown source of a page
  -p, --platform=VALUE       Override the default platform
  -s, --search=VALUE         Search for a string
  -u, --update               Update the local cache
      --self-update          Check for tldr-sharp updates
  -v, --version              Show version information
```

## License

This project is licensed under the GPL license - see the [LICENSE](LICENSE) file for details.

## Contributing
Contributions are always welcome! Please open an issue first.
