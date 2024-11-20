namespace Benday.CommandsFramework;

public static class CommandFrameworkUtilities
{
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