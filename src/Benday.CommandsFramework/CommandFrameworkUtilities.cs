﻿using Microsoft.Extensions.DependencyInjection;

namespace Benday.CommandsFramework;

public static class CommandFrameworkUtilities
{
    /// <summary>
    /// Gets the service provider for a program, building it the first time it is asked for
    /// and then reusing it.
    /// </summary>
    /// <remarks>
    /// The provider is built once and cached on the options, so singleton services really
    /// are singletons across every command in the process. A program that registered nothing
    /// still gets a provider rather than a null, so command activation is the same code path
    /// whether or not the tool uses dependency injection.
    /// </remarks>
    /// <param name="options">Program options</param>
    /// <returns>The service provider</returns>
    public static IServiceProvider GetServiceProvider(ICommandProgramOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        if (options.ServiceProvider is not null)
        {
            return options.ServiceProvider;
        }

        var services = options.ServiceCollection ?? new ServiceCollection();

        var provider = services.BuildServiceProvider();

        options.ServiceProvider = provider;

        return provider;
    }

    public static string GetPathToSourceFile(string sourceFile, bool mustExist)
    {
        if (Path.IsPathFullyQualified(sourceFile) == true)
        {
            if (mustExist == true)
            {
                if (File.Exists(sourceFile) == true)
                {
                    return sourceFile;
                }
                else
                {
                    throw new InvalidOperationException($"Couldn't find source file.");
                }
            }
            else
            {
                return sourceFile;
            }
        }
        else
        {
            if (File.Exists(sourceFile) == true)
            {
                return sourceFile;
            }
            else
            {
                sourceFile = Path.Combine(Directory.GetCurrentDirectory(), sourceFile);

                if (mustExist == true)
                {
                    if (File.Exists(sourceFile) == true)
                    {
                        return sourceFile;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Couldn't find source file.");
                    }
                }
                else
                {
                    return sourceFile;
                }

            }
        }
    }

    public static string GetFullyQualifiedPath(string argumentValue)
    {
        if (Path.IsPathFullyQualified(argumentValue) == true)
        {

            return argumentValue;
        }
        else
        {
            argumentValue = Path.Combine(Directory.GetCurrentDirectory(), argumentValue);

            return argumentValue;
        }
    }

    public static string GetPathToSourceDir(string sourceDir, bool mustExist)
    {
        if (Path.IsPathFullyQualified(sourceDir) == true)
        {
            if (mustExist == true)
            {
                if (Directory.Exists(sourceDir) == true)
                {
                    return sourceDir;
                }
                else
                {
                    throw new InvalidOperationException($"Couldn't find source file.");
                }
            }
            else
            {
                return sourceDir;
            }
        }
        else
        {
            if (Directory.Exists(sourceDir) == true)
            {
                return sourceDir;
            }
            else
            {
                sourceDir = Path.Combine(Directory.GetCurrentDirectory(), sourceDir);

                if (mustExist == true)
                {
                    if (Directory.Exists(sourceDir) == true)
                    {
                        return sourceDir;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Couldn't find source file.");
                    }
                }
                else
                {
                    return sourceDir;
                }
            }
        }
    }
}