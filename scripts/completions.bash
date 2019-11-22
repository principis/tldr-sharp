#! /bin/bash

function _tldr_completions() 
{
        pages=$(tldr -a)
        commands='-h --help -a --list-all -c --clear-cache -f --render -l --list --list-os --list-languages -L --language -m --markdown -p --platform -s --search -u --update --self-update -v --version \
                --lang= --list-languages'
        COMPREPLY=()

        if [ $COMP_CWORD = 1 ]; then
                COMPREPLY=(`compgen -W "$pages" -- $2`)
                COMPREPLY+=(`compgen -W "$commands" -- $2`)
        fi


}

complete -F _tldr_completions tldr
