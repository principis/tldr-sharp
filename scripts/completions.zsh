#compdef tldr-sharp

# SPDX-FileCopyrightText: None
# SPDX-License-Identifier: CC0-1.0

(( $+functions[_tldr_sharp_list_platforms] )) ||
  _tldr_sharp_list_platforms() {
    local -a _platforms
    _platforms=($(tldr-sharp --list-platforms))
    _describe 'platform' _platforms
  }

(( $+functions[_tldr_sharp_list_languages] )) ||
  _tldr_sharp_list_languages() {
    local -a _langs
    _langs=("${(f)$(tldr-sharp --list-languages)//$'\t'/}")
    _describe 'language' _langs
  }

(( $+functions[_tldr_sharp_pages] )) ||
  _tldr_sharp_pages() {
    compadd - ${(f)"$(tldr-sharp -a)"}
  }

_tldr-sharp() {
  _arguments \
    {-a,--list-all}'[List all pages]' \
    {-c,--clear-cache}'[Clear the cache]' \
    {-f,--render}'[Render a specific markdown file]: : _files' \
    {-h,--help}'[Display this help text]' \
    {-l,--list}'[List all pages for the current platform and language]' \
    '--list-platforms[List all platforms]' \
    '--list-languages[List all languages]' \
    {-L,--language}'[Specifies the preferred language]:language:_tldr_sharp_list_languages' \
    {-m,--markdown}'[Show the markdown source of a page]' \
    {-p,--platform}'[Override the default platform]:platform:_tldr_sharp_list_platforms' \
    {-s,--search}'[Search for a string]' \
    {-u,--update}'[Update the cache]' \
    '--self-update[Check for tldr-sharp updates]' \
    {-v,--version}'[Show version information]' \
    '1::page:_tldr_sharp_pages' && return 0
}

_tldr-sharp