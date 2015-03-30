using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Windows.Globalization.Collation;
using System.IO;
using Windows.Storage;
using System.Threading.Tasks;

namespace UniversalNomadUploader.Common
{
    public static class Helper
    {
        /// <summary>
        /// Groups and sorts into a list of alpha groups based on a string selector.
        /// </summary>
        /// <typeparam name="TSource">Type of the items in the list.</typeparam>
        /// <param name="source">List to be grouped and sorted.</param>
        /// <param name="selector">A selector that will provide a value that items to be sorted and grouped by.</param>
        /// <returns>A list of JumpListGroups.</returns>
        public static List<EvidenceListGroup<TSource>> ToAlphaGroups<TSource>(
            this IEnumerable<TSource> source, Func<TSource, string> selector)
        {
            var characterGroupings = new CharacterGroupings();
            // Create dictionary for the letters and replace '...' with proper globe icon
            var keys = characterGroupings.Where(x => x.Label.Count() >= 1)
                                                                    .Select(x => x.Label)
                                                                    .ToDictionary(x => x);
            keys["..."] = "\uD83C\uDF10";


            // Create groups for each letters
            var groupDictionary = keys.Select(x => new EvidenceListGroup<TSource>() { Key = x.Value })
                .ToDictionary(x => (string)x.Key);

            var query = from item in source
                        orderby selector(item)
                        select item;

            foreach (var item in query)
            {
                var sortvalue = selector(item);
                groupDictionary[keys[characterGroupings.Lookup(sortvalue)]].Add(item);
            }

            return groupDictionary.Select(x => x.Value).ToList();
        }

        public async static Task<List<StorageFile>> SplitFile(StorageFile _inputFile, int _chunkSize, StorageFolder _path)
        {            
            byte[] buffer = new byte[_chunkSize];
            List<StorageFile> partFile = new List<StorageFile>();

            using (Stream input = await _inputFile.OpenStreamForReadAsync() )
            {
                int index = 0;
                while (input.Position < input.Length)
                {
                    StorageFile destFilePart = await _path.CreateFileAsync(_inputFile.DisplayName+"."+index,CreationCollisionOption.ReplaceExisting);
                    using (Stream output = await destFilePart.OpenStreamForWriteAsync())
                    {
                        int chunkBytesRead = 0;
                        while (chunkBytesRead < _chunkSize)
                        {
                            int bytesRead = input.Read(buffer, chunkBytesRead, _chunkSize - chunkBytesRead);
                            if (bytesRead == 0)
                            {
                                break;
                            }
                            chunkBytesRead += bytesRead;
                        }
                        output.Write(buffer, 0, chunkBytesRead);
                    }
                    partFile.Add(destFilePart);
                    index++;
                }
            }

            return partFile;
        }
    }
}
