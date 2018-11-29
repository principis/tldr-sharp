#compdef tldr
  
local -a pages oses languages
pages=$(tldr -a1)
oses=$(tldr --list-os)
languages=$(tldr --list-languages)

_arguments \
  '(- *)'{-a,--list-all}'[List all pages]' \
  '(- *)'{-c,--clear-cache}'[Clear local cache]' \
  '(- *)'{-f,--render}'[Render a specific markdown file]' \
  '(- *)'{-h,--help}'[Display this help text]' \
  '(- *)'{-l,--list}'[List all pages for the current platform]' \
  "--list-os[List all OSs]" \
  '--list-languages[List all languages]' \
  "--lang[Override the default language]:lang:(${languages})" \
  '(- *)'{-m,--markdown}'[Show the markdown source of a page]' \
  "--os[Override the default OS]:os:(${oses})" \
  '(- *)'{-u,--update}'[Update the local cache]' \
  "*:page:(${pages})" && return 0