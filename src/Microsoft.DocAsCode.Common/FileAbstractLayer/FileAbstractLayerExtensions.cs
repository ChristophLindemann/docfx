﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System.Collections.Immutable;
    using System.IO;

    public static class FileAbstractLayerExtensions
    {
        public static bool Exists(this FileAbstractLayer fal, string file) =>
            fal.Exists((RelativePath)file);

        public static FileStream OpenRead(this FileAbstractLayer fal, string file) =>
            fal.OpenRead((RelativePath)file);

        public static StreamReader OpenReadText(this FileAbstractLayer fal, RelativePath file) =>
            new StreamReader(fal.OpenRead(file));

        public static StreamReader OpenReadText(this FileAbstractLayer fal, string file) =>
            OpenReadText(fal, (RelativePath)file);

        public static string ReadAllText(this FileAbstractLayer fal, RelativePath file)
        {
            using (var sr = OpenReadText(fal, file))
            {
                return sr.ReadToEnd();
            }
        }

        public static string ReadAllText(this FileAbstractLayer fal, string file) =>
            ReadAllText(fal, (RelativePath)file);

        public static FileStream Create(this FileAbstractLayer fal, string file) =>
            fal.Create((RelativePath)file);

        public static StreamWriter CreateText(this FileAbstractLayer fal, RelativePath file) =>
            new StreamWriter(fal.Create(file));

        public static StreamWriter CreateText(this FileAbstractLayer fal, string file) =>
            CreateText(fal, (RelativePath)file);

        public static void WriteAllText(this FileAbstractLayer fal, RelativePath file, string content)
        {
            using (var writer = CreateText(fal, file))
            {
                writer.Write(content);
            }
        }

        public static void WriteAllText(this FileAbstractLayer fal, string file, string content) =>
            WriteAllText(fal, (RelativePath)file, content);

        public static void Copy(this FileAbstractLayer fal, string sourceFileName, string destFileName) =>
            fal.Copy((RelativePath)sourceFileName, (RelativePath)destFileName);

        public static ImmutableDictionary<string, string> GetProperties(this FileAbstractLayer fal, string file) =>
            fal.GetProperties((RelativePath)file);

        public static bool HasProperty(this FileAbstractLayer fal, RelativePath file, string propertyName)
        {
            var dict = fal.GetProperties(file);
            return dict.ContainsKey(propertyName);
        }

        public static bool HasProperty(this FileAbstractLayer fal, string file, string propertyName) =>
            HasProperty(fal, (RelativePath)file, propertyName);

        public static string GetProperty(this FileAbstractLayer fal, RelativePath file, string propertyName)
        {
            var dict = fal.GetProperties(file);
            string result;
            dict.TryGetValue(propertyName, out result);
            return result;
        }

        public static string GetProperty(this FileAbstractLayer fal, string file, string propertyName) =>
            GetProperty(fal, (RelativePath)file, propertyName);
    }
}
