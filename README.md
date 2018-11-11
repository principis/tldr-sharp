# tldr-sharp
A C# based command-line client for [tldr](https://github.com/tldr-pages/tldr).

![tldr screenshot](screenshot.png)

## Requirements
* Mono
* Mono.Data.Sqlite

On ubuntu install mono-complete

## Installing
### Linux
Download and execute the install script from the latest [release](https://github.com/principis/tldr-sharp/releases).
```bash
wget <linux_install.sh url>
chmod +x linux_install.sh
./linux_install.sh
```

#### Manual
Download and extract the latest [release](https://github.com/principis/tldr-sharp/releases).
```bash
mkdir tldr
tar xzf v1.1.0.tar.gz -C tldr
sudo mv tldr /usr/local/lib
cd /usr/local/bin
sudo wget https://raw.githubusercontent.com/principis/tldr-sharp/master/tldr
sudo chmod +x tldr
```

### Windows
Sometime in the future....

## Usage
```
Usage: tldr command [options]

Simplified and community-driven man pages

  -h, --help                 Display this help text.
  -l, --list                 Show all pages for the current platform
  -a, --list-all             Show all pages
  -u, --update               Update the local cache.
  -c, --clear-cache          Clear the local cache.
```

## License

This project is licensed under the GPL license - see the [LICENSE](LICENSE) file for details

## Contributing
This project is the result of a friday night boredom. I only tested it on Ubuntu 18.04 but should be able to run on every platform which supports mono.

Contributions are always welcome!
