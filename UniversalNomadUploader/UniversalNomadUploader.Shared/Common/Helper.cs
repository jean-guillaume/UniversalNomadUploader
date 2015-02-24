using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Windows.Globalization.Collation;

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

    }
}
