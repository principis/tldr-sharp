# bash completion for tldr-sharp

# SPDX-FileCopyrightText: None
# SPDX-License-Identifier: CC0-1.0

_tldr-sharp_completions_filter() {
    local words="$1"
    local cur=${COMP_WORDS[COMP_CWORD]}
    local result=()

    if [[ "${cur:0:1}" == "-" ]]; then
        echo "$words"

    else
        for word in $words; do
            [[ "${word:0:1}" != "-" ]] && result+=("$word")
        done

        echo "${result[*]}"

    fi
}

_tldr-sharp_completions() {
    local cur=${COMP_WORDS[COMP_CWORD]}

    case "${cur[0]}" in
    '-'*)
        while read -r; do COMPREPLY+=("$REPLY"); done < <(compgen -W "$(_tldr-sharp_completions_filter "-a --list-all -c --clear-cache -f --render -h --help -l --list --list-platforms --list-languages -L --language -m --markdown -p --platform -s --search -u --update --self-update -v --version")" -- "$cur")
        ;;

    *)
        while read -r; do COMPREPLY+=("$REPLY"); done < <(compgen -W "$(_tldr-sharp_completions_filter "$(tldr-sharp -a)")" -- "$cur")
        ;;
    esac
} &&
    complete -F _tldr-sharp_completions tldr-sharp
