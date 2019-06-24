# tldr-sharp

[![Build Status](https://travis-ci.org/principis/tldr-sharp.svg?branch=master)](https://travis-ci.org/principis/tldr-sharp)

A C# based command-line client for [tldr](https://github.com/tldr-pages/tldr).

![tldr screenshot](screenshot.png)

## Requirements
Because of TLS 1.2 support, Mono >= 4.8, built with TLS 1.2 support, is required. If your distro comes with an older version, please install the [latest stable](https://www.mono-project.com/download/stable/).
* Mono >= 4.8 because of TLS 1.2 support

## Installing
### Linux
Download and execute the install script from the latest [release](https://github.com/principis/tldr-sharp/releases).
```
wget <linux_install.sh url>
chmod +x linux_install.sh
./linux_install.sh
```

#### Manual
Download and extract the latest [release](https://github.com/principis/tldr-sharp/releases).
```
mkdir tldr
tar xzf v1.4.0.tar.gz -C tldr
sudo mv tldr /usr/local/lib
cd /usr/local/bin
sudo wget https://raw.githubusercontent.com/principis/tldr-sharp/master/tldr
sudo chmod +x tldr
```

### Windows
Extract the latest [release](https://github.com/principis/tldr-sharp/releases) and download the latest [sqlite3 dll](https://www.sqlite.org/download.html). 
Extract the dll in the same folder as tldr_sharp.exe.

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
      --lang=VALUE           Override the default language
  -m, --markdown             Show the markdown source of a page
  -p, --platform=VALUE       Override the default OS
  -s, --search=VALUE         Search for a string.
  -u, --update               Update the local cache.
      --self-update          Check for tldr-sharp updates.
  -v, --version              Show version information.
```

## License

This project is licensed under the GPL license - see the [LICENSE](LICENSE) file for details

## Contributing
This project is the result of a friday night boredom. I only tested it on Ubuntu 18.04 but should be able to run on every platform which supports mono.

Contributions are always welcome!
