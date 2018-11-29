#! /bin/bash

function _tldr_completions() 
{
        pages=$(tldr -l -1)
        commands='-h --help -l --list -a --list-all -u --update -c --clear-cache --os= --list-os \
                --lang= --list-languages'
        COMPREPLY=()

        if [ $COMP_CWORD = 1 ]; then
                COMPREPLY=(`compgen -W "$pages" -- $2`)
                COMPREPLY+=(`compgen -W "$commands" -- $2`)
        fi


}

complete -F _tldr_completions tldr