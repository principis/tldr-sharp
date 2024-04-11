# tldr-sharp

A C# based, feature-rich command-line client for [tldr-pages](https://github.com/tldr-pages/tldr).

![tldr screenshot](screenshot.png)

## Requirements

- .NET Runtime 6.0 (or newer). Download it from [here](https://dotnet.microsoft.com/en-us/download).

## Installation

### Linux

#### Ubuntu/Debian

Install the [latest](https://github.com/principis/tldr-sharp/releases/latest/) Debian package.

#### Fedora
tldr-sharp is available in [COPR](https://copr.fedorainfracloud.org/coprs/principis/tldr-sharp/)

```sh
sudo dnf copr enable principis/tldr-sharp
sudo dnf --refresh install tldr-sharp
```

#### Manual

Download and extract the latest [release](https://github.com/principis/tldr-sharp/releases/latest/).

```sh
mkdir tldr-sharp
tar xzf <version>.tar.gz -C tldr-sharp
sudo chown -R root:root tldr-sharp
sudo cp -pr tldr-sharp /usr/local/lib/
sudo ln -s /usr/local/lib/tldr-sharp/tldr-sharp /usr/local/bin/tldr-sharp
```

### Windows

_Note: Your antivirus may detect tldr-sharp as a virus._

It is recommended to use the new [Windows Terminal](https://aka.ms/terminal), so the highlighting works as expected.

#### Installer

Download and run the [installer](https://github.com/principis/tldr-sharp/releases/latest/).

#### Manual installation

* Download the latest [release](https://github.com/principis/tldr-sharp/releases)
* Extract to a folder of choice, for example `C:\ProgramData\tldr-sharp`
* Copy `tldr-sharp.exe` to `tldr.exe`
* Add it to the path
    * Open Control Panel (old style)
    * On the **System** page, click **Advanced system settings** on the left-hand side
    * In the dialog window, click on the **Environment Variables** button
    * In **User variables** select `Path` and click **Edit**
    * In the popup, click **New** and add the path where you've installed tldr-sharp as
      follows: `C:\ProgramData\tldr-sharp`
* Reboot to make sure the Path is updated

You can now use tldr-sharp by entering `tldr tar` in your favorite shell.

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
      --list-platforms       List all platforms
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

* This project is licensed under GPL-3.0-or-later.
* Some configuration and scripts are licensed under CC0-1.0.

For more accurate information, check the individual files.

## Contributing

Contributions are always welcome! Please open an issue first.
