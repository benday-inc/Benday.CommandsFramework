namespace Benday.CommandsFramework;

/// <summary>
/// The shell stubs that a tool prints for its user to install.
/// </summary>
/// <remarks>
/// Each stub does the same small job: hand the whole command line back to the tool through the
/// hidden --complete keyword and turn the answer into whatever the shell wants. Nothing about
/// the tool's commands is baked in, so the stub never goes stale when the tool is updated --
/// which is the whole reason completion is dynamic rather than generated.
///
/// The wire format is one candidate per line: the value, then optionally a tab and a
/// description. A line starting with ':' is a directive to the shell rather than a candidate:
/// ':file:PATTERN' and ':dir'. Paths are the shell's job -- it already knows how to complete
/// them, and how to quote what it finds.
/// </remarks>
public static class CompletionScripts
{
    /// <summary>
    /// The shells a stub can be produced for.
    /// </summary>
    public static IReadOnlyList<string> SupportedShells { get; } = ["pwsh", "zsh", "bash"];

    /// <summary>
    /// Gets the stub for a shell.
    /// </summary>
    /// <param name="shell">pwsh, zsh or bash</param>
    /// <param name="toolName">Name of the tool as it is typed</param>
    /// <returns>The stub script</returns>
    /// <exception cref="KnownException">Thrown for a shell there is no stub for.</exception>
    public static string GetScript(string shell, string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName) == true)
        {
            throw new ArgumentException("Tool name is required.", nameof(toolName));
        }

        return shell?.ToLowerInvariant() switch
        {
            "pwsh" or "powershell" => GetPowerShellScript(toolName),
            "zsh" => GetZshScript(toolName),
            "bash" => GetBashScript(toolName),
            _ => throw new KnownException(
                $"No completion script for '{shell}'. Supported shells: " +
                $"{string.Join(", ", SupportedShells)}.")
        };
    }

    /// <summary>
    /// PowerShell gets the most out of this: descriptions become the tooltip in the completion
    /// menu, and a file directive becomes a real provider path completion.
    /// </summary>
    private static string GetPowerShellScript(string toolName) =>
        $$"""
        # {{toolName}} completion for PowerShell.
        # Add to your profile with:
        #   {{toolName}} completion /shell:pwsh >> $PROFILE

        Register-ArgumentCompleter -Native -CommandName '{{toolName}}' -ScriptBlock {
            param($wordToComplete, $commandAst, $cursorPosition)

            $line = $commandAst.ToString().Substring(0, $cursorPosition)

            & '{{toolName}}' '{{ArgumentFrameworkConstants.ArgumentComplete}}' $line 2>$null |
                ForEach-Object {
                    $parts = $_ -split "`t", 2
                    $value = $parts[0]
                    $description = if ($parts.Length -gt 1) { $parts[1] } else { $value }

                    if ($value.StartsWith(':')) {
                        # a directive: let PowerShell complete paths itself
                        if ($value -eq ':dir') {
                            Get-ChildItem -Directory -Filter "$wordToComplete*" |
                                ForEach-Object {
                                    [System.Management.Automation.CompletionResult]::new(
                                        $_.Name, $_.Name, 'ProviderContainer', $_.FullName)
                                }
                        }
                        elseif ($value.StartsWith(':file:')) {
                            $pattern = $value.Substring(6)
                            Get-ChildItem -File -Filter $pattern |
                                ForEach-Object {
                                    [System.Management.Automation.CompletionResult]::new(
                                        $_.Name, $_.Name, 'ProviderItem', $_.FullName)
                                }
                        }
                    }
                    else {
                        [System.Management.Automation.CompletionResult]::new(
                            $value, $value, 'ParameterValue', $description)
                    }
                }
        }

        """;

    /// <summary>
    /// Turns a tool name into something usable as a shell function name.
    /// </summary>
    /// <remarks>
    /// A tool name can contain characters a shell function name cannot -- a dot, most
    /// obviously, which any tool named after its assembly has several of.
    /// </remarks>
    public static string GetShellFunctionName(string toolName)
    {
        var sanitized = new string(
            [.. toolName.Select(x => char.IsLetterOrDigit(x) == true ? x : '_')]);

        // a shell function name cannot start with a digit
        return char.IsDigit(sanitized[0]) == true ? $"_{sanitized}" : sanitized;
    }

    private static string GetZshScript(string toolName)
    {
        var functionName = GetShellFunctionName(toolName);

        return $$"""
        # {{toolName}} completion for zsh.
        # Add to your .zshrc with:
        #   {{toolName}} completion /shell:zsh >> ~/.zshrc

        _{{functionName}}_complete() {
            local line candidates value description
            line="${words[*]}"

            candidates=()

            while IFS=$'\t' read -r value description; do
                case "$value" in
                    :dir)
                        _files -/
                        return
                        ;;
                    :file:*)
                        _files -g "${value#:file:}"
                        return
                        ;;
                    *)
                        if [[ -n "$description" ]]; then
                            candidates+=("${value}:${description}")
                        else
                            candidates+=("${value}")
                        fi
                        ;;
                esac
            done < <('{{toolName}}' '{{ArgumentFrameworkConstants.ArgumentComplete}}' "$line" 2>/dev/null)

            _describe -t commands '{{toolName}}' candidates
        }

        compdef _{{functionName}}_complete {{toolName}}

        """;
    }

    /// <summary>
    /// bash gets the least: it has no way to show a description next to a candidate, so the
    /// descriptions are dropped rather than pasted into the command line.
    /// </summary>
    private static string GetBashScript(string toolName)
    {
        var functionName = GetShellFunctionName(toolName);

        return $$"""
        # {{toolName}} completion for bash.
        # Add to your .bashrc with:
        #   {{toolName}} completion /shell:bash >> ~/.bashrc

        _{{functionName}}_complete() {
            local line candidates value word
            line="${COMP_LINE}"
            word="${COMP_WORDS[COMP_CWORD]}"

            COMPREPLY=()
            candidates=()

            while IFS=$'\t' read -r value _; do
                case "$value" in
                    :dir)
                        COMPREPLY=( $(compgen -d -- "$word") )
                        return
                        ;;
                    :file:*)
                        COMPREPLY=( $(compgen -f -- "$word") )
                        return
                        ;;
                    *)
                        candidates+=("$value")
                        ;;
                esac
            done < <('{{toolName}}' '{{ArgumentFrameworkConstants.ArgumentComplete}}' "$line" 2>/dev/null)

            COMPREPLY=( $(compgen -W "${candidates[*]}" -- "$word") )
        }

        complete -F _{{functionName}}_complete {{toolName}}

        """;
    }
}
