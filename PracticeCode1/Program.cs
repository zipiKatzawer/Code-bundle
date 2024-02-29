using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

var bundleOption = new Option<FileInfo>("--output", "File path and name");
var languageOption = new Option<string>("--language", "List of programming languages. The application will include only code files of the selected languages. If the user enters the word 'all', all code files in the directory will be included.");
var noteOption = new Option<bool>("--note", "Include source code comments in the bundle file");
var sortOption = new Option<string>("--sort", "Sort order for code files: name (default) or language");
var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "Remove empty lines from the code files");
var authorOption = new Option<string>("--author", "Name of the file author");
bundleOption.IsRequired = true;
bundleOption.AddAlias("-o");
languageOption.IsRequired = true;
languageOption.AddAlias("-l");
noteOption.AddAlias("-n");
sortOption.SetDefaultValue("name");
sortOption.AddAlias("-s");
removeEmptyLinesOption.AddAlias("-rel");
authorOption.AddAlias("-a");
var bundleCommand = new Command("bundle", "Bundle code files to a single file");
bundleCommand.AddOption(bundleOption);
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);
string[] supportedExtensions = { "c", "cs", "cpp", "h", "java", "asm", "sql", "css", "html", "js", "py" };

bundleCommand.SetHandler((output, language, note, sort, removeEmptyLines, author) =>
{
    try
    {
        // Get the current directory
        var currentDirectory = Directory.GetCurrentDirectory();

        // Write author comment if specified
        if (!string.IsNullOrEmpty(author))
        {
            File.WriteAllText(output.FullName, $"// Author: {author}\n");
        }

        // Get all the files in the directory
        var files = Directory.EnumerateFiles(currentDirectory, "*", SearchOption.AllDirectories);
        string[] languages = GetLanguages(language, supportedExtensions);
        static string[] GetLanguages(string language, string[] supportedExtensions)
        {
            if (language.Equals("all", StringComparison.OrdinalIgnoreCase))
                return supportedExtensions;

            var selectedLanguages = language.Split(',')
                                            .Where(l => supportedExtensions.Contains(l))
                                            .ToArray();

            return selectedLanguages;
        }
        // Check if the user entered 'all'
        if (languages.Length == 1 && languages[0].Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            // Include all code files in the directory
            languages = Array.Empty<string>(); // Clear the array to avoid unnecessary filtering
        }

        // Filter the files by language
        var filteredFiles = files.Where(f =>
        {
            if (languages.Length == 0 || languages[0].Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                // Include all files with supported extensions
                return supportedExtensions.Any(e => Path.GetExtension(f).Equals("." + e, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // Include files with specified extensions
                return languages.Any(l => Path.GetExtension(f).Equals("." + l, StringComparison.OrdinalIgnoreCase));
            }
        });

        // Sort the files based on the chosen sort option
        if (sort.Equals("name", StringComparison.OrdinalIgnoreCase))
        {
            filteredFiles = filteredFiles.OrderBy(f => f); // Sort by filename
        }
        else if (sort.Equals("language", StringComparison.OrdinalIgnoreCase))
        {
            filteredFiles = filteredFiles.OrderBy(f => Path.GetExtension(f)); // Sort by language extension
        }
        else
        {
            Console.WriteLine("Invalid sort option. Valid options are: name, language");
            return;
        }

        // Write the files to the output file
        foreach (var file in filteredFiles)
        {
            // Get the file contents
            var fileContents = File.ReadAllText(file);

            // Remove empty lines if the --remove-empty-lines option is specified
            if (removeEmptyLines)
            {
                fileContents = fileContents.Replace("\r\n", "\n").Replace("\n\n", "\n").TrimEnd('\n');
            }

            // Write the source code comment if the --note option is specified
            if (note)
            {
                File.AppendAllText(output.FullName, $"// Source code: {Path.GetFileName(file)} ({Path.GetRelativePath(currentDirectory, file)})\n");
            }

            // Write the file contents to the output file
            File.AppendAllText(output.FullName, fileContents);
        }

        // Display a message
        Console.WriteLine("The files were written to the output file.");
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine("Error: File path is invalid");
    }

}, bundleOption, languageOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);


var rootCommand = new RootCommand("Root command for file bundle CLI");
rootCommand.AddCommand(bundleCommand);

var createRspCommand = new Command("create-rsp", "Create a response file for the bundle command");

createRspCommand.SetHandler((_) =>
{
    Console.Write("Enter output file path and name: ");
    var outputPath = Console.ReadLine();

    Console.Write("Enter programming languages (comma-separated, or 'all'): ");
    var languages = Console.ReadLine();

    Console.Write("Include source code comments? (yes/no): ");
    var includeComments = Console.ReadLine().ToLowerInvariant() == "yes";

    Console.Write("Sort order for code files (name or language): ");
    var sortOrder = Console.ReadLine();

    Console.Write("Remove empty lines from code files? (yes/no): ");
    var removeEmptyLines = Console.ReadLine().ToLowerInvariant() == "yes";

    Console.Write("Enter author name: ");
    var author = Console.ReadLine();

    // Create the response file content
    var responseFileContent = $"bundle --output {outputPath}";
    if (languages.Length > 0)
    {
        responseFileContent += $" --language {languages}";
    }
    if (includeComments)
    {
        responseFileContent += " --note";
    }
    if (!string.IsNullOrEmpty(sortOrder))
    {
        responseFileContent += $" --sort {sortOrder}";
    }
    if (removeEmptyLines)
    {
        responseFileContent += " --remove-empty-lines";
    }
    if (!string.IsNullOrEmpty(author))
    {
        responseFileContent += $" --author {author}";
    }

    // Save the response file
    File.WriteAllText("bundle.rsp", responseFileContent);
    Console.WriteLine("Response file created successfully: bundle.rsp");
});

rootCommand.AddCommand(createRspCommand);
rootCommand.InvokeAsync(args).Wait();

