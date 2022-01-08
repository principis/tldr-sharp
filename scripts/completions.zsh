#compdef tldr-sharp

_tldr_sharp() {
  local -a platforms
  platforms=(linux osx sunos windows)

  _arguments \
    {-a,--list-all}'[List all pages]' \
    {-c,--clear-cache}'[Clear the cache]' \
    {-f,--render}'[Render a specific markdown file]' \
    {-h,--help}'[Display this help text]' \
    {-l,--list}'[List all pages for the current platform and language]' \
    '--list-os[List all platforms]' \
    '--list-languages[List all languages]' \
    {-L,--language}'[Specifies the preferred language]:lang:(("${(@f)$(tldr --list-languages)}"))' \
    {-m,--markdown}'[Show the markdown source of a page]' \
    {-p,--platform}'[Override the default platform]:platform:(${platforms})' \
    {-s,--search}'[Search for a string]' \
    {-u,--update}'[Update the cache]' \
    '--self-update[Check for tldr-sharp updates]' \
    {-v,--version}'[Show version information]' \
    ':page:("${(@f)$(tldr-sharp -a)}")' && return 0
}

_tldr_sharp
