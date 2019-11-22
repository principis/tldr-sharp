#compdef tldr
  
local -a pages oses languages
pages=$(tldr -a)
platforms='(linux osx sunos windows)'
languages=$(tldr --list-languages)

_arguments \
  '(- *)'{-a,--list-all}'[List all pages]' \
  '(- *)'{-c,--clear-cache}'[Clear the cache]' \
  '(- *)'{-f,--render}'[Render a specific markdown file]' \
  '(- *)'{-h,--help}'[Display this help text]' \
  '(- *)'{-l,--list}'[List all pages for the current platform]' \
  "--list-os[List all platforms]" \
  '--list-languages[List all languages]' \
  '(- *)'{-L,--language}'[Specifies the preferred language]:lang:(${languages})' \
  '(- *)'{-m,--markdown}'[Show the markdown source of a page]' \
  '(- *)'{-p,--platform}'[Override the default OS]:os:(${oses})' \
  '(- *)'{-s,--search}'[Search for a string]'\
  '(- *)'{-u,--update}'[Update the cache]' \
  '--self-update[Check for tldr-sharp updates]'
  '(- *)'{-v,--version}'[Show version information]'\
  "*:page:(${pages})" && return 0
