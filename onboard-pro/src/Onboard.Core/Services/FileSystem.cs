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
}
