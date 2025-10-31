// <copyright file="FileSystem.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Services;

using System.IO;

using Onboard.Core.Abstractions;

/// <summary>
/// Concrete implementation of <see cref="IFileSystem"/> wrapping <see cref="System.IO"/>.
/// </summary>
public class FileSystem : IFileSystem
{
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }

    public void WriteAllText(string path, string contents)
    {
        File.WriteAllText(path, contents);
    }

    public void MoveFile(string sourcePath, string destinationPath, bool overwrite)
    {
        File.Move(sourcePath, destinationPath, overwrite);
    }

    public void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
