﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Common;

    internal sealed class XRefCollection
    {
        private const int MaxParallelism = 0x10;

        public XRefCollection()
        {
            Uris = ImmutableList<Uri>.Empty;
        }

        public XRefCollection(IEnumerable<Uri> uris)
        {
            Uris = uris.ToImmutableList();
        }

        public ImmutableList<Uri> Uris { get; set; }

        public Task<IXRefContainerReader> GetReaderAsync()
        {
            var creator = new ReaderCreator(Uris, MaxParallelism);
            return creator.CreateAsync();
        }

        private sealed class ReaderCreator
        {
            private readonly ImmutableList<Uri> _uris;
            private readonly HashSet<string> _set = new HashSet<string>();
            private readonly Dictionary<Task<IXRefContainer>, Uri> _processing = new Dictionary<Task<IXRefContainer>, Uri>();
            private readonly XRefMapDownloader _downloader;

            public ReaderCreator(ImmutableList<Uri> uris, int maxParallelism)
            {
                _uris = uris;
                _downloader = new XRefMapDownloader(maxParallelism);
            }

            public async Task<IXRefContainerReader> CreateAsync()
            {
                AddToDownloadList(_uris);
                var dict = new Dictionary<string, IXRefContainer>();
                while (_processing.Count > 0)
                {
                    var task = await Task.WhenAny(_processing.Keys);
                    var uri = _processing[task];
                    _processing.Remove(task);
                    try
                    {
                        var container = await task;
                        AddToDownloadList(
                            from r in container.GetRedirections()
                            where r != null
                            select GetUri(uri, r.Href) into u
                            where u != null
                            select u);
                        dict[uri.AbsoluteUri] = container;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Unable to download xref map file from {uri}, details: {ex.Message}");
                    }
                }
                var fakeEntry = Guid.NewGuid().ToString();
                dict[fakeEntry] = new XRefMap
                {
                    HrefUpdated = true,
                    Redirections = (from pair in dict
                                    select new XRefMapRedirection
                                    {
                                        Href = pair.Key,
                                    }).ToList()
                };
                return new XRefMapReader(fakeEntry, dict);
            }

            private static Uri GetUri(Uri baseUri, string href)
            {
                if (string.IsNullOrWhiteSpace(href))
                {
                    return null;
                }
                Uri uri;
                if (!Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out uri))
                {
                    return null;
                }
                if (uri.IsAbsoluteUri)
                {
                    return uri;
                }
                return new Uri(baseUri, uri);
            }

            private void AddToDownloadList(IEnumerable<Uri> uris)
            {
                foreach (var uri in uris)
                {
                    if (_set.Add(uri.AbsoluteUri))
                    {
                        var task = _downloader.DownloadAsync(uri);
                        _processing[task] = uri;
                    }
                }
            }

            private async Task<Tuple<Uri, IXRefContainer>> DownloadAsync(Uri uri) =>
                Tuple.Create(uri, await _downloader.DownloadAsync(uri));
        }
    }
}